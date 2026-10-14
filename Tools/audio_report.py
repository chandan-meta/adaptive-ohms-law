"""Per-scene audio analysis of a recorded play test: loudness, true peak, silence gaps and the music/voice balance.

  python Tools/audio_report.py <run>      run = before | after … (Reports/OhmsLawAudit/<run>/ with Gameplay_Full_Run.mp4
                                           and scene_times.txt); prints a markdown table and writes audio_report.md
Balance proxy: the bed level is the loudness of the quietest 20 % of 400 ms windows in the scene (between lines, where
only music/ambience/loops play); the voice level is the loudest 20 %. Their difference is the effective duck depth.
"""
import os, re, subprocess, sys, wave
import numpy as np

run = sys.argv[1] if len(sys.argv) > 1 else "after"
folder = os.path.join("Reports", "OhmsLawAudit", run)
video = os.path.join(folder, "Gameplay_Full_Run.mp4")
wav = os.path.join(folder, "_audio.wav")
subprocess.run(["ffmpeg", "-y", "-hide_banner", "-loglevel", "error", "-i", video, "-vn", "-ac", "1", "-ar", "48000", wav])
starts = [(float(l.split()[0]), l.split()[1]) for l in open(os.path.join(folder, "scene_times.txt")) if l.strip() and not l.startswith("#")]
w = wave.open(wav); sr = w.getframerate(); x = np.frombuffer(w.readframes(w.getnframes()), dtype=np.int16).astype(np.float32) / 32768
total = len(x) / sr
rows = []


def db(v):
    return 20 * np.log10(max(v, 1e-9))


def lufs(a, b):
    r = subprocess.run(["ffmpeg", "-hide_banner", "-nostats", "-ss", f"{a:.2f}", "-t", f"{b - a:.2f}", "-i", video, "-vn", "-af", "ebur128=peak=true", "-f", "null", "-"], capture_output=True, text=True).stderr
    I = re.findall(r"I:\s+(-?[\d.]+) LUFS", r); P = re.findall(r"Peak:\s+(-?[\d.]+) dBFS", r)
    return (float(I[-1]) if I else float("nan")), (float(P[-1]) if P else float("nan"))


for i, (t0, name) in enumerate(starts):
    t1 = starts[i + 1][0] if i + 1 < len(starts) else total
    seg = x[int(t0 * sr): int(t1 * sr)]
    win = int(0.4 * sr)
    rms = np.array([np.sqrt((seg[j:j + win] ** 2).mean()) for j in range(0, len(seg) - win, win)])
    rms_db = np.array([db(v) for v in rms])
    quiet = np.percentile(rms_db, 20); loud = np.percentile(rms_db, 80)
    gaps = (rms_db < -45).sum() * 0.4  # seconds of near-silence (both voice and bed absent)
    I, P = lufs(t0, t1)
    rows.append((name, t0, t1 - t0, I, P, loud, quiet, loud - quiet, gaps))

lines = ["| Scene | start | length | integrated LUFS | true peak dBFS | voice level (80th pct RMS dB) | bed level (20th pct) | voice−bed dB | near-silence s |",
         "|---|---|---|---|---|---|---|---|---|"]
for r in rows:
    lines.append("| %s | %d:%02d | %.0f s | %.1f | %.1f | %.1f | %.1f | %.1f | %.1f |" % (r[0], r[1] // 60, r[1] % 60, r[2], r[3], r[4], r[5], r[6], r[7], r[8]))
text = "\n".join(lines)
print(text)
open(os.path.join(folder, "audio_report.md"), "w", encoding="utf-8").write(f"# Audio analysis — {run}\n\n{text}\n")
os.remove(wav)
