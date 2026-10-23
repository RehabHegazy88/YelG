"""
Compares screenshots of the site with the reference render of the prototypes (both from screenshot.py).

  python -I tools/website/pixel-diff.py <reference.png> <site.png> [<out-diff.png>]

Prints the share of differing pixels (per-channel difference above a small threshold, to ignore
anti-aliasing noise) and the vertical bands where the differences are, so a section can be found
quickly. With an output path it also writes a picture: the site dimmed, differences in red.
"""
import sys

import numpy as np
from PIL import Image

THRESHOLD = 40   # 0..255 per channel
BAND_GAP = 24    # rows without differences that end a band


def main():
    ref_path, site_path = sys.argv[1], sys.argv[2]
    out = sys.argv[3] if len(sys.argv) > 3 else None
    ref = np.asarray(Image.open(ref_path).convert('RGB')).astype(np.int16)
    site = np.asarray(Image.open(site_path).convert('RGB')).astype(np.int16)
    h = min(ref.shape[0], site.shape[0])
    w = min(ref.shape[1], site.shape[1])
    if ref.shape != site.shape:
        print(f'size differs: reference {ref.shape[1]}x{ref.shape[0]}, site {site.shape[1]}x{site.shape[0]} (compared {w}x{h})')
    a, b = ref[:h, :w], site[:h, :w]
    mask = (np.abs(a - b).max(axis=2) > THRESHOLD)
    share = mask.mean() * 100
    print(f'differing pixels: {share:.3f}%')

    rows = np.where(mask.any(axis=1))[0]
    bands, start, last = [], None, None
    for r in rows:
        if start is None:
            start = last = r
        elif r - last > BAND_GAP:
            bands.append((start, last))
            start = last = r
        else:
            last = r
    if start is not None:
        bands.append((start, last))
    for s, e in bands[:40]:
        cols = np.where(mask[s:e + 1].any(axis=0))[0]
        print(f'  rows {s}-{e} ({e - s + 1}px), cols {cols.min()}-{cols.max()}, {mask[s:e + 1].sum()} px')
    if len(bands) > 40:
        print(f'  … {len(bands) - 40} more bands')

    if out:
        img = (b * 0.35 + 165).clip(0, 255).astype(np.uint8)
        img[mask] = [230, 30, 30]
        Image.fromarray(img).save(out)


if __name__ == '__main__':
    main()
