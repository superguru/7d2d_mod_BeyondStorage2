"""
Utility functions for code analysis
"""

import os
from typing import List

def clean_file_path(file_path: str) -> str:
    """Clean file path by removing './' or '.\\' prefixes and converting to relative path"""
    if file_path.startswith('./') or file_path.startswith('.\\'):
        return file_path[2:]
    elif not file_path.startswith('.'):
        # Convert absolute path to relative
        return os.path.relpath(file_path, '.')
    return file_path


def find_cs_files(root_dir: str = ".") -> List[str]:
    """Find all .cs files in the directory and subdirectories"""
    cs_files = []
    for root, dirs, files in os.walk(root_dir):
        dirs[:] = [d for d in dirs if d not in {'.git', '.vs', 'bin', 'obj', 'packages', '.vscode'}]
        
        for file in files:
            if file.endswith('.cs'):
                cs_files.append(os.path.join(root, file))
    
    return sorted(cs_files)


def extract_method_name(line: str) -> str:
    """Extract method name from a method signature line"""
    import re
    match = re.search(r'\s+(\w+)\s*\(', line)
    return match.group(1) if match else "unknown"


def is_guid_context(line: str, number_start: int, number_end: int) -> bool:
    """Check if a number appears to be part of a GUID"""
    import re
    
    # Get context around the number
    context_start = max(0, number_start - 20)
    context_end = min(len(line), number_end + 20)
    context = line[context_start:context_end]
    
    # Check for GUID patterns
    guid_patterns = [
        r'[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}',  # Full GUID
        r'"[0-9a-fA-F-]+"',  # Quoted hex string that might be a GUID
        r'\{[0-9a-fA-F-]+\}',  # GUID in braces
    ]
    
    for pattern in guid_patterns:
        if re.search(pattern, context):
            return True
    
    # Check for common GUID-related keywords
    guid_keywords = ['guid', 'Guid', 'GUID', 'assembly:', '[assembly:', 'typelib']
    line_lower = line.lower()
    for keyword in guid_keywords:
        if keyword.lower() in line_lower:
            return True
    
    return False