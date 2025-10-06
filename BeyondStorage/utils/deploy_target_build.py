#!/usr/bin/env python3
"""
Deploy Target Build Script

This script takes three command-line arguments:
1. BuildTarget - The build configuration target (e.g., Debug, Release)
2. OutputDir - The output directory containing the built assemblies
3. ProjectDir - The project directory path

The script:
1. Always copies <ModName>.dll from OutputDir to deployment directory (overwrites)
2. For Debug builds, also copies <ModName>.pdb from OutputDir (overwrites)
3. Recursively copies files from <ProjectDir>/ModPackage to deployment directories
4. For Release builds, creates a ZIP file: <ModName>_<version>-RC.zip in Uploads directory

Deployment directories based on BuildTarget:
- Debug: %APPDATA%/7DaysToDie/Mods/Dev_<ModName>
- Release: <ProjectDir>/Uploads/_staging/<ModName>

By default, only files that are newer in the source than in the target will be copied.

Usage:
    python deploy_target_build.py <BuildTarget> <OutputDir> <ProjectDir>

Examples: (keep the dir names generic)
    python deploy_target_build.py Debug "bin/Debug" "D:/Projects/ModName"
    python deploy_target_build.py Release "bin/Release" "C:/Source/ModSource"
    python deploy_target_build.py Debug "bin/Debug" "D:/Projects/ModName" --clean --force
"""

import sys
import os
import argparse
import shutil
import subprocess
import zipfile
from pathlib import Path
from datetime import datetime


# Configuration
MOD_NAME = "BeyondStorage2"


def get_assembly_version_via_reflection(dll_path):
    """
    Get assembly version using .NET reflection via PowerShell or direct .NET call.
    
    Args:
        dll_path (str): Path to the DLL file
        
    Returns:
        str: Version string (e.g., "2.4.0.0") or None if extraction fails
    """
    try:
        # Try using pythonnet if available
        try:
            import clr
            import sys
            sys.path.append(os.path.dirname(dll_path))
            
            from System.Reflection import Assembly
            from System import Exception as DotNetException
            
            assembly = Assembly.LoadFrom(os.path.abspath(dll_path))
            version = assembly.GetName().Version
            return f"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}"
            
        except ImportError:
            # Fallback to PowerShell approach
            pass
        
        # Use PowerShell to get assembly version
        powershell_script = f"""
        try {{
            $assembly = [System.Reflection.Assembly]::LoadFrom('{dll_path.replace("'", "''")}')
            $version = $assembly.GetName().Version
            Write-Output "$($version.Major).$($version.Minor).$($version.Build).$($version.Revision)"
        }} catch {{
            Write-Output "Error: $($_.Exception.Message)"
        }}
        """
        
        result = subprocess.run(
            ["powershell", "-Command", powershell_script],
            capture_output=True,
            text=True,
            timeout=10
        )
        
        if result.returncode == 0:
            version = result.stdout.strip()
            if version and not version.startswith("Error:"):
                return version
        
        return None
        
    except Exception as e:
        print(f"Warning: Failed to get assembly version via reflection: {e}")
        return None


def get_assembly_version_via_dotnet_tool(dll_path):
    """
    Get assembly version using dotnet CLI or custom tool.
    
    Args:
        dll_path (str): Path to the DLL file
        
    Returns:
        str: Version string or None if extraction fails
    """
    try:
        # Try using dotnet CLI if available
        result = subprocess.run(
            ["dotnet", "--version"],
            capture_output=True,
            text=True,
            timeout=5
        )
        
        if result.returncode == 0:
            # Use a simple C# program to get the version
            temp_cs_file = os.path.join(os.path.dirname(dll_path), "GetVersion.cs")
            cs_code = f'''
using System;
using System.Reflection;

class Program
{{
    static void Main()
    {{
        try
        {{
            var assembly = Assembly.LoadFrom(@"{dll_path}");
            var version = assembly.GetName().Version;
            Console.WriteLine($"{{version.Major}}.{{version.Minor}}.{{version.Build}}.{{version.Revision}}");
        }}
        catch (Exception ex)
        {{
            Console.WriteLine($"Error: {{ex.Message}}");
        }}
    }}
}}
'''
            
            # Write temporary C# file
            with open(temp_cs_file, 'w') as f:
                f.write(cs_code)
            
            # Compile and run
            exe_file = temp_cs_file.replace('.cs', '.exe')
            compile_result = subprocess.run(
                ["csc", "/out:" + exe_file, temp_cs_file],
                capture_output=True,
                text=True,
                timeout=10
            )
            
            if compile_result.returncode == 0:
                run_result = subprocess.run(
                    [exe_file],
                    capture_output=True,
                    text=True,
                    timeout=5
                )
                
                if run_result.returncode == 0:
                    version = run_result.stdout.strip()
                    if version and not version.startswith("Error:"):
                        # Cleanup
                        try:
                            os.remove(temp_cs_file)
                            os.remove(exe_file)
                        except:
                            pass
                        return version
            
            # Cleanup on failure
            try:
                os.remove(temp_cs_file)
                if os.path.exists(exe_file):
                    os.remove(exe_file)
            except:
                pass
        
        return None
        
    except Exception as e:
        print(f"Warning: Failed to get assembly version via dotnet tool: {e}")
        return None


def get_assembly_version(dll_path):
    """
    Get assembly version using various .NET reflection methods.
    
    Args:
        dll_path (str): Path to the DLL file
        
    Returns:
        str: Version string or "Unknown" if all methods fail
    """
    if not os.path.exists(dll_path):
        return "File not found"
    
    # Try reflection approach first
    version = get_assembly_version_via_reflection(dll_path)
    if version:
        return version
    
    # Try dotnet tool approach
    version = get_assembly_version_via_dotnet_tool(dll_path)
    if version:
        return version
    
    return "Unknown"


def create_release_zip(deploy_dir, uploads_dir, assembly_version, stats):
    """
    Create a ZIP file of the mod directory for Release builds.
    
    Args:
        deploy_dir (str): Path to the deployed mod directory
        uploads_dir (str): Path to the Uploads directory (parent of _staging)
        assembly_version (str): Assembly version string
        stats (DeploymentStats): Statistics tracker
        
    Returns:
        str: Path to created ZIP file or None if failed
    """
    try:
        # Ensure uploads directory exists
        os.makedirs(uploads_dir, exist_ok=True)
        
        # Create ZIP filename with full version
        if assembly_version and assembly_version not in ["Unknown", "File not found"]:
            # Use the full version as provided (e.g., "2.4.0.0")
            version_string = assembly_version
        else:
            version_string = "0.0.0.0"
        
        zip_filename = f"{MOD_NAME}_{version_string}-RC.zip"
        zip_path = os.path.join(uploads_dir, zip_filename)
        
        # Remove existing ZIP file if it exists
        if os.path.exists(zip_path):
            os.remove(zip_path)
            print(f"Removed existing ZIP file: {zip_filename}")
        
        # Create ZIP file
        print(f"Creating release ZIP: {zip_filename}")
        
        with zipfile.ZipFile(zip_path, 'w', zipfile.ZIP_DEFLATED, compresslevel=9) as zipf:
            # Get the mod directory name (should be MOD_NAME)
            mod_dir_name = os.path.basename(deploy_dir)
            
            # Walk through all files in the deploy directory
            files_added = 0
            total_size = 0
            
            for root, dirs, files in os.walk(deploy_dir):
                for file in files:
                    file_path = os.path.join(root, file)
                    
                    # Calculate relative path from deploy_dir
                    rel_path = os.path.relpath(file_path, deploy_dir)
                    
                    # Add to ZIP with mod directory structure
                    arcname = os.path.join(mod_dir_name, rel_path).replace(os.path.sep, '/')
                    
                    zipf.write(file_path, arcname)
                    files_added += 1
                    total_size += os.path.getsize(file_path)
                    
                    print(f"  Added to ZIP: {rel_path}")
        
        # Get ZIP file size
        zip_size = os.path.getsize(zip_path)
        compression_ratio = (1 - zip_size / total_size) * 100 if total_size > 0 else 0
        
        print(f"ZIP created successfully:")
        print(f"  Files: {files_added}")
        print(f"  Original size: {total_size / 1024:.1f} KB")
        print(f"  Compressed size: {zip_size / 1024:.1f} KB")
        print(f"  Compression: {compression_ratio:.1f}%")
        print(f"  Location: {zip_path}")
        
        # Update statistics
        stats.zip_created = True
        stats.zip_path = zip_path
        stats.zip_files_count = files_added
        stats.zip_size = zip_size
        
        return zip_path
        
    except Exception as e:
        print(f"Failed to create release ZIP: {e}", file=sys.stderr)
        stats.zip_failed = True
        stats.zip_error = str(e)
        return None


class DeploymentStats:
    """Class to track deployment statistics"""
    
    def __init__(self):
        self.files_copied = 0
        self.files_skipped = 0
        self.files_up_to_date = 0
        self.files_failed = 0
        self.files_forced = 0
        self.assemblies_copied = 0
        self.assemblies_failed = 0
        self.directories_created = 0
        self.directories_existing = 0
        self.total_files_processed = 0
        self.total_bytes_copied = 0
        self.zip_created = False
        self.zip_failed = False
        self.zip_path = None
        self.zip_files_count = 0
        self.zip_size = 0
        self.zip_error = None
        self.start_time = datetime.now()
        self.copied_files = []
        self.skipped_files = []
        self.failed_files = []
        self.forced_files = []
        self.assembly_files = []
    
    def file_copied(self, source_file, target_file, file_size):
        """Record a successful file copy"""
        self.files_copied += 1
        self.total_bytes_copied += file_size
        self.copied_files.append((source_file, target_file))
    
    def file_forced(self, source_file, target_file, file_size):
        """Record a file that was force-copied"""
        self.files_forced += 1
        self.total_bytes_copied += file_size
        self.forced_files.append((source_file, target_file))
    
    def assembly_copied(self, source_file, target_file, file_size):
        """Record a successful assembly file copy"""
        self.assemblies_copied += 1
        self.total_bytes_copied += file_size
        self.assembly_files.append((source_file, target_file))
    
    def assembly_failed(self, source_file, error):
        """Record a failed assembly file copy"""
        self.assemblies_failed += 1
        self.failed_files.append((source_file, str(error)))
    
    def file_skipped_up_to_date(self, source_file):
        """Record a file that was skipped because it's up to date"""
        self.files_up_to_date += 1
        self.skipped_files.append(source_file)
    
    def file_failed(self, source_file, error):
        """Record a file that failed to copy"""
        self.files_failed += 1
        self.failed_files.append((source_file, str(error)))
    
    def directory_created(self):
        """Record a directory creation"""
        self.directories_created += 1
    
    def directory_existing(self):
        """Record an existing directory"""
        self.directories_existing += 1
    
    def get_duration(self):
        """Get the elapsed time since start"""
        return datetime.now() - self.start_time
    
    def has_errors(self):
        """Check if there were any errors during deployment"""
        return self.files_failed > 0 or self.assemblies_failed > 0 or self.zip_failed
    
    def print_summary(self):
        """Print comprehensive deployment statistics"""
        duration = self.get_duration()
        
        print("=" * 60)
        print("DEPLOYMENT STATISTICS")
        print("=" * 60)
        print(f"Total execution time: {duration.total_seconds():.2f} seconds")
        print()
        
        print("Assembly Operations:")
        print(f"  [DLL] Assemblies copied:       {self.assemblies_copied}")
        print(f"  [ER]  Assembly copy failed:    {self.assemblies_failed}")
        print()
        
        print("File Operations:")
        print(f"  [OK] Files copied (newer):     {self.files_copied}")
        print(f"  [>>] Files force-copied:       {self.files_forced}")
        print(f"  [--] Files skipped (up-to-date): {self.files_up_to_date}")
        print(f"  [ER] Files failed:             {self.files_failed}")
        print(f"  [##] Total files processed:    {self.files_copied + self.files_forced + self.files_up_to_date + self.files_failed}")
        print()
        
        print("Directory Operations:")
        print(f"  [+] Directories created:      {self.directories_created}")
        print(f"  [=] Directories existing:     {self.directories_existing}")
        print()
        
        # ZIP Operations (for Release builds)
        if self.zip_created or self.zip_failed:
            print("ZIP Operations:")
            if self.zip_created:
                print(f"  [ZIP] Release ZIP created:     1")
                print(f"  [ZIP] Files in ZIP:            {self.zip_files_count}")
                print(f"  [ZIP] ZIP size:                {self.zip_size / 1024:.1f} KB")
                print(f"  [ZIP] ZIP location:            {os.path.basename(self.zip_path) if self.zip_path else 'N/A'}")
            if self.zip_failed:
                print(f"  [ER]  ZIP creation failed:     1")
                if self.zip_error:
                    print(f"  [ER]  ZIP error:               {self.zip_error}")
            print()
        
        if self.total_bytes_copied > 0:
            if self.total_bytes_copied > 1024 * 1024:
                size_str = f"{self.total_bytes_copied / (1024 * 1024):.2f} MB"
            elif self.total_bytes_copied > 1024:
                size_str = f"{self.total_bytes_copied / 1024:.2f} KB"
            else:
                size_str = f"{self.total_bytes_copied} bytes"
            print(f"Total data copied: {size_str}")
            print()
        
        # Show assembly files if any
        if self.assembly_files:
            print("Assembly files copied:")
            for source, target in self.assembly_files:
                print(f"  [DLL] {os.path.basename(source)}")
            print()
        
        # Show copied files if any
        all_copied = self.copied_files + self.forced_files
        if all_copied and len(all_copied) <= 20:
            print("Package files copied:")
            for source, target in self.copied_files:
                print(f"  [OK] {os.path.basename(source)}")
            for source, target in self.forced_files:
                print(f"  [>>] {os.path.basename(source)} (forced)")
            print()
        elif len(all_copied) > 20:
            print(f"Package files copied: {len(all_copied)} files (too many to list)")
            if self.files_forced > 0:
                print(f"  ({self.files_forced} were force-copied)")
            print()
        
        # Show failed files if any
        if self.failed_files:
            print("Failed files:")
            for source, error in self.failed_files:
                rel_source = os.path.basename(source)
                print(f"  [ER] {rel_source}: {error}")
            print()
        
        # Overall status
        if self.has_errors():
            print("WARNING: Deployment completed with errors!")
        elif (self.files_copied + self.files_forced + self.assemblies_copied) > 0 or self.zip_created:
            print("SUCCESS: Deployment completed successfully!")
        elif self.files_up_to_date > 0:
            print("INFO: All files are up to date - no copying needed.")
        else:
            print("WARNING: No files found to process.")


def get_deploy_directory(build_target, project_dir):
    """
    Get the deployment directory path based on the build target.
    
    Args:
        build_target (str): Build configuration target (Debug, Release, etc.)
        project_dir (str): Project directory path
    
    Returns:
        str: Full path to the deployment directory
    """
    build_target_lower = build_target.lower()
    
    if build_target_lower == "debug":
        # Debug builds go to APPDATA with Dev_ prefix
        appdata = os.environ.get('APPDATA')
        if not appdata:
            raise EnvironmentError("APPDATA environment variable not found")
        
        deploy_dir = os.path.join(appdata, "7DaysToDie", "Mods", f"Dev_{MOD_NAME}")
        return deploy_dir
        
    elif build_target_lower == "release":
        # Release builds go to project Uploads/_staging without Dev_ prefix
        deploy_dir = os.path.join(project_dir, "Uploads", "_staging", MOD_NAME)
        return deploy_dir
        
    else:
        # For other build targets, default to Debug behavior but show warning
        print(f"Warning: Unknown build target '{build_target}', defaulting to Debug behavior")
        appdata = os.environ.get('APPDATA')
        if not appdata:
            raise EnvironmentError("APPDATA environment variable not found")
        
        deploy_dir = os.path.join(appdata, "7DaysToDie", "Mods", f"Dev_{MOD_NAME}")
        return deploy_dir


def ensure_directory_exists(directory_path, stats):
    """
    Ensure that the specified directory exists, creating it if necessary.
    
    Args:
        directory_path (str): Path to the directory
        stats (DeploymentStats): Statistics tracker
    """
    try:
        if not os.path.exists(directory_path):
            os.makedirs(directory_path, exist_ok=True)
            stats.directory_created()
            print(f"Created directory: {directory_path}")
        else:
            stats.directory_existing()
    except OSError as e:
        raise OSError(f"Failed to create directory {directory_path}: {e}")


def copy_assembly_files(output_dir, deploy_dir, build_target, stats):
    """
    Copy assembly files (.dll and .pdb for Debug) from output directory to deployment directory.
    These files are always overwritten regardless of modification time or other flags.
    
    Args:
        output_dir (str): Output directory containing built assemblies
        deploy_dir (str): Deployment directory
        build_target (str): Build target (Debug, Release, etc.)
        stats (DeploymentStats): Statistics tracker
    """
    assembly_files = [f"{MOD_NAME}.dll"]
    
    # Add .pdb file for Debug builds
    if build_target.lower() == "debug":
        assembly_files.append(f"{MOD_NAME}.pdb")
    
    print("Copying assembly files (always overwrite):")
    
    for assembly_file in assembly_files:
        source_file = os.path.join(output_dir, assembly_file)
        target_file = os.path.join(deploy_dir, assembly_file)
        
        try:
            if not os.path.exists(source_file):
                print(f"Warning: Assembly file not found: {source_file}")
                stats.assembly_failed(source_file, "File not found")
                continue
            
            # Get file size
            file_size = os.path.getsize(source_file)
            
            # Always copy assembly files (force overwrite)
            shutil.copy2(source_file, target_file)
            stats.assembly_copied(source_file, target_file, file_size)
            print(f"Assembly copied: {assembly_file}")
            
        except (IOError, OSError) as e:
            stats.assembly_failed(source_file, e)
            print(f"Failed to copy assembly {assembly_file}: {e}", file=sys.stderr)
    
    print()


def should_copy_file(source_file, target_file, force_copy=False):
    """
    Determine if a file should be copied based on modification time or force flag.
    
    Args:
        source_file (str): Path to source file
        target_file (str): Path to target file
        force_copy (bool): If True, always copy regardless of modification time
        
    Returns:
        tuple: (should_copy, is_forced) where should_copy is bool and is_forced indicates if it's a forced copy
    """
    # If force_copy is True, always copy
    if force_copy:
        return True, True
    
    # If target doesn't exist, always copy
    if not os.path.exists(target_file):
        return True, False
    
    try:
        source_mtime = os.path.getmtime(source_file)
        target_mtime = os.path.getmtime(target_file)
        
        # Copy if source is newer than target
        return source_mtime > target_mtime, False
    except OSError:
        # If we can't get modification times, err on the side of copying
        return True, False


def copy_mod_package(source_dir, target_dir, stats, force_copy=False):
    """
    Recursively copy files and directories from source to target.
    Only copies files that are newer in source than in target unless force_copy is True.
    
    Args:
        source_dir (str): Source directory path (ModPackage)
        target_dir (str): Target directory path (deployment directory)
        stats (DeploymentStats): Statistics tracker
        force_copy (bool): If True, copy all files regardless of modification time
    """
    # Walk through all files and directories in source
    for root, dirs, files in os.walk(source_dir):
        # Calculate relative path from source_dir
        rel_path = os.path.relpath(root, source_dir)
        
        # Create corresponding directory in target
        if rel_path == ".":
            target_root = target_dir
        else:
            target_root = os.path.join(target_dir, rel_path)
        
        # Ensure the directory exists
        ensure_directory_exists(target_root, stats)
        
        # Process all files in current directory
        for file in files:
            source_file = os.path.join(root, file)
            target_file = os.path.join(target_root, file)
            
            try:
                # Check if file should be copied
                should_copy, is_forced = should_copy_file(source_file, target_file, force_copy)
                
                if should_copy:
                    # Get file size before copying
                    file_size = os.path.getsize(source_file)
                    
                    # Copy the file
                    shutil.copy2(source_file, target_file)
                    
                    rel_source = os.path.relpath(source_file, source_dir)
                    
                    if is_forced:
                        stats.file_forced(source_file, target_file, file_size)
                        print(f"Force-copied: {rel_source}")
                    else:
                        stats.file_copied(source_file, target_file, file_size)
                        print(f"Copied: {rel_source}")
                else:
                    # File is up to date
                    stats.file_skipped_up_to_date(source_file)
                    rel_source = os.path.relpath(source_file, source_dir)
                    print(f"Up-to-date: {rel_source}")
                    
            except (IOError, OSError) as e:
                stats.file_failed(source_file, e)
                print(f"Failed to copy {os.path.relpath(source_file, source_dir)}: {e}", file=sys.stderr)


def clean_deploy_directory(deploy_dir):
    """
    Clean the deployment directory by removing all existing files and directories.
    
    Args:
        deploy_dir (str): Deployment directory path
    """
    if os.path.exists(deploy_dir):
        print(f"Cleaning deployment directory: {deploy_dir}")
        try:
            shutil.rmtree(deploy_dir)
            print("Deployment directory cleaned successfully")
        except (IOError, OSError) as e:
            print(f"Warning: Failed to clean deployment directory: {e}", file=sys.stderr)


def wait_for_keypress():
    """
    Wait for user to press any key before continuing.
    Cross-platform implementation.
    """
    try:
        print("\nPress any key to continue...")
        
        # Try to use platform-specific method
        if os.name == 'nt':  # Windows
            import msvcrt
            msvcrt.getch()
        else:  # Unix/Linux/macOS
            import termios
            import tty
            fd = sys.stdin.fileno()
            old_settings = termios.tcgetattr(fd)
            try:
                tty.setraw(sys.stdin.fileno())
                sys.stdin.read(1)
            finally:
                termios.tcsetattr(fd, termios.TCSADRAIN, old_settings)
                
    except (ImportError, OSError):
        # Fallback to input() if platform-specific methods fail
        input("Press Enter to continue...")


def main():
    """Main function to parse arguments and deploy the mod."""
    
    # Create argument parser
    parser = argparse.ArgumentParser(
        description=f'Deploy Target Build Script - deploys {MOD_NAME} mod to target-specific directory',
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog=f"""
Examples:
  %(prog)s Debug "bin/Debug" "D:/Projects/ModName"
  %(prog)s Release "bin/Release" "C:/Source/ModSource"
  %(prog)s Debug "bin/Debug" "D:/Projects/ModName" --clean
  %(prog)s Debug "bin/Debug" "D:/Projects/ModName" --force
  %(prog)s Debug "bin/Debug" "D:/Projects/ModName" --clean --force
  %(prog)s Debug "bin/Debug" "D:/Projects/ModName" --no-pause

This script will:
1. Copy assembly files from <OutputDir> (always overwrite):
   - {MOD_NAME}.dll (for all build targets)
   - {MOD_NAME}.pdb (for Debug builds only)

2. Copy package files from <ProjectDir>/ModPackage to:
   DEBUG builds:   %APPDATA%/7DaysToDie/Mods/Dev_{MOD_NAME}
   RELEASE builds: <ProjectDir>/Uploads/_staging/{MOD_NAME}

3. For RELEASE builds: Create {MOD_NAME}_<version>-RC.zip in <ProjectDir>/Uploads/

By default, only package files that are newer will be copied (incremental deployment).
Assembly files are ALWAYS copied regardless of modification time or other flags.
Use --force to copy all package files regardless of modification time.
Use --clean to remove all existing files before copying.
Use --no-pause to skip pausing on errors (default: pause on errors).
        """
    )
    
    # Add required arguments
    parser.add_argument(
        'BuildTarget',
        help='Build configuration target (e.g., Debug, Release)'
    )
    
    parser.add_argument(
        'OutputDir',
        help='Output directory containing built assemblies (e.g., bin/Debug, bin/Release)'
    )
    
    parser.add_argument(
        'ProjectDir', 
        help='Project directory path'
    )
    
    # Add optional arguments
    parser.add_argument(
        '--clean',
        action='store_true',
        default=False,
        help='Clean the deployment directory before copying (default: False)'
    )
    
    parser.add_argument(
        '--force',
        action='store_true',
        default=False,
        help='Copy all package files regardless of modification time (default: False)'
    )
    
    parser.add_argument(
        '--no-pause',
        action='store_true',
        default=False,
        help='Skip pausing for keypress on errors or exceptions (default: False, meaning pause on errors)'
    )
    
    parser.add_argument(
        '--verbose',
        action='store_true',
        help='Show detailed output for all file operations'
    )
    
    # Parse arguments
    try:
        args = parser.parse_args()
    except SystemExit:
        return 1
    
    # Track if we had an exception or errors for pause decision
    had_exception = False
    exit_code = 0
    assembly_version = None
    
    try:
        # Initialize statistics
        stats = DeploymentStats()
        
        # Determine deployment mode
        mode_parts = []
        if args.clean:
            mode_parts.append("Clean")
        if args.force:
            mode_parts.append("Force")
        
        if mode_parts:
            mode = " + ".join(mode_parts) + " deployment"
        else:
            mode = "Incremental deployment"
        
        # Print configuration
        print(f"Deploy Target Build Script for {MOD_NAME}")
        print("=" * 50)
        print(f"BuildTarget: {args.BuildTarget}")
        print(f"OutputDir: {args.OutputDir}")
        print(f"ProjectDir: {args.ProjectDir}")
        print(f"Mode: {mode}")
        print()
        
        # Validate ProjectDir
        project_path = Path(args.ProjectDir)
        if not project_path.exists():
            print(f"Error: Project directory does not exist: {args.ProjectDir}", file=sys.stderr)
            return 1
        
        # Validate OutputDir
        output_path = Path(args.OutputDir)
        if not output_path.exists():
            print(f"Error: Output directory does not exist: {args.OutputDir}", file=sys.stderr)
            return 1
        
        # Check for ModPackage directory
        mod_package_dir = project_path / "ModPackage"
        if not mod_package_dir.exists():
            print(f"Error: ModPackage directory not found: {mod_package_dir}", file=sys.stderr)
            return 1
        
        # Get deployment directory based on build target
        deploy_dir = get_deploy_directory(args.BuildTarget, str(project_path))
        print(f"DeployDir: {deploy_dir}")
        
        # For Release builds, get and display assembly version
        if args.BuildTarget.lower() == "release":
            dll_path = os.path.join(str(output_path), f"{MOD_NAME}.dll")
            assembly_version = get_assembly_version(dll_path)
            print(f"Assembly Version: {assembly_version}")
        
        # Show deployment strategy
        if args.BuildTarget.lower() == "debug":
            print("Deployment strategy: Debug build -> Development directory (APPDATA with Dev_ prefix)")
            print(f"Assembly files: {MOD_NAME}.dll, {MOD_NAME}.pdb")
        elif args.BuildTarget.lower() == "release":
            print("Deployment strategy: Release build -> Staging directory (ProjectDir/Uploads/_staging)")
            print(f"Assembly files: {MOD_NAME}.dll")
            # Use full version string for display
            display_version = assembly_version if assembly_version and assembly_version not in ['Unknown', 'File not found'] else '0.0.0.0'
            print(f"ZIP file: {MOD_NAME}_{display_version}-RC.zip")
        else:
            print(f"Deployment strategy: Unknown target '{args.BuildTarget}' -> Defaulting to Debug behavior")
            print(f"Assembly files: {MOD_NAME}.dll, {MOD_NAME}.pdb")
        print()
        
        # Clean deployment directory if requested
        if args.clean:
            clean_deploy_directory(deploy_dir)
        
        # Ensure deployment directory exists
        ensure_directory_exists(deploy_dir, stats)
        
        # Copy assembly files first (always overwrite)
        copy_assembly_files(str(output_path), deploy_dir, args.BuildTarget, stats)
        
        # Copy ModPackage contents to deployment directory
        print("Copying package files:")
        print(f"From: {mod_package_dir}")
        print(f"To: {deploy_dir}")
        print()
        
        copy_mod_package(str(mod_package_dir), deploy_dir, stats, args.force)
        
        # For Release builds, create ZIP file
        if args.BuildTarget.lower() == "release":
            print()
            uploads_dir = os.path.join(str(project_path), "Uploads")
            create_release_zip(deploy_dir, uploads_dir, assembly_version, stats)
        
        # Print comprehensive statistics
        stats.print_summary()
        
        # Set exit code based on results
        exit_code = 1 if stats.has_errors() else 0
        
    except Exception as e:
        had_exception = True
        exit_code = 1
        print(f"\nError during deployment: {e}", file=sys.stderr)
        
        # Try to print partial statistics if available
        try:
            if 'stats' in locals():
                print("\nPartial statistics before error:")
                stats.print_summary()
        except:
            pass  # Don't let statistics printing cause additional errors
    
    finally:
        # Pause if there were exceptions or failed files, unless --no-pause is specified
        should_pause = not args.no_pause and (had_exception or ('stats' in locals() and stats.has_errors()))
        
        if should_pause:
            print("\n" + "=" * 60)
            if had_exception:
                print("WARNING: Script encountered an exception!")
            if 'stats' in locals() and stats.has_errors():
                total_errors = stats.files_failed + stats.assemblies_failed
                if stats.zip_failed:
                    total_errors += 1
                print(f"WARNING: {total_errors} operation(s) failed!")
            
            print("Please review the error messages above.")
            wait_for_keypress()
    
    return exit_code


if __name__ == "__main__":
    sys.exit(main())