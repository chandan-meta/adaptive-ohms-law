"""Wipes the chalk off a lab-set board texture, for the lab scenes before the law is found. The board material's base map
(named in <set folder>/<set json>) keeps its wooden frame and banner; below them (the slate starts at the first row under the
middle that is mostly board green) every pixel far from the board's green is chalk, filled from the slate around it (a
normalised box blur that leaves the chalk out) plus the slate's own grain, so the board keeps its smudges.
Writes <base map>_blank.png next to it.
Usage: python Tools/blank_board.py <set folder> <set json> <material>
       e.g. python Tools/blank_board.py Assets/OhmsLaw/Shared/Art/LabSet LabSet.json black_board1"""
import json
import os
import sys

import numpy as np
from PIL import Image

folder, spec, material = sys.argv[1:4]
base = next(m["baseMap"] for m in json.load(open(os.path.join(folder, spec), encoding="utf-8"))["materials"] if m["name"] == material)
src = os.path.join(folder, "Textures", base)
img = np.asarray(Image.open(src).convert("RGBA")).astype(np.float64) / 255
rgb = img[..., :3]
h, w = rgb.shape[:2]
r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
green = (g > r + 0.03) & (g > b - 0.02) & (rgb.max(-1) < 0.45)
top = h // 2 + int(np.argmax(green[h // 2:].mean(1) > 0.6))  # the slate starts under the banner
slate = np.median(rgb[top:][green[top:]], 0)


def box(a, k):
    """Mean over the (2k+1)-pixel square around each pixel (edges clamped), by a summed-area table."""
    n = 2 * k + 1
    s = np.pad(a, [(k + 1, k), (k + 1, k)] + [(0, 0)] * (a.ndim - 2), mode="edge").cumsum(0).cumsum(1)
    return (s[n:, n:] - s[:-n, n:] - s[n:, :-n] + s[:-n, :-n]) / n ** 2


chalk = np.zeros((h, w))
chalk[top:] = np.abs(rgb[top:] - slate).max(-1) > 0.1
rim = max(2, w // 680)  # ~3 px at 2048: the strokes' soft edges
chalk = (box(chalk, rim) > 0.01).astype(np.float64)  # grown over the edges
keep = (1 - chalk)[..., None]
fill = box(rgb * keep, w // 40) / np.maximum(box(keep, w // 40), 1e-6)
grain = (rgb - box(rgb, 2))[top:][chalk[top:] < 0.5].std(0)
noise = np.random.default_rng(7).normal(0, 1, rgb.shape) * grain
soft = box(chalk, rim)[..., None]
img[..., :3] = rgb * (1 - soft) + np.clip(fill + noise, 0, 1) * soft
dst = src.rsplit(".", 1)[0] + "_blank.png"
Image.fromarray((np.clip(img, 0, 1) * 255 + 0.5).astype(np.uint8)).save(dst)
print(f"BLANK {dst}: slate from row {top} of {h}, {chalk[top:].mean():.1%} of it chalk")
