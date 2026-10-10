"""Contact sheets of the last play test: one JPG per scene (up to 12 evenly spaced frames, 480x270 each) for a quick review.
Usage: python Tools/play_sheets.py <outdir> [shots dir = Logs/Blockout/shots]"""
import glob
import os
import sys

from PIL import Image, ImageDraw

out = sys.argv[1]
root = sys.argv[2] if len(sys.argv) > 2 else "Logs/Blockout/shots"
os.makedirs(out, exist_ok=True)
W, H, COLS = 480, 270, 3
for scene in sorted(os.listdir(root)):
    files = sorted(glob.glob(os.path.join(root, scene, "*.png")))
    if not files:
        continue
    n = min(12, len(files))
    picks = [files[round(i * (len(files) - 1) / (n - 1))] if n > 1 else files[0] for i in range(n)]
    rows = (len(picks) + COLS - 1) // COLS
    sheet = Image.new("RGB", (COLS * W, rows * (H + 18)), (20, 20, 20))
    draw = ImageDraw.Draw(sheet)
    for k, f in enumerate(picks):
        x, y = (k % COLS) * W, (k // COLS) * (H + 18)
        sheet.paste(Image.open(f).convert("RGB").resize((W, H)), (x, y + 18))
        draw.text((x + 4, y + 2), os.path.basename(f)[:60], fill=(255, 255, 255))
    sheet.save(os.path.join(out, scene + ".jpg"), quality=80)
    print(scene, len(files), "frames ->", len(picks))
