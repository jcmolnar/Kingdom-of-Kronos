import re
import random

random.seed(42)

ZONE_ITEMS = {
    "Yuliple": {"weapons": ["RustyIronBlade", "ButterKnife", "CrackedStick", "SharpIronBlade", "LongKnife", "IronStick", "IronBroadSword", "IronSpear", "IronMace"], "armor": ["RatSkinShirt"], "shields": []},
    "Sanctuary": {"weapons": ["SteelBroadSword", "SteelSpear", "SteelMace", "SteelLongSword", "SteelPike", "SteelHammer", "GoldenLongSword", "GoldenPike", "SteelWarHammer"], "armor": ["StuddedLeatherSuit", "ToughHideSuit", "IronScaleMail"], "shields": []},
    "Curama": {"weapons": ["GoldenBastardSword", "CrystalPike", "GoldenWarHammer", "CrystalBastardSword", "CrystalTrident", "GoldenDivineMace"], "armor": ["SteelScaleMail", "SteelBrigandineMail", "GoldenBrigandineMail"], "shields": []},
    "Arbal": {"weapons": ["TemperedCrystalBastardSword", "TemperedCrystalTrident", "CrystalDivineMace", "CrystalClaymore", "DiamondTrident", "DiamondDivineMace"], "armor": ["GoldenChainMail", "CrystalChainMail", "CrystalRingMail"], "shields": []},
    "Kronos": {"weapons": ["DiamondClaymore", "DiamondDeathSpear", "DiamondBrainSpiller"], "armor": ["CrystalBandedMail", "CrystalSplintMail", "TungstenSplintMail"], "shields": []},
    "Market": {"weapons": ["DiamondLegendSword", "DiamondLegendSpear", "DiamondLegendMace"], "armor": ["TungstenPlateMail", "DiamondPlateMail", "DiamondFieldPlate"], "shields": ["SteelKnightShield"]},
    "Lamisor": {"weapons": ["BlackDiamondDreamSword", "BlackDiamondDreamSpear", "BlackDiamondDreamMace"], "armor": ["DiamondFullPlate"], "shields": ["CrystalKnightShield"]},
    "Asna": {"weapons": ["Hatchet", "Club", "Knife"], "armor": [], "shields": []},
    "Porellis": {"weapons": ["BlackDiamondAtomSplitter", "BlackDiamondAtomPiercer", "BlackDiamondAtomSmasher"], "armor": ["BlackDiamondFullPlate"], "shields": ["DiamondKnightShield"]},
    "Gian": {"weapons": ["TerminusEst", "AecoSeorei", "MorningStar"], "armor": ["RedDiamondPlate", "WhiteDiamondPlate"], "shields": ["BlackDiamondKnightShield", "RedDiamondKingShield", "WhiteDiamondKingShield"]}
}

def get_zone(marker):
    if not marker: return None
    m = marker.lower()
    if "yuliple" in m: return "Yuliple"
    if "sanctuary" in m: return "Sanctuary"
    if "curama" in m: return "Curama"
    if "arbal" in m: return "Arbal"
    if "kronos" in m: return "Kronos"
    if "market" in m: return "Market"
    if "lamisor" in m: return "Lamisor"
    if "asna" in m: return "Asna"
    if "porellis" in m: return "Porellis"
    if "gian" in m: return "Gian"
    return None

file_path = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.7 BOV Plugins - JAVASCRIPT DEBUG\rpg\MISSIONS\KingdomKronos.mis"

with open(file_path, 'r', encoding='utf-8') as f:
    lines = f.readlines()

i = 0
current_marker = None
changes = 0

while i < len(lines):
    line = lines[i]
    
    # Find marker
    m = re.search(r'instant Marker "([^"]+)"', line)
    if m:
        current_marker = m.group(1)
    
    # Find ITEMS line
    m = re.search(r'instant SimGroup "ITEMS ([^"]+)"', line)
    if m:
        items = m.group(1)
        zone = get_zone(current_marker)
        
        # Remove Dagger
        new_items = re.sub(r'\s+Dagger\s+\d+\s*', ' ', items).strip()
        new_items = re.sub(r'\s+Dagger\s*', ' ', new_items).strip()
        
        # Add zone items
        if zone and zone in ZONE_ITEMS:
            z = ZONE_ITEMS[zone]
            if z["weapons"]:
                new_items += " " + random.choice(z["weapons"]) + " 1"
            if z["armor"]:
                new_items += " " + random.choice(z["armor"]) + " 1"
            if z["shields"] and random.random() < 0.5:
                new_items += " " + random.choice(z["shields"]) + " 1"
        
        # Replace line
        lines[i] = line.replace('"ITEMS ' + items + '"', '"ITEMS ' + new_items + '"')
        changes += 1
        if zone:
            print(f"{zone}: {current_marker} -> {new_items}")
    
    i += 1

with open(file_path, 'w', encoding='utf-8') as f:
    f.writelines(lines)

print(f"\nDone! {changes} changes made.")

