"""Builds one comparison image per scene: the reference image(s) on the left, the latest dry-run screenshots on the right.
Usage: python Tools/compare_sheet.py [out_dir]   (reads Logs/Blockout/shots/<Scene>/ and Docs/Polish/References/)"""
import glob
import os
import sys

from PIL import Image, ImageDraw

root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
out_dir = sys.argv[1] if len(sys.argv) > 1 else os.path.join(root, "Logs", "Blockout", "compare")
os.makedirs(out_dir, exist_ok=True)
refs = sorted(glob.glob(os.path.join(root, "Docs", "Polish", "References", "*.png")))
W, H = 640, 360


def tile(paths, cols, label):
    rows = max(1, (len(paths) + cols - 1) // cols)
    sheet = Image.new("RGB", (cols * W, rows * (H + 20) + 30), (24, 26, 32))
    d = ImageDraw.Draw(sheet)
    d.text((8, 6), label, fill=(240, 240, 240))
    for i, p in enumerate(paths):
        im = Image.open(p).convert("RGB")
        im.thumbnail((W, H))
        x, y = (i % cols) * W, 30 + (i // cols) * (H + 20)
        sheet.paste(im, (x, y))
        d.text((x + 4, y + H + 3), os.path.basename(p)[:60], fill=(200, 200, 200))
    return sheet


for scene_dir in sorted(glob.glob(os.path.join(root, "Logs", "Blockout", "shots", "Scene*"))):
    scene = os.path.basename(scene_dir)
    sid = scene[:7]  # Scene05
    shots = sorted(glob.glob(os.path.join(scene_dir, "*.png")))
    if not shots:
        continue
    scene_refs = [r for r in refs if os.path.basename(r).startswith(sid)]
    left = tile(scene_refs, 1, f"REFERENCE {sid}") if scene_refs else None
    right = tile(shots, 3, f"CURRENT {scene} ({len(shots)} beats)")
    if left:
        h = max(left.height, right.height)
        combo = Image.new("RGB", (left.width + right.width + 16, h), (24, 26, 32))
        combo.paste(left, (0, 0))
        combo.paste(right, (left.width + 16, 0))
    else:
        combo = right
    path = os.path.join(out_dir, f"{scene}.png")
    combo.save(path)
    print(path, combo.size)
