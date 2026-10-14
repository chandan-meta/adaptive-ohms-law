"""Pair the before_*.png frames in Reports/OhmsLawAudit/Before_After with the same beat from an after run and write
side-by-side comparison JPGs (before left, after right, labelled).

  python Tools/before_after.py [after-run-dir]   default Reports/OhmsLawAudit/after/Shots
"""
import os, sys
from PIL import Image, ImageDraw

root = os.path.join("Reports", "OhmsLawAudit")
pairs_dir = os.path.join(root, "Before_After")
after_shots = sys.argv[1] if len(sys.argv) > 1 else os.path.join(root, "after", "Shots")
# before_S10_07_gate_S10_Predict3V.png -> Scene10_*/07_gate_S10_Predict3V.png ; the harness numbers beats per run, so
# match on the beat name after the two-digit index (a new beat shifts the indices).
scenes = {d[5:7]: d for d in os.listdir(after_shots)} if os.path.isdir(after_shots) else {}
made = 0
for name in sorted(os.listdir(pairs_dir)):
    if not name.startswith("before_") or not name.endswith(".png"):
        continue
    tag = name[len("before_"):-4]              # S10_07_gate_S10_Predict3V
    scene_no, rest = tag[1:3], tag[4:]         # 10, 07_gate_S10_Predict3V
    beat = rest[3:]                            # gate_S10_Predict3V
    folder = scenes.get(scene_no)
    if not folder:
        continue
    cands = [f for f in os.listdir(os.path.join(after_shots, folder)) if f[3:-4] == beat]
    if not cands:
        print("no after frame for", tag)
        continue
    after_path = os.path.join(after_shots, folder, sorted(cands)[0])
    a = Image.open(os.path.join(pairs_dir, name)).convert("RGB").resize((960, 540), Image.LANCZOS)
    b = Image.open(after_path).convert("RGB").resize((960, 540), Image.LANCZOS)
    sheet = Image.new("RGB", (1920 + 8, 540 + 28), (10, 12, 18))
    sheet.paste(a, (0, 28)); sheet.paste(b, (968, 28))
    d = ImageDraw.Draw(sheet)
    d.text((8, 7), f"BEFORE  {tag}", fill=(255, 196, 107)); d.text((976, 7), f"AFTER  {tag}", fill=(127, 233, 255))
    out = os.path.join(pairs_dir, f"compare_{tag}.jpg")
    sheet.save(out, quality=88)
    b.save(os.path.join(pairs_dir, f"after_{tag}.png"))
    made += 1
print(f"{made} comparisons written to {pairs_dir}")
