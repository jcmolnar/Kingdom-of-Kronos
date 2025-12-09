#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Format variables in ANALYSIS_RESULTS.txt - Improved matching algorithm"""

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
            
            # Find all variable patterns
            # Pattern 1: $var = assignments
            pattern1 = r'\$([a-zA-Z0-9_]+)(?=\s*=)'
            # Pattern 2: $Namespace::var = assignments
            pattern2 = r'\$([a-zA-Z0-9_]+::[a-zA-Z0-9_]+)(?=\s*=)'
            # Pattern 3: $var[index] = assignments
            pattern3 = r'\$([a-zA-Z0-9_]+)\[[^\]]+\](?=\s*=)'
            # Pattern 4: $Namespace::var[index] = assignments
            pattern4 = r'\$([a-zA-Z0-9_]+::[a-zA-Z0-9_]+)\[[^\]]+\](?=\s*=)'
            
            for pattern in [pattern1, pattern2, pattern3, pattern4]:
                matches = re.findall(pattern, content)
                for match in matches:
                    var_names.add(match)
            
        except Exception as e:
            pass
    
    return var_names

# Extract variable names
print("Extracting variable names from source files...")
known_vars = extract_variable_names()
print(f"Found {len(known_vars)} unique variable names")

def split_using_greedy_matching(text, known_vars):
    """Greedy matching - always match longest possible variable name"""
    if not text or not text.strip():
        return []
    
    # Create patterns for all known variables, sorted by length (longest first)
    # Also create patterns for variables with array indices
    patterns = []
    for var in known_vars:
        # Exact match
        patterns.append((var, var))
        # With array index [number]
        patterns.append((var + r'\[\d+\]', var + '[N]'))  # Placeholder for matching
        # Namespace variables
        if '::' in var:
            patterns.append((var, var))
    
    # Sort by length (longest first) for greedy matching
    patterns.sort(key=lambda x: len(x[0]), reverse=True)
    
    variables = []
    remaining = text
    seen = set()
    
    while remaining:
        matched = False
        
        # Try to match longest patterns first
        for pattern_str, var_name in patterns:
            # Replace [N] placeholder with actual number pattern
            if '[N]' in var_name:
                pattern = re.compile('^' + pattern_str.replace('[N]', r'\[\d+\]'))
            else:
                pattern = re.compile('^' + re.escape(pattern_str))
            
            match = pattern.match(remaining)
            if match:
                matched_var = match.group(0)
                # Handle array indices
                if '[N]' in var_name:
                    # Extract the actual variable name with index
                    actual_match = re.match(r'^([a-zA-Z0-9_]+(?:::[a-zA-Z0-9_]+)?)(\[\d+\])', matched_var)
                    if actual_match:
                        matched_var = actual_match.group(1) + actual_match.group(2)
                
                if matched_var not in seen:
                    variables.append(matched_var)
                    seen.add(matched_var)
                remaining = remaining[len(match.group(0)):]
                matched = True
                break
        
        if not matched:
            # Try generic pattern matching
            generic_match = re.match(r'^([a-zA-Z0-9_]+(?:::[a-zA-Z0-9_]+)?(?:\[\d+\])?)', remaining)
            if generic_match:
                var_candidate = generic_match.group(1)
                if var_candidate not in seen:
                    variables.append(var_candidate)
                    seen.add(var_candidate)
                remaining = remaining[len(var_candidate):]
            else:
                # Skip one character if we can't match
                remaining = remaining[1:]
                if len(remaining) == 0:
                    break
    
    return variables

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
                variables = split_using_greedy_matching(variables_text, known_vars)
                
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
                variables = split_using_greedy_matching(variables_text, known_vars)
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

