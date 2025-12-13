
import os
from glob import glob
try:
    from PIL import Image
except ImportError:
    print("PIL not installed")
    exit(1)

src_dir = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.8.1 Development\Original Skin Extracts"

files = glob(os.path.join(src_dir, "*.bmp"))
print(f"Found {len(files)} BMP files.")

for f in files:
    head, tail = os.path.split(f)
    name, ext = os.path.splitext(tail)
    dst = os.path.join(head, name + ".png")
    
    # Skip if already exists
    if os.path.exists(dst):
        continue
        
    try:
        # Try PIL first
        with Image.open(f) as img:
            img.save(dst)
        print(f"Converted {tail}")
    except Exception as e:
        print(f"PIL Failed for {tail}: {e} -> Trying nconvert...")
        # Fallback to nconvert
        nconvert_path = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.8.1 Development\RPG Skins Upscaler to PBMP Files\SkinTools\nconvert.exe"
        cmd = [nconvert_path, "-out", "png", "-o", dst, f]
        try:
            import subprocess
            subprocess.run(cmd, check=True, stdout=subprocess.DEVNULL, stderr=subprocess.PIPE)
            print(f"nconvert Converted {tail} (Fallback)")
        except Exception as n_err:
            print(f"FAILED {tail}: {n_err}")
