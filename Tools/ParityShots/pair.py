"""Stitch out/wpf/<name>.png and out/avalonia/<name>.png (beside this script) side by side into out/pairs/<name>.png."""
import os
import sys
from PIL import Image, ImageDraw

import os
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "out")
OUT = os.path.join(ROOT, "pairs")
os.makedirs(OUT, exist_ok=True)
names = sys.argv[1:] or sorted(
    n[:-4] for n in os.listdir(os.path.join(ROOT, "wpf"))
    if n.endswith(".png") and os.path.exists(os.path.join(ROOT, "avalonia", n)))
max_width = 1900
for name in names:
    left = Image.open(os.path.join(ROOT, "wpf", name + ".png")).convert("RGB")
    right = Image.open(os.path.join(ROOT, "avalonia", name + ".png")).convert("RGB")
    gap = 12
    label = 22
    width = left.width + right.width + gap
    height = max(left.height, right.height) + label
    canvas = Image.new("RGB", (width, height), (255, 0, 255))
    draw = ImageDraw.Draw(canvas)
    draw.text((4, 4), f"WPF {name} {left.width}x{left.height}", fill=(0, 0, 0))
    draw.text((left.width + gap + 4, 4), f"AVALONIA {name} {right.width}x{right.height}", fill=(0, 0, 0))
    canvas.paste(left, (0, label))
    canvas.paste(right, (left.width + gap, label))
    if canvas.width > max_width:
        scale = max_width / canvas.width
        canvas = canvas.resize((max_width, int(canvas.height * scale)), Image.LANCZOS)
    path = os.path.join(OUT, name + ".png")
    canvas.save(path)
    print(path, canvas.size)
