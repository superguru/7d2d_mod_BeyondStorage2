"""
Roslyn-based C# code analyzer for enhanced accuracy
"""

from typing import List, Tuple, Optional
from models import Issue

# Try to import Roslyn for enhanced C# parsing
ROSLYN_AVAILABLE = False
try:
    import clr
    # Add references to .NET Framework assemblies (compatible with .NET Framework 4.8)
    clr.AddReference("System.Core")
    
    # Try to add Roslyn references
    try:
        clr.AddReference("Microsoft.CodeAnalysis")
        clr.AddReference("Microsoft.CodeAnalysis.CSharp")
        
        # Import the main namespaces first
        import System
        from Microsoft.CodeAnalysis import SyntaxTree, SyntaxNode
        from Microsoft.CodeAnalysis.CSharp import SyntaxFactory, CSharpSyntaxTree, SyntaxKind
        import Microsoft.CodeAnalysis.CSharp as CSharp
        
        # Import specific syntax classes - using the correct Python.NET syntax
        from Microsoft.CodeAnalysis.CSharp.Syntax import (
            ClassDeclarationSyntax, 
            MethodDeclarationSyntax,
            CatchClauseSyntax,
            LiteralExpressionSyntax,
            AttributeListSyntax,
            AttributeSyntax,
            CompilationUnitSyntax,
            InvocationExpressionSyntax,
            MemberAccessExpressionSyntax
        )
        
        ROSLYN_AVAILABLE = True
        print("Roslyn C# parsing enabled - enhanced accuracy for selected checks")
    except Exception as e:
        print(f"Roslyn not available, using string-based parsing: {e}")
        ROSLYN_AVAILABLE = False
        
except ImportError:
    print("Python.NET not available - using string-based parsing only")
    ROSLYN_AVAILABLE = False


class RoslynAnalyzer:
    """Roslyn-based C# code analyzer for enhanced accuracy"""
    
    def __init__(self):
        self.acceptable_numbers = {0, 1, 2, -1, 100, 1000, 25, 60, 1024}  # Common acceptable numbers
    
    def parse_file(self, file_path: str) -> Tuple[Optional[SyntaxTree], Optional[CompilationUnitSyntax]]:
        """Parse C# file using Roslyn"""
        if not ROSLYN_AVAILABLE:
            return None, None
            
        try:
            with open(file_path, 'r', encoding='utf-8') as f:
                source_code = f.read()
            
            # Parse the source code into a syntax tree
            syntax_tree = CSharpSyntaxTree.ParseText(source_code)
            root = syntax_tree.GetCompilationUnitRoot()
            
            return syntax_tree, root
        except Exception as e:
            print(f"Failed to parse {file_path} with Roslyn: {e}")
            return None, None
    
    def check_harmony_patch_classes(self, file_path: str, syntax_tree: SyntaxTree, root: CompilationUnitSyntax) -> List[Issue]:
        """Check HarmonyPatch class declarations using Roslyn AST"""
        issues = []
        
        try:
            # Use DescendantNodes with proper type filtering for Python.NET
            all_nodes = list(root.DescendantNodes())
            classes = [node for node in all_nodes if isinstance(node, ClassDeclarationSyntax)]
            
            for class_decl in classes:
                # Check if class has HarmonyPatch attribute
                harmony_attr = None
                for attr_list in class_decl.AttributeLists:
                    for attr in attr_list.Attributes:
                        attr_name = str(attr.Name)
                        if "HarmonyPatch" in attr_name:
                            harmony_attr = attr
                            break
                    if harmony_attr:
                        break
                
                if harmony_attr:
                    # Check modifiers using Roslyn's proper parsing
                    modifiers = [str(mod) for mod in class_decl.Modifiers]
                    has_internal = "internal" in modifiers
                    has_static = "static" in modifiers
                    
                    if not (has_internal and has_static):
                        line_number = syntax_tree.GetLineSpan(class_decl.Span).StartLinePosition.Line + 1
                        
                        missing = []
                        if not has_internal:
                            missing.append("internal")
                        if not has_static:
                            missing.append("static")
                        
                        class_name = str(class_decl.Identifier)
                        issues.append(Issue(
                            file_path=file_path,
                            line_number=line_number,
                            severity="error",
                            code="BCS050",
                            description=f"Class '{class_name}' with [HarmonyPatch] must be 'internal static' (missing: {' '.join(missing)})"
                        ))
        except Exception as e:
            print(f"Error in Roslyn HarmonyPatch check for {file_path}: {e}")
        
        return issues
    
    def check_harmony_patch_methods(self, file_path: str, syntax_tree: SyntaxTree, root: CompilationUnitSyntax) -> List[Issue]:
        """Check HarmonyPatch method declarations using Roslyn AST"""
        issues = []
        
        try:
            all_nodes = list(root.DescendantNodes())
            methods = [node for node in all_nodes if isinstance(node, MethodDeclarationSyntax)]
            
            for method_decl in methods:
                # Check if method has HarmonyPatch or Harmony-related attributes
                harmony_attrs = []
                is_transpiler = False
                
                for attr_list in method_decl.AttributeLists:
                    for attr in attr_list.Attributes:
                        attr_name = str(attr.Name)
                        if any(harmony_name in attr_name for harmony_name in ["HarmonyPatch", "HarmonyPrefix", "HarmonyPostfix", "HarmonyTranspiler", "HarmonyFinalizer"]):
                            harmony_attrs.append(attr_name)
                            if "Transpiler" in attr_name:
                                is_transpiler = True
                
                if harmony_attrs:
                    # Check if method is private
                    modifiers = [str(mod) for mod in method_decl.Modifiers]
                    is_private = "private" in modifiers
                    is_static = "static" in modifiers
                    
                    if not is_private:
                        line_number = syntax_tree.GetLineSpan(method_decl.Span).StartLinePosition.Line + 1
                        method_name = str(method_decl.Identifier)
                        
                        issues.append(Issue(
                            file_path=file_path,
                            line_number=line_number,
                            severity="error",
                            code="BCS052",
                            description=f"Method '{method_name}' with Harmony attributes {harmony_attrs} must be private"
                        ))
                    
                    # If it's a transpiler method, check for method calls to other methods that should be private
                    if is_transpiler and is_private:
                        callee_issues = self._check_transpiler_method_calls(method_decl, syntax_tree, file_path, root)
                        issues.extend(callee_issues)
        
        except Exception as e:
            print(f"Error in Roslyn HarmonyPatch method check for {file_path}: {e}")
        
        return issues
    
    def _check_transpiler_method_calls(self, transpiler_method: MethodDeclarationSyntax, syntax_tree: SyntaxTree, file_path: str, root: CompilationUnitSyntax) -> List[Issue]:
        """Check if transpiler methods call private utility methods"""
        issues = []
        
        try:
            # Get all method calls in the transpiler method
            invocations = [node for node in transpiler_method.DescendantNodes() if isinstance(node, InvocationExpressionSyntax)]
            
            # Get all methods in the same file to check their visibility
            all_methods = [node for node in root.DescendantNodes() if isinstance(node, MethodDeclarationSyntax)]
            method_visibility_map = {}
            
            for method in all_methods:
                method_name = str(method.Identifier)
                modifiers = [str(mod) for mod in method.Modifiers]
                is_private = "private" in modifiers
                is_public = "public" in modifiers
                is_static = "static" in modifiers
                
                method_visibility_map[method_name] = {
                    'is_private': is_private,
                    'is_public': is_public,
                    'is_static': is_static,
                    'method_node': method
                }
            
            # Check each invocation
            for invocation in invocations:
                try:
                    # Try to get the method name being called
                    method_name = None
                    
                    # Handle direct method calls: MethodName()
                    if hasattr(invocation.Expression, 'Identifier'):
                        method_name = str(invocation.Expression.Identifier)
                    
                    # Handle member access calls: Class.MethodName() or instance.MethodName()
                    elif isinstance(invocation.Expression, MemberAccessExpressionSyntax):
                        method_name = str(invocation.Expression.Name)
                    
                    if method_name and method_name in method_visibility_map:
                        method_info = method_visibility_map[method_name]
                        
                        # Skip utility methods that are explicitly public static (those are OK)
                        if method_info['is_public'] and method_info['is_static']:
                            continue
                        
                        # If it's not private and not a public static utility, flag it
                        if not method_info['is_private']:
                            line_number = syntax_tree.GetLineSpan(invocation.Span).StartLinePosition.Line + 1
                            transpiler_name = str(transpiler_method.Identifier)
                            
                            issues.append(Issue(
                                file_path=file_path,
                                line_number=line_number,
                                severity="error",
                                code="BCS053",
                                description=f"Transpiler method '{transpiler_name}' calls method '{method_name}' which should be private (utility methods can be public static)"
                            ))
                
                except Exception:
                    # Skip invocations we can't analyze
                    continue
        
        except Exception as e:
            print(f"Error analyzing transpiler method calls: {e}")
        
        return issues
    
    def check_empty_catch_blocks(self, file_path: str, syntax_tree: SyntaxTree, root: CompilationUnitSyntax) -> List[Issue]:
        """Check for empty catch blocks using Roslyn AST"""
        issues = []
        
        try:
            all_nodes = list(root.DescendantNodes())
            catch_clauses = [node for node in all_nodes if isinstance(node, CatchClauseSyntax)]
            
            for catch_clause in catch_clauses:
                # Check if catch block is truly empty (no statements)
                if catch_clause.Block and catch_clause.Block.Statements.Count == 0:
                    line_number = syntax_tree.GetLineSpan(catch_clause.Span).StartLinePosition.Line + 1
                    
                    issues.append(Issue(
                        file_path=file_path,
                        line_number=line_number,
                        severity="error",
                        code="BCS003",
                        description="Empty catch block - should handle exceptions properly"
                    ))
        except Exception as e:
            print(f"Error in Roslyn empty catch check for {file_path}: {e}")
        
        return issues
    
    def check_magic_numbers(self, file_path: str, syntax_tree: SyntaxTree, root: CompilationUnitSyntax) -> List[Issue]:
        """Check for magic numbers using Roslyn AST with context awareness"""
        issues = []
        
        try:
            # Find all numeric literals
            all_nodes = list(root.DescendantNodes())
            literals = [node for node in all_nodes if isinstance(node, LiteralExpressionSyntax)]
            
            for literal in literals:
                try:
                    # More robust token type checking for Python.NET
                    token = literal.Token
                    
                    # Try different approaches to check if it's a numeric literal
                    is_numeric = False
                    
                    # Method 1: Try RawKind property
                    try:
                        if hasattr(token, 'RawKind'):
                            # NumericLiteralToken has RawKind value of 8534 in Roslyn
                            is_numeric = token.RawKind == 8534
                    except:
                        pass
                    
                    # Method 2: Try Kind property (not method)
                    if not is_numeric:
                        try:
                            if hasattr(token, 'Kind'):
                                is_numeric = token.Kind == SyntaxKind.NumericLiteralToken
                        except:
                            pass
                    
                    # Method 3: Try string-based check on token text
                    if not is_numeric:
                        try:
                            token_text = str(token.ValueText) if hasattr(token, 'ValueText') else str(token.Text)
                            if token_text and token_text.replace('.', '').replace('-', '').isdigit():
                                is_numeric = True
                        except:
                            pass
                    
                    # Method 4: Check if we can parse the value as a number
                    if not is_numeric:
                        try:
                            token_text = str(token.ValueText) if hasattr(token, 'ValueText') else str(token.Text)
                            float(token_text)  # Try to parse as number
                            is_numeric = True
                        except (ValueError, AttributeError):
                            pass
                    
                    if is_numeric:
                        # Try to parse as integer
                        value_text = str(token.ValueText) if hasattr(token, 'ValueText') else str(token.Text)
                        value = int(value_text)
                        
                        if abs(value) >= 100 and value not in self.acceptable_numbers:
                            # Check if it's in a const declaration using AST traversal
                            is_const = self._is_in_const_declaration(literal)
                            
                            # Check if it's part of a GUID or similar pattern
                            is_guid_related = self._is_guid_related_context(literal, syntax_tree)
                            
                            if not is_const and not is_guid_related:
                                line_number = syntax_tree.GetLineSpan(literal.Span).StartLinePosition.Line + 1
                                
                                issues.append(Issue(
                                    file_path=file_path,
                                    line_number=line_number,
                                    severity="warning",
                                    code="BCW012",
                                    description=f"Magic number '{value}' - consider using a named constant"
                                ))
                                
                except (ValueError, OverflowError, AttributeError):
                    # Skip literals we can't process
                    continue
                    
        except Exception as e:
            print(f"Error in Roslyn magic numbers check for {file_path}: {e}")
        
        return issues
    
    def _is_in_const_declaration(self, literal: SyntaxNode) -> bool:
        """Check if literal is part of a const declaration"""
        try:
            parent = literal.Parent
            while parent:
                if hasattr(parent, 'Modifiers'):
                    modifiers = [str(mod) for mod in parent.Modifiers]
                    if "const" in modifiers:
                        return True
                parent = getattr(parent, 'Parent', None)
            return False
        except:
            return False
    
    def _is_guid_related_context(self, literal: SyntaxNode, syntax_tree: SyntaxTree) -> bool:
        """Check if literal appears in GUID-related context"""
        try:
            # Get the text around the literal
            span = literal.Span
            line_span = syntax_tree.GetLineSpan(span)
            
            # Get the full line text
            source_text = syntax_tree.GetText()
            line_text = str(source_text.Lines[line_span.StartLinePosition.Line])
            
            # Check for GUID patterns
            line_lower = line_text.lower()
            guid_keywords = ['guid', 'assembly:', '[assembly:', 'typelib', 'version=']
            
            return any(keyword in line_lower for keyword in guid_keywords)
        except:
            return False
    
    def check_cyclomatic_complexity(self, file_path: str, syntax_tree: SyntaxTree, root: CompilationUnitSyntax) -> List[Issue]:
        """Check cyclomatic complexity using Roslyn AST"""
        issues = []
        
        try:
            all_nodes = list(root.DescendantNodes())
            methods = [node for node in all_nodes if isinstance(node, MethodDeclarationSyntax)]
            
            for method in methods:
                complexity = self._calculate_complexity_roslyn(method)
                
                if complexity > 10:
                    line_number = syntax_tree.GetLineSpan(method.Span).StartLinePosition.Line + 1
                    method_name = str(method.Identifier)
                    
                    issues.append(Issue(
                        file_path=file_path,
                        line_number=line_number,
                        severity="warning",
                        code="BCW040",
                        description=f"Method '{method_name}' has high complexity ({complexity}) - consider refactoring"
                    ))
        except Exception as e:
            print(f"Error in Roslyn complexity check for {file_path}: {e}")
        
        return issues
    
    def _calculate_complexity_roslyn(self, method: MethodDeclarationSyntax) -> int:
        """Calculate cyclomatic complexity from Roslyn AST"""
        try:
            complexity = 1  # Base complexity
            
            # Count decision points using specific syntax kinds
            complexity_kinds = [
                SyntaxKind.IfStatement,
                SyntaxKind.WhileStatement,
                SyntaxKind.ForStatement,
                SyntaxKind.ForEachStatement,
                SyntaxKind.DoStatement,
                SyntaxKind.SwitchStatement,
                SyntaxKind.CaseSwitchLabel,
                SyntaxKind.DefaultSwitchLabel,
                SyntaxKind.CatchClause,
                SyntaxKind.ConditionalExpression,  # ? :
            ]
            
            for node in method.DescendantNodes():
                node_kind = node.Kind()
                if any(node_kind == kind for kind in complexity_kinds):
                    complexity += 1
                
                # Count logical operators
                if node_kind == SyntaxKind.LogicalAndExpression or node_kind == SyntaxKind.LogicalOrExpression:
                    complexity += 1
            
            return complexity
        except:
            return 1  # Fallback to base complexity


# Module-level convenience function
def is_roslyn_available() -> bool:
    """Check if Roslyn is available for enhanced parsing"""
    return ROSLYN_AVAILABLE