#!/usr/bin/env python3
"""
Code Quality Checker for C# files - Main Entry Point
Modular code quality checking system for BeyondStorage project

MSBUILD Integration Example:
  <Target Name="CodeQualityChecks" BeforeTargets="Build">
    <Exec Command="python code_check.py"
          ContinueOnError="false"
          WorkingDirectory="$(ProjectDir)">
      <Output TaskParameter="ExitCode" PropertyName="CodeQualityChecksExitCode" />
    </Exec>
    <Error Condition="'$(CodeQualityChecksExitCode)' != '0'"
           Text="Code quality checks failed with exit code $(CodeQualityChecksExitCode). Build halted due to errors." />
  </Target>
"""

import os
import sys
from typing import List, Dict, Tuple

# Configure UTF-8 output
sys.stdout.reconfigure(encoding='utf-8')

# Import our modular components
from models import Issue, CheckResult
from utils import clean_file_path, find_cs_files
from roslyn_analyzer import RoslynAnalyzer, is_roslyn_available
from string_checks import StringBasedChecker
from harmony_checks import HarmonyChecker
from reporter import Reporter


class CodeQualityChecker:
    """Main orchestrator class for code quality checking"""
    
    def __init__(self):
        self.forbidden_strings: Dict[str, Tuple[str, str, str]] = {}  # pattern -> (severity, code, description)
        self.roslyn_analyzer = RoslynAnalyzer() if is_roslyn_available() else None
        self.string_checker = StringBasedChecker()
        self.harmony_checker = HarmonyChecker()
        self._register_forbidden_strings()
    
    def _register_forbidden_strings(self):
        """Register forbidden string patterns"""
        # Error-level string checks
        self.add_forbidden_string_check(
            "throw new NotImplementedException(",
            "error", 
            "BCS002",
            "NotImplementedException found - should be properly implemented"
        )
        
        self.add_forbidden_string_check(
            "name} ",
            "error",
            "BCS004",
            "Logging format violation 'name} ' found - check for malformed interpolation"
        )
        
        self.add_forbidden_string_check(
            "System.Threading.Thread.Sleep",
            "error", 
            "BCS030",
            "Thread.Sleep can cause performance issues - use async/await patterns"
        )
        
        # Warning-level string checks
        self.add_forbidden_string_check(
            "Console.WriteLine",
            "warning",
            "BCW001", 
            "Console.WriteLine should be replaced with ModLogger"
        )
        
        self.add_forbidden_string_check(
            "Debug.Log",
            "warning",
            "BCW002",
            "Debug.Log should be replaced with ModLogger"
        )
        
        self.add_forbidden_string_check(
            "UnityEngine.Debug.Log",
            "warning",
            "BCW003",
            "Unity Debug.Log should be replaced with ModLogger"
        )
        
        self.add_forbidden_string_check(
            ".Result",
            "warning",
            "BCW060", 
            "Synchronous access to async result can cause deadlocks - use await"
        )

        self.add_forbidden_string_check(
            "ExtraLogging = true",
            "warning",
            "BCW061",
            "Too much logging of Harmony Patch. Ok for Debug, but not ok for Release"
        )
    
    def add_forbidden_string_check(self, forbidden_string: str, severity: str, code: str, description: str):
        """Add a check for a string that should not exist in the code"""
        self.forbidden_strings[forbidden_string] = (severity, code, description)
    
    def check_file(self, file_path: str) -> List[CheckResult]:
        """Check a single C# file for all registered issues"""
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
        except (IOError, UnicodeDecodeError) as e:
            return [CheckResult(
                check_name="file_read_error",
                issues=[Issue(
                    file_path=clean_file_path(file_path),
                    line_number=1,
                    severity="error",
                    code="BCS999",
                    description=f"Failed to read file: {e}"
                )]
            )]

        results = []
        
        # Run forbidden strings check
        if self.forbidden_strings:
            issues = self.string_checker.check_forbidden_strings(file_path, content, self.forbidden_strings)
            if issues:
                results.append(CheckResult(
                    check_name="forbidden_strings",
                    issues=issues
                ))
        
        # Run Roslyn-enhanced checks if available
        if self.roslyn_analyzer:
            # Parse file once for all Roslyn checks
            syntax_tree, root = self.roslyn_analyzer.parse_file(file_path)
            if syntax_tree and root:
                # Harmony patch class checks
                issues = self.roslyn_analyzer.check_harmony_patch_classes(file_path, syntax_tree, root)
                if issues:
                    for issue in issues:
                        issue.file_path = clean_file_path(issue.file_path)
                    results.append(CheckResult(
                        check_name="harmony_patch_classes_roslyn",
                        issues=issues
                    ))
                
                # Harmony patch method checks
                issues = self.roslyn_analyzer.check_harmony_patch_methods(file_path, syntax_tree, root)
                if issues:
                    for issue in issues:
                        issue.file_path = clean_file_path(issue.file_path)
                    results.append(CheckResult(
                        check_name="harmony_patch_methods_roslyn",
                        issues=issues
                    ))
                
                # Empty catch blocks
                issues = self.roslyn_analyzer.check_empty_catch_blocks(file_path, syntax_tree, root)
                if issues:
                    for issue in issues:
                        issue.file_path = clean_file_path(issue.file_path)
                    results.append(CheckResult(
                        check_name="empty_catch_blocks_roslyn",
                        issues=issues
                    ))
                
                # Magic numbers
                issues = self.roslyn_analyzer.check_magic_numbers(file_path, syntax_tree, root)
                if issues:
                    for issue in issues:
                        issue.file_path = clean_file_path(issue.file_path)
                    results.append(CheckResult(
                        check_name="magic_numbers_roslyn",
                        issues=issues
                    ))
                
                # Cyclomatic complexity
                issues = self.roslyn_analyzer.check_cyclomatic_complexity(file_path, syntax_tree, root)
                if issues:
                    for issue in issues:
                        issue.file_path = clean_file_path(issue.file_path)
                    results.append(CheckResult(
                        check_name="cyclomatic_complexity_roslyn",
                        issues=issues
                    ))
            else:
                # Fallback to string-based checks if Roslyn parsing fails
                self._run_string_based_checks(file_path, content, results)
        else:
            # Run string-based checks if Roslyn is not available
            self._run_string_based_checks(file_path, content, results)
        
        # Always run these string-based checks
        self._run_additional_string_checks(file_path, content, results)
        
        return results
    
    def _run_string_based_checks(self, file_path: str, content: str, results: List[CheckResult]):
        """Run string-based versions of core checks when Roslyn is not available"""
        # Harmony patch class checks
        issues = self.harmony_checker.check_harmony_patch_class_declaration(file_path, content)
        if issues:
            results.append(CheckResult(
                check_name="harmony_patch_classes_string",
                issues=issues
            ))
        
        # Harmony patch method checks
        issues = self.harmony_checker.check_harmony_patch_method_declaration(file_path, content)
        if issues:
            results.append(CheckResult(
                check_name="harmony_patch_methods_string",
                issues=issues
            ))
        
        # Empty catch blocks
        issues = self.string_checker.check_empty_catch_blocks(file_path, content)
        if issues:
            results.append(CheckResult(
                check_name="empty_catch_blocks_string",
                issues=issues
            ))
        
        # Magic numbers
        issues = self.string_checker.check_magic_numbers(file_path, content)
        if issues:
            results.append(CheckResult(
                check_name="magic_numbers_string",
                issues=issues
            ))
        
        # Cyclomatic complexity
        issues = self.string_checker.check_cyclomatic_complexity(file_path, content)
        if issues:
            results.append(CheckResult(
                check_name="cyclomatic_complexity_string",
                issues=issues
            ))
    
    def _run_additional_string_checks(self, file_path: str, content: str, results: List[CheckResult]):
        """Run additional string-based checks that are always performed"""
        # TODO comments
        issues = self.string_checker.check_todo_comments(file_path, content)
        if issues:
            results.append(CheckResult(
                check_name="todo_comments",
                issues=issues
            ))
        
        # Null checks
        issues = self.string_checker.check_null_checks(file_path, content)
        if issues:
            results.append(CheckResult(
                check_name="null_checks",
                issues=issues
            ))
        
        # Disposable usage
        issues = self.string_checker.check_disposable_usage(file_path, content)
        if issues:
            results.append(CheckResult(
                check_name="disposable_usage",
                issues=issues
            ))
        
        # String interpolation
        issues = self.string_checker.check_string_interpolation(file_path, content)
        if issues:
            results.append(CheckResult(
                check_name="string_interpolation",
                issues=issues
            ))
        
        # String in loops
        issues = self.string_checker.check_string_in_loops(file_path, content)
        if issues:
            results.append(CheckResult(
                check_name="string_in_loops",
                issues=issues
            ))
        
        # LINQ performance
        issues = self.string_checker.check_linq_performance(file_path, content)
        if issues:
            results.append(CheckResult(
                check_name="linq_performance",
                issues=issues
            ))
    
    def run_all_checks(self, root_dir: str = ".") -> bool:
        """Run all checks on all C# files and return True if no errors found"""
        print("BeyondStorage Code Quality Checker")
        print("=" * 50)
        
        # Show Roslyn status
        if is_roslyn_available():
            print("✓ Roslyn AST parsing: ENABLED")
            print("  Enhanced accuracy for: HarmonyPatch classes, HarmonyPatch methods, empty catch blocks, magic numbers, cyclomatic complexity")
        else:
            print("⚠ Roslyn AST parsing: DISABLED - using string-based parsing")
            print("  To enable: pip install pythonnet and ensure Roslyn assemblies are available")
        print()
        
        # Clean up old result files first
        deleted_files = Reporter.cleanup_old_result_files(keep_latest=5)
        if deleted_files > 0:
            print(f"Cleaned up {deleted_files} old result file(s)")
            print()
        
        cs_files = find_cs_files(root_dir)
        if not cs_files:
            print("No C# files found in the current directory and subdirectories.")
            return True
        
        print(f"Checking {len(cs_files)} C# files...")
        print()
        
        all_issues = []
        
        for file_path in cs_files:
            results = self.check_file(file_path)
            for result in results:
                all_issues.extend(result.issues)
        
        # Separate errors and warnings
        errors = [issue for issue in all_issues if issue.severity == "error"]
        warnings = [issue for issue in all_issues if issue.severity == "warning"]
        
        # Generate report
        parsing_method = "Hybrid (Roslyn + String-based)" if is_roslyn_available() else "String-based only"
        results_file = Reporter.write_results(errors, warnings, len(cs_files), parsing_method)
        
        if results_file:
            print(f"\nResults written to: {results_file}")
        
        # Return True only if no errors (warnings are OK)
        return len(errors) == 0


def main():
    """Main entry point"""
    checker = CodeQualityChecker()
    
    # Run all checks
    success = checker.run_all_checks()
    
    # Set exit code: 0 for success (no errors), 1 for errors found
    sys.exit(0 if success else 1)


if __name__ == "__main__":
    main()