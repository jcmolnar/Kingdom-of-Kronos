#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Format variables in ANALYSIS_RESULTS.txt - Perfect matching using source code"""

import re
import sys
import os

input_file = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts\ANALYSIS_RESULTS.txt"
output_file = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts\ANALYSIS_RESULTS.txt"
script_dir = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts"

def extract_all_variable_names():
    """Extract ALL variable names from source files comprehensively"""
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
            
            # Extract variables more comprehensively
            # Pattern 1: $var = (simple variables)
            matches1 = re.findall(r'\$([a-zA-Z0-9_]+)(?=\s*=)', content)
            for m in matches1:
                var_names.add(m)
            
            # Pattern 2: $Namespace::var = (namespace variables)
            matches2 = re.findall(r'\$([a-zA-Z0-9_]+::[a-zA-Z0-9_]+)(?=\s*=)', content)
            for m in matches2:
                var_names.add(m)
            
            # Pattern 3: $var[index] = (array variables)
            matches3 = re.findall(r'\$([a-zA-Z0-9_]+)(?=\[[^\]]+\]\s*=)', content)
            for m in matches3:
                var_names.add(m)
                # Also add with common indices
                var_names.add(m + '[1]')
                var_names.add(m + '[2]')
            
            # Pattern 4: $Namespace::var[index] = (namespace array variables)
            matches4 = re.findall(r'\$([a-zA-Z0-9_]+::[a-zA-Z0-9_]+)(?=\[[^\]]+\]\s*=)', content)
            for m in matches4:
                var_names.add(m)
                var_names.add(m + '[1]')
            
            # Pattern 5: Variables used in expressions (without =)
            matches5 = re.findall(r'\$([a-zA-Z0-9_]+(?:::[a-zA-Z0-9_]+)?)(?=\s*[;,\[\]\)])', content)
            for m in matches5:
                var_names.add(m)
            
        except Exception:
            pass
    
    return var_names

# Extract variable names
print("Extracting variable names from source files...")
known_vars = extract_all_variable_names()
print(f"Found {len(known_vars)} unique variable names")

def split_with_longest_match(text, known_vars):
    """Split using longest match algorithm - always match longest possible variable"""
    if not text or not text.strip():
        return []
    
    # Sort variables by length (longest first) for greedy matching
    sorted_vars = sorted(known_vars, key=lambda x: len(x), reverse=True)
    
    variables = []
    remaining = text
    max_iterations = len(text) * 10
    iterations = 0
    
    while remaining and iterations < max_iterations:
        iterations += 1
        best_match = None
        best_length = 0
        
        # Try all variables, find longest match
        for var in sorted_vars:
            # Try exact match
            if remaining.startswith(var):
                if len(var) > best_length:
                    best_match = var
                    best_length = len(var)
                continue  # Already found, no need to check array version
            
            # Try with array index [number]
            array_pattern = re.compile('^' + re.escape(var) + r'\[\d+\]')
            array_match = array_pattern.match(remaining)
            if array_match:
                matched_str = array_match.group(0)
                if len(matched_str) > best_length:
                    best_match = matched_str
                    best_length = len(matched_str)
        
        if best_match:
            variables.append(best_match)
            remaining = remaining[best_length:]
        else:
            # No known variable matches - try generic pattern
            # Match: word, word::word, word[number], word::word[number]
            generic = re.match(r'^([a-zA-Z0-9_]+(?:::[a-zA-Z0-9_]+)?(?:\[\d+\])?)', remaining)
            if generic:
                var_candidate = generic.group(1)
                variables.append(var_candidate)
                remaining = remaining[len(var_candidate):]
            else:
                # Can't match - skip one character
                remaining = remaining[1:]
                if len(remaining) == 0:
                    break
    
    # Remove duplicates while preserving order
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
                variables = split_with_longest_match(variables_text, known_vars)
                
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
                variables = split_with_longest_match(variables_text, known_vars)
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

