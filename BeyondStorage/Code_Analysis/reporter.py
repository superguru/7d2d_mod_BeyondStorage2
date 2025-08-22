"""
Code analysis reporting functionality
"""

import os
import glob
import re
from datetime import datetime
from typing import List
from models import Issue


class Reporter:
    """Handles reporting and output formatting for code analysis results"""
    
    @staticmethod
    def format_issue_compiler_style(issue: Issue) -> str:
        """Format issue in compiler-style format for parsing"""
        # Format: file(line): severity code: description
        return f"{issue.file_path}({issue.line_number}): {issue.severity} {issue.code}: {issue.description}"
    
    @staticmethod
    def cleanup_old_result_files(keep_latest: int = 5) -> int:
        """
        Delete old code check result files, keeping only the latest N files per 5-minute window,
        with a global maximum of 10 files total.
        Only deletes files that match the expected timestamp format.
        Renamed files that don't match the format are preserved.
        
        Args:
            keep_latest: Number of latest files to keep per 5-minute window (default: 5)
            
        Returns:
            Number of files deleted
        """
        # Find all files matching the pattern
        pattern = "code_check_results_*.txt"
        all_result_files = glob.glob(pattern)
        
        if not all_result_files:
            return 0
        
        # Filter to only files that match our expected timestamp format
        # Expected format: code_check_results_YYYYMMDD_HHMMSS.txt
        timestamp_pattern = re.compile(r'^code_check_results_(\d{8}_\d{6})\.txt$')
        valid_files = []
        
        for file in all_result_files:
            filename = os.path.basename(file)
            match = timestamp_pattern.match(filename)
            if match:
                try:
                    # Parse the timestamp to ensure it's valid
                    timestamp_str = match.group(1)
                    timestamp = datetime.strptime(timestamp_str, "%Y%m%d_%H%M%S")
                    valid_files.append((file, timestamp))
                except ValueError:
                    # Invalid timestamp format, skip this file
                    continue
        
        if not valid_files:
            return 0
        
        # Group files by 5-minute windows
        # Create a window key by truncating minutes to 5-minute intervals
        windows = {}
        for file_path, timestamp in valid_files:
            # Create 5-minute window key: truncate minutes to nearest 5-minute boundary
            window_minutes = (timestamp.minute // 5) * 5
            window_key = timestamp.replace(minute=window_minutes, second=0, microsecond=0)
            
            if window_key not in windows:
                windows[window_key] = []
            windows[window_key].append((file_path, timestamp))
        
        # Sort files within each window by timestamp (newest first) and keep only the latest N
        files_to_delete = []
        files_to_keep = []
        
        for window_key, files_in_window in windows.items():
            # Sort by timestamp (newest first)
            files_in_window.sort(key=lambda x: x[1], reverse=True)
            
            # Keep the latest N files in this window
            files_to_keep.extend(files_in_window[:keep_latest])
            
            # Mark excess files for deletion
            if len(files_in_window) > keep_latest:
                files_to_delete.extend(files_in_window[keep_latest:])
        
        # Apply global maximum limit of 10 files
        # Sort all files to keep by timestamp (newest first)
        files_to_keep.sort(key=lambda x: x[1], reverse=True)
        
        if len(files_to_keep) > 10:
            # Move excess files from keep list to delete list
            excess_files = files_to_keep[10:]
            files_to_keep = files_to_keep[:10]
            files_to_delete.extend(excess_files)
            
            print(f"Global file limit: keeping {len(files_to_keep)} newest files, marking {len(excess_files)} additional files for deletion")
        
        # Delete the marked files
        deleted_count = 0
        for file_path, timestamp in files_to_delete:
            try:
                os.remove(file_path)
                deleted_count += 1
                print(f"Deleted old result file: {file_path}")
            except OSError as e:
                print(f"Warning: Could not delete {file_path}: {e}")
        
        return deleted_count
    
    @staticmethod
    def write_results(errors: List[Issue], warnings: List[Issue], cs_files_count: int, parsing_method: str) -> str:
        """Write results to timestamped file and return filename"""
        # Create timestamped results file
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        results_file = f"code_check_results_{timestamp}.txt"
        
        # Output to both console and file
        output_lines = []
        
        # Show warnings first (less critical)
        if warnings:
            output_lines.append("WARNINGS:")
            output_lines.append("-" * 40)
            for warning in warnings:
                line = Reporter.format_issue_compiler_style(warning)
                output_lines.append(line)
            output_lines.append("")

        # Show errors last (most critical - will be at bottom of output)
        if errors:
            output_lines.append("ERRORS:")
            output_lines.append("-" * 40)
            for error in errors:
                line = Reporter.format_issue_compiler_style(error)
                output_lines.append(line)
            output_lines.append("")
        
        # Summary
        summary = f"Code check completed: {len(errors)} error(s), {len(warnings)} warning(s) found in {cs_files_count} files."
        output_lines.append(summary)
        
        # Add build status indicator
        if errors:
            output_lines.append("BUILD STATUS: FAILED (errors found)")
        elif warnings:
            output_lines.append("BUILD STATUS: PASSED (warnings only)")
        else:
            output_lines.append("BUILD STATUS: PASSED (no issues)")
        
        # Add parsing method info
        output_lines.append(f"PARSING METHOD: {parsing_method}")
        
        # Write to file
        try:
            with open(results_file, 'w', encoding='utf-8') as f:
                f.write(f"BeyondStorage Code Quality Check Results\n")
                f.write(f"Generated: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n")
                f.write(f"Parsing Method: {parsing_method}\n")
                f.write("=" * 60 + "\n\n")
                f.write("\n".join(output_lines))
        except IOError as e:
            print(f"Warning: Could not write results file {results_file}: {e}")
            return ""
        
        # Output to console
        for line in output_lines:
            print(line)
        
        return results_file