#!/usr/bin/env python3
"""Generate a classic 9-slice PNG: corners = radius, edges = border, center = fill.

Usage:
  python generate_nineslice.py <out.png> --fill R,G,B,A --border R,G,B,A --width W --radius R
"""
from __future__ import annotations

import argparse
from pathlib import Path

from PIL import Image, ImageDraw


def parse_rgba(s: str) -> tuple[int, int, int, int]:
    parts = [int(x) for x in s.split(",")]
    if len(parts) == 3:
        parts.append(255)
    return tuple(parts)  # type: ignore


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("output")
    ap.add_argument("--fill", required=True)
    ap.add_argument("--border", required=True)
    ap.add_argument("--width", type=int, required=True)
    ap.add_argument("--radius", type=int, required=True)
    args = ap.parse_args()

    bw = max(1, args.width)
    rad = max(bw, args.radius)
    # Texture size: 2*slice + 1 middle pixel (Gum CustomFrameTextureCoordinateWidth = rad).
    size = rad * 2 + 1
    fill = parse_rgba(args.fill)
    border = parse_rgba(args.border)

    im = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    draw = ImageDraw.Draw(im)
    # Outer rounded rect = border color; inner = fill (inset by border width).
    draw.rounded_rectangle([0, 0, size - 1, size - 1], radius=rad, fill=border)
    inset = bw
    inner = size - 1 - inset
    if inner > inset:
        inner_rad = max(0, rad - bw)
        draw.rounded_rectangle([inset, inset, inner, inner], radius=inner_rad, fill=fill)

    out = Path(args.output)
    out.parent.mkdir(parents=True, exist_ok=True)
    im.save(out)
    print(f"wrote {out} size={size} frame={rad}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
