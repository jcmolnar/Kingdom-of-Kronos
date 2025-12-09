#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Format variables in ANALYSIS_RESULTS.txt to list format (one per line)"""

import re
import sys

input_file = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts\ANALYSIS_RESULTS.txt"
output_file = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts\ANALYSIS_RESULTS.txt"

try:
    with open(input_file, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()
except Exception as e:
    print(f"Error reading file: {e}")
    sys.exit(1)

output = []
in_variables_section = False
variables_text = ""

def split_variables(text):
    """Split concatenated variable names into a list"""
    if not text or not text.strip():
        return []
    
    variables = []
    current = ""
    i = 0
    
    while i < len(text):
        char = text[i]
        
        # If we hit a ], it's likely the end of an array variable
        if char == ']':
            current += char
            # Check if next char is a letter (start of new variable)
            if i + 1 < len(text) and text[i + 1].isalpha():
                if current.strip():
                    variables.append(current.strip())
                current = ""
        # If we hit ::, it's part of the current variable (namespace)
        elif i + 1 < len(text) and text[i:i+2] == '::':
            current += '::'
            i += 1  # Skip next char since we consumed it
        # If we hit [, it's part of array syntax
        elif char == '[':
            current += char
        # If lowercase/number followed by uppercase, might be camelCase boundary
        elif current and current[-1].islower() and char.isupper():
            # This could be end of one var and start of another
            # But also could be camelCase continuation
            # Check if we have a pattern that suggests end of variable
            # (like ending with ] or ::)
            if current.rstrip().endswith(']') or '::' in current:
                if current.strip():
                    variables.append(current.strip())
                current = char
            else:
                current += char
        else:
            current += char
        
        i += 1
    
    if current.strip():
        variables.append(current.strip())
    
    return variables

for i, line in enumerate(lines):
    original_line = line
    line_stripped = line.rstrip('\n\r')
    
    # Check if entering VARIABLES section
    if line_stripped == "VARIABLES (first 50):":
        in_variables_section = True
        variables_text = ""
        output.append(original_line)
        continue
    
    # Check if leaving VARIABLES section
    if in_variables_section:
        if line_stripped.startswith("========================================") or line_stripped.startswith("CROSS-REFERENCE"):
            # Process the accumulated variables text
            if variables_text.strip():
                # Try to split variables intelligently
                # Use regex to find variable patterns
                # Variables can be: name, name::name, name[number], name::name[number]
                
                # Pattern: match variable names
                # $?optional (optional $ prefix)
                # [a-zA-Z0-9_]+ (name part)
                # (::[a-zA-Z0-9_]+)* (optional namespace parts)
                # (\[[^\]]+\])? (optional array index)
                
                # Actually, let's use a simpler approach - find all potential variable boundaries
                # Split on ] followed by letter (end of array var)
                # Split on lowercase+number followed by uppercase (camelCase boundary, but careful)
                
                # More reliable: use regex to find complete variable patterns
                var_pattern = r'\$?[a-zA-Z0-9_]+(?:::[a-zA-Z0-9_]+)*(?:\[[^\]]+\])?'
                matches = re.findall(var_pattern, variables_text)
                
                # Also look for variables that might be missing the [ part
                # Like "SkillSlashing]" - these might be incomplete
                # Let's also match patterns ending with ]
                var_pattern2 = r'[a-zA-Z0-9_]+(?:::[a-zA-Z0-9_]+)*\]'
                matches2 = re.findall(var_pattern2, variables_text)
                
                # Combine and deduplicate
                all_vars = list(set(matches + matches2))
                
                # Sort to maintain some order
                all_vars.sort()
                
                # Output each variable on its own line
                for var in all_vars:
                    if not var.startswith('$'):
                        output.append(f"  - ${var}\n")
                    else:
                        output.append(f"  - {var}\n")
            
            in_variables_section = False
            variables_text = ""
            output.append(original_line)
            continue
        
        # Accumulate variable text (might span multiple lines)
        if line_stripped.strip():
            variables_text += line_stripped
        else:
            # Empty line - if we have accumulated text, process it
            if variables_text.strip():
                var_pattern = r'\$?[a-zA-Z0-9_]+(?:::[a-zA-Z0-9_]+)*(?:\[[^\]]+\])?|[a-zA-Z0-9_]+(?:::[a-zA-Z0-9_]+)*\]'
                matches = re.findall(var_pattern, variables_text)
                all_vars = list(set(matches))
                all_vars.sort()
                
                for var in all_vars:
                    if not var.startswith('$'):
                        output.append(f"  - ${var}\n")
                    else:
                        output.append(f"  - {var}\n")
                
                variables_text = ""
            output.append(original_line)
        continue
    
    # Not in variables section
    output.append(original_line)

# Write output
with open(output_file, 'w', encoding='utf-8') as f:
    f.writelines(output)

print(f"Formatted variables to list format in ANALYSIS_RESULTS.txt")
