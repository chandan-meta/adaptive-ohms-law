using System;
using System.Collections.Generic;
using System.Linq;

namespace OhmsLaw.Editor
{
    // The module's sound bank, synthesized at build time: 44.1 kHz mono floats, peaks <= 0.8, deterministic (fixed seeds).
    // A restrained educational-lab palette: soft transients, bell and marimba pips, warm pads, seamless ambience loops.
    // Loop rule: every partial completes whole cycles and sits in cosine phase, noise beds are palindromes, so the seam
    // (and each loop's midpoint) is a smooth time reversal rather than a cut.
    public static class SfxBank
    {
        public const int Hz = 44100;
        const float Tau = (float)(Math.PI * 2);
        public static readonly string[] Loops = { "WireHum", "RoomTone", "PadLab", "PadWire", "StudyTone", "PadIntro", "PadLabTheme", "CircuitHum" };

        public static Dictionary<string, Func<float[]>> Recipes() => new Dictionary<string, Func<float[]>>
        {
            // ------------------------------------------------------------ UI and apparatus
            ["Tap"] = () => Make(0.08f, 0.6f, 0.02f, b =>
            {
                Add(b, t => 170 - 1100 * t, t => Env(t, 0.002f, 0.022f));                                                 // low thud, 170 -> 85 Hz
                Tone(b, 900, t => 0.25f * Env(t, 0.001f, 0.008f)); Click(b, 0, 0.002f, 4000, 0.5f, 1);                   // tiny tick + click
            }),
            ["Select"] = () => Make(0.18f, 0.6f, 0.03f, b =>
            {
                foreach (var (f, at) in new[] { (660f, 0f), (880f, 0.08f) })
                { Tone(b, f, t => 0.5f * Env(t - at, 0.006f, 0.045f)); Tone(b, 2 * f, t => 0.08f * Env(t - at, 0.006f, 0.03f)); }
                Verb(b, 0.15f, 0.5f);
            }),
            ["Place"] = () => Make(0.35f, 0.7f, 0.05f, b =>
            {
                Add(b, t => 115 - 150 * Math.Min(t, 0.2f), t => Env(t, 0.004f, 0.08f)); Saturate(b, 2f);                  // thump, 115 -> 85 Hz
                Tone(b, 3150, t => 0.07f * Env(t, 0.0005f, 0.018f)); Tone(b, 4870, t => 0.05f * Env(t, 0.0005f, 0.012f)); // metallic tick
                Click(b, 0, 0.002f, 6000, 0.3f, 2); Verb(b, 0.12f, 0.5f);
            }),
            ["Connect"] = () => Make(0.25f, 0.6f, 0.03f, b =>
            {
                Whoosh(b, t => 500 * (float)Math.Pow(9, t / 0.17f), t => 0.5f * (t < 0.17f ? Smooth(t / 0.02f) : Smooth(1 - (t - 0.17f) / 0.01f)), 0.5f, 3);   // zip, 500 -> 4500 Hz
                Click(b, 0.17f, 0.004f, 5000, 0.5f, 4); Tone(b, 1900, t => 0.3f * Env(t - 0.17f, 0.0005f, 0.012f)); Tone(b, 260, t => 0.35f * Env(t - 0.17f, 0.001f, 0.02f));
            }),
            ["Switch"] = () => Make(0.22f, 0.7f, 0.04f, b =>
            {
                Knock(b, 0, 900, 0.35f, 0.006f, 1.2f, 5); Tone(b, 380, t => 0.5f * Env(t, 0.001f, 0.02f));                 // wooden clack
                foreach (float f in new[] { 2350f, 3720f }) Tone(b, f, t => 0.12f * Env(t - 0.04f, 0.0005f, 0.022f));       // metal catch 40 ms later
                Click(b, 0.04f, 0.003f, 7000, 0.6f, 6); Tone(b, 180, t => 0.4f * Env(t - 0.04f, 0.002f, 0.03f)); Verb(b, 0.12f, 0.45f);
            }),
            ["VoltStep"] = () => Make(0.3f, 0.65f, 0.06f, b =>
            {
                Tone(b, 880, t => 0.5f * Env(t, 0.012f, 0.09f)); Tone(b, 880, t => 0.3f * Env(t, 0.012f, 0.07f), Tri); Tone(b, 1760, t => 0.12f * Env(t, 0.012f, 0.05f));
                LowPass(b, _ => 6000); Verb(b, 0.15f, 0.5f);
            }),
            ["Count"] = () => Make(0.07f, 0.35f, 0.02f, b => { Knock(b, 0, 1600, 0.25f, 0.004f, 1, 12); Tone(b, 1150, t => 0.4f * Env(t, 0.0005f, 0.012f)); }),
            // ------------------------------------------------------------ measurement and discovery
            ["Record"] = () => Make(0.6f, 0.65f, 0.15f, b =>
            {
                foreach (var (f, at) in new[] { (523.25f, 0f), (659.26f, 0.14f) })                                            // C5 -> E5
                    foreach (var (h, a, d) in new[] { (1f, 0.5f, 0.22f), (2f, 0.18f, 0.12f), (3f, 0.06f, 0.08f) }) Tone(b, f * h, t => a * Env(t - at, 0.008f, d));
                Verb(b, 0.35f, 0.65f);
            }),
            ["Plot"] = () => Make(0.45f, 0.65f, 0.1f, b =>
            {
                Tone(b, 783.99f, t => 0.55f * Env(t, 0.002f, 0.11f)); Tone(b, 1567.98f, t => 0.18f * Env(t, 0.002f, 0.045f)); Tone(b, 3135.96f, t => 0.05f * Env(t, 0.001f, 0.02f));
                Click(b, 0, 0.0015f, 3500, 0.2f, 7); Verb(b, 0.2f, 0.55f);                                                     // mallet, room
            }),
            ["Highlight"] = () => Make(0.5f, 0.5f, 0.15f, b =>
            {
                foreach (var (f, at) in new[] { (2093f, 0f), (2637.02f, 0.06f) }) Pair(b, f, 0.002f, t => 0.3f * Env(t - at, 0.004f, 0.14f));   // C7, E7
                Verb(b, 0.3f, 0.65f);
            }),
            ["Rise"] = () => Make(1.8f, 0.7f, 0.1f, b =>
            {
                float[] notes = { 261.63f, 329.63f, 392f, 523.25f, 659.26f };                                                   // C4 E4 G4 C5 E5
                for (int i = 0; i < notes.Length; i++)
                { float f = notes[i], at = 0.18f * i; Tone(b, f, t => 0.32f * Env(t - at, 0.012f, 0.9f)); Tone(b, 2 * f, t => 0.07f * Env(t - at, 0.012f, 0.25f)); }
                foreach (var (f, a) in new[] { (130.81f, 0.16f), (196f, 0.1f), (261.63f, 0.08f) }) Pair(b, f, 0.002f, t => a * Smooth(t / 0.5f));   // soft pad under
                Verb(b, 0.3f, 0.65f); Mul(b, t => Smooth((1.8f - t) / 0.5f));                                                  // the held chord fades
            }),
            ["Reveal"] = () => Make(0.9f, 0.55f, 0.2f, b =>
            {
                float[] notes = { 1318.51f, 1567.98f, 2093f };                                                                  // E6 G6 C7, slightly detuned
                for (int i = 0; i < notes.Length; i++) { float at = 0.07f * i; Pair(b, notes[i], 0.003f, t => 0.3f * Env(t - at, 0.004f, 0.28f)); }
                Tone(b, 4186f, t => 0.06f * Env(t - 0.14f, 0.004f, 0.15f)); Verb(b, 0.35f, 0.7f);
            }),
            ["Omega"] = () => Make(2.2f, 0.6f, 0.4f, b =>
            {
                const float f = 523.25f;
                Tone(b, f, t => 0.45f * Env(t, 0.006f, 0.8f)); Tone(b, f * 1.0025f, t => 0.3f * Env(t, 0.006f, 0.7f));        // slow beat = warmth
                Tone(b, f / 2, t => 0.22f * Env(t, 0.01f, 1.1f)); Tone(b, f * 2.76f, t => 0.22f * Env(t, 0.003f, 0.35f)); Tone(b, f * 5.4f, t => 0.08f * Env(t, 0.002f, 0.18f));
                Verb(b, 0.25f, 0.7f);
            }),
            ["Finale"] = () => Make(4.5f, 0.65f, 0.5f, b =>
            {
                float Swell(float t) => Adsr(t, 4.5f, 1.8f, 1.2f, 0.75f, 1.5f);
                foreach (var (f, a) in new[] { (130.81f, 0.3f), (196f, 0.22f), (261.63f, 0.2f) })                                // root, 5th, octave
                { Pair(b, f, 0.0025f, t => a * Swell(t)); Pair(b, f, 0.0025f, t => 0.25f * a * Swell(t), Tri); }
                Pair(b, 349.23f, 0.0025f, t => 0.16f * Swell(t) * Smooth((2.7f - t) / 0.6f));                                   // suspended 4th...
                Pair(b, 329.63f, 0.0025f, t => 0.18f * Swell(t) * Smooth((t - 2.3f) / 0.6f));                                   // ...resolving onto the 10th
                LowPass(b, t => 900 + 1800 * Swell(t)); Verb(b, 0.45f, 0.78f);
            }),
            // ------------------------------------------------------------ transitions
            ["Transform"] = () => Make(2f, 0.6f, 0.15f, b =>
            {
                float Bell(float t) => (float)Math.Pow(Math.Sin(Math.PI * t / 2), 2);
                foreach (float f in new[] { 1046.5f, 1318.51f, 1567.98f, 2093f }) Pair(b, f, 0.004f, t => 0.09f * Bell(t) * (0.6f + 0.4f * Sin(6 * t)));   // 6 Hz shimmer
                Whoosh(b, t => 250 * (float)Math.Pow(14, Math.Sin(Math.PI * t / 2)), t => 0.5f * Bell(t), 0.6f, 8); Verb(b, 0.25f, 0.65f);   // 250 -> 3500 -> 250 Hz
            }),
            ["LabSweep"] = () => Make(3f, 0.65f, 0.3f, b =>
            {
                Whoosh(b, t => 120 * (float)Math.Pow(40, t / 3), t => 0.55f * Adsr(t, 3f, 1.2f, 0.9f, 0.8f, 0.9f), 0.7f, 9);      // 120 -> 4800 Hz
                foreach (var (f, a) in new[] { (261.63f, 0.14f), (329.63f, 0.11f), (392f, 0.1f), (523.25f, 0.07f) })
                    Pair(b, f, 0.003f, t => a * Smooth((t - 1.8f) / 0.35f) * Smooth((3 - t) / 0.7f));                           // chord bloom at 60 %
                Verb(b, 0.3f, 0.7f);
            }),
            ["Enter"] = () => Make(1.6f, 0.65f, 0.05f, b =>
            {
                Whoosh(b, t => 3500 * (float)Math.Pow(0.06, t / 1.1f), t => 0.5f * Smooth(t / 0.08f) * Smooth((1.25f - t) / 0.5f), 0.6f, 10);
                Add(b, t => 880 * (float)Math.Pow(1 / 16f, Math.Min(t, 1.1f) / 1.1f), t => 0.3f * Smooth(t / 0.05f) * Smooth((1.2f - t) / 0.4f));   // scale change, 880 -> 55 Hz
                Hum(b, t => Smooth((t - 0.7f) / 0.5f)); Mul(b, t => Smooth((1.6f - t) / 0.25f));                               // ducks into the wire's hum
            }),
            ["Exit"] = () => Make(1.4f, 0.65f, 0.2f, b =>
            {
                Hum(b, t => Smooth((0.7f - t) / 0.5f));                                                                        // the hum lets go
                Add(b, t => 55 * (float)Math.Pow(16, Math.Min(t, 1f)), t => 0.28f * Smooth(t / 0.05f) * Smooth((1.2f - t) / 0.3f));   // 55 -> 880 Hz
                Whoosh(b, t => 200 * (float)Math.Pow(20, t / 1.2f), t => 0.5f * Smooth(t / 0.3f) * Smooth((1.4f - t) / 0.4f), 0.6f, 11); Verb(b, 0.3f, 0.7f);   // opening up
            }),
            // ------------------------------------------------------------ loops (seamless)
            ["WireHum"] = () =>
            {
                var b = Bed(8f, 13, 220, 0.3f);
                foreach (var (f, a) in new[] { (55f, 1f), (110f, 0.5f), (165f, 0.15f), (220f, 0.06f) }) Loop(b, f, t => a * (0.75f + 0.25f * Cos(0.25f * t)));   // 0.25 Hz wobble, 2 per loop
                return Level(b, 0.5f);
            },
            ["RoomTone"] = () =>
            {
                var b = Bed(12f, 14, 300, 0.2f);
                foreach (var (f, a) in new[] { (100f, 0.05f), (200f, 0.02f), (300f, 0.008f) }) Loop(b, f, _ => a);           // faint fluorescent hum
                return Level(b, 0.2f);
            },
            ["StudyTone"] = () =>
            {
                var b = Bed(12f, 15, 250, 0.08f);
                var rnd = new Random(16);
                for (float at = 0.1f; at < 11.6f; at += 0.05f + 0.6f * (float)rnd.NextDouble())                                // sparse fire pops, none near the seam
                    Click(b, at, 0.004f + 0.02f * (float)rnd.NextDouble(), 900 + 2500 * (float)rnd.NextDouble(), 0.15f + 0.35f * (float)rnd.NextDouble(), 100 + (int)(at * 100));
                return Level(b, 0.3f);
            },
            ["PadLab"] = () => Pad(16f, new[] { 130.81f, 196f, 261.63f, 329.63f, 392f, 523.25f }, 0.5f, 0.25f),
            ["PadWire"] = () => Pad(16f, new[] { 110f, 130.81f, 164.81f, 220f, 261.63f }, 1.1f, 0.22f),
            // The title (ModuleFlow plays it while the title screen holds Scene 4 at 0): a dark Am9 pad breathing under
            // sparse bells, a question left open. Tails wrap round the seam; the reverb runs to its steady state first.
            ["PadIntro"] = () =>
            {
                var b = Pad(24f, new[] { 110f, 164.81f, 220f, 261.63f, 329.63f, 493.88f }, 0.9f, 0.3f);                   // A2 E3 A3 C4 E4 B4
                foreach (var (f, at) in new[] { (659.26f, 1.3f), (987.77f, 3.9f), (880f, 5.6f), (1318.51f, 8.7f), (1046.5f, 10.4f), (987.77f, 13.6f), (783.99f, 16.9f), (880f, 18.5f), (659.26f, 21.2f) })
                    Bell(b, f, at, 0.09f);                                                                                 // E5 B5 A5 E6 C6 B5 G5 A5 E5, none on the seam
                Verb(b, 0.15f, 0.7f, 2);
                return Level(b, 0.3f);
            },
            // The modern lab (Scenes 8-13): 72 bpm, ten bars, Cmaj7 Em7 Am7 Fmaj7 Gadd9 two bars each. A brighter pad (a
            // triangle under each detuned pair) crossfades between chords, round the seam too, under a soft plucked
            // eighth-note arpeggio that rests on the last beat of every chord. Plucks sit 40 ms off the seam.
            ["PadLabTheme"] = () =>
            {
                const float eighth = 60f / 72f / 2f, chord = 16 * eighth, seconds = 5 * chord;                             // 0.417 s, 6.67 s, 33.3 s
                var b = Buf(seconds);
                var pads = new[]
                {
                    new[] { 130.81f, 196f, 246.94f, 329.63f, 392f },      // C3 G3 B3 E4 G4
                    new[] { 164.81f, 246.94f, 293.66f, 392f, 493.88f },   // E3 B3 D4 G4 B4
                    new[] { 110f, 164.81f, 196f, 261.63f, 329.63f },      // A2 E3 G3 C4 E4
                    new[] { 174.61f, 220f, 261.63f, 329.63f, 440f },      // F3 A3 C4 E4 A4
                    new[] { 98f, 146.83f, 246.94f, 392f, 440f },          // G2 D3 B3 G4 A4
                };
                var arps = new[]
                {
                    new[] { 261.63f, 329.63f, 392f, 523.25f },            // C4 E4 G4 C5
                    new[] { 246.94f, 329.63f, 392f, 493.88f },            // B3 E4 G4 B4
                    new[] { 220f, 261.63f, 329.63f, 440f },               // A3 C4 E4 A4
                    new[] { 220f, 261.63f, 349.23f, 440f },               // A3 C4 F4 A4
                    new[] { 196f, 246.94f, 293.66f, 392f },               // G3 B3 D4 G4
                };
                int[] pattern = { 0, 1, 2, 3, 2, 1, 2, 3, 0, 2, 1, 3, 2, 1, -1, -1 };                                        // eighths over the two bars, -1 = rest
                for (int c = 0; c < 5; c++)
                    for (int i = 0; i < 5; i++)
                    {
                        var amp = Window(seconds, c * chord, (c + 1) * chord, 0.6f, 0.25f / (1 + 0.5f * i) * (i % 2 == 0 ? 1 : -1));
                        Loop(b, pads[c][i] - 10 / seconds, amp); Loop(b, pads[c][i] + 10 / seconds, t => 0.6f * amp(t));   // 0.6 Hz beat: a slow shimmer
                        Loop(b, pads[c][i], t => 0.25f * amp(t), Tri);
                    }
                for (int c = 0; c < 5; c++)
                    for (int e = 0; e < 16; e++)
                        if (pattern[e] >= 0) Pluck(b, arps[c][pattern[e]], c * chord + e * eighth + 0.04f, e == 0 ? 0.22f : e % 2 == 0 ? 0.17f : 0.12f);
                Verb(b, 0.12f, 0.65f, 2);
                return Level(b, 0.27f);
            },
            // A live circuit (Scene 10, the level following the current): mains hum with the 120 Hz harmonic on top as a
            // transformer's is, one slow wobble per loop, and a faint sizzle buzzing at mains rate.
            ["CircuitHum"] = () =>
            {
                var b = Buf(6f);
                var partials = new[] { (60f, 0.5f), (120f, 1f), (180f, 0.45f), (240f, 0.2f), (300f, 0.12f), (360f, 0.06f), (480f, 0.03f) };
                for (int i = 0; i < partials.Length; i++) { var (f, a) = partials[i]; float sign = i % 2 == 0 ? 1 : -1; Loop(b, f, t => sign * a * (0.82f + 0.18f * Cos(t / 6f))); }
                var sizzle = Hiss(6f, 17, 5200, 0.3f);
                Mul(sizzle, t => 0.5f + 0.5f * Math.Abs(Sin(60f * t)));                                                      // 120 Hz buzz, whole cycles, even about the seam
                Mix(b, sizzle, 0.15f, 0);
                return Level(b, 0.35f);
            },
        };

        // Renders every recipe: length > 0, peak <= 0.85, and loops meet themselves (first/last 64-sample RMS within 10 %).
        // Returns the number of failures; `log` (e.g. Debug.LogWarning) receives one line per failure.
        public static int Check(Action<string> log = null)
        {
            int failures = 0;
            foreach (var recipe in Recipes())
            {
                var b = recipe.Value();
                string fault = b.Length == 0 ? "empty" : Peak(b) > 0.85f ? "peak " + Peak(b) : null;
                if (fault == null && Loops.Contains(recipe.Key))
                {
                    float head = Rms(b, 0, 64), tail = Rms(b, b.Length - 64, 64);
                    if (Math.Abs(head - tail) >= 0.1f * Math.Max(head, tail)) fault = "seam " + head + " vs " + tail;
                }
                if (fault == null) continue;
                failures++;
                log?.Invoke("SfxBank " + recipe.Key + ": " + fault);
            }
            return failures;
        }

        // Pad: detuned cosine pairs snapped to whole cycles, alternating signs so the seam has no coherent peak, each voice
        // breathing 1–3 times per loop; rolloff darkens the upper voices.
        static float[] Pad(float seconds, float[] chord, float rolloff, float peak)
        {
            var b = Buf(seconds);
            for (int i = 0; i < chord.Length; i++)
            {
                int k = 1 + i % 3;
                float a = 0.3f / (1 + rolloff * i) * (i % 2 == 0 ? 1 : -1), flip = i % 4 < 2 ? 1 : -1;
                float Breathe(float t) => a * (0.72f + 0.28f * flip * Cos(k / seconds * t));
                Loop(b, chord[i] - k / seconds, Breathe); Loop(b, chord[i] + k / seconds, t => 0.6f * Breathe(t));
            }
            return Level(b, peak);
        }

        // ------------------------------------------------------------------ helpers

        static float[] Make(float seconds, float peak, float fadeOut, Action<float[]> layers) { var b = Buf(seconds); layers(b); return Out(b, peak, fadeOut); }
        static float[] Buf(float seconds) => new float[(int)Math.Round(seconds * Hz)];
        static float Sin(float cycles) => (float)Math.Sin(cycles * Tau);
        static float Cos(float cycles) => (float)Math.Cos(cycles * Tau);
        static float Tri(float p) => 1 - 4 * Math.Abs(p - 0.5f);
        static float Saw(float p) => 2 * p - 1;
        static float Smooth(float x) => x <= 0 ? 0 : x >= 1 ? 1 : x * x * (3 - 2 * x);
        // Percussive envelope: smooth attack, then exponential decay with the given time constant; silent before t = 0.
        static float Env(float t, float attack, float decay) => t < 0 ? 0 : Smooth(t / attack) * (float)Math.Exp(-t / decay);
        // ADSR over a sound of `dur` seconds; the release is a fade into the end of the buffer.
        static float Adsr(float t, float dur, float a, float d, float s, float r) => (t < a ? Smooth(t / a) : 1 + (s - 1) * Smooth((t - a) / d)) * Smooth((dur - t) / r);

        // Adds a partial: wave(phase in cycles) * amp(t) at freq(t) Hz; the phase accumulates so sweeps stay smooth.
        static void Add(float[] b, Func<float, float> freq, Func<float, float> amp, Func<float, float> wave = null, float phase = 0)
        {
            if (wave == null) wave = Sin;
            double p = phase;
            for (int i = 0; i < b.Length; i++) { float t = i / (float)Hz; b[i] += wave((float)(p - Math.Floor(p))) * amp(t); p += freq(t) / Hz; }
        }
        static void Tone(float[] b, float f, Func<float, float> amp, Func<float, float> wave = null, float phase = 0) => Add(b, _ => f, amp, wave, phase);
        // Two partials detuned by ±detune (a fraction of f): slow beating reads as warmth or shimmer.
        static void Pair(float[] b, float f, float detune, Func<float, float> amp, Func<float, float> wave = null) { Tone(b, f * (1 - detune), amp, wave); Tone(b, f * (1 + detune), amp, wave); }
        // A looping partial: frequency snapped to whole cycles over the buffer, cosine phase (a triangle at its peak): even about the seam.
        static void Loop(float[] b, float f, Func<float, float> amp, Func<float, float> wave = null)
        { float seconds = b.Length / (float)Hz; Tone(b, (float)Math.Round(f * seconds) / seconds, amp, wave ?? Sin, wave == null ? 0.25f : 0.5f); }
        // One voice of a chord progression over a loop of `seconds`: on from `from` to `to`, crossfading over `edge` at both
        // ends (the fades of neighbouring chords sum to one), the seam wrapped so the first chord fades in under the last.
        static Func<float, float> Window(float seconds, float from, float to, float edge, float amp) => t =>
        {
            float u = t - from;
            u -= seconds * (float)Math.Floor(u / seconds);
            if (u > seconds - edge) u -= seconds;
            return amp * Smooth((u + edge * 0.5f) / edge) * Smooth((to - from + edge * 0.5f - u) / edge);
        };
        // Bell: a slightly detuned fundamental, a faster octave and the inharmonic 2.76 partial that reads as metal, wrapped into the loop at `at`.
        static void Bell(float[] b, float f, float at, float amp)
        {
            var n = Buf(3f);
            Pair(n, f, 0.0015f, t => amp * Env(t, 0.003f, 1.1f));
            Tone(n, 2 * f, t => 0.22f * amp * Env(t, 0.002f, 0.45f)); Tone(n, 2.76f * f, t => 0.14f * amp * Env(t, 0.002f, 0.3f)); Tone(n, 5.4f * f, t => 0.04f * amp * Env(t, 0.001f, 0.12f));
            Mul(n, t => Smooth((3f - t) / 0.5f));
            Wrap(b, n, 1, at);
        }
        // Pluck: a soft string, its upper harmonics dying first, a touch of pick noise, wrapped into the loop at `at`.
        static void Pluck(float[] b, float f, float at, float amp)
        {
            var n = Buf(1f);
            Tone(n, f, t => amp * Env(t, 0.004f, 0.4f)); Tone(n, 2 * f, t => 0.35f * amp * Env(t, 0.003f, 0.15f)); Tone(n, 3 * f, t => 0.1f * amp * Env(t, 0.002f, 0.07f));
            Click(n, 0, 0.0015f, 2500, 0.12f * amp, (int)(f * 10 + at * 100));
            Mul(n, t => Smooth((1f - t) / 0.2f));
            Wrap(b, n, 1, at);
        }
        // The wire's low hum: a 55 Hz saw rounded off by a low-pass, so it carries on small speakers too.
        static void Hum(float[] b, Func<float, float> amp) { var hum = new float[b.Length]; Tone(hum, 55, amp, Saw); LowPass(hum, _ => 300); Mix(b, hum, 0.35f, 0); }

        static float[] Noise(int n, int seed) { var r = new Random(seed); var b = new float[n]; for (int i = 0; i < n; i++) b[i] = (float)(r.NextDouble() * 2 - 1); return b; }
        // One-pole low-pass with a cutoff(t) in Hz.
        static void LowPass(float[] b, Func<float, float> cutoff) { float y = 0; for (int i = 0; i < b.Length; i++) b[i] = y += (b[i] - y) * (1 - (float)Math.Exp(-Tau * cutoff(i / (float)Hz) / Hz)); }
        // Chamberlin state-variable filter, 2x oversampled: resonant band-pass (or low-pass) at cutoff(t) Hz; damping ~0.25 rings, ~1.2 is gentle.
        static void Svf(float[] b, Func<float, float> cutoff, float damping, bool bandPass)
        {
            float lo = 0, bp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float f = 2 * (float)Math.Sin(Math.PI * Math.Min(cutoff(i / (float)Hz), 8000) / (2 * Hz));
                for (int k = 0; k < 2; k++) { lo += f * bp; bp += f * (b[i] - lo - damping * bp); }
                b[i] = bandPass ? bp : lo;
            }
        }
        // Filtered noise click: a low-passed white burst decaying over `decay` seconds, added at `at` seconds.
        static void Click(float[] b, float at, float decay, float cutoff, float gain, int seed)
        { var n = Noise((int)(decay * 8 * Hz) + 8, seed); LowPass(n, _ => cutoff); Mul(n, t => Env(t, 0.0002f, decay)); Mix(b, n, gain, at); }
        // Knock: a short noise burst through the resonant low-pass, i.e. wood or a muted block.
        static void Knock(float[] b, float at, float cutoff, float damping, float decay, float gain, int seed)
        { var n = Noise(Hz / 30, seed); Svf(n, _ => cutoff, damping, false); Mul(n, t => Env(t, 0.0003f, decay)); Mix(b, n, gain, at); }
        // Whoosh: white noise through the resonant band-pass with a moving cutoff, shaped by amp(t).
        static void Whoosh(float[] b, Func<float, float> cutoff, Func<float, float> amp, float damping, int seed)
        { var n = Noise(b.Length, seed); Svf(n, cutoff, damping, true); Mul(n, amp); Mix(b, n, 1, 0); }
        // Seamless noise bed: pink-ish noise (two low-passes) for half the loop, mirrored into a palindrome. Scaled to the given RMS.
        static float[] Bed(float seconds, int seed, float cutoff, float rms)
        {
            int n = (int)Math.Round(seconds * Hz), half = (n + 1) / 2, warm = Hz / 2;
            var lo = Noise(half + warm, seed); var hi = (float[])lo.Clone();
            LowPass(lo, _ => cutoff); LowPass(hi, _ => cutoff * 12);
            var b = new float[n];
            for (int i = 0; i < half; i++) b[i] = b[n - 1 - i] = lo[warm + i] + 0.15f * hi[warm + i];
            float r = Rms(b, 0, n);
            for (int i = 0; i < n; i++) b[i] *= rms / r;
            return b;
        }
        // A touch of room: four damped feedback combs (25–40 ms, mutually prime) averaged under the dry sound. A loop asks
        // for 2 passes: the combs run on through the buffer again and only the last pass is kept, so the room at the seam
        // is the room mid-loop, not an empty one.
        static void Verb(float[] b, float mix, float feedback, int passes = 1)
        {
            var wet = new float[b.Length];
            foreach (int d in new[] { 1117, 1327, 1559, 1753 })
            {
                var line = new float[d]; float lp = 0; int k = 0;
                for (int pass = 1; pass <= passes; pass++)
                    for (int i = 0; i < b.Length; i++, k++) { lp += (line[k % d] - lp) * 0.35f; line[k % d] = b[i] + lp * feedback; if (pass == passes) wet[i] += lp; }
            }
            for (int i = 0; i < b.Length; i++) b[i] += wet[i] * mix * 0.25f;
        }
        // Seamless hiss: white noise through the resonant band-pass at `centre` Hz for half the loop, mirrored into a palindrome, scaled to the given RMS.
        static float[] Hiss(float seconds, int seed, float centre, float rms)
        {
            int n = (int)Math.Round(seconds * Hz), half = (n + 1) / 2, warm = Hz / 10;
            var s = Noise(half + warm, seed); Svf(s, _ => centre, 0.8f, true);
            var b = new float[n];
            for (int i = 0; i < half; i++) b[i] = b[n - 1 - i] = s[warm + i];
            float r = Rms(b, 0, n);
            for (int i = 0; i < n; i++) b[i] *= rms / r;
            return b;
        }

        static void Mul(float[] b, Func<float, float> amp) { for (int i = 0; i < b.Length; i++) b[i] *= amp(i / (float)Hz); }
        // Adds src * gain into dst from `at` seconds, clipped to dst.
        static void Mix(float[] dst, float[] src, float gain, float at) { int s = (int)(at * Hz); for (int i = Math.Max(0, -s); i < src.Length && s + i < dst.Length; i++) dst[s + i] += src[i] * gain; }
        // The same into a loop, wrapping past the end to the start: a tail over the seam carries on.
        static void Wrap(float[] dst, float[] src, float gain, float at) { int s = (int)(at * Hz), n = dst.Length; for (int i = 0; i < src.Length; i++) dst[(s + i) % n] += src[i] * gain; }
        static void Saturate(float[] b, float drive) { for (int i = 0; i < b.Length; i++) b[i] = (float)Math.Tanh(b[i] * drive) / drive; }
        static float Peak(float[] b) => b.Max(x => Math.Abs(x));
        static float Rms(float[] b, int from, int count) { double s = 0; for (int i = from; i < from + count; i++) s += b[i] * b[i]; return (float)Math.Sqrt(s / count); }
        static float[] Level(float[] b, float peak) { float p = Peak(b); if (p > 0) for (int i = 0; i < b.Length; i++) b[i] *= peak / p; return b; }
        // Every one-shot ends here: a 1 ms fade in, a fade out so truncated tails never click, then peak-normalised.
        static float[] Out(float[] b, float peak, float fadeOut) { float dur = b.Length / (float)Hz; Mul(b, t => Smooth(t / 0.001f) * Smooth((dur - t) / fadeOut)); return Level(b, peak); }
    }
}
