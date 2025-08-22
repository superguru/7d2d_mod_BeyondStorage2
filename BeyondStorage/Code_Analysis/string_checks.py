"""
String-based code analysis checks
"""

import re
from typing import List
from models import Issue
from utils import clean_file_path, extract_method_name, is_guid_context


class StringBasedChecker:
    """String-based code quality checker for C# files"""
    
    def check_forbidden_strings(self, file_path: str, content: str, forbidden_strings: dict) -> List[Issue]:
        """Check for strings that should not exist in the code"""
        issues = []
        lines = content.split('\n')

        # Should always be a case-sensitive check
        for forbidden_string, (severity, code, description) in forbidden_strings.items():
            for line_num, line in enumerate(lines, 1):
                if forbidden_string in line:
                    issues.append(Issue(
                        file_path=clean_file_path(file_path),
                        line_number=line_num,
                        severity=severity,
                        code=code,
                        description=description
                    ))
        
        return issues
    
    def check_excessive_nesting(self, file_path: str, content: str) -> List[Issue]:
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
                        file_path=clean_file_path(file_path),
                        line_number=line_num,
                        severity="warning",
                        code="BCW010",
                        description=f"Excessive nesting level ({indent_level} > {max_nesting}) - consider refactoring"
                    ))
        
        return issues
    
    def check_long_methods(self, file_path: str, content: str) -> List[Issue]:
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
                method_name = extract_method_name(stripped)
                brace_count = stripped.count('{') - stripped.count('}')
                continue
            
            if in_method:
                brace_count += stripped.count('{') - stripped.count('}')
                
                if brace_count <= 0:
                    method_length = line_num - method_start_line + 1
                    if method_length > max_method_length:
                        issues.append(Issue(
                            file_path=clean_file_path(file_path),
                            line_number=method_start_line,
                            severity="warning",
                            code="BCW011",
                            description=f"Method '{method_name}' is too long ({method_length} lines > {max_method_length}) - consider breaking it down"
                        ))
                    in_method = False
        
        return issues
    
    def check_magic_numbers(self, file_path: str, content: str) -> List[Issue]:
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
                        # Check if this number is part of a GUID
                        if not is_guid_context(line, match.start(), match.end()):
                            issues.append(Issue(
                                file_path=clean_file_path(file_path),
                                line_number=line_num,
                                severity="warning",
                                code="BCW012",
                                description=f"Magic number '{number}' - consider using a named constant"
                            ))
        
        return issues
    
    def check_todo_comments(self, file_path: str, content: str) -> List[Issue]:
        """Check for TODO comments that should be addressed - WARNING"""
        issues = []
        lines = content.split('\n')
        
        for line_num, line in enumerate(lines, 1):
            stripped = line.strip().lower()
            if 'todo' in stripped and ('//' in line or '/*' in line):
                # Extract the actual comment text
                comment_text = line.strip()
                
                # Remove common comment prefixes
                if '//' in comment_text:
                    comment_text = comment_text.split('//', 1)[1].strip()
                elif '/*' in comment_text:
                    comment_text = comment_text.split('/*', 1)[1].strip()
                    if '*/' in comment_text:
                        comment_text = comment_text.split('*/', 1)[0].strip()
                
                # Check if the comment already starts with "todo" (case-insensitive)
                if comment_text.lower().startswith('todo'):
                    # Remove the existing "todo" prefix and any following colon/whitespace
                    # This handles: "TODO:", "todo:", "Todo ", "TODO ", etc.
                    comment_without_todo = re.sub(r'^todo\s*:?\s*', '', comment_text, flags=re.IGNORECASE)
                    
                    # Always start with "TODO: " in uppercase
                    if len(comment_without_todo) > 97:  # 100 - len("TODO: ") = 97
                        description = f"TODO: {comment_without_todo[:97]}..."
                    else:
                        description = f"TODO: {comment_without_todo}"
                else:
                    # Prepend "TODO: " if it doesn't already start with it
                    if len(comment_text) > 94:  # 100 - len("TODO: ") = 94
                        description = f"TODO: {comment_text[:94]}..."
                    else:
                        description = f"TODO: {comment_text}"
                
                issues.append(Issue(
                    file_path=clean_file_path(file_path),
                    line_number=line_num,
                    severity="warning",
                    code="BCW013",
                    description=description
                ))
        
        return issues
    
    def check_empty_catch_blocks(self, file_path: str, content: str) -> List[Issue]:
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
                            file_path=clean_file_path(file_path),
                            line_number=catch_line,
                            severity="error",
                            code="BCS003",
                            description="Empty catch block found - should handle exceptions properly"
                        ))
                    in_catch = False
        
        return issues
    
    def check_null_checks(self, file_path: str, content: str) -> List[Issue]:
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
                    file_path=clean_file_path(file_path),
                    line_number=line_num,
                    severity="warning",
                    code="BCW022", 
                    description="Consider using null-conditional operator (?.) instead of explicit null check"
                ))
        
        return issues

    def check_disposable_usage(self, file_path: str, content: str) -> List[Issue]:
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
                        file_path=clean_file_path(file_path),
                        line_number=line_num,
                        severity="error",
                        code="BCS010",
                        description=f"IDisposable type '{disposable_type}' should be wrapped in using statement"
                    ))
        
        return issues

    def check_string_interpolation(self, file_path: str, content: str) -> List[Issue]:
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
                    file_path=clean_file_path(file_path),
                    line_number=line_num,
                    severity="warning", 
                    code="BCW021",
                    description="Consider using string interpolation ($\"...\") instead of concatenation"
                ))
        
        return issues

    def check_string_in_loops(self, file_path: str, content: str) -> List[Issue]:
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
            
            # Check for string concatenation in loops - be more specific about string operations
            if in_loop and '+=' in line:
                # Only flag if we can determine it's actually string concatenation
                if ('string' in line.lower() or 
                    re.search(r'"\s*\+|string\s*\+|\w+\s*\+=\s*"', line) or
                    re.search(r'\w+\s*\+=\s*\w+\s*\+\s*"', line)):
                    issues.append(Issue(
                        file_path=clean_file_path(file_path),
                        line_number=line_num,
                        severity="warning",
                        code="BCW025",
                        description="String concatenation in loop - consider using StringBuilder"
                    ))
        
        return issues

    def check_linq_performance(self, file_path: str, content: str) -> List[Issue]:
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
                    file_path=clean_file_path(file_path),
                    line_number=line_num,
                    severity="warning",
                    code="BCW026",
                    description="Use Any() instead of Count() > 0 for better performance"
                ))
                
            # Check for multiple enumerations
            if line.count('.ToList()') > 1:
                issues.append(Issue(
                    file_path=clean_file_path(file_path),
                    line_number=line_num,
                    severity="warning", 
                    code="BCW027",
                    description="Multiple ToList() calls may cause multiple enumerations"
                ))

        return issues

    def check_cyclomatic_complexity(self, file_path: str, content: str) -> List[Issue]:
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
                        file_path=clean_file_path(file_path),
                        line_number=method_start,
                        severity="warning",
                        code="BCW040",
                        description=f"Method '{method_name}' has high cyclomatic complexity ({complexity}) - consider refactoring"
                    ))
                
                in_method = True
                method_start = line_num
                complexity = 1
                method_name = extract_method_name(line)
                
            if in_method:
                for keyword in complexity_keywords:
                    complexity += line.lower().count(keyword)
                
                if '}' in line and line.count('}') >= line.count('{'):
                    if complexity > 10:
                        issues.append(Issue(
                            file_path=clean_file_path(file_path),
                            line_number=method_start,
                            severity="warning",
                            code="BCW040",
                            description=f"Method '{method_name}' has high cyclomatic complexity ({complexity}) - consider refactoring"
                        ))
                    in_method = False
        
        return issues