#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Format variables in ANALYSIS_RESULTS.txt to list format - Final version with better splitting"""

import re
import sys

input_file = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts\ANALYSIS_RESULTS.txt"
output_file = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts\ANALYSIS_RESULTS.txt"

def split_variables_aggressive(text):
    """Aggressively split concatenated variable names using multiple strategies"""
    if not text or not text.strip():
        return []
    
    # Strategy 1: Split on ] followed by letter (end of array, start of new var)
    parts = re.split(r'(\])(?=[A-Za-z])', text)
    
    # Strategy 2: For each part, split on camelCase boundaries
    # lowercase/number followed by uppercase (but not if it's part of :: or [])
    all_parts = []
    for part in parts:
        if not part.strip():
            continue
        # Split on lowercase/number + uppercase, but preserve :: and []
        sub_parts = re.split(r'([a-z0-9\]\)])([A-Z])', part)
        if len(sub_parts) > 1:
            current = sub_parts[0]
            for j in range(1, len(sub_parts), 2):
                if j+1 < len(sub_parts):
                    # Check if this split makes sense
                    if current.strip() and not current.endswith('::'):
                        all_parts.append(current + sub_parts[j])
                    else:
                        all_parts.append(current)
                    current = sub_parts[j+1]
                else:
                    current += sub_parts[j]
            if current.strip():
                all_parts.append(current)
        else:
            all_parts.append(part)
    
    # Strategy 3: Further split on common patterns
    # - After numbers at end (like Mode2, var2, etc.)
    # - After common suffixes
    result = []
    for part in all_parts:
        if not part.strip():
            continue
        
        # Split on number followed by letter (like "Mode2dbecho" -> "Mode2" and "dbecho")
        sub_parts = re.split(r'([0-9]+)([A-Za-z])', part)
        if len(sub_parts) > 1:
            current = sub_parts[0]
            for j in range(1, len(sub_parts), 2):
                if j+1 < len(sub_parts):
                    if current.strip():
                        result.append(current + sub_parts[j])
                    current = sub_parts[j+1]
                else:
                    current += sub_parts[j]
            if current.strip():
                result.append(current)
        else:
            result.append(part)
    
    # Clean up: remove empty, deduplicate, and format
    seen = set()
    final = []
    for var in result:
        var_clean = var.strip()
        if var_clean and var_clean not in seen:
            seen.add(var_clean)
            final.append(var_clean)
    
    return final

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
            # Process accumulated variables
            if variables_text.strip():
                # Use regex to find all variable patterns
                # Pattern: word, word::word, word[number], word::word[number], word]
                var_pattern = r'[a-zA-Z0-9_]+(?:::[a-zA-Z0-9_]+)*(?:\[[0-9]+\])?|[a-zA-Z0-9_]+(?:::[a-zA-Z0-9_]+)*\]'
                matches = re.findall(var_pattern, variables_text)
                
                # Also try the aggressive splitting
                aggressive_matches = split_variables_aggressive(variables_text)
                
                # Combine and deduplicate
                all_vars = list(set(matches + aggressive_matches))
                all_vars.sort()
                
                for var in all_vars:
                    if var.strip():
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
            # Empty line - process if we have text
            if variables_text.strip():
                var_pattern = r'[a-zA-Z0-9_]+(?:::[a-zA-Z0-9_]+)*(?:\[[0-9]+\])?|[a-zA-Z0-9_]+(?:::[a-zA-Z0-9_]+)*\]'
                matches = re.findall(var_pattern, variables_text)
                aggressive_matches = split_variables_aggressive(variables_text)
                all_vars = list(set(matches + aggressive_matches))
                all_vars.sort()
                
                for var in all_vars:
                    if var.strip():
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

