#!/usr/bin/env python3
"""
Convert a PNG to a Rust 1bpp icon const for the EPD display.

Usage:
    python3 tools/png_to_icon.py icon.png [NAME] [--threshold 128]

The image is converted to grayscale, then thresholded: pixels darker than
--threshold become black (1), lighter become white (0).

Requirements:
    pip install Pillow
"""

import sys
import argparse
from pathlib import Path

try:
    from PIL import Image
except ImportError:
    print("Install Pillow first:  pip install Pillow")
    sys.exit(1)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("image", help="Path to PNG file")
    parser.add_argument("name", nargs="?", help="Const name (default: filename stem)")
    parser.add_argument("--threshold", type=int, default=128,
                        help="Pixels darker than this → black (default 128)")
    args = parser.parse_args()

    path = Path(args.image)
    name = (args.name or path.stem).upper().replace("-", "_")

    src = Image.open(path)
    if src.mode in ("RGBA", "LA") or (src.mode == "P" and "transparency" in src.info):
        bg = Image.new("RGBA", src.size, (255, 255, 255, 255))
        bg.paste(src.convert("RGBA"), mask=src.convert("RGBA").split()[3])
        img = bg.convert("L")
    else:
        img = src.convert("L")  # grayscale
    w, h = img.size
    pixels = [img.getpixel((x, y)) for y in range(h) for x in range(w)]

    bytes_per_row = (w + 7) // 8
    rows = []
    for row in range(h):
        row_bytes = []
        for byte_idx in range(bytes_per_row):
            byte = 0
            for bit in range(8):
                col = byte_idx * 8 + bit
                if col < w:
                    px = pixels[row * w + col]
                    if px < args.threshold:
                        byte |= (1 << (7 - bit))  # MSB = leftmost
            row_bytes.append(byte)
        rows.append(row_bytes)

    total_bytes = bytes_per_row * h

    print(f"pub const {name}_W: u16 = {w};")
    print(f"pub const {name}_H: u16 = {h};")
    print(f"pub const {name}: [u8; {total_bytes}] = [")
    for r, row_bytes in enumerate(rows):
        hex_vals = ", ".join(f"0x{b:02X}" for b in row_bytes)
        print(f"    {hex_vals},  // {r:2d}")
    print("];")


if __name__ == "__main__":
    main()
