#!/usr/bin/env python3
"""
Apply ACT palette to image - converts RGB colors to palette indices
This implements the same logic as the Blender plugin: find closest color in palette
and replace RGB with palette index (0-255)
"""

import sys
import struct
from PIL import Image
import numpy as np

def read_act_palette(act_file):
    """Read ACT palette file and return array of RGB colors"""
    palette = []
    with open(act_file, 'rb') as f:
        for i in range(256):
            rgb = f.read(3)
            if len(rgb) < 3:
                break
            palette.append((rgb[0], rgb[1], rgb[2]))
    return palette

def find_closest_color(rgb, palette):
    """Find the index of the closest color in the palette using Euclidean distance"""
    r, g, b = rgb
    min_dist = float('inf')
    best_index = 0
    
    for i, (pr, pg, pb) in enumerate(palette):
        # Euclidean distance in RGB space
        dist = ((r - pr) ** 2 + (g - pg) ** 2 + (b - pb) ** 2) ** 0.5
        if dist < min_dist:
            min_dist = dist
            best_index = i
            if dist == 0:  # Exact match
                break
    
    return best_index

def apply_palette(input_file, output_file, palette_file):
    """Apply palette to image - convert RGB to palette indices"""
    print(f"Reading palette from: {palette_file}")
    palette = read_act_palette(palette_file)
    print(f"Loaded {len(palette)} colors from palette")
    
    print(f"Loading image: {input_file}")
    img = Image.open(input_file)
    
    # Convert to RGB if needed
    if img.mode != 'RGB':
        img = img.convert('RGB')
    
    # Resize to 256x256 if needed
    if img.size != (256, 256):
        print(f"Resizing from {img.size} to 256x256")
        img = img.resize((256, 256), Image.Resampling.LANCZOS)
    
    print("Applying palette (finding closest color for each pixel)...")
    # Convert to numpy array for faster processing
    pixels = np.array(img)
    height, width = pixels.shape[:2]
    
    # Create output array (grayscale - palette indices)
    output = np.zeros((height, width), dtype=np.uint8)
    
    # Process each pixel
    for y in range(height):
        for x in range(width):
            r, g, b = pixels[y, x]
            index = find_closest_color((r, g, b), palette)
            output[y, x] = index
    
    print(f"Saving indexed image: {output_file}")
    # Create indexed image with the palette
    indexed_img = Image.fromarray(output, mode='P')
    
    # Set the palette
    palette_data = []
    for r, g, b in palette:
        palette_data.extend([r, g, b])
    # Pad to 256 colors (768 bytes)
    while len(palette_data) < 768:
        palette_data.append(0)
    
    indexed_img.putpalette(palette_data)
    
    # Save as 8-bit BMP
    indexed_img.save(output_file, 'BMP')
    print(f"Successfully created 8-bit indexed BMP: {output_file}")

if __name__ == '__main__':
    if len(sys.argv) != 4:
        print("Usage: ApplyPalette.py <input.png> <output.bmp> <palette.act>")
        print("  Converts image to 8-bit indexed BMP using the specified ACT palette")
        sys.exit(1)
    
    input_file = sys.argv[1]
    output_file = sys.argv[2]
    palette_file = sys.argv[3]
    
    try:
        apply_palette(input_file, output_file, palette_file)
    except Exception as e:
        print(f"Error: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)




