
import os
from PIL import Image

src = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.8.1 Development\Original Skin Extracts\HWALL1.bmp"
dst = r"C:\Users\Joe\Desktop\Kingdom of Kronos V0.8.1 Development\Original Skin Extracts\HWALL1.png"

try:
    print(f"Attempting to convert {src}")
    if not os.path.exists(src):
        print("Source file does not exist!")
        exit(1)
        
    with Image.open(src) as img:
        print(f"Format: {img.format}, Mode: {img.mode}, Size: {img.size}")
        img.save(dst)
    print("Success!")
except Exception as e:
    print(f"Error: {e}")
