"""
Harmony-specific code analysis checks
"""

import re
from typing import List
from models import Issue
from utils import clean_file_path


class HarmonyChecker:
    """Harmony-specific code quality checker"""
    
    def check_harmony_patch_class_declaration(self, file_path: str, content: str) -> List[Issue]:
        """Check that classes with [HarmonyPatch] attribute are declared as internal static - ERROR"""
        issues = []
        lines = content.split('\n')
        
        i = 0
        while i < len(lines):
            line = lines[i].strip()
            
            # Skip empty lines and comments
            if not line or line.startswith('//') or line.startswith('*'):
                i += 1
                continue
            
            # Look for [HarmonyPatch] attribute (with or without parameters)
            if re.match(r'\s*\[HarmonyPatch(\(.*\))?\]', line):
                # Found HarmonyPatch attribute, now look for what follows
                declaration_line_num = i + 1
                declaration_found = False
                
                # Look ahead for the declaration (skip other attributes and empty lines)
                j = i + 1
                while j < len(lines):
                    next_line = lines[j].strip()
                    
                    # Skip empty lines, comments, and other attributes
                    if (not next_line or 
                        next_line.startswith('//') or 
                        next_line.startswith('*') or
                        next_line.startswith('[') or
                        next_line.startswith('#')):
                        j += 1
                        continue
                    
                    # Check if this line contains a class declaration
                    if 'class ' in next_line:
                        declaration_found = True
                        declaration_line_num = j + 1  # Convert to 1-based line number
                        
                        # Parse the class declaration to check modifiers
                        # Remove generic type parameters for analysis
                        class_decl = re.sub(r'<[^>]*>', '', next_line)
                        
                        # Check if it's properly declared as internal static
                        if not (re.search(r'\binternal\b', class_decl) and re.search(r'\bstatic\b', class_decl)):
                            # Extract class name for better error message
                            class_name_match = re.search(r'class\s+(\w+)', class_decl)
                            class_name = class_name_match.group(1) if class_name_match else "unknown"
                            
                            # Determine what's missing
                            has_internal = re.search(r'\binternal\b', class_decl)
                            has_static = re.search(r'\bstatic\b', class_decl)
                            
                            if not has_internal and not has_static:
                                missing = "internal static"
                            elif not has_internal:
                                missing = "internal"
                            else:  # not has_static
                                missing = "static"
                            
                            issues.append(Issue(
                                file_path=clean_file_path(file_path),
                                line_number=declaration_line_num,
                                severity="error",
                                code="BCS050",
                                description=f"Class '{class_name}' with [HarmonyPatch] attribute must be declared as '{missing}' (currently missing: {missing})"
                            ))
                        break
                    
                    # Check if this line contains a method declaration - if so, skip this HarmonyPatch
                    elif (re.search(r'\b(public|private|protected|internal|static).*\s+\w+\s*\([^)]*\)', next_line) or
                          re.search(r'\w+\s+\w+\s*\([^)]*\)', next_line)):
                        # This HarmonyPatch is on a method, not a class - ignore it
                        declaration_found = True
                        break
                    
                    # If we hit something else that looks like a declaration, break
                    elif any(keyword in next_line for keyword in ['struct ', 'interface ', 'enum ', 'delegate ']):
                        # This could be a struct, interface, enum, or delegate - not what we're looking for
                        declaration_found = True
                        break
                    
                    j += 1
                
                # If no declaration found after HarmonyPatch attribute, that's unusual
                if not declaration_found:
                    issues.append(Issue(
                        file_path=clean_file_path(file_path),
                        line_number=i + 1,
                        severity="error", 
                        code="BCS051",
                        description="[HarmonyPatch] attribute found but no recognizable declaration follows"
                    ))
                
                i = j  # Continue from where we left off
            else:
                i += 1
        
        return issues

    def check_harmony_patch_method_declaration(self, file_path: str, content: str) -> List[Issue]:
        """Check that methods with Harmony attributes are declared as private - ERROR"""
        issues = []
        lines = content.split('\n')
        
        i = 0
        while i < len(lines):
            line = lines[i].strip()
            
            # Skip empty lines and comments
            if not line or line.startswith('//') or line.startswith('*'):
                i += 1
                continue
            
            # Look for Harmony attributes
            harmony_attributes = ['HarmonyPatch', 'HarmonyPrefix', 'HarmonyPostfix', 'HarmonyTranspiler', 'HarmonyFinalizer']
            found_harmony_attr = None
            is_transpiler = False
            
            for attr_name in harmony_attributes:
                if re.match(rf'\s*\[{attr_name}(\(.*\))?\]', line):
                    found_harmony_attr = attr_name
                    if 'Transpiler' in attr_name:
                        is_transpiler = True
                    break
            
            if found_harmony_attr:
                # Found Harmony attribute, now look for the method declaration
                method_line_num = i + 1
                method_found = False
                
                # Look ahead for the method declaration (skip other attributes and empty lines)
                j = i + 1
                while j < len(lines):
                    next_line = lines[j].strip()
                    
                    # Skip empty lines, comments, and other attributes
                    if (not next_line or 
                        next_line.startswith('//') or 
                        next_line.startswith('*') or
                        next_line.startswith('[') or
                        next_line.startswith('#')):
                        j += 1
                        continue
                    
                    # Check if this line contains a method declaration
                    method_pattern = r'(public|private|protected|internal|static).*\s+\w+\s*\([^)]*\)'
                    if re.search(method_pattern, next_line):
                        method_found = True
                        method_line_num = j + 1  # Convert to 1-based line number
                        
                        # Check if method is private
                        if not re.search(r'\bprivate\b', next_line):
                            # Extract method name for better error message
                            method_name_match = re.search(r'\s+(\w+)\s*\(', next_line)
                            method_name = method_name_match.group(1) if method_name_match else "unknown"
                            
                            issues.append(Issue(
                                file_path=clean_file_path(file_path),
                                line_number=method_line_num,
                                severity="error",
                                code="BCS052",
                                description=f"Method '{method_name}' with [{found_harmony_attr}] attribute must be private"
                            ))
                        
                        # If it's a transpiler method, check for method calls (simple string-based check)
                        elif is_transpiler:
                            transpiler_issues = self._check_transpiler_method_calls_simple(
                                lines, j, method_name_match.group(1) if method_name_match else "unknown", file_path, content
                            )
                            issues.extend(transpiler_issues)
                        
                        break
                    
                    j += 1
                
                # If no method declaration found after Harmony attribute
                if not method_found:
                    issues.append(Issue(
                        file_path=clean_file_path(file_path),
                        line_number=i + 1,
                        severity="error", 
                        code="BCS054",
                        description=f"[{found_harmony_attr}] attribute found but no method declaration follows"
                    ))
                
                i = j  # Continue from where we left off
            else:
                i += 1
        
        return issues

    def _check_transpiler_method_calls_simple(self, lines: List[str], method_start: int, transpiler_name: str, file_path: str, content: str) -> List[Issue]:
        """Simple string-based check for transpiler method calls"""
        issues = []
        
        try:
            # Find the end of the method (simple brace counting)
            brace_count = 0
            method_end = len(lines)
            
            for i in range(method_start, len(lines)):
                line = lines[i].strip()
                if not line or line.startswith('//'):
                    continue
                
                brace_count += line.count('{') - line.count('}')
                if brace_count == 0 and i > method_start:
                    method_end = i
                    break
            
            # Get all method names in the file and their visibility
            method_visibility = {}
            all_lines = content.split('\n')
            
            for line_num, line in enumerate(all_lines, 1):
                # Look for method declarations
                method_pattern = r'(public|private|protected|internal)\s+(static\s+)?[\w<>]+\s+(\w+)\s*\('
                match = re.search(method_pattern, line)
                if match:
                    visibility = match.group(1)
                    method_name = match.group(3)
                    is_static = match.group(2) is not None
                    
                    method_visibility[method_name] = {
                        'is_private': visibility == 'private',
                        'is_public': visibility == 'public',
                        'is_static': is_static
                    }
            
            # Check method calls within the transpiler method
            for i in range(method_start, method_end):
                line = lines[i].strip()
                line_num = i + 1
                
                if not line or line.startswith('//'):
                    continue
                
                # Look for method calls (simple pattern)
                call_patterns = [
                    r'(\w+)\s*\(',  # Direct method calls
                    r'\.(\w+)\s*\('  # Method calls on objects
                ]
                
                for pattern in call_patterns:
                    matches = re.finditer(pattern, line)
                    for match in matches:
                        called_method = match.group(1)
                        
                        if called_method in method_visibility:
                            method_info = method_visibility[called_method]
                            
                            # Skip utility methods that are public static (those are OK)
                            if method_info['is_public'] and method_info['is_static']:
                                continue
                            
                            # If it's not private and not a public static utility, flag it
                            if not method_info['is_private']:
                                issues.append(Issue(
                                    file_path=clean_file_path(file_path),
                                    line_number=line_num,
                                    severity="error",
                                    code="BCS053",
                                    description=f"Transpiler method '{transpiler_name}' calls method '{called_method}' which should be private (utility methods can be public static)"
                                ))
        
        except Exception as e:
            # Skip if we can't analyze the method
            pass
        
        return issues