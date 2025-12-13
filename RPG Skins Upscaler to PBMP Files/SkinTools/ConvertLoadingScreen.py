#!/usr/bin/env python3
"""
Convert image to 8-bit indexed BMP using ACT palette for loading screens
Uses pixel-by-pixel remapping: find closest color in palette, replace RGB with index (0-255)
Outputs at 640x480 resolution
"""

import sys
import os
from PIL import Image
import numpy as np

def read_act_palette(act_file):
    """Read ACT palette file - 256 RGB colors (3 bytes each = 768 bytes)"""
    palette = []
    with open(act_file, 'rb') as f:
        for i in range(256):
            rgb = f.read(3)
            if len(rgb) < 3:
                # Pad with black if file is shorter
                rgb = rgb + b'\x00' * (3 - len(rgb))
            palette.append((rgb[0], rgb[1], rgb[2]))
    return palette

def find_closest_color(rgb, palette):
    """Find the index of the closest color in the palette using Euclidean distance"""
    r, g, b = int(rgb[0]), int(rgb[1]), int(rgb[2])
    min_dist = float('inf')
    best_index = 0
    
    for i, (pr, pg, pb) in enumerate(palette):
        # Euclidean distance in RGB space (use int to avoid overflow warnings)
        dr = int(r) - int(pr)
        dg = int(g) - int(pg)
        db = int(b) - int(pb)
        dist = (dr * dr + dg * dg + db * db) ** 0.5
        if dist < min_dist:
            min_dist = dist
            best_index = i
            if dist == 0:  # Exact match
                break
    
    return best_index

def convert_image(input_file, output_file, palette_file, width=640, height=480):
    """Convert image to 8-bit indexed BMP using ACT palette"""
    print(f"Reading palette from: {palette_file}")
    palette = read_act_palette(palette_file)
    print(f"Loaded {len(palette)} colors from palette")
    
    print(f"Loading image: {input_file}")
    img = Image.open(input_file)
    
    # Convert to RGB if needed
    if img.mode != 'RGB':
        img = img.convert('RGB')
    
    # Resize to specified dimensions if needed
    if img.size != (width, height):
        print(f"Resizing from {img.size} to {width}x{height}")
        img = img.resize((width, height), Image.Resampling.LANCZOS)
    
    print(f"Applying palette (pixel-by-pixel remapping) to {width}x{height} image...")
    print("  Finding closest palette color for each pixel...")
    print("  Replacing RGB values with palette indices (0-255)...")
    
    # Convert to numpy array for faster processing
    pixels = np.array(img)
    img_height, img_width = pixels.shape[:2]
    
    # Create output array (grayscale - palette indices)
    output = np.zeros((img_height, img_width), dtype=np.uint8)
    
    # Process each pixel
    total_pixels = img_height * img_width
    processed = 0
    for y in range(img_height):
        for x in range(img_width):
            r, g, b = pixels[y, x]
            index = find_closest_color((r, g, b), palette)
            output[y, x] = index
            processed += 1
            if processed % 10000 == 0:
                print(f"  Processed {processed}/{total_pixels} pixels...")
    
    print(f"  Completed {total_pixels} pixels")
    
    print(f"Creating indexed image with palette...")
    # Create indexed image with the palette
    indexed_img = Image.fromarray(output, mode='P')
    
    # Set the palette (PIL expects 768 bytes: 256 colors * 3 bytes RGB)
    palette_data = []
    for r, g, b in palette:
        palette_data.extend([r, g, b])
    # Pad to exactly 768 bytes if needed
    while len(palette_data) < 768:
        palette_data.append(0)
    palette_data = palette_data[:768]  # Ensure exactly 768 bytes
    
    indexed_img.putpalette(palette_data)
    
    print(f"Saving 8-bit indexed BMP: {output_file}")
    # Save as 8-bit BMP
    indexed_img.save(output_file, 'BMP')
    print(f"Successfully created 8-bit indexed BMP: {output_file}")
    print(f"  Resolution: {width}x{height}")
    print(f"  File size: {os.path.getsize(output_file)} bytes")

if __name__ == '__main__':
    if len(sys.argv) < 4:
        print("Usage: ConvertLoadingScreen.py <input.png|bmp> <output.bmp> <palette.act> [width] [height]")
        print("  Converts image to 8-bit indexed BMP using the specified ACT palette")
        print("  Default resolution: 640x480")
        print("  Logic: Pixel-by-pixel remapping - finds closest color in palette,")
        print("         replaces RGB (3 bytes) with palette index (1 byte, 0-255)")
        sys.exit(1)
    
    input_file = sys.argv[1]
    output_file = sys.argv[2]
    palette_file = sys.argv[3]
    width = int(sys.argv[4]) if len(sys.argv) > 4 else 640
    height = int(sys.argv[5]) if len(sys.argv) > 5 else 480
    
    if not os.path.exists(input_file):
        print(f"Error: Input file not found: {input_file}")
        sys.exit(1)
    
    if not os.path.exists(palette_file):
        print(f"Error: Palette file not found: {palette_file}")
        sys.exit(1)
    
    try:
        convert_image(input_file, output_file, palette_file, width, height)
    except Exception as e:
        print(f"Error: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)




