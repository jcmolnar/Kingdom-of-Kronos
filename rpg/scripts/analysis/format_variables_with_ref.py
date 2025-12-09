#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Format variables in ANALYSIS_RESULTS.txt using actual variable names from source code"""

import re
import sys
import os

input_file = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts\ANALYSIS_RESULTS.txt"
output_file = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts\ANALYSIS_RESULTS.txt"
script_dir = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts"

# First, extract all variable names from source files
def extract_variable_names():
    """Extract all variable names from .cs files"""
    var_names = set()
    scripts = [
        "globals.cs", "Ai.cs", "rpgfunk.cs", "skills.cs", "house.cs", "rpgarena.cs",
        "sleep.cs", "game.cs", "Admin.cs", "marker.cs", "trigger.cs", "zone.cs",
        "spells.cs", "classes.cs", "party.cs", "jail.cs", "nsound.cs", "Help.cs",
        "baseExpData.cs", "baseDebrisData.cs", "baseProjData.cs", "armordata.cs",
        "mission.cs", "item.cs", "Accessory.cs", "weapons.cs", "armors.cs",
        "Crystal.cs", "Spawn.cs", "connectivity.cs", "gameevents.cs", "shopping.cs",
        "weight.cs", "mana.cs", "hp.cs", "rpgstats.cs", "rpghud.cs", "playerdamage.cs",
        "playerspawn.cs", "itemevents.cs", "Belt.cs", "economy.cs", "remote.cs",
        "weaponHandling.cs", "BonusState.cs", "ferry.cs", "Player.cs", "Vehicle.cs",
        "Turret.cs", "beacon.cs", "staticshape.cs", "Station.cs", "moveable.cs",
        "sensor.cs", "Mine.cs", "interiorLight.cs", "comchat.cs", "plugs.cs",
        "version.cs", "hackfix.cs", "newstuff.cs", "advertisements.cs", "remortseal.cs",
        "DebugInit.cs", "objectives.cs", "Server.cs"
    ]
    
    for script in scripts:
        script_path = os.path.join(script_dir, script)
        if not os.path.exists(script_path):
            continue
        
        try:
            with open(script_path, 'r', encoding='utf-8', errors='ignore') as f:
                content = f.read()
            
            # Find all variable assignments: $var = or $var[ or $Namespace::var
            patterns = [
                r'\$([a-zA-Z0-9_]+)(?:\[[^\]]+\])?(?=\s*=)',  # $var = or $var[index] =
                r'\$([a-zA-Z0-9_]+::[a-zA-Z0-9_]+)(?:\[[^\]]+\])?(?=\s*=)',  # $Namespace::var =
                r'\$([a-zA-Z0-9_]+)(?:\[[^\]]+\])?(?=\s*[;,\[\]])',  # $var in other contexts
            ]
            
            for pattern in patterns:
                matches = re.findall(pattern, content)
                for match in matches:
                    var_names.add(match)
                    # Also add without namespace if it has one
                    if '::' in match:
                        parts = match.split('::')
                        var_names.add(parts[-1])  # Last part
            
        except Exception as e:
            print(f"Error reading {script}: {e}")
    
    return var_names

# Extract variable names
print("Extracting variable names from source files...")
known_vars = extract_variable_names()
print(f"Found {len(known_vars)} unique variable names")

def split_using_known_vars(text, known_vars):
    """Split concatenated variables using known variable names"""
    if not text or not text.strip():
        return []
    
    # Sort by length (longest first) to match longer names first
    sorted_vars = sorted(known_vars, key=len, reverse=True)
    
    variables = []
    remaining = text
    i = 0
    max_iterations = len(text) * 2  # Safety limit
    iterations = 0
    
    while remaining and iterations < max_iterations:
        iterations += 1
        found = False
        
        # Try to match known variables
        for var in sorted_vars:
            # Try exact match at start
            if remaining.startswith(var):
                variables.append(var)
                remaining = remaining[len(var):]
                found = True
                break
            
            # Try with :: prefix (namespace)
            if '::' not in var and remaining.startswith(var + '::'):
                # This might be part of a namespace variable
                # Look for complete namespace pattern
                ns_match = re.match(r'([a-zA-Z0-9_]+::[a-zA-Z0-9_]+)', remaining)
                if ns_match:
                    variables.append(ns_match.group(1))
                    remaining = remaining[len(ns_match.group(1)):]
                    found = True
                    break
            
            # Try with [number] suffix (array)
            array_match = re.match(r'(' + re.escape(var) + r')(\[[0-9]+\])', remaining)
            if array_match:
                variables.append(array_match.group(1) + array_match.group(2))
                remaining = remaining[len(array_match.group(0)):]
                found = True
                break
        
        if not found:
            # Try to find any pattern that looks like a variable
            # Match: word, word::word, word[number], word::word[number]
            pattern_match = re.match(r'([a-zA-Z0-9_]+(?:::[a-zA-Z0-9_]+)*(?:\[[0-9]+\])?)', remaining)
            if pattern_match:
                var_candidate = pattern_match.group(1)
                variables.append(var_candidate)
                remaining = remaining[len(var_candidate):]
            else:
                # Can't match, skip one character
                remaining = remaining[1:]
    
    # Deduplicate while preserving order
    seen = set()
    result = []
    for var in variables:
        if var not in seen:
            seen.add(var)
            result.append(var)
    
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
            # Process accumulated variables
            if variables_text.strip():
                variables = split_using_known_vars(variables_text, known_vars)
                
                for var in variables:
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
                variables = split_using_known_vars(variables_text, known_vars)
                for var in variables:
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

