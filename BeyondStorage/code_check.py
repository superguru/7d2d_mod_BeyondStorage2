#!/usr/bin/env python3
"""
Code Quality Checker for C# files
Checks various code quality issues and design principles in .cs files
Separates errors (build-breaking) from warnings (informational)

Returns 0 as an exit code if no errors found, 1 if any errors are present.
Warnings do not affect the exit code, so if there are only warnings, the exit code will still be 0.

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
import re
from typing import List, Tuple, Dict, Callable, Optional
from pathlib import Path
from dataclasses import dataclass
from datetime import datetime


@dataclass
class Issue:
    """Represents a single code quality issue"""
    file_path: str
    line_number: int
    severity: str  # "error" or "warning"
    code: str      # Error/warning code (e.g., "CS001", "CW001")
    description: str


@dataclass
class CheckResult:
    """Represents the result of a single check"""
    check_name: str
    issues: List[Issue]
    
    @property
    def errors(self) -> List[Issue]:
        return [issue for issue in self.issues if issue.severity == "error"]
    
    @property
    def warnings(self) -> List[Issue]:
        return [issue for issue in self.issues if issue.severity == "warning"]


class CodeQualityChecker:
    """Main class for performing code quality checks on C# files"""
    
    def __init__(self):
        self.checks: Dict[str, Callable[[str, str], List[Issue]]] = {}
        self.forbidden_strings: Dict[str, Tuple[str, str, str]] = {}  # pattern -> (severity, code, description)
        self._register_checks()
    
    def _register_checks(self):
        """Register all available checks"""
        # Register string-based checks (errors)
        self.add_forbidden_string_check(
            "throw new NotImplementedException(",
            "error", 
            "BCS002",
            "NotImplementedException found - should be properly implemented"
        )
        
        # Add some warning-level string checks
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
            "System.Threading.Thread.Sleep",
            "error", 
            "BCS030",
            "Thread.Sleep can cause performance issues - use async/await patterns"
        )
        
        self.add_forbidden_string_check(
            ".Result",
            "warning",
            "BCW060", 
            "Synchronous access to async result can cause deadlocks - use await"
        )

        # Register other checks
        self.checks["excessive_nesting"] = self._check_excessive_nesting
        self.checks["long_methods"] = self._check_long_methods
        self.checks["magic_numbers"] = self._check_magic_numbers
        self.checks["todo_comments"] = self._check_todo_comments
        self.checks["empty_catch_blocks"] = self._check_empty_catch_blocks
        self.checks["null_checks"] = self._check_null_checks
        self.checks["disposable_usage"] = self._check_disposable_usage  
        self.checks["string_interpolation"] = self._check_string_interpolation
        self.checks["string_in_loops"] = self._check_string_in_loops
        self.checks["linq_performance"] = self._check_linq_performance
        self.checks["cyclomatic_complexity"] = self._check_cyclomatic_complexity
    
    def add_forbidden_string_check(self, forbidden_string: str, severity: str, code: str, description: str):
        """Add a check for a string that should not exist in the code"""
        self.forbidden_strings[forbidden_string] = (severity, code, description)
    
    def _check_forbidden_strings(self, file_path: str, content: str) -> List[Issue]:
        """Check for strings that should not exist in the code"""
        issues = []
        lines = content.split('\n')
        
        for forbidden_string, (severity, code, description) in self.forbidden_strings.items():
            for line_num, line in enumerate(lines, 1):
                if forbidden_string in line:
                    issues.append(Issue(
                        file_path=file_path,
                        line_number=line_num,
                        severity=severity,
                        code=code,
                        description=description
                    ))
        
        return issues
    
    def _check_excessive_nesting(self, file_path: str, content: str) -> List[Issue]:
        """Check for excessive nesting levels (more than 4 levels) - WARNING"""
        issues = []
        lines = content.split('\n')
        max_nesting = 4
        
        for line_num, line in enumerate(lines, 1):
            stripped = line.strip()
            if stripped and not stripped.startswith('//') and not stripped.startswith('*'):
                indent_level = (len(line) - len(line.lstrip())) // 4
                if indent_level > max_nesting and '{' in line:
                    issues.append(Issue(
                        file_path=file_path,
                        line_number=line_num,
                        severity="warning",
                        code="BCW010",
                        description=f"Excessive nesting level ({indent_level} > {max_nesting}) - consider refactoring"
                    ))
        
        return issues
    
    def _check_long_methods(self, file_path: str, content: str) -> List[Issue]:
        """Check for methods that are too long (more than 80 lines) - WARNING"""
        issues = []
        lines = content.split('\n')
        max_method_length = 80
        
        in_method = False
        method_start_line = 0
        method_name = ""
        brace_count = 0
        
        for line_num, line in enumerate(lines, 1):
            stripped = line.strip()
            
            if not stripped or stripped.startswith('//') or stripped.startswith('*'):
                continue
            
            method_pattern = r'(public|private|protected|internal|static).*\s+\w+\s*\([^)]*\)\s*\{?'
            if re.search(method_pattern, stripped) and not in_method:
                in_method = True
                method_start_line = line_num
                method_name = self._extract_method_name(stripped)
                brace_count = stripped.count('{') - stripped.count('}')
                continue
            
            if in_method:
                brace_count += stripped.count('{') - stripped.count('}')
                
                if brace_count <= 0:
                    method_length = line_num - method_start_line + 1
                    if method_length > max_method_length:
                        issues.append(Issue(
                            file_path=file_path,
                            line_number=method_start_line,
                            severity="warning",
                            code="BCW011",
                            description=f"Method '{method_name}' is too long ({method_length} lines > {max_method_length}) - consider breaking it down"
                        ))
                    in_method = False
        
        return issues
    
    def _extract_method_name(self, line: str) -> str:
        """Extract method name from a method signature line"""
        match = re.search(r'\s+(\w+)\s*\(', line)
        return match.group(1) if match else "unknown"
    
    def _check_magic_numbers(self, file_path: str, content: str) -> List[Issue]:
        """Check for magic numbers (hardcoded numbers except common ones) - WARNING"""
        issues = []
        lines = content.split('\n')
        
        acceptable_numbers = {0, 1, 2, -1, 100, 1000}
        
        for line_num, line in enumerate(lines, 1):
            stripped = line.strip()
            
            if stripped.startswith('//') or stripped.startswith('*'):
                continue
            
            number_pattern = r'\b(\d{3,})\b'
            matches = re.finditer(number_pattern, line)
            
            for match in matches:
                number = int(match.group(1))
                if number not in acceptable_numbers:
                    if 'const' not in line.lower():
                        issues.append(Issue(
                            file_path=file_path,
                            line_number=line_num,
                            severity="warning",
                            code="BCW012",
                            description=f"Magic number '{number}' - consider using a named constant"
                        ))
        
        return issues
    
    def _check_todo_comments(self, file_path: str, content: str) -> List[Issue]:
        """Check for TODO comments that should be addressed - WARNING"""
        issues = []
        lines = content.split('\n')
        
        for line_num, line in enumerate(lines, 1):
            stripped = line.strip().lower()
            if 'todo' in stripped and ('//' in line or '/*' in line):
                issues.append(Issue(
                    file_path=file_path,
                    line_number=line_num,
                    severity="warning",
                    code="BCW013",
                    description="TODO comment found - should be addressed before release"
                ))
        
        return issues
    
    def _check_empty_catch_blocks(self, file_path: str, content: str) -> List[Issue]:
        """Check for empty catch blocks - ERROR"""
        issues = []
        lines = content.split('\n')
        
        in_catch = False
        catch_line = 0
        brace_count = 0
        has_content = False
        
        for line_num, line in enumerate(lines, 1):
            stripped = line.strip()
            
            if not stripped or stripped.startswith('//'):
                continue
            
            if 'catch' in stripped and '{' in stripped:
                in_catch = True
                catch_line = line_num
                brace_count = stripped.count('{') - stripped.count('}')
                has_content = False
                continue
            
            if in_catch:
                brace_count += stripped.count('{') - stripped.count('}')
                
                if stripped and not stripped.startswith('//'):
                    has_content = True
                
                if brace_count <= 0:
                    if not has_content:
                        issues.append(Issue(
                            file_path=file_path,
                            line_number=catch_line,
                            severity="error",
                            code="BCS003",
                            description="Empty catch block found - should handle exceptions properly"
                        ))
                    in_catch = False
        
        return issues
    
    def _check_null_checks(self, file_path: str, content: str) -> List[Issue]:
        """Check for old-style null checks - WARNING"""
        issues = []
        lines = content.split('\n')
        
        for line_num, line in enumerate(lines, 1):
            stripped = line.strip()
            if stripped.startswith('//'):
                continue
                
            # Look for old-style null checks
            if re.search(r'\w+\s*!=\s*null\s*&&\s*\w+\.\w+', line):
                issues.append(Issue(
                    file_path=file_path,
                    line_number=line_num,
                    severity="warning",
                    code="BCW022", 
                    description="Consider using null-conditional operator (?.) instead of explicit null check"
                ))
        
        return issues

    def _check_disposable_usage(self, file_path: str, content: str) -> List[Issue]:
        """Check for IDisposable objects not in using statements - ERROR"""
        issues = []
        lines = content.split('\n')
        
        disposable_types = ['FileStream', 'StreamWriter', 'StreamReader', 'SqlConnection', 'HttpClient']
        
        for line_num, line in enumerate(lines, 1):
            stripped = line.strip()
            if stripped.startswith('//'):
                continue
                
            for disposable_type in disposable_types:
                if f'new {disposable_type}' in line and 'using' not in line:
                    issues.append(Issue(
                        file_path=file_path,
                        line_number=line_num,
                        severity="error",
                        code="BCS010",
                        description=f"IDisposable type '{disposable_type}' should be wrapped in using statement"
                    ))
        
        return issues

    def _check_string_interpolation(self, file_path: str, content: str) -> List[Issue]:
        """Check for string concatenation that should use interpolation - WARNING"""
        issues = []
        lines = content.split('\n')
        
        for line_num, line in enumerate(lines, 1):
            stripped = line.strip()
            if stripped.startswith('//') or '"' not in stripped:
                continue
                
            # Look for string concatenation patterns
            if re.search(r'"\s*\+\s*\w+\s*\+\s*"', line) or re.search(r'"\w*"\s*\+\s*\w+', line):
                issues.append(Issue(
                    file_path=file_path,
                    line_number=line_num,
                    severity="warning", 
                    code="BCW021",
                    description="Consider using string interpolation ($\"...\") instead of concatenation"
                ))
        
        return issues

    def _check_string_in_loops(self, file_path: str, content: str) -> List[Issue]:
        """Check for string concatenation in loops - WARNING"""
        issues = []
        lines = content.split('\n')
        
        in_loop = False
        loop_start = 0
        
        for line_num, line in enumerate(lines, 1):
            stripped = line.strip()
            if stripped.startswith('//'):
                continue
                
            # Detect loop starts
            if re.search(r'\b(for|foreach|while)\s*\(', line):
                in_loop = True
                loop_start = line_num
                continue
                
            if in_loop and '{' in line:
                continue
                
            if in_loop and '}' in line:
                in_loop = False
                continue
                
            if in_loop and ('+=' in line and 'string' in line.lower()) or ('result +' in line):
                issues.append(Issue(
                    file_path=file_path,
                    line_number=line_num,
                    severity="warning",
                    code="BCW025",
                    description="String concatenation in loop - consider using StringBuilder"
                ))
        
        return issues

    def _check_linq_performance(self, file_path: str, content: str) -> List[Issue]:
        """Check for potentially inefficient LINQ usage - WARNING"""
        issues = []
        lines = content.split('\n')
        
        for line_num, line in enumerate(lines, 1):
            stripped = line.strip()
            if stripped.startswith('//'):
                continue
                
            # Check for Count() vs Any()
            if re.search(r'\.Count\(\)\s*>\s*0', line):
                issues.append(Issue(
                    file_path=file_path,
                    line_number=line_num,
                    severity="warning",
                    code="BCW026",
                    description="Use Any() instead of Count() > 0 for better performance"
                ))
                
            # Check for multiple enumerations
            if line.count('.ToList()') > 1:
                issues.append(Issue(
                    file_path=file_path,
                    line_number=line_num,
                    severity="warning", 
                    code="BCW027",
                    description="Multiple ToList() calls may cause multiple enumerations"
                ))

        return issues

    def _check_cyclomatic_complexity(self, file_path: str, content: str) -> List[Issue]:
        """Check for high cyclomatic complexity - WARNING"""
        issues = []
        lines = content.split('\n')
        
        in_method = False
        method_start = 0
        complexity = 1  # Base complexity
        method_name = ""
        
        complexity_keywords = ['if', 'else if', 'while', 'for', 'foreach', 'switch', 'case', 'catch', '&&', '||', '?']
        
        for line_num, line in enumerate(lines, 1):
            stripped = line.strip()
            if stripped.startswith('//'):
                continue
                
            # Method detection logic (simplified)
            if re.search(r'(public|private|protected|internal).*\w+\s*\([^)]*\)', line) and '{' in line:
                if in_method and complexity > 10:
                    issues.append(Issue(
                        file_path=file_path,
                        line_number=method_start,
                        severity="warning",
                        code="BCW040",
                        description=f"Method '{method_name}' has high cyclomatic complexity ({complexity}) - consider refactoring"
                    ))
                
                in_method = True
                method_start = line_num
                complexity = 1
                method_name = self._extract_method_name(line)
                
            if in_method:
                for keyword in complexity_keywords:
                    complexity += line.lower().count(keyword)
                
                if '}' in line and line.count('}') >= line.count('{'):
                    if complexity > 10:
                        issues.append(Issue(
                            file_path=file_path,
                            line_number=method_start,
                            severity="warning",
                            code="BCW040",
                            description=f"Method '{method_name}' has high cyclomatic complexity ({complexity}) - consider refactoring"
                        ))
                    in_method = False
        
        return issues

    def check_file(self, file_path: str) -> List[CheckResult]:
        """Check a single C# file for all registered issues"""
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                content = f.read()
        except (IOError, UnicodeDecodeError) as e:
            return [CheckResult(
                check_name="file_read_error",
                issues=[Issue(
                    file_path=file_path,
                    line_number=1,
                    severity="error",
                    code="BCS999",
                    description=f"Failed to read file: {e}"
                )]
            )]

        results = []
        
        # Run forbidden strings check
        if self.forbidden_strings:
            issues = self._check_forbidden_strings(file_path, content)
            if issues:
                results.append(CheckResult(
                    check_name="forbidden_strings",
                    issues=issues
                ))
        
        # Run other checks
        for check_name, check_func in self.checks.items():
            issues = check_func(file_path, content)
            if issues:
                results.append(CheckResult(
                    check_name=check_name,
                    issues=issues
                ))
        
        return results
    
    def find_cs_files(self, root_dir: str = ".") -> List[str]:
        """Find all .cs files in the directory and subdirectories"""
        cs_files = []
        for root, dirs, files in os.walk(root_dir):
            dirs[:] = [d for d in dirs if d not in {'.git', '.vs', 'bin', 'obj', 'packages', '.vscode'}]
            
            for file in files:
                if file.endswith('.cs'):
                    cs_files.append(os.path.join(root, file))
        
        return sorted(cs_files)
    
    def format_issue_compiler_style(self, issue: Issue) -> str:
        """Format issue in compiler-style format for parsing"""
        # Format: file(line): severity code: description
        return f"{issue.file_path}({issue.line_number}): {issue.severity} {issue.code}: {issue.description}"
    
    def run_all_checks(self, root_dir: str = ".") -> bool:
        """Run all checks on all C# files and return True if no errors found"""
        print("BeyondStorage Code Quality Checker")
        print("=" * 50)
        
        cs_files = self.find_cs_files(root_dir)
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
                line = self.format_issue_compiler_style(warning)
                output_lines.append(line)
            output_lines.append("")
    
        # Show errors last (most critical - will be at bottom of output)
        if errors:
            output_lines.append("ERRORS:")
            output_lines.append("-" * 40)
            for error in errors:
                line = self.format_issue_compiler_style(error)
                output_lines.append(line)
            output_lines.append("")
        
        # Summary
        summary = f"Code check completed: {len(errors)} error(s), {len(warnings)} warning(s) found in {len(cs_files)} files."
        output_lines.append(summary)
        
        # Add build status indicator
        if errors:
            output_lines.append("BUILD STATUS: FAILED (errors found)")
        elif warnings:
            output_lines.append("BUILD STATUS: PASSED (warnings only)")
        else:
            output_lines.append("BUILD STATUS: PASSED (no issues)")
        
        # Write to file
        try:
            with open(results_file, 'w', encoding='utf-8') as f:
                f.write(f"BeyondStorage Code Quality Check Results\n")
                f.write(f"Generated: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}\n")
                f.write("=" * 60 + "\n\n")
                f.write("\n".join(output_lines))
        except IOError as e:
            print(f"Warning: Could not write results file {results_file}: {e}")
        
        # Output to console
        for line in output_lines:
            print(line)
        
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