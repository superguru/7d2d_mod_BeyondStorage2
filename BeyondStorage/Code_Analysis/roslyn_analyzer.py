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
            MemberAccessExpressionSyntax,
            ThrowStatementSyntax,
            # For nesting analysis
            IfStatementSyntax,
            ElseClauseSyntax,
            ForStatementSyntax,
            ForEachStatementSyntax,
            WhileStatementSyntax,
            DoStatementSyntax,
            SwitchStatementSyntax,
            TryStatementSyntax,
            UsingStatementSyntax,
            LockStatementSyntax,
            FixedStatementSyntax,
            BlockSyntax
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
        # Common acceptable integers
        self.acceptable_numbers = {0, 1, 2, -1, 100, 1000, 25, 60, 1024}
        # Allow-list and thresholds for non-integer numeric literals
        self.acceptable_float_values = {0.5, 0.25, 0.75, 1.5, 2.5}
        self.float_min_abs_threshold = 1.0   # Flag floats/decimals with |value| >= this
        self.decimal_min_abs_threshold = 1.0
    
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

    # Harmony Patch class checks
    def check_harmony_patch_classes(self, file_path: str, syntax_tree: SyntaxTree, root: CompilationUnitSyntax) -> List[Issue]:
        """Check HarmonyPatch class declarations using Roslyn AST"""
        issues: List[Issue] = []
        try:
            all_nodes = list(root.DescendantNodes())
            classes = [node for node in all_nodes if isinstance(node, ClassDeclarationSyntax)]
            
            for class_decl in classes:
                # Check if class has HarmonyPatch attribute
                harmony_attr = None
                for attr_list in class_decl.AttributeLists:
                    for attr in attr_list.Attributes:
                        attr_name = str(attr.Name) if hasattr(attr, "Name") else str(attr)
                        if "harmonypatch" in attr_name.lower():
                            harmony_attr = attr
                            break
                    if harmony_attr:
                        break
                
                if harmony_attr:
                    # Check modifiers using Roslyn's proper parsing
                    modifiers = [getattr(mod, "ValueText", str(mod)).lower() for mod in class_decl.Modifiers]
                    has_internal = "internal" in modifiers
                    has_static = "static" in modifiers

                    # Report on the class declaration line (identifier), not the attribute line
                    class_line = syntax_tree.GetLineSpan(class_decl.Identifier.Span).StartLinePosition.Line + 1
                    
                    if not (has_internal and has_static):
                        missing = []
                        if not has_internal:
                            missing.append("internal")
                        if not has_static:
                            missing.append("static")
                        
                        class_name = str(class_decl.Identifier)
                        issues.append(Issue(
                            file_path=file_path,
                            line_number=class_line,
                            severity="error",
                            code="BCS050",
                            description=f"Class '{class_name}' with [HarmonyPatch] must be 'internal static' (missing: {' '.join(missing)})"
                        ))
        except Exception as e:
            print(f"Error in Roslyn HarmonyPatch check for {file_path}: {e}")
        
        return issues

    # Harmony Patch method checks
    def check_harmony_patch_methods(self, file_path: str, syntax_tree: SyntaxTree, root: CompilationUnitSyntax) -> List[Issue]:
        """Check HarmonyPatch method declarations using Roslyn AST"""
        issues: List[Issue] = []
        try:
            all_nodes = list(root.DescendantNodes())
            methods = [node for node in all_nodes if isinstance(node, MethodDeclarationSyntax)]
            
            for method_decl in methods:
                # Check if method has HarmonyPatch or Harmony-related attributes
                harmony_attrs = []
                is_transpiler = False
                
                for attr_list in method_decl.AttributeLists:
                    for attr in attr_list.Attributes:
                        attr_name = str(attr.Name) if hasattr(attr, "Name") else str(attr)
                        attr_name_lower = attr_name.lower()
                        if any(h in attr_name_lower for h in ["harmonypatch", "harmonyprefix", "harmonypostfix", "harmonytranspiler", "harmonyfinalizer"]):
                            harmony_attrs.append(attr_name)
                            if "transpiler" in attr_name_lower:
                                is_transpiler = True
                
                if harmony_attrs:
                    # Check if method is private
                    modifiers = [getattr(mod, "ValueText", str(mod)).lower() for mod in method_decl.Modifiers]
                    is_private = "private" in modifiers

                    # Use the method identifier token for line number (avoids reporting attribute line)
                    id_span = method_decl.Identifier.Span
                    method_line = syntax_tree.GetLineSpan(id_span).StartLinePosition.Line + 1
                    method_name = str(method_decl.Identifier)
                    
                    if not is_private:
                        issues.append(Issue(
                            file_path=file_path,
                            line_number=method_line,
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
        issues: List[Issue] = []
        try:
            # Get all method calls in the transpiler method
            invocations = [node for node in transpiler_method.DescendantNodes() if isinstance(node, InvocationExpressionSyntax)]
            
            # Get all methods in the same file to check their visibility
            all_methods = [node for node in root.DescendantNodes() if isinstance(node, MethodDeclarationSyntax)]
            method_visibility_map = {}
            
            for method in all_methods:
                method_name = str(method.Identifier)
                modifiers = [getattr(mod, "ValueText", str(mod)).lower() for mod in method.Modifiers]
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

    # Empty catch blocks
    def check_empty_catch_blocks(self, file_path: str, syntax_tree: SyntaxTree, root: CompilationUnitSyntax) -> List[Issue]:
        """Check for empty catch blocks using Roslyn AST"""
        issues: List[Issue] = []
        try:
            all_nodes = list(root.DescendantNodes())
            catch_clauses = [node for node in all_nodes if isinstance(node, CatchClauseSyntax)]
            
            for catch_clause in catch_clauses:
                if catch_clause.Block:
                    stmts = catch_clause.Block.Statements
                    # Truly empty
                    if stmts.Count == 0:
                        line_number = syntax_tree.GetLineSpan(catch_clause.Span).StartLinePosition.Line + 1
                        issues.append(Issue(
                            file_path=file_path,
                            line_number=line_number,
                            severity="error",
                            code="BCS003",
                            description="Empty catch block - should handle exceptions properly"
                        ))
                    # Allow rethrow-only
                    elif stmts.Count == 1 and isinstance(stmts[0], ThrowStatementSyntax):
                        continue
        except Exception as e:
            print(f"Error in Roslyn empty catch check for {file_path}: {e}")
        
        return issues

    # Magic numbers (int + non-int)
    def check_magic_numbers(self, file_path: str, syntax_tree: SyntaxTree, root: CompilationUnitSyntax) -> List[Issue]:
        """Check for magic numbers using Roslyn AST with context awareness"""
        issues: List[Issue] = []
        try:
            # Find all numeric literals
            all_nodes = list(root.DescendantNodes())
            literals = [node for node in all_nodes if isinstance(node, LiteralExpressionSyntax)]
            
            for literal in literals:
                try:
                    token = literal.Token

                    # Prefer Roslyn's typed value when available
                    value_obj = getattr(token, "Value", None)
                    is_numeric = isinstance(value_obj, (int, float))
                    # Also consider System.Decimal as numeric
                    try:
                        if value_obj is not None and value_obj.GetType().FullName == "System.Decimal":
                            is_numeric = True
                    except Exception:
                        pass

                    if not is_numeric:
                        # Fallback: try to parse ValueText/Text
                        token_text = str(getattr(token, "ValueText", getattr(token, "Text", "")))
                        sanitized = token_text.replace("_", "").strip()
                        lowered = sanitized.lower()

                        # Strip common numeric suffixes
                        for suffix in ("ul", "lu", "u", "l", "f", "d", "m"):
                            if lowered.endswith(suffix):
                                lowered = lowered[: -len(suffix)]
                                break

                        # Try base prefixes
                        try:
                            if lowered.startswith(("0x", "0b", "0o")):
                                value_obj = int(lowered, 0)
                            else:
                                try:
                                    value_obj = int(lowered)
                                except ValueError:
                                    value_obj = float(lowered)
                            is_numeric = True
                        except Exception:
                            is_numeric = False

                    if is_numeric:
                        # Determine integer-like vs non-integer numeric
                        int_value: Optional[int] = None
                        float_value: Optional[float] = None

                        if isinstance(value_obj, int):
                            int_value = value_obj
                        else:
                            try:
                                f = float(value_obj)
                                if f.is_integer():
                                    int_value = int(f)
                                else:
                                    float_value = f
                            except Exception:
                                # If conversion to float fails, try via string fallback
                                try:
                                    f = float(str(value_obj))
                                    if f.is_integer():
                                        int_value = int(f)
                                    else:
                                        float_value = f
                                except Exception:
                                    pass

                        # Integer-like magic numbers
                        if int_value is not None and abs(int_value) >= 100 and int_value not in self.acceptable_numbers:
                            is_const = self._is_in_const_declaration(literal)
                            is_guid_related = self._is_guid_related_context(literal, syntax_tree)
                            if not is_const and not is_guid_related:
                                line_number = syntax_tree.GetLineSpan(literal.Span).StartLinePosition.Line + 1
                                issues.append(Issue(
                                    file_path=file_path,
                                    line_number=line_number,
                                    severity="warning",
                                    code="BCW012",
                                    description=f"Magic number '{int_value}' - consider using a named constant"
                                ))

                        # Non-integer numeric magic numbers (float/double/decimal)
                        elif float_value is not None:
                            min_abs = self.float_min_abs_threshold
                            # Approximate membership check to avoid FP errors for literals
                            def approx_in(values: set, x: float, eps: float = 1e-9) -> bool:
                                for v in values:
                                    try:
                                        if abs(x - float(v)) <= eps:
                                            return True
                                    except Exception:
                                        continue
                                return False

                            if abs(float_value) >= min_abs and not approx_in(self.acceptable_float_values, float_value):
                                is_const = self._is_in_const_declaration(literal)
                                is_guid_related = self._is_guid_related_context(literal, syntax_tree)
                                if not is_const and not is_guid_related:
                                    line_number = syntax_tree.GetLineSpan(literal.Span).StartLinePosition.Line + 1
                                    issues.append(Issue(
                                        file_path=file_path,
                                        line_number=line_number,
                                        severity="warning",
                                        code="BCW012",
                                        description=f"Magic non-integer number '{float_value}' - consider using a named constant"
                                    ))
                                
                except Exception:
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
                    modifiers = [getattr(mod, "ValueText", str(mod)).lower() for mod in parent.Modifiers]
                    if "const" in modifiers:
                        return True
                parent = getattr(parent, 'Parent', None)
            return False
        except Exception:
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
        except Exception:
            return False

    # Cyclomatic complexity
    def check_cyclomatic_complexity(self, file_path: str, syntax_tree: SyntaxTree, root: CompilationUnitSyntax) -> List[Issue]:
        """Check cyclomatic complexity using Roslyn AST"""
        issues: List[Issue] = []
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
            # Avoid double counting switch + cases: count only cases/default
            complexity_kinds = [
                SyntaxKind.IfStatement,
                SyntaxKind.WhileStatement,
                SyntaxKind.ForStatement,
                SyntaxKind.ForEachStatement,
                SyntaxKind.DoStatement,
                SyntaxKind.CatchClause,
                SyntaxKind.ConditionalExpression,  # ? :
                SyntaxKind.CaseSwitchLabel,
                SyntaxKind.DefaultSwitchLabel,
            ]
            
            for node in method.DescendantNodes():
                node_kind = node.Kind()
                if any(node_kind == kind for kind in complexity_kinds):
                    complexity += 1
                
                # Count logical operators
                if node_kind == SyntaxKind.LogicalAndExpression or node_kind == SyntaxKind.LogicalOrExpression:
                    complexity += 1
            
            return complexity
        except Exception:
            return 1  # Fallback to base complexity

    # Excessive nesting (Roslyn)
    def check_excessive_nesting(self, file_path: str, syntax_tree: SyntaxTree, root: CompilationUnitSyntax, max_nesting: int = 5) -> List[Issue]:
        """
        Check for excessive nesting depth per method using Roslyn AST.
        Counts nesting introduced by: if/else, for/foreach/while/do, switch, try/catch/finally, using, lock, fixed.
        Threshold is max_nesting (default 5).
        """
        issues: List[Issue] = []
        try:
            all_nodes = list(root.DescendantNodes())
            methods = [node for node in all_nodes if isinstance(node, MethodDeclarationSyntax)]

            for method in methods:
                max_depth = 0

                # Handle block-bodied methods
                if getattr(method, "Body", None):
                    block: BlockSyntax = method.Body
                    for stmt in block.Statements:
                        max_depth = max(max_depth, self._max_nesting_in_statement(stmt, 0))

                # Expression-bodied methods have no nesting depth
                if max_depth > max_nesting:
                    method_name = str(method.Identifier)
                    line_number = syntax_tree.GetLineSpan(method.Span).StartLinePosition.Line + 1
                    issues.append(Issue(
                        file_path=file_path,
                        line_number=line_number,
                        severity="warning",
                        code="BCW010",
                        description=f"Excessive nesting level ({max_depth} > {max_nesting}) in method '{method_name}' - consider refactoring"
                    ))
        except Exception as e:
            print(f"Error in Roslyn excessive nesting check for {file_path}: {e}")

        return issues

    def _max_nesting_in_block(self, block: BlockSyntax, depth: int) -> int:
        """Compute maximum nesting depth inside a block without increasing depth for the block itself."""
        if block is None:
            return depth
        max_d = depth
        try:
            for stmt in block.Statements:
                max_d = max(max_d, self._max_nesting_in_statement(stmt, depth))
        except Exception:
            pass
        return max_d

    def _max_nesting_in_statement(self, stmt: SyntaxNode, depth: int) -> int:
        """
        Recursively compute nesting depth introduced by control statements.
        We increment depth when entering a control construct; we do not increment again for the child block itself to avoid double counting.
        """
        if stmt is None:
            return depth

        try:
            # if / else
            if isinstance(stmt, IfStatementSyntax):
                d = depth + 1
                max_d = self._max_nesting_in_statement(stmt.Statement, d)
                # Else can be another if (else-if) or a block/statement
                if getattr(stmt, "Else", None):
                    else_stmt = stmt.Else.Statement
                    max_d = max(max_d, self._max_nesting_in_statement(else_stmt, d))
                return max_d

            # loops
            if isinstance(stmt, (ForStatementSyntax, ForEachStatementSyntax, WhileStatementSyntax, DoStatementSyntax)):
                d = depth + 1
                inner = getattr(stmt, "Statement", None)
                return self._max_nesting_in_statement(inner, d)

            # switch
            if isinstance(stmt, SwitchStatementSyntax):
                d = depth + 1
                max_d = d
                for section in stmt.Sections:
                    for s in section.Statements:
                        max_d = max(max_d, self._max_nesting_in_statement(s, d))
                return max_d

            # try/catch/finally
            if isinstance(stmt, TryStatementSyntax):
                d = depth + 1
                max_d = self._max_nesting_in_block(stmt.Block, d)
                for c in stmt.Catches:
                    max_d = max(max_d, self._max_nesting_in_block(c.Block, d))
                if getattr(stmt, "Finally", None):
                    max_d = max(max_d, self._max_nesting_in_block(stmt.Finally.Block, d))
                return max_d

            # using / lock / fixed
            if isinstance(stmt, (UsingStatementSyntax, LockStatementSyntax, FixedStatementSyntax)):
                d = depth + 1
                inner = getattr(stmt, "Statement", None)
                # Using/Lock/Fixed can have either a Block or a single Statement
                if isinstance(inner, BlockSyntax):
                    return self._max_nesting_in_block(inner, d)
                return self._max_nesting_in_statement(inner, d)

            # plain block (does not on its own increase nesting; enclosing control already did)
            if isinstance(stmt, BlockSyntax):
                return self._max_nesting_in_block(stmt, depth)

            # Other statements: compute over child nodes that are statements/blocks
            max_d = depth
            for child in stmt.ChildNodes():
                if isinstance(child, (BlockSyntax, IfStatementSyntax, ForStatementSyntax, ForEachStatementSyntax, WhileStatementSyntax,
                                      DoStatementSyntax, SwitchStatementSyntax, TryStatementSyntax, UsingStatementSyntax,
                                      LockStatementSyntax, FixedStatementSyntax)):
                    max_d = max(max_d, self._max_nesting_in_statement(child, depth))
            return max_d

        except Exception:
            # If anything goes wrong, do not crash analysis; return current depth
            return depth


# Module-level convenience function
def is_roslyn_available() -> bool:
    """Check if Roslyn is available for enhanced parsing"""
    return ROSLYN_AVAILABLE