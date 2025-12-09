#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Format variables in ANALYSIS_RESULTS.txt to list format (one per line) - Smart splitting"""

import re
import sys

input_file = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts\ANALYSIS_RESULTS.txt"
output_file = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts\ANALYSIS_RESULTS.txt"

def smart_split_variables(text):
    """Intelligently split concatenated variable names"""
    if not text or not text.strip():
        return []
    
    variables = []
    i = 0
    current_var = ""
    
    while i < len(text):
        char = text[i]
        peek_ahead = text[i:i+10] if i + 10 < len(text) else text[i:]
        
        # If we see ::, it's part of namespace - definitely part of current variable
        if i + 1 < len(text) and text[i:i+2] == '::':
            current_var += '::'
            i += 2
            continue
        
        # If we see [, it's array syntax - part of current variable
        if char == '[':
            # Find matching ]
            bracket_count = 1
            j = i + 1
            while j < len(text) and bracket_count > 0:
                if text[j] == '[':
                    bracket_count += 1
                elif text[j] == ']':
                    bracket_count -= 1
                j += 1
            if bracket_count == 0:
                # Found complete array syntax
                current_var += text[i:j]
                i = j
                # After ], if next is a letter, it's likely a new variable
                if i < len(text) and text[i].isalpha():
                    if current_var.strip():
                        variables.append(current_var.strip())
                    current_var = ""
                continue
        
        # If we see ], it might be end of array (if we're in array context)
        # or it might be a standalone ] (incomplete array notation)
        if char == ']':
            current_var += char
            i += 1
            # If next char is a letter, likely start of new variable
            if i < len(text) and text[i].isalpha():
                if current_var.strip():
                    variables.append(current_var.strip())
                current_var = ""
            continue
        
        # Check for camelCase boundaries: lowercase/number followed by uppercase
        # But be careful - this could also be continuation of camelCase
        if current_var and current_var[-1].islower() and char.isupper():
            # Check if this looks like a new variable start
            # If current_var ends with certain patterns, it's likely complete
            if (current_var.rstrip().endswith(']') or 
                current_var.rstrip().endswith('2') or
                current_var.rstrip().endswith('Mode') or
                '::' in current_var and not current_var.endswith('::')):
                # Likely end of variable
                if current_var.strip():
                    variables.append(current_var.strip())
                current_var = char
            else:
                # Might be camelCase continuation
                current_var += char
        else:
            current_var += char
        
        i += 1
    
    if current_var.strip():
        variables.append(current_var.strip())
    
    # Clean up: remove duplicates and empty
    seen = set()
    result = []
    for var in variables:
        var_clean = var.strip()
        if var_clean and var_clean not in seen:
            seen.add(var_clean)
            result.append(var_clean)
    
    return result

try:
    with open(input_file, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()
except Exception as e:
    print(f"Error reading file: {e}")
    sys.exit(1)

output = []
in_variables_section = False
variables_text = ""

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
                variables = smart_split_variables(variables_text)
                
                # Output each variable on its own line
                for var in variables:
                    if not var.startswith('$'):
                        output.append(f"  - ${var}\n")
                    else:
                        output.append(f"  - {var}\n")
            
            in_variables_section = False
            variables_text = ""
            output.append(original_line)
            continue
        
        # Accumulate variable text
        if line_stripped.strip():
            variables_text += line_stripped
        else:
            # Empty line - process accumulated text
            if variables_text.strip():
                variables = smart_split_variables(variables_text)
                for var in variables:
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

