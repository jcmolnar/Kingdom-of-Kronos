
import os
import shutil
from glob import glob

ARTIFACTS_DIR = r"C:\Users\Joe\.gemini\antigravity\brain\d6624cd4-38b1-4f12-90d8-fd36fae617c0"
DEST_DIR = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.8.1 Development\Upscaled Skins"

patterns = ["*_upscaled_*.png"]

print(f"Scanning {ARTIFACTS_DIR} for generated skins...")

count = 0
for pattern in patterns:
    files = glob(os.path.join(ARTIFACTS_DIR, pattern))
    for f in files:
        filename = os.path.basename(f)
        # Parse original name: min_larmor_upscaled_12345.png -> min.larmor.png
        # Strategy: The prompt generation logic likely replaced dots with underscores or appended suffix.
        # But wait, looking at my previous calls:
        # ImageName: min_larmor_upscaled
        # Result: min_larmor_upscaled_TIMESTAMP.png
        # Original: min.larmor.png
        
        # Mapping: 
        # min_larmor_upscaled_... -> min.larmor.png
        # min_marmor_upscaled_... -> min.marmor.png
        # robeblack_larmor_upscaled_... -> robeblack.larmor.png
        
        # Hardcoding the mapping logic based on known file list might be safer, 
        # but let's try to reverse the normalization if possible. 
        # Actually, simpler: I'll just rename them to a clean format closer to the original if I can match them.
        
        # Let's just move them for now and maybe rename manually or via script if I can match exact source files.
        # Actually, if I look at the source dir `Player Skins Upscaled`, I can fuzzy match.
        
        # For now, let's just move them to the Dest dir so the user sees them.
        # I will strip the timestamp.
        
        # Example: robeblack_larmor_upscaled_1765345566791.png
        # Target: robeblack.larmor.png (if possible) OR robeblack_larmor_upscaled.png
        
        # Let's try to reconstruct the name:
        # Remove _upscaled_TIMESTAMP.png
        parts = filename.split("_upscaled_")
        if len(parts) == 2:
            base = parts[0] # robeblack_larmor
            # Try to restore dots? 
            # robeblack_larmor -> robeblack.larmor
            # min_larmor -> min.larmor
            # This is ambiguous (rpg_human -> rpg.human? or rpg_human?)
            # But looking at the file list, most use dots.
            
            # Let's check if the dot-version exists in the source folder
            source_dir = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.8.1 Development\Upscaled Skins\Player Skins Upscaled"
            candidate_dot = base.replace("_", ".") + ".png" # min.larmor.png
            
            if os.path.exists(os.path.join(source_dir, candidate_dot)):
                final_name = candidate_dot
            else:
                # Fallback: just keep base name
                final_name = base + ".png"
                
            dest_path = os.path.join(DEST_DIR, final_name)
            
            try:
                shutil.move(f, dest_path)
                print(f"Moved {filename} -> {final_name}")
                count += 1
            except Exception as e:
                print(f"Error moving {filename}: {e}")

print(f"Moved {count} files.")
