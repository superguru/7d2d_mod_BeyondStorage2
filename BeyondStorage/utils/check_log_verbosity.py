#!/usr/bin/env python3
"""
Script to scan C# files for ModLogger calls that are not wrapped in #if DEBUG/#endif blocks.
This helps identify logging calls that might impact performance in release builds.
"""

import os
import re
import glob
from datetime import datetime
from pathlib import Path

# Configuration
BASE_DIR = os.path.join(os.path.dirname(os.path.dirname(__file__)), "Source")
MAX_RESULT_FILES = 5
SKIP_LIST_FILE = os.path.join(os.path.dirname(__file__), "log_check_skip_list.cfg")

# ModLogger methods to search for
MODLOGGER_METHODS = [
    "ModLogger.Info",
    "ModLogger.Error", 
    "ModLogger.DebugLog",
    "ModLogger.Warning"
]

def load_skip_list():
    """
    Load the list of files to skip from the skip list file.
    
    Returns:
        set: Set of file paths (relative to BASE_DIR) to skip during scanning
    """
    skip_files = set()
    
    if not os.path.exists(SKIP_LIST_FILE):
        # Create a default skip list file with some examples
        create_default_skip_list()
        return skip_files
    
    try:
        with open(SKIP_LIST_FILE, 'r', encoding='utf-8') as f:
            for line in f:
                line = line.strip()
                # Skip empty lines and comments
                if line and not line.startswith('#'):
                    # Normalize path separators
                    normalized_path = line.replace('\\', os.sep).replace('/', os.sep)
                    skip_files.add(normalized_path)
    except (IOError, UnicodeDecodeError) as e:
        print(f"Warning: Could not read skip list file {SKIP_LIST_FILE}: {e}")
    
    return skip_files

def create_default_skip_list():
    """Create a default skip list file with examples and instructions."""
    default_content = """# ModLogger Verbosity Check - Skip List
# 
# This file contains a list of C# files to skip when checking for unwrapped ModLogger calls.
# Each line should contain a file path relative to the Source directory.
# Lines starting with # are treated as comments and ignored.
# Empty lines are also ignored.
#
# Examples:
# Infrastructure/ModLogger.cs
# Tests/TestLogger.cs
# External/ThirdPartyCode.cs
#
# Use forward slashes (/) or backslashes (\\) - both will work on all platforms.
#
# Files to skip:

# Example: Skip the ModLogger class itself since it's expected to have unwrapped calls
Infrastructure/ModLogger.cs
"""
    
    try:
        with open(SKIP_LIST_FILE, 'w', encoding='utf-8') as f:
            f.write(default_content)
        print(f"Created default skip list file: {os.path.basename(SKIP_LIST_FILE)}")
    except IOError as e:
        print(f"Warning: Could not create skip list file: {e}")

def cleanup_old_results():
    """Keep only the most recent MAX_RESULT_FILES result files."""
    script_dir = os.path.dirname(__file__)
    pattern = os.path.join(script_dir, "log_check_results_*.txt")
    result_files = glob.glob(pattern)
    
    if len(result_files) > MAX_RESULT_FILES:
        # Sort by modification time (oldest first)
        result_files.sort(key=os.path.getmtime)
        # Remove excess files
        for file_to_remove in result_files[:-MAX_RESULT_FILES]:
            try:
                os.remove(file_to_remove)
            except OSError as e:
                pass  # Silent cleanup

def is_wrapped_in_debug(lines, line_index):
    """
    Check if the current line is wrapped in #if DEBUG/#endif statements.
    
    Args:
        lines: List of all lines in the file
        line_index: Index of the line containing ModLogger call
        
    Returns:
        bool: True if wrapped in DEBUG statements, False otherwise
    """
    # Look backwards for #if DEBUG
    debug_if_found = False
    for i in range(line_index - 1, -1, -1):
        line = lines[i].strip()
        if line.startswith("#if DEBUG"):
            debug_if_found = True
            break
        elif line.startswith("#endif"):
            # Found endif before if DEBUG, so not wrapped
            break
        elif line.startswith("#if ") and not line.startswith("#if DEBUG"):
            # Found different preprocessor directive, stop looking
            break
    
    if not debug_if_found:
        return False
    
    # Look forwards for #endif
    for i in range(line_index + 1, len(lines)):
        line = lines[i].strip()
        if line.startswith("#endif"):
            return True
        elif line.startswith("#if "):
            # Found another preprocessor directive, stop looking
            break
    
    return False

def scan_file(file_path):
    """
    Scan a single C# file for ModLogger calls not wrapped in DEBUG statements.
    
    Args:
        file_path: Path to the C# file to scan
        
    Returns:
        list: List of tuples (line_number, line_content, method_used)
    """
    unwrapped_calls = []
    
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            lines = f.readlines()
    except (UnicodeDecodeError, IOError) as e:
        return unwrapped_calls
    
    for line_index, line in enumerate(lines):
        line_content = line.strip()
        
        # Check if line contains any ModLogger method call
        for method in MODLOGGER_METHODS:
            if method in line_content:
                # Check if this call is wrapped in DEBUG statements
                if not is_wrapped_in_debug(lines, line_index):
                    unwrapped_calls.append((
                        line_index + 1,  # 1-based line numbers
                        line_content,
                        method
                    ))
                break  # Only count once per line even if multiple methods present
    
    return unwrapped_calls

def scan_directory(base_dir, skip_files):
    """
    Scan all C# files in the specified directory and subdirectories.
    
    Args:
        base_dir: Base directory to start scanning from
        skip_files: Set of file paths to skip (relative to base_dir)
        
    Returns:
        tuple: (results_dict, skipped_files_dict) where results_dict maps file paths to lists of unwrapped calls
               and skipped_files_dict contains the files that were skipped
    """
    if not os.path.exists(base_dir):
        raise FileNotFoundError(f"Base directory does not exist: {base_dir}")
    
    results = {}
    skipped_files_found = set()
    cs_files = glob.glob(os.path.join(base_dir, "**", "*.cs"), recursive=True)
    
    for file_path in cs_files:
        # Get relative path for comparison with skip list
        rel_path = os.path.relpath(file_path, base_dir)
        
        # Check if this file should be skipped
        if rel_path in skip_files:
            skipped_files_found.add(rel_path)
            continue
        
        unwrapped_calls = scan_file(file_path)
        if unwrapped_calls:
            results[rel_path] = unwrapped_calls
    
    return results, skipped_files_found

def sort_files_by_count(results, skipped_files_found):                                  
    """
    Sort files by count, with lowest counts first, then SKIP files, then alphabetically.
    
    Args:
        results: Dictionary of scan results
        skipped_files_found: Set of files that were skipped
        
    Returns:
        list: List of tuples (file_path, count_or_skip) sorted by priority
    """
    # Create combined list with counts and skip markers
    all_files = []
    
    # Add files with unwrapped calls
    for file_path, calls in results.items():
        all_files.append((file_path, len(calls)))
    
    # Add skipped files
    for file_path in skipped_files_found:
        all_files.append((file_path, "SKIP"))
    
    # Sort by: 1) Numeric counts (ascending), 2) SKIP files, 3) Alphabetically
    def sort_key(item):
        file_path, count = item
        if count == "SKIP":
            # SKIP files come after numeric counts, sorted alphabetically
            return (1, file_path)
        else:
            # Numeric counts come first, sorted by count (ascending), then alphabetically
            return (0, count, file_path)
    
    return sorted(all_files, key=sort_key)

def generate_detailed_report(results, skipped_files_found, skip_files):
    """
    Generate a detailed formatted report of the scan results for file output.
    
    Args:
        results: Dictionary of scan results
        skipped_files_found: Set of files that were actually skipped during scanning
        skip_files: Set of file paths from the skip list
        
    Returns:
        str: Formatted report text
    """
    timestamp = datetime.now().strftime("%Y-%m-%d %H:%M:%S")
    
    report_lines = [
        "=" * 80,
        f"ModLogger Verbosity Check Report - {timestamp}",
        "=" * 80,
        f"Base Directory: {BASE_DIR}",
        f"Skip List File: {SKIP_LIST_FILE}",
        f"Files skipped: {len(skipped_files_found)}",
        f"Total files with unwrapped ModLogger calls: {len(results)}",
        ""
    ]
    
    if skipped_files_found:
        report_lines.extend([
            "Skipped files:",
            "---------------"
        ])
        for skip_file in sorted(skipped_files_found):
            report_lines.append(f"  {skip_file}")
        report_lines.append("")
    
    if not results:
        report_lines.extend([
            "✅ All ModLogger calls are properly wrapped in #if DEBUG/#endif blocks!",
            "   No performance impact from logging in release builds.",
            ""
        ])
    else:
        total_calls = sum(len(calls) for calls in results.values())
        report_lines.extend([
            f"⚠️  Found {total_calls} unwrapped ModLogger calls across {len(results)} files.",
            "   These calls may impact performance in release builds.",
            ""
        ])
        
        # Sort files by number of unwrapped calls (ascending)
        sorted_files = sorted(results.items(), key=lambda x: (len(x[1]), x[0]))
        
        for file_path, calls in sorted_files:
            report_lines.extend([
                f"📁 {file_path} ({len(calls)} unwrapped calls):",
                "-" * (len(file_path) + 20)
            ])
            
            for line_num, line_content, method in calls:
                # Truncate very long lines for readability
                display_line = line_content if len(line_content) <= 100 else line_content[:97] + "..."
                report_lines.append(f"  Line {line_num:4d}: {method:<20} | {display_line}")
            
            report_lines.append("")
    
    report_lines.extend([
        "=" * 80,
        "Scan completed successfully.",
        "=" * 80
    ])
    
    return "\n".join(report_lines)

def print_simple_summary(results, skipped_files_found):
    """
    Print a simple summary to stdout showing filenames with counts or SKIP status,
    sorted by count (lowest first).
    
    Args:
        results: Dictionary of scan results
        skipped_files_found: Set of files that were actually skipped during scanning
    """
    if not results and not skipped_files_found:
        print("No unwrapped ModLogger calls found.")
        return
    
    # Sort files by count (lowest first), then SKIP files, then alphabetically
    sorted_files = sort_files_by_count(results, skipped_files_found)
    
    for file_path, count in sorted_files:
        print(f"{file_path}: {count}")

def main():
    """Main function to run the ModLogger verbosity check."""
    try:
        # Clean up old result files
        cleanup_old_results()
        
        # Load skip list
        skip_files = load_skip_list()
        
        # Scan for unwrapped ModLogger calls
        results, skipped_files_found = scan_directory(BASE_DIR, skip_files)
        
        # Output simple summary to stdout
        print_simple_summary(results, skipped_files_found)
        
        # Generate detailed report for file output
        detailed_report = generate_detailed_report(results, skipped_files_found, skip_files)
        
        # Output detailed report to file
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        script_dir = os.path.dirname(__file__)
        output_file = os.path.join(script_dir, f"log_check_results_{timestamp}.txt")
        
        with open(output_file, 'w', encoding='utf-8') as f:
            f.write(detailed_report)
        
        # Return exit code based on results
        return 1 if results else 0
            
    except Exception as e:
        print(f"Error: {e}")
        
        # Still try to write error to file
        try:
            timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
            script_dir = os.path.dirname(__file__)
            output_file = os.path.join(script_dir, f"log_check_results_{timestamp}.txt")
            with open(output_file, 'w', encoding='utf-8') as f:
                f.write(f"ERROR: {e}\n")
        except:
            pass
        
        return 1

if __name__ == "__main__":
    exit(main())