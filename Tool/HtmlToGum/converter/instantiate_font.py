#!/usr/bin/env python3
"""Bake a static .ttf instance from a (possibly variable / woff2) web font.

Usage:
  python instantiate_font.py <input> <output.ttf> [--wght N] [--ital N]

gumcli fonts only accepts .ttf paths (BmfcSave.IsFontFilePath). Variable fonts and
woff2 downloads must be reduced to a static TTF at the CSS weight/style the page used.
"""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

from fontTools.ttLib import TTFont
from fontTools.varLib.instancer import instantiateVariableFont


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("input")
    ap.add_argument("output")
    ap.add_argument("--wght", type=float, default=None)
    ap.add_argument("--ital", type=float, default=None)
    ap.add_argument("--slnt", type=float, default=None)
    args = ap.parse_args()

    src = Path(args.input)
    dst = Path(args.output)
    if not src.is_file():
        print(f"input not found: {src}", file=sys.stderr)
        return 1

    font = TTFont(src)
    axes = {}
    if "fvar" in font:
        available = {a.axisTag for a in font["fvar"].axes}
        if args.wght is not None and "wght" in available:
            axes["wght"] = args.wght
        if args.ital is not None and "ital" in available:
            axes["ital"] = args.ital
        if args.slnt is not None and "slnt" in available:
            axes["slnt"] = args.slnt
        if axes:
            font = instantiateVariableFont(font, axes, inplace=False)

    dst.parent.mkdir(parents=True, exist_ok=True)
    # Always write sfnt (TrueType/CFF) — not woff2 — so gumcli's .ttf path check passes.
    font.flavor = None
    font.save(dst)
    print(f"wrote {dst} axes={axes or 'static'}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
