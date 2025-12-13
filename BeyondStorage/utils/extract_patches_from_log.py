import re
import argparse
from pathlib import Path
import os
import sys
from rich.console import Console
from rich.panel import Panel
from rich.progress import Progress, TaskID
from rich.text import Text
from rich.table import Table
from rich import print as rprint
from datetime import datetime

# Initialize rich console
console = Console()

# Default values
DEFAULT_OUTPUT_DIR_PREFIX = "00_patches"
DEFAULT_TARGET_PATTERN = "INF [MODS][Harmony](IL) Generated patch" 
DEFAULT_FILE_NAME = "output_log_client__yyyy-mm-dd__hh-nn-ss.txt"

WINDOWS_PROFILE_DIR = os.path.expanduser("~")
SEVEN_DAYS_LOG_PATH = Path(WINDOWS_PROFILE_DIR) / "AppData" / "Roaming" / "7DaysToDie" / "Logs"
SEVEN_DAYS_LOG_DIR = str(SEVEN_DAYS_LOG_PATH)

def find_newest_file(directory_path, file_pattern="*.txt"):
    """Find the newest file matching the pattern in the specified directory"""
    try:
        # Convert to Path object if it's a string
        directory = Path(directory_path)
        
        if not directory.exists():
            console.print(f"[yellow]Warning:[/yellow] Directory {directory} does not exist.")
            return None
            
        # Get all files matching the pattern
        files = list(directory.glob(file_pattern))
        
        if not files:
            console.print(f"[yellow]Warning:[/yellow] No {file_pattern} files found in {directory}")
            return None
            
        # Return the newest file by modification time
        newest_file = max(files, key=lambda f: f.stat().st_mtime)
        return newest_file
            
    except Exception as e:
        console.print(f"[bold red]Error:[/bold red] Failed to find newest file: {e}")
        return None

def extract_version_from_filename(filename):
    # Look for _vX.Y.Z_ pattern
    match = re.search(r'_(v\d+\.\d+\.\d+)_', filename)
    if match:
        return match.group(1)
    return None

def sanitize_filename(name):
    # Static counter for unnamed method fallbacks
    if not hasattr(sanitize_filename, 'unnamed_counter'):
        sanitize_filename.unnamed_counter = 0
    
    # Replace invalid filename characters with '_'
    # Windows invalid chars: < > : " / \ | ? * and also control chars (0-31)
    # Note: parentheses () and commas , are actually valid in Windows filenames
    sanitized = re.sub(r'[<>:"/\\|?*\x00-\x1f,]', '_', name)
    
    # Remove leading/trailing whitespace and dots (Windows doesn't like these)
    sanitized = sanitized.strip(' .')
    
    # Windows has a path length limit of ~260 chars, filename portion should be much shorter
    # Truncate to 200 chars to be safe, keeping the extension area clear
    if len(sanitized) > 200:
        sanitized = sanitized[:200]
    
    # Ensure we don't end with a space or dot after truncation
    sanitized = sanitized.rstrip(' .')
    
    # Handle empty result with numbered fallback
    if not sanitized:
        sanitize_filename.unnamed_counter += 1
        sanitized = f"unnamed_method_{sanitize_filename.unnamed_counter:03d}"
    
    return sanitized

def process_patch_line(line, line_number, target):
    """Process a single line containing patch information"""
    # Get the part after the target string
    rest = line.split(target, 1)[1].strip()
    if not rest:
        console.print(f"[yellow]Warning[/yellow] (line {line_number}): No terms found after '{target}'.")
        return None, None
        
    # Split by '::'
    terms = [term.strip() for term in rest.split("::")]
    last_two = terms[-2:] if len(terms) >= 2 else terms
    
    if len(last_two) != 2:
        console.print(f"[yellow]Warning[/yellow] (line {line_number}): Expected two terms after '{target}', found {len(last_two)}:")
        console.print(f"  [dim]{last_two}[/dim]")
        return None, None
        
    if not last_two[1].endswith("):"):
        console.print(f"[yellow]Warning[/yellow] (line {line_number}): Second term does not end with '):': [dim]{last_two[1]}[/dim]")
        return None, None
        
    # Remove the trailing '):' from the second term
    method_name = last_two[1][:-2]
    patched_method = last_two[0] + "." + method_name
    
    return patched_method, method_name

def extract_patches(input_file, output_dir, target_pattern):
    """Extract patches from the log file and save to output directory"""
    patch_count = 0
    methods_found = []
    
    try:
        # Count total lines for progress bar
        total_lines = sum(1 for _ in open(input_file, 'r', encoding='utf-8'))
        
        with open(input_file, 'r', encoding='utf-8') as f:
            with Progress() as progress:
                task = progress.add_task("[cyan]Processing log file...", total=total_lines)
                
                for line_number, line in enumerate(f, start=1):
                    progress.update(task, advance=1)
                    
                    if target_pattern in line:
                        patched_method, _ = process_patch_line(line, line_number, target_pattern)
                        
                        if not patched_method:
                            continue
                        
                        safe_method = sanitize_filename(patched_method)
                        file_path = Path(output_dir) / f"{safe_method}.txt"
                        methods_found.append((patched_method, line_number, file_path))
                        
                        # Delete the file if it exists
                        if file_path.exists():
                            file_path.unlink()
                            
                        # Collect following lines until blank or EOF
                        lines_to_write = []
                        lines_to_write.append(patched_method + ":\n")
                        for following_line in f:
                            progress.update(task, advance=1)
                            if following_line.strip() == "":
                                break
                            lines_to_write.append(following_line.rstrip())
                            
                        # Write to file
                        with open(file_path, 'w', encoding='utf-8') as out_file:
                            out_file.write('\n'.join(lines_to_write))
                        
                        patch_count += 1
                        
        # Display summary in a table
        if patch_count > 0:
            table = Table(title=f"[bold green]Patch Extraction Summary[/bold green]")
            table.add_column("Method Name", style="cyan")
            table.add_column("Line", style="magenta")
            table.add_column("Output File", style="green")
            
            for method, line, filepath in methods_found:
                table.add_row(
                    Text(method, style="bold cyan"), 
                    str(line),
                    str(filepath)
                )
                
            console.print(table)
        
        console.print(Panel(f"[bold green]Total patches extracted: {patch_count}[/bold green]"))
        return 0
    except FileNotFoundError:
        console.print(f"[bold red]Error:[/bold red] The file '{input_file}' does not exist.")
        return 1
    except OSError as e:
        console.print(f"[bold red]Error:[/bold red] Could not read the file '{input_file}'. Reason: {e}")
        return 1

def setup_output_directory(input_file, output_prefix):
    """Set up and create the output directory based on version in filename"""
    # First check for version pattern
    version = extract_version_from_filename(input_file)
    if version:
        console.print(f"Extracted version: [bold green]{version}[/bold green]")
        output_dir = f"{output_prefix}_{version}"
        Path(output_dir).mkdir(parents=True, exist_ok=True)
        return output_dir
    else:
        DEFAULT_VERSION_PREFIX = "v2.5.x__"  # This should match the mod series versioning for major.minor

        # Try to extract timestamp from filename if it matches the expected format
        timestamp_match = re.search(r'output_log_client__(\d{4}-\d{2}-\d{2})__(\d{2}-\d{2}-\d{2})', Path(input_file).name)
        
        if timestamp_match:
            # Extract date and time from the filename
            date_str = timestamp_match.group(1)
            time_str = timestamp_match.group(2)
            timestamp_str = f"{date_str}__{time_str}"
            
            default_version = DEFAULT_VERSION_PREFIX + timestamp_str
            console.print(f"Extracted timestamp from filename: [bold green]{timestamp_str}[/bold green]")
        else:
            # Use current timestamp as before
            default_version = DEFAULT_VERSION_PREFIX + datetime.now().strftime("%Y-%m-%d__%H-%M-%S")
            console.print(f"No timestamp pattern found in filename, using current time")
        
        output_dir = f"{output_prefix}_{default_version}"
        Path(output_dir).mkdir(parents=True, exist_ok=True)
        console.print(f'[yellow]Version string not found[/yellow] in filename "{input_file}"')
        console.print(f'Using default version: [blue]{default_version}[/blue]')
        console.print(f'Output directory: [blue]{output_dir}[/blue]')
        return output_dir

def parse_args():
    """Parse command line arguments"""
    parser = argparse.ArgumentParser(
        description='Extract Harmony patches from 7 Days To Die log files')
    
    # Create a mutually exclusive group for input file options
    input_group = parser.add_mutually_exclusive_group()
    
    # Option to use default file (takes precedence over positional argument)
    input_group.add_argument('-d', '--default', action='store_true',
                        help='Use the default input file')
                        
    # Option to use latest log file from 7DTD logs directory
    input_group.add_argument('-l', '--latest-log', action='store_true',
                        help='Use the latest log file from 7 Days To Die logs directory')
    
    # Optional positional argument for input file
    input_group.add_argument('input_file', nargs='?', 
                        default=None,
                        help='Path to the log file')
    
    parser.add_argument('-o', '--output-prefix', 
                        default=DEFAULT_OUTPUT_DIR_PREFIX,
                        help=f'Prefix for the output directory (default: {DEFAULT_OUTPUT_DIR_PREFIX})')
    
    parser.add_argument('-t', '--target', 
                        default=DEFAULT_TARGET_PATTERN,
                        help='Target string to search for (default: Harmony patch identifier)')
    
    args = parser.parse_args()
    
    # Ignore empty string arguments
    if args.input_file == '':
        args.input_file = None
    
    if args.output_prefix == '':
        args.output_prefix = DEFAULT_OUTPUT_DIR_PREFIX
    
    if args.target == '':
        args.target = DEFAULT_TARGET_PATTERN
        
    return args

def main():
    # Add a nice header
    console.print(Panel.fit(
        "[bold cyan]Harmony Patch Extractor v0.0.1[/bold cyan]\n"
        "[dim]Extracts patched methods from 7 Days To Die logs[/dim]",
        border_style="blue"
    ))
    
    console.print(f"7 Days To Die Log Dir: [green]{SEVEN_DAYS_LOG_DIR}[/green]")
    
    args = parse_args()
    
    # Determine which input file to use
    input_file = DEFAULT_FILE_NAME
    
    if args.latest_log:
        latest_log = find_newest_file(SEVEN_DAYS_LOG_DIR)
        if latest_log:
            input_file = latest_log
            console.print(f"Using latest log file: [blue]{input_file}[/blue]")
        else:
            console.print(f"[yellow]Warning:[/yellow] Couldn't find latest log file. Using default.")
    elif not args.default and args.input_file is not None:
        input_file = args.input_file
    
    console.print(f"Using input file: [blue]{input_file}[/blue]")
    
    # Check if the file exists before proceeding
    if not Path(input_file).is_file():
        console.print(f"[bold red]Error:[/bold red] The file '{input_file}' does not exist.")
        return 1
    
    output_dir = setup_output_directory(str(input_file), args.output_prefix)
    
    return extract_patches(input_file, output_dir, args.target)

if __name__ == '__main__':
    sys.exit(main())