#!/usr/bin/env python
"""Pixel-diff two PNGs and report the largest regions of visual difference.

Usage:
    python diff_screenshots.py <reference.png> <candidate.png> <outdir> [--cell=16] [--top=5]

Writes <outdir>/diff-summary.json and, for each of the top-N differing regions,
<outdir>/region-N-ref.png / region-N-candidate.png crops (with a margin) so a
reviewer (human or agent) can see exactly what differs without opening the
full-page screenshots.

Prints a short human-readable summary to stdout: overall diff percentage and
one line per top region (bbox + pixel count), ordered worst first.
"""
import sys
import json
import os
import numpy as np
from PIL import Image

DIFF_THRESHOLD = 40  # summed abs RGB diff (0..765) above which a pixel counts as "differs"
MARGIN = 24  # px of context around each cropped region


def load_rgb(path):
    im = Image.open(path).convert('RGB')
    return im, np.asarray(im, dtype=np.int16)


def main():
    args = [a for a in sys.argv[1:] if not a.startswith('--')]
    if len(args) < 3:
        print('Usage: diff_screenshots.py <reference.png> <candidate.png> <outdir> [--cell=16] [--top=5]')
        sys.exit(2)
    ref_path, cand_path, outdir = args[0], args[1], args[2]
    cell = 16
    top_n = 5
    for a in sys.argv[1:]:
        if a.startswith('--cell='):
            cell = int(a.split('=', 1)[1])
        if a.startswith('--top='):
            top_n = int(a.split('=', 1)[1])

    os.makedirs(outdir, exist_ok=True)

    ref_im, ref = load_rgb(ref_path)
    cand_im, cand = load_rgb(cand_path)

    h = min(ref.shape[0], cand.shape[0])
    w = min(ref.shape[1], cand.shape[1])
    ref = ref[:h, :w]
    cand = cand[:h, :w]

    diff = np.abs(ref - cand).sum(axis=2)  # HxW, 0..765
    differs = diff > DIFF_THRESHOLD

    total = h * w
    differing = int(differs.sum())
    pct = 100.0 * differing / total

    # Coarse grid: fraction of differing pixels per cell.
    gh = (h + cell - 1) // cell
    gw = (w + cell - 1) // cell
    grid = np.zeros((gh, gw), dtype=np.float32)
    for gy in range(gh):
        for gx in range(gw):
            y0, y1 = gy * cell, min((gy + 1) * cell, h)
            x0, x1 = gx * cell, min((gx + 1) * cell, w)
            block = differs[y0:y1, x0:x1]
            grid[gy, gx] = block.mean() if block.size else 0.0

    hot = grid > 0.15  # cell counts as "hot" if >15% of its pixels differ

    # Flood-fill connected hot cells into regions (4-connectivity).
    visited = np.zeros_like(hot, dtype=bool)
    regions = []
    for gy in range(gh):
        for gx in range(gw):
            if not hot[gy, gx] or visited[gy, gx]:
                continue
            stack = [(gy, gx)]
            visited[gy, gx] = True
            cells = []
            while stack:
                cy, cx = stack.pop()
                cells.append((cy, cx))
                for ny, nx in ((cy - 1, cx), (cy + 1, cx), (cy, cx - 1), (cy, cx + 1)):
                    if 0 <= ny < gh and 0 <= nx < gw and hot[ny, nx] and not visited[ny, nx]:
                        visited[ny, nx] = True
                        stack.append((ny, nx))
            ys = [c[0] for c in cells]
            xs = [c[1] for c in cells]
            y0px, y1px = min(ys) * cell, min(max(ys) + 1, gh) * cell
            x0px, x1px = min(xs) * cell, min(max(xs) + 1, gw) * cell
            y1px, x1px = min(y1px, h), min(x1px, w)
            weight = int(differs[y0px:y1px, x0px:x1px].sum())
            regions.append({
                'bbox': [int(x0px), int(y0px), int(x1px), int(y1px)],
                'differingPixels': weight,
                'cellCount': len(cells),
            })

    regions.sort(key=lambda r: -r['differingPixels'])
    regions = regions[:top_n]

    for i, r in enumerate(regions, start=1):
        x0, y0, x1, y1 = r['bbox']
        x0m, y0m = max(0, x0 - MARGIN), max(0, y0 - MARGIN)
        x1m, y1m = min(w, x1 + MARGIN), min(h, y1 + MARGIN)
        ref_crop_path = os.path.join(outdir, f'region-{i}-ref.png')
        cand_crop_path = os.path.join(outdir, f'region-{i}-candidate.png')
        ref_im.crop((x0m, y0m, x1m, y1m)).save(ref_crop_path)
        cand_im.crop((x0m, y0m, x1m, y1m)).save(cand_crop_path)
        r['cropBbox'] = [x0m, y0m, x1m, y1m]
        r['refCrop'] = ref_crop_path
        r['candidateCrop'] = cand_crop_path

    summary = {
        'referenceImage': ref_path,
        'candidateImage': cand_path,
        'width': w,
        'height': h,
        'diffPixelPercent': round(pct, 3),
        'diffThreshold': DIFF_THRESHOLD,
        'regions': regions,
    }
    with open(os.path.join(outdir, 'diff-summary.json'), 'w') as f:
        json.dump(summary, f, indent=2)

    print(f'overall diff: {pct:.2f}% of pixels ({differing}/{total})')
    if not regions:
        print('no significant differing regions found (below hotness threshold)')
    for i, r in enumerate(regions, start=1):
        x0, y0, x1, y1 = r['bbox']
        print(f'region {i}: bbox=({x0},{y0})-({x1},{y1}) differingPixels={r["differingPixels"]} '
              f'ref={r["refCrop"]} candidate={r["candidateCrop"]}')


if __name__ == '__main__':
    main()
