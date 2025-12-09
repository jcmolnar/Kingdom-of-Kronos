#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Remove duplicate functions from ANALYSIS_RESULTS.txt"""

import re
import sys

input_file = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts\ANALYSIS_RESULTS.txt"
output_file = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts\ANALYSIS_RESULTS.txt"

# Read the file
try:
    with open(input_file, 'r', encoding='utf-8', errors='ignore') as f:
        content = f.read()
except Exception as e:
    print(f"Error reading file: {e}")
    sys.exit(1)

# Split by script sections (using the separator pattern)
# Pattern: ======================================== followed by script name
sections = re.split(r'(========================================\n?([a-zA-Z0-9_]+\.cs)\n?)', content)

output_lines = []
current_script = ""
in_functions_section = False
seen_functions = set()
original_line_count = len(content.split('\n')) if '\n' in content else len(content.split('========================================'))

# Process each section
i = 0
while i < len(sections):
    section = sections[i]
    
    # Check if this is a script header
    match = re.match(r'========================================\n?([a-zA-Z0-9_]+\.cs)\n?', section)
    if match:
        current_script = match.group(1)
        in_functions_section = False
        seen_functions = set()
        output_lines.append(section)
        i += 1
        continue
    
    # Check if we're entering FUNCTIONS section
    if 'FUNCTIONS:' in section:
        in_functions_section = True
        seen_functions = set()
        output_lines.append(section)
        i += 1
        continue
    
    # Check if we're leaving FUNCTIONS section
    if in_functions_section and ('VARIABLES' in section or 'CROSS-REFERENCE' in section):
        in_functions_section = False
        output_lines.append(section)
        i += 1
        continue
    
    # Process function lines if in FUNCTIONS section
    if in_functions_section:
        # Split section into lines (handle both \n and no line breaks)
        # Look for function patterns: functionName(params)
        lines = section.split('\n') if '\n' in section else [section]
        
        for line in lines:
            line = line.strip()
            if not line:
                output_lines.append('')
                continue
            
            # Check if this looks like a function signature
            func_match = re.match(r'^([a-zA-Z0-9_:]+\([^)]*\))$', line)
            if func_match:
                func_sig = func_match.group(1)
                func_key = f"{current_script}|{func_sig}"
                
                if func_key not in seen_functions:
                    seen_functions.add(func_key)
                    output_lines.append(line)
            else:
                # Not a function line, keep it
                output_lines.append(line)
    else:
        # Not in functions section, keep everything
        output_lines.append(section)
    
    i += 1

# Write output
output_content = '\n'.join(output_lines)
with open(output_file, 'w', encoding='utf-8') as f:
    f.write(output_content)

print(f"Removed duplicate functions from ANALYSIS_RESULTS.txt")
print(f"Original segments: {len(sections)}")
print(f"Output lines: {len(output_lines)}")
print(f"Removed duplicates based on function signatures")

