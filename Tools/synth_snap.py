"""Synthesizes Wattson's finger snap: a crisp click with a short electric sparkle tail (he is made of electricity).
Usage: python Tools/synth_snap.py [out.wav]   (default Assets/OhmsLaw/Audio/SFX/Character/SFX_Wattson_Snap.wav)
Mono 44.1 kHz 16-bit, ~0.25 s, peak normalised to -3 dBFS. Pure Python (wave, math, random): no dependencies. The result
is self-made and released CC0 (Assets/OhmsLaw/Audio/Licenses/Self_Made_CC0.txt)."""
import math
import os
import random
import struct
import sys
import wave

RATE, SECONDS, PEAK_DB = 44100, 0.25, -3.0


def bandpass(signal, centre, q):
    """RBJ biquad band-pass (constant 0 dB peak gain)."""
    w = 2 * math.pi * centre / RATE
    alpha = math.sin(w) / (2 * q)
    b0, b1, b2 = alpha, 0.0, -alpha
    a0, a1, a2 = 1 + alpha, -2 * math.cos(w), 1 - alpha
    b0, b1, b2, a1, a2 = b0 / a0, b1 / a0, b2 / a0, a1 / a0, a2 / a0
    out, x1, x2, y1, y2 = [], 0.0, 0.0, 0.0, 0.0
    for x in signal:
        y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2
        x2, x1, y2, y1 = x1, x, y1, y
        out.append(y)
    return out


def highpass(signal, cutoff):
    """One-pole high-pass."""
    rc = 1 / (2 * math.pi * cutoff)
    k = rc / (rc + 1 / RATE)
    out, prev_x, prev_y = [], 0.0, 0.0
    for x in signal:
        y = k * (prev_y + x - prev_x)
        prev_x, prev_y = x, y
        out.append(y)
    return out


def main():
    out_path = sys.argv[1] if len(sys.argv) > 1 else os.path.join("Assets", "OhmsLaw", "Audio", "SFX", "Character", "SFX_Wattson_Snap.wav")
    rng = random.Random(11)
    n = int(RATE * SECONDS)
    t = [i / RATE for i in range(n)]
    onset = int(0.004 * RATE)  # a few samples of silence so the attack is not clipped by a player's start

    # The snap: a noise burst decaying in ~6 ms through two resonances (the finger hitting the palm ~2.6 kHz, the skin
    # "pop" ~1.1 kHz), plus a very short low thump.
    burst = [0.0] * n
    for i in range(onset, n):
        dt = (i - onset) / RATE
        burst[i] = rng.uniform(-1, 1) * math.exp(-dt / 0.006)
    crack = [a * 1.0 + b * 0.55 for a, b in zip(bandpass(burst, 2600, 1.6), bandpass(burst, 1100, 1.2))]
    thump = [0.0] * n
    for i in range(onset, n):
        dt = (i - onset) / RATE
        thump[i] = math.sin(2 * math.pi * 420 * dt) * math.exp(-dt / 0.012) * 0.35

    # The sparkle: sparse tiny crackles, high-passed, thinning out over ~0.2 s, and a faint "zing" that rises a little.
    crackle = [0.0] * n
    for i in range(onset + int(0.012 * RATE), n):
        dt = (i - onset) / RATE
        if rng.random() < 0.012 * math.exp(-dt / 0.07):
            amp = rng.uniform(0.25, 0.6) * math.exp(-dt / 0.09)
            for k in range(min(40, n - i)):
                crackle[i + k] += amp * rng.uniform(-1, 1) * math.exp(-k / 6.0)
    crackle = highpass(crackle, 3500)
    zing = [0.0] * n
    phase = 0.0
    for i in range(onset, n):
        dt = (i - onset) / RATE
        phase += 2 * math.pi * (3800 + 900 * min(1.0, dt / 0.15)) / RATE
        zing[i] = math.sin(phase) * 0.06 * (1 - math.exp(-dt / 0.01)) * math.exp(-dt / 0.06)

    mix = [c + h + s * 0.5 + z for c, h, s, z in zip(crack, thump, crackle, zing)]
    # A short fade at the very end so the tail never clicks off.
    fade = int(0.02 * RATE)
    for k in range(fade):
        mix[n - 1 - k] *= k / fade
    peak = max(abs(x) for x in mix) or 1.0
    gain = 10 ** (PEAK_DB / 20) / peak
    data = b"".join(struct.pack("<h", int(max(-1, min(1, x * gain)) * 32767)) for x in mix)

    os.makedirs(os.path.dirname(out_path) or ".", exist_ok=True)
    with wave.open(out_path, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(data)
    print(f"wrote {out_path}: {SECONDS:.2f} s, peak {PEAK_DB:.1f} dBFS")


if __name__ == "__main__":
    main()
