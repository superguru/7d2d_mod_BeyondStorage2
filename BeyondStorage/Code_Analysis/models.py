"""
Data models for code analysis results
"""

from dataclasses import dataclass
from typing import List

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