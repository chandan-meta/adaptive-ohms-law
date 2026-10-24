"""Key gameplay frames of the last play test, one per stage of the module, as numbered JPGs plus one overview sheet.
Usage: python Tools/stage_snapshots.py [outdir = Reports/Revision_2026-10-22/Snapshots] [shots dir = Logs/Blockout/shots]
A stage whose frame the run did not shoot is listed as missing and skipped."""
import glob
import os
import sys

from PIL import Image, ImageDraw, ImageFont

out = sys.argv[1] if len(sys.argv) > 1 else "Reports/Revision_2026-10-22/Snapshots"
root = sys.argv[2] if len(sys.argv) > 2 else "Logs/Blockout/shots"

# (scene folder prefix, frame name pattern after the "NN_" index, caption)
STAGES = [
    ("Scene04", "title_screen", "Title screen"),
    ("Scene04", "gate_S04_TapWire", "Tap the wire: the music plays on through the tap"),
    ("Scene05", "gate_S05_ApplyVoltage", "Inside the wire: tap + to apply the voltage"),
    ("Scene05", "S05_V09", "Current I: charge through this area per second"),
    ("Scene06", "gate_S06_Prediction", "Detective Test, right after V and I"),
    ("Scene06", "hint_S06_Prediction", "A wrong pick: option out, targeted hint, retry"),
    ("Scene06", "S06_R_A", "Right answer: brief confirmation, move on"),
    ("Scene09", "gate_S09_PlaceAmmeter", "One tap places the ammeter (in series)"),
    ("Scene09", "S09_L08", "Both meters in: the circuit completes itself"),
    ("Scene10", "gate_S10_Set3V", "The experiment: slider, live readings, rows and points"),
    ("Scene10", "at_link_0_1", "Data centre stage: rows linked to their points"),
    ("Scene10", "at_hero_V", "Double the voltage, double the current"),
    ("Scene11", "at_graph.axis_V", "Axes named and lit: V up the side"),
    ("Scene11", "at_link_0_1", "Wattson points at the doubled point"),
    ("Scene12", "at_show_Wire_Sample", "Copper vs nichrome: same length, thickness, temperature, voltage"),
    ("Scene12", "hint_S12_MoreResistance", "Second challenge (R up at fixed V): hint after a miss"),
    ("Scene12", "at_table.ratio_1", "V / I fills row by row: 5 V/A every time"),
    ("Scene12", "at_graph.triangle", "Slope = ΔV ÷ ΔI = R"),
    ("Scene13", "at_law.piece_R", "The three clues arrive on Wattson's snap"),
    ("Scene13", "gate_S13_BuildLaw", "Tap V, I, R to build the law"),
    ("Scene13", "at_law.name", "OHM'S LAW"),
    ("Scene13", "at_law.forms", "Conditions and the rearrangements"),
    ("Scene13", "at_law.demo", "The bench sweeps 1-5 V: the current rises in step"),
    ("Scene13", "end_card", "Case closed: Explore Now"),
    ("Scene13", "explore_open", "The Ohm's law lab"),
    ("Scene13", "explore_unlocked", "Lab: R = V ÷ I checked, second wire unlocked"),
]

os.makedirs(out, exist_ok=True)
for old in glob.glob(os.path.join(out, "*.jpg")):
    os.remove(old)
picked = []
for n, (scene, pattern, caption) in enumerate(STAGES, 1):
    folders = [d for d in glob.glob(os.path.join(root, scene + "_*")) if os.path.isdir(d)]
    files = sorted(f for d in folders for f in glob.glob(os.path.join(d, "*.png")) if os.path.basename(f)[3:].startswith(pattern))
    if not files:
        print(f"missing: {scene} {pattern}")
        continue
    name = f"{n:02d}_{scene}_{pattern.replace('.', '_')}.jpg"
    Image.open(files[0]).convert("RGB").save(os.path.join(out, name), quality=86)
    picked.append((os.path.join(out, name), f"{n:02d}  {caption}"))
    print(name, "<-", os.path.basename(files[0]))

# The overview: every picked stage, captioned, 4 across.
W, H, COLS, BAR = 480, 270, 4, 26
rows = (len(picked) + COLS - 1) // COLS
sheet = Image.new("RGB", (COLS * W, rows * (H + BAR)), (16, 20, 28))
draw = ImageDraw.Draw(sheet)
try:
    font = ImageFont.truetype("arial.ttf", 15)
except OSError:
    font = ImageFont.load_default()
for k, (path, caption) in enumerate(picked):
    x, y = (k % COLS) * W, (k // COLS) * (H + BAR)
    sheet.paste(Image.open(path).resize((W, H)), (x, y + BAR))
    draw.text((x + 6, y + 5), caption[:62], fill=(235, 240, 250), font=font)
sheet.save(os.path.join(out, "00_overview.jpg"), quality=84)
print(len(picked), "stages ->", out)
