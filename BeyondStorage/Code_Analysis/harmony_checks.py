"""
Harmony-specific code analysis checks
"""

from typing import List, Optional, Tuple
from models import Issue
from utils import clean_file_path


class HarmonyChecker:
    """Harmony-specific code quality checker"""
    
    def _find_word_in_line(self, line: str, word: str) -> bool:
        """Check if a word exists as a complete word (not part of another word) in a line"""
        line = line.strip()
        words = []
        current_word = []
        
        for char in line:
            if char.isalnum() or char == '_':
                current_word.append(char)
            else:
                if current_word:
                    words.append(''.join(current_word))
                    current_word = []
        
        if current_word:
            words.append(''.join(current_word))
        
        return word in words
    
    def _extract_class_name(self, line: str) -> Optional[str]:
        """Extract class name from a class declaration line"""
        # Find 'class ' keyword
        class_index = line.find('class ')
        if class_index == -1:
            return None
        
        # Start after 'class '
        start = class_index + 6
        class_name = []
        
        # Extract the class name (stop at whitespace, <, :, or {)
        for i in range(start, len(line)):
            char = line[i]
            if char.isalnum() or char == '_':
                class_name.append(char)
            elif char in [' ', '<', ':', '{', '\t']:
                break
        
        return ''.join(class_name) if class_name else None
    
    def _extract_method_name(self, declaration: str) -> Optional[str]:
        """Extract method name from a method declaration"""
        # Look for pattern: something <method_name> (
        paren_index = declaration.find('(')
        if paren_index == -1:
            return None
        
        # Work backwards from the '(' to find the method name
        method_name = []
        i = paren_index - 1
        
        # Skip whitespace before '('
        while i >= 0 and declaration[i] in [' ', '\t']:
            i -= 1
        
        # Collect the method name
        while i >= 0:
            char = declaration[i]
            if char.isalnum() or char == '_':
                method_name.insert(0, char)
                i -= 1
            else:
                break
        
        return ''.join(method_name) if method_name else None
    
    def _is_attribute_line(self, line: str) -> Optional[str]:
        """Check if line is an attribute and return attribute name if found"""
        line = line.strip()
        if not line.startswith('['):
            return None
        
        # Find the closing bracket
        close_bracket = line.find(']')
        if close_bracket == -1:
            return None
        
        # Extract attribute name (between [ and ( or ])
        attr_content = line[1:close_bracket]
        
        # Find attribute name (before parenthesis if it exists)
        paren_index = attr_content.find('(')
        if paren_index != -1:
            attr_name = attr_content[:paren_index].strip()
        else:
            attr_name = attr_content.strip()
        
        return attr_name if attr_name else None
    
    def _remove_generic_params(self, text: str) -> str:
        """Remove generic type parameters from text (e.g., List<T> -> List)"""
        result = []
        depth = 0
        
        for char in text:
            if char == '<':
                depth += 1
            elif char == '>':
                depth -= 1
            elif depth == 0:
                result.append(char)
        
        return ''.join(result)
    
    def _is_preprocessor_directive(self, line: str) -> bool:
        """Check if line is a preprocessor directive like #if, #endif, etc."""
        stripped = line.strip()
        return stripped.startswith('#if') or stripped.startswith('#endif') or stripped.startswith('#else')
    
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
            
            # Look for [HarmonyPatch] attribute
            attr_name = self._is_attribute_line(line)
            if attr_name == 'HarmonyPatch':
                # Found HarmonyPatch attribute, now look for what follows
                declaration_found = False
                
                # Look ahead for the declaration (skip other attributes and empty lines)
                j = i + 1
                while j < len(lines):
                    next_line = lines[j].strip()
                    
                    # Skip empty lines, comments, preprocessor directives, and other attributes
                    if (not next_line or 
                        next_line.startswith('//') or 
                        next_line.startswith('*') or
                        next_line.startswith('[') or
                        self._is_preprocessor_directive(next_line)):
                        j += 1
                        continue
                    
                    # Check if this line contains a class declaration
                    if 'class ' in next_line:
                        declaration_found = True
                        declaration_line_num = j + 1  # Convert to 1-based line number
                        
                        # Remove generic type parameters for analysis
                        class_decl = self._remove_generic_params(next_line)
                        
                        # Check if it's properly declared as internal static
                        has_internal = self._find_word_in_line(class_decl, 'internal')
                        has_static = self._find_word_in_line(class_decl, 'static')
                        
                        if not (has_internal and has_static):
                            # Extract class name for better error message
                            class_name = self._extract_class_name(class_decl) or "unknown"
                            
                            # Determine what's missing
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
                    elif '(' in next_line and any(self._find_word_in_line(next_line, mod) for mod in ['public', 'private', 'protected', 'internal', 'static']):
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
        
        # Only check method-level Harmony attributes (not HarmonyPatch which can be on classes)
        harmony_method_attributes = ['HarmonyPrefix', 'HarmonyPostfix', 'HarmonyTranspiler', 'HarmonyFinalizer']
        
        i = 0
        while i < len(lines):
            line = lines[i].strip()
            
            # Skip empty lines and comments
            if not line or line.startswith('//') or line.startswith('*'):
                i += 1
                continue
            
            # Look for Harmony method attributes
            attr_name = self._is_attribute_line(line)
            
            found_harmony_attr = None
            is_transpiler = False
            
            if attr_name in harmony_method_attributes:
                found_harmony_attr = attr_name
                if 'Transpiler' in attr_name:
                    is_transpiler = True
                
                # Collect all Harmony attributes for this method (including HarmonyPatch if it follows)
                all_attributes = [attr_name]
                k = i + 1
                while k < len(lines):
                    next_attr_line = lines[k].strip()
                    if not next_attr_line or next_attr_line.startswith('//') or next_attr_line.startswith('*') or self._is_preprocessor_directive(next_attr_line):
                        k += 1
                        continue
                    
                    next_attr = self._is_attribute_line(next_attr_line)
                    if next_attr and (next_attr in harmony_method_attributes or next_attr == 'HarmonyPatch' or next_attr == 'HarmonyDebug'):
                        all_attributes.append(next_attr)
                        if 'Transpiler' in next_attr:
                            is_transpiler = True
                        k += 1
                    else:
                        break
                
                # Now look for the method declaration starting from k
                method_found = False
                method_declaration_lines = []
                declaration_start_line = -1
                
                j = k
                while j < len(lines):
                    next_line = lines[j].strip()
                    
                    # Skip empty lines, comments, preprocessor directives, and other attributes
                    if (not next_line or 
                        next_line.startswith('//') or 
                        next_line.startswith('*') or
                        next_line.startswith('[') or
                        self._is_preprocessor_directive(next_line)):
                        j += 1
                        continue
                    
                    # Start collecting method declaration lines
                    if declaration_start_line == -1:
                        declaration_start_line = j
                    
                    method_declaration_lines.append(next_line)
                    
                    # Check if we've reached the end of the method signature
                    if '{' in next_line or '=>' in next_line or next_line.endswith(';'):
                        # Combine all declaration lines into one string for analysis
                        full_declaration = ' '.join(method_declaration_lines)
                        
                        # Check if this contains a method declaration (has parentheses and modifiers)
                        has_parentheses = '(' in full_declaration
                        has_modifier = any(self._find_word_in_line(full_declaration, mod) for mod in ['public', 'private', 'protected', 'internal', 'static'])
                        
                        if has_parentheses and has_modifier:
                            method_found = True
                            method_line_num = declaration_start_line + 1  # Convert to 1-based line number
                            
                            # Extract method name
                            method_name = self._extract_method_name(full_declaration) or "unknown"
                            
                            # Check if method is private
                            has_private = self._find_word_in_line(full_declaration, 'private')
                            
                            if not has_private:
                                issues.append(Issue(
                                    file_path=clean_file_path(file_path),
                                    line_number=method_line_num,
                                    severity="error",
                                    code="BCS052",
                                    description=f"Method '{method_name}' with [{', '.join(all_attributes)}] attribute(s) must be private"
                                ))
                            
                            # If it's a transpiler method, check for method calls
                            elif is_transpiler:
                                transpiler_issues = self._check_transpiler_method_calls_simple(
                                    lines, j, method_name, file_path, content
                                )
                                issues.extend(transpiler_issues)
                            
                            break
                        else:
                            # Not a method declaration, continue looking
                            break
                    
                    j += 1
                
                # If no method declaration found after Harmony attribute
                if not method_found:
                    issues.append(Issue(
                        file_path=clean_file_path(file_path),
                        line_number=i + 1,
                        severity="error", 
                        code="BCS054",
                        description=f"[{', '.join(all_attributes)}] attribute(s) found but no method declaration follows"
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
                # Look for method declarations - simple check for modifiers and parentheses
                if '(' not in line:
                    continue
                
                # Check for visibility modifiers
                visibility = None
                is_static = 'static' in line.split('(')[0]  # Check before the parenthesis
                
                line_before_paren = line.split('(')[0]
                words = line_before_paren.split()
                
                for word in words:
                    if word in ['public', 'private', 'protected', 'internal']:
                        visibility = word
                        break
                
                if visibility:
                    method_name = self._extract_method_name(line)
                    if method_name:
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
                
                # Look for method calls - find text followed by '('
                for j in range(len(line)):
                    if line[j] == '(':
                        # Work backwards to get the method name
                        method_name = []
                        k = j - 1
                        
                        # Skip whitespace
                        while k >= 0 and line[k] in [' ', '\t']:
                            k -= 1
                        
                        # Collect method name
                        while k >= 0 and (line[k].isalnum() or line[k] == '_'):
                            method_name.insert(0, line[k])
                            k -= 1
                        
                        if method_name:
                            called_method = ''.join(method_name)
                            
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