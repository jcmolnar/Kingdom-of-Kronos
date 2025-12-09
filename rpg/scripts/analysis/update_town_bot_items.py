#!/usr/bin/env python3
"""
Script to update town bot inventories based on zone merchants.
Removes Dagger and adds appropriate weapons, armor, and shields based on zone.
"""

import re
import random
import sys

# Set random seed for reproducibility
random.seed(42)

# Zone to items mapping (weapons, armor, shields - excluding helmets)
ZONE_ITEMS = {
    "Yuliple": {
        "weapons": ["RustyIronBlade", "ButterKnife", "CrackedStick", "SharpIronBlade", "LongKnife", "IronStick", "IronBroadSword", "IronSpear", "IronMace"],
        "armor": ["RatSkinShirt"],
        "shields": []
    },
    "Sanctuary": {
        "weapons": ["SteelBroadSword", "SteelSpear", "SteelMace", "SteelLongSword", "SteelPike", "SteelHammer", "GoldenLongSword", "GoldenPike", "SteelWarHammer"],
        "armor": ["StuddedLeatherSuit", "ToughHideSuit", "IronScaleMail"],
        "shields": []
    },
    "Curama": {
        "weapons": ["GoldenBastardSword", "CrystalPike", "GoldenWarHammer", "CrystalBastardSword", "CrystalTrident", "GoldenDivineMace"],
        "armor": ["SteelScaleMail", "SteelBrigandineMail", "GoldenBrigandineMail"],
        "shields": []
    },
    "Arbal": {
        "weapons": ["TemperedCrystalBastardSword", "TemperedCrystalTrident", "CrystalDivineMace", "CrystalClaymore", "DiamondTrident", "DiamondDivineMace"],
        "armor": ["GoldenChainMail", "CrystalChainMail", "CrystalRingMail"],
        "shields": []
    },
    "Kronos": {
        "weapons": ["DiamondClaymore", "DiamondDeathSpear", "DiamondBrainSpiller"],
        "armor": ["CrystalBandedMail", "CrystalSplintMail", "TungstenSplintMail"],
        "shields": []
    },
    "Market": {
        "weapons": ["DiamondLegendSword", "DiamondLegendSpear", "DiamondLegendMace"],
        "armor": ["TungstenPlateMail", "DiamondPlateMail", "DiamondFieldPlate"],
        "shields": ["SteelKnightShield"]
    },
    "Lamisor": {
        "weapons": ["BlackDiamondDreamSword", "BlackDiamondDreamSpear", "BlackDiamondDreamMace"],
        "armor": ["DiamondFullPlate"],
        "shields": ["CrystalKnightShield"]
    },
    "Asna": {
        "weapons": ["Hatchet", "Club", "Knife"],
        "armor": [],
        "shields": []
    },
    "Porellis": {
        "weapons": ["BlackDiamondAtomSplitter", "BlackDiamondAtomPiercer", "BlackDiamondAtomSmasher"],
        "armor": ["BlackDiamondFullPlate"],
        "shields": ["DiamondKnightShield"]
    },
    "Gian": {
        "weapons": ["TerminusEst", "AecoSeorei", "MorningStar"],
        "armor": ["RedDiamondPlate", "WhiteDiamondPlate"],
        "shields": ["BlackDiamondKnightShield", "RedDiamondKingShield", "WhiteDiamondKingShield"]
    }
}

def identify_zone(marker_name):
    """Identify which zone a bot belongs to based on marker name."""
    if not marker_name:
        return None
    
    marker_lower = marker_name.lower()
    
    if "yuliple" in marker_lower:
        return "Yuliple"
    elif "sanctuary" in marker_lower:
        return "Sanctuary"
    elif "curama" in marker_lower:
        return "Curama"
    elif "arbal" in marker_lower:
        return "Arbal"
    elif "kronos" in marker_lower:
        return "Kronos"
    elif "market" in marker_lower:
        return "Market"
    elif "lamisor" in marker_lower:
        return "Lamisor"
    elif "asna" in marker_lower:
        return "Asna"
    elif "porellis" in marker_lower:
        return "Porellis"
    elif "gian" in marker_lower:
        return "Gian"
    
    return None

def remove_dagger_from_items(items_line):
    """Remove Dagger from ITEMS line."""
    # Remove "Dagger 1" or "Dagger" pattern
    items_line = re.sub(r'\s+Dagger\s+\d+\s*', ' ', items_line)
    items_line = re.sub(r'\s+Dagger\s*', ' ', items_line)
    return items_line.strip()

def add_items_to_bot(items_line, zone):
    """Add appropriate items to bot based on zone."""
    if zone not in ZONE_ITEMS:
        return items_line
    
    zone_items = ZONE_ITEMS[zone]
    
    # Randomly select 1 weapon
    if zone_items["weapons"]:
        weapon = random.choice(zone_items["weapons"])
        items_line += " " + weapon + " 1"
    
    # Randomly select 1 armor
    if zone_items["armor"]:
        armor = random.choice(zone_items["armor"])
        items_line += " " + armor + " 1"
    
    # Randomly select 1 shield (50% chance if shields available)
    if zone_items["shields"] and random.random() < 0.5:
        shield = random.choice(zone_items["shields"])
        items_line += " " + shield + " 1"
    
    return items_line

def process_mission_file(file_path):
    """Process the mission file and update bot inventories."""
    print(f"Reading file: {file_path}")
    
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            content = f.read()
    except Exception as e:
        print(f"Error reading file: {e}")
        return
    
    # Pattern to match bot groups with their marker and ITEMS
    # This pattern matches: bot group start -> marker -> ITEMS line
    pattern = r'(instant SimGroup "(?:merchant|banker|porter|quest|hunt|tournyquest|guildmaster|sealnpc|assassin|duelmanager|manager|hometele)\d+"\s*\{[^}]*?instant Marker "([^"]+)"[^}]*?instant SimGroup "ITEMS ([^"]+)"[^}]*?\})'
    
    def replace_bot(match):
        full_match = match.group(0)
        marker_name = match.group(1)
        items_line = match.group(2)
        
        # Identify zone
        zone = identify_zone(marker_name)
        
        # Process items
        if zone:
            # Remove Dagger and add zone-appropriate items
            new_items = remove_dagger_from_items(items_line)
            new_items = add_items_to_bot(new_items, zone)
            print(f"Updated bot in {zone}: {marker_name} - Items: {new_items}")
        else:
            # Just remove Dagger if zone not identified
            new_items = remove_dagger_from_items(items_line)
            if new_items != items_line:
                print(f"Removed Dagger from: {marker_name or 'unknown'}")
        
        # Replace ITEMS line in the match
        new_match = full_match.replace('"ITEMS ' + items_line + '"', '"ITEMS ' + new_items + '"')
        return new_match
    
    # Process all bot groups
    new_content = re.sub(pattern, replace_bot, content, flags=re.DOTALL)
    
    # Write back to file
    try:
        with open(file_path, 'w', encoding='utf-8') as f:
            f.write(new_content)
        print(f"\nSuccessfully updated file: {file_path}")
    except Exception as e:
        print(f"Error writing file: {e}")

if __name__ == "__main__":
    mission_file = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\MISSIONS\KingdomKronos.mis"
    print("Starting town bot item update...")
    process_mission_file(mission_file)
    print("Done!")
