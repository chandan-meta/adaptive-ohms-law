"""Turn a recorded play test (Logs/Blockout/recording/video.mp4 + audio.wav + play-report.txt) into audit evidence.

  python Tools/audit_frames.py <run-name> [--src Logs/Blockout]   e.g. before / after

Writes Reports/OhmsLawAudit/<run>/
  Gameplay_Full_Run.mp4     720p mux of video + audio (h264/aac)
  Captures/<Scene>/         1 fps frames of the whole run, named by video time, assigned to scenes by the fades
  Cinematic_Frames/         10 fps frames around every scene boundary (cuts, fades, dives)
  Shots/<Scene>/            the harness' own per-beat PNGs (copied)
  Frame_Index.csv           one row per Capture / Shot: timecode, scene, frame (the findings table is ../Frame_Audit.csv)
  scene_times.txt           scene start times in the video (seconds)
ffmpeg is on PATH; frames go out as JPEG q=3 to keep the folder small.
"""
import csv, os, re, shutil, subprocess, sys

run = sys.argv[1] if len(sys.argv) > 1 else "before"
src = sys.argv[sys.argv.index("--src") + 1] if "--src" in sys.argv else "Logs/Blockout"
out = os.path.join("Reports", "OhmsLawAudit", run)
rec = os.path.join(src, "recording")
video, audio = os.path.join(rec, "video.mp4"), os.path.join(rec, "audio.wav")
report = os.path.join(src, "play-report.txt")
os.makedirs(out, exist_ok=True)


def sh(*args):
    return subprocess.run(args, capture_output=True, text=True)


def duration(path):
    r = sh("ffprobe", "-v", "error", "-show_entries", "format=duration", "-of", "csv=p=0", path)
    return float(r.stdout.strip() or 0)


# 1. scenes in play order, from the report
scenes = re.findall(r"^=== (\S+) ===", open(report, encoding="utf-8", errors="replace").read(), re.M)

# 2. the review video (720p, with audio)
full = os.path.join(out, "Gameplay_Full_Run.mp4")
if not os.path.exists(full):
    sh("ffmpeg", "-y", "-hide_banner", "-loglevel", "error", "-i", video, "-i", audio, "-vf", "scale=1280:720",
       "-c:v", "libx264", "-preset", "fast", "-crf", "20", "-c:a", "aac", "-b:a", "160k", "-shortest", full)

# 3. 1 fps captures
tmp = os.path.join(out, "_tmp")
shutil.rmtree(tmp, ignore_errors=True)
os.makedirs(tmp)
sh("ffmpeg", "-y", "-hide_banner", "-loglevel", "error", "-i", video, "-vf", "fps=1,scale=1280:720", "-q:v", "3",
   os.path.join(tmp, "%05d.jpg"))
names = sorted(os.listdir(tmp))

# 4. scene boundaries: each scene's first harness shot (01_enter.png) is matched against the 1 fps frames, in order,
#    searching forward from the previous boundary; the best-matching second is that scene's start in the video.
from PIL import Image, ImageChops, ImageStat


def thumb(path):
    return Image.open(path).convert("L").resize((64, 36), Image.BILINEAR)


def diff(a, b):
    return ImageStat.Stat(ImageChops.difference(a, b)).mean[0]


thumbs = [thumb(os.path.join(tmp, n)) for n in names]
shots = os.path.join(src, "shots")
# the report's last timed event of each scene (a line or a gate, scene-relative seconds) bounds the search window:
# the next scene starts after that event, within ~45 s (the last line, a hand-off and a gate wait)
text = open(report, encoding="utf-8", errors="replace").read()
last_event, gates = {}, {}
for block in re.split(r"^=== ", text, flags=re.M)[1:]:
    scene = block.split(" ===")[0]
    times = [float(m) for m in re.findall(r"^\s*(?:PASS\s+)?([\d.]+)s\s", block, re.M)]
    last_event[scene] = max(times) if times else 0.0
    gates[scene] = len(re.findall(r"^\s*[\d.]+s\s+GATE", block, re.M))  # Timeline time stops at a gate; autoAnswer ≈ 3.2 s
starts = [0.0]
for prev, scene in zip(scenes, scenes[1:]):
    enter = os.path.join(shots, scene, "01_enter.png")
    lo = int(starts[-1] + last_event.get(prev, 0.0) + 3.2 * gates.get(prev, 0) + 2)
    hi = min(len(thumbs), lo + 30)
    if not os.path.exists(enter) or lo >= len(thumbs):
        starts.append(float(lo))
        continue
    e = thumb(enter)
    diffs = {i: diff(e, thumbs[i]) for i in range(lo, hi)}
    floor = min(diffs.values())
    best = min(i for i, v in diffs.items() if v <= floor + 1.0)  # the earliest frame that matches as well as the best
    starts.append(float(best))
with open(os.path.join(out, "scene_times.txt"), "w") as f:
    for s, t in zip(scenes, starts):
        f.write(f"{t:8.2f}  {s}\n")


def scene_at(t):
    name = scenes[0]
    for s, st in zip(scenes, starts):
        if t >= st - 0.01:
            name = s
    return name


rows = []
for name in names:
    t = int(name[:5]) - 1
    scene = scene_at(t)
    d = os.path.join(out, "Captures", scene)
    os.makedirs(d, exist_ok=True)
    dst = os.path.join(d, f"{t // 60:02d}m{t % 60:02d}s.jpg")
    shutil.move(os.path.join(tmp, name), dst)
    rows.append((f"{t // 60:02d}:{t % 60:02d}", scene, os.path.relpath(dst, out)))
shutil.rmtree(tmp)

# 4b. a contact sheet per scene of the 1 fps captures (6 across, 320x180 each, time stamped) for the frame audit
from PIL import ImageDraw
for scene in scenes:
    d = os.path.join(out, "Captures", scene)
    if not os.path.isdir(d):
        continue
    files = sorted(n for n in os.listdir(d) if n.endswith(".jpg"))
    cols, w, h = 6, 320, 180
    rows_n = (len(files) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * w, rows_n * (h + 16)), (10, 12, 18))
    draw = ImageDraw.Draw(sheet)
    for i, n in enumerate(files):
        im = Image.open(os.path.join(d, n)).resize((w, h), Image.BILINEAR)
        x, y = (i % cols) * w, (i // cols) * (h + 16)
        sheet.paste(im, (x, y + 16))
        draw.text((x + 4, y + 2), n[:-4], fill=(230, 230, 230))
    sheet.save(os.path.join(out, "Captures", f"{scene}_sheet.jpg"), quality=80)

# 5. 10 fps around every boundary (3 s before, 4 s after) and the first 20 s (title + intro)
cin = os.path.join(out, "Cinematic_Frames")
os.makedirs(cin, exist_ok=True)
windows = [(0, 20)] + [(max(0, st - 3), st + 4) for st in starts[1:]]
for a, b in windows:
    sh("ffmpeg", "-y", "-hide_banner", "-loglevel", "error", "-ss", f"{a:.2f}", "-t", f"{b - a:.2f}", "-i", video,
       "-vf", "fps=10,scale=960:540", "-q:v", "4", os.path.join(cin, f"t{int(a):04d}_%03d.jpg"))
    files = sorted(n for n in os.listdir(cin) if n.startswith(f"t{int(a):04d}_"))
    cols, w, h = 10, 192, 108
    sheet = Image.new("RGB", (cols * w, ((len(files) + cols - 1) // cols) * (h + 14)), (10, 12, 18))
    draw = ImageDraw.Draw(sheet)
    for i, n in enumerate(files):
        x, y = (i % cols) * w, (i // cols) * (h + 14)
        sheet.paste(Image.open(os.path.join(cin, n)).resize((w, h), Image.BILINEAR), (x, y + 14))
        draw.text((x + 3, y + 1), f"{a + i / 10:.1f}s", fill=(230, 230, 230))
    sheet.save(os.path.join(cin, f"sheet_t{int(a):04d}.jpg"), quality=80)

# 6. the harness' per-beat shots
shots = os.path.join(src, "shots")
if os.path.isdir(shots):
    shutil.copytree(shots, os.path.join(out, "Shots"), dirs_exist_ok=True)
    for scene in sorted(os.listdir(shots)):
        for name in sorted(os.listdir(os.path.join(shots, scene))):
            rows.append(("beat", scene, os.path.join("Shots", scene, name)))

with open(os.path.join(out, "Frame_Index.csv"), "w", newline="", encoding="utf-8") as f:
    w = csv.writer(f)
    w.writerow(["Timecode", "Scene", "Frame", "Problem", "Severity", "Suggested Improvement", "Files to Modify", "Status"])
    for r in rows:
        w.writerow(list(r) + ["", "", "", "", ""])
print(f"{out}: {len(rows)} rows, scenes at {[round(s) for s in starts]}, video {duration(video):.0f} s")
