#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Format variables in ANALYSIS_RESULTS.txt - Final improved version"""

import re
import sys
import os

input_file = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts\ANALYSIS_RESULTS.txt"
output_file = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts\ANALYSIS_RESULTS.txt"
script_dir = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\scripts"

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
            
            # Find all variable patterns - be comprehensive
            patterns = [
                r'\$([a-zA-Z0-9_]+)(?=\s*=)',  # $var =
                r'\$([a-zA-Z0-9_]+::[a-zA-Z0-9_]+)(?=\s*=)',  # $Namespace::var =
                r'\$([a-zA-Z0-9_]+)\[',  # $var[
                r'\$([a-zA-Z0-9_]+::[a-zA-Z0-9_]+)\[',  # $Namespace::var[
            ]
            
            for pattern in patterns:
                matches = re.findall(pattern, content)
                for match in matches:
                    var_names.add(match)
                    # Also add array versions
                    var_names.add(match + '[1]')  # Example array index
            
        except Exception:
            pass
    
    return var_names

# Extract variable names
print("Extracting variable names from source files...")
known_vars = extract_variable_names()
print(f"Found {len(known_vars)} unique variable names")

def split_variables_greedy(text, known_vars):
    """Greedy longest-match algorithm for splitting variables"""
    if not text or not text.strip():
        return []
    
    variables = []
    remaining = text
    
    # Build a trie-like structure: for each position, find longest match
    while remaining:
        best_match = None
        best_length = 0
        
        # Try all known variables, find longest match at current position
        for var in known_vars:
            # Try exact match
            if remaining.startswith(var):
                if len(var) > best_length:
                    best_match = var
                    best_length = len(var)
            
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
            # No match found - try generic pattern
            generic_match = re.match(r'^([a-zA-Z0-9_]+(?:::[a-zA-Z0-9_]+)?(?:\[\d+\])?)', remaining)
            if generic_match:
                var_candidate = generic_match.group(1)
                variables.append(var_candidate)
                remaining = remaining[len(var_candidate):]
            else:
                # Can't match anything, skip one char
                if len(remaining) > 0:
                    remaining = remaining[1:]
                else:
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
                variables = split_variables_greedy(variables_text, known_vars)
                
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
                variables = split_variables_greedy(variables_text, known_vars)
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

