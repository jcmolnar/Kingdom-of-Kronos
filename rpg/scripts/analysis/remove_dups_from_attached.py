#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Remove duplicate functions from ANALYSIS_RESULTS.txt based on attached file structure"""

# Since the file got corrupted, we need to work with the structure
# The attached file shows duplicates like BuildDotString appearing 9 times in comchat.cs

import re
import sys

# Try to read the original file structure
# The file might be corrupted, so we'll process it carefully

input_file = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts\ANALYSIS_RESULTS_restored.txt"
output_file = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts\ANALYSIS_RESULTS.txt"

try:
    with open(input_file, 'r', encoding='utf-8', errors='ignore') as f:
        lines = f.readlines()
except Exception as e:
    print(f"Error reading file: {e}")
    sys.exit(1)

output = []
current_script = ""
in_functions_section = False
seen_functions = set()
duplicate_count = 0

i = 0
while i < len(lines):
    line = lines[i]
    original_line = line
    
    # Remove trailing newline for processing
    line_stripped = line.rstrip('\n\r')
    
    # Check for script separator
    if line_stripped == "========================================":
        # Next line should be script name
        output.append(original_line)
        i += 1
        if i < len(lines):
            next_line = lines[i].rstrip('\n\r')
            if re.match(r'^[a-zA-Z0-9_]+\.cs$', next_line):
                current_script = next_line
                in_functions_section = False
                seen_functions = set()
                output.append(lines[i])
        i += 1
        continue
    
    # Check if entering FUNCTIONS section
    if line_stripped == "FUNCTIONS:":
        in_functions_section = True
        seen_functions = set()
        output.append(original_line)
        i += 1
        continue
    
    # Check if leaving FUNCTIONS section
    if in_functions_section:
        if line_stripped.startswith("VARIABLES") or line_stripped.startswith("CROSS-REFERENCE"):
            in_functions_section = False
            output.append(original_line)
            i += 1
            continue
        
        # Check if this is a function signature
        # Pattern: functionName(params) on its own line
        if line_stripped and re.match(r'^[a-zA-Z0-9_:]+\([^)]*\)$', line_stripped):
            func_key = f"{current_script}|{line_stripped}"
            if func_key not in seen_functions:
                seen_functions.add(func_key)
                output.append(original_line)
            else:
                duplicate_count += 1
            i += 1
            continue
    
    # Not a function line, keep it
    output.append(original_line)
    i += 1

# Write output
with open(output_file, 'w', encoding='utf-8') as f:
    f.writelines(output)

print(f"Removed duplicate functions from ANALYSIS_RESULTS.txt")
print(f"Original lines: {len(lines)}")
print(f"Output lines: {len(output)}")
print(f"Removed: {duplicate_count} duplicate function entries")

