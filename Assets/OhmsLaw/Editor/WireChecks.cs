using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // Self-checks for the electron space (WireInterior), without Play mode: the simulation is stepped directly with a
    // fixed dt. Ohm's Law > Tests > Wire Electrons logs one PASS/FAIL line per check and a summary line.
    public static class WireChecks
    {
        const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        const float Dt = 1f / 60f;

        [MenuItem("Ohm's Law/Tests/Wire Electrons")]
        public static void Run()
        {
            int failed = 0;
            void Check(bool ok, string what)
            {
                if (!ok) failed++;
                Debug.Log((ok ? "PASS" : "FAIL") + " wire: " + what);
            }

            var carry = typeof(WireInterior).GetField("carry", BindingFlags.Static | BindingFlags.NonPublic);
            carry?.SetValue(null, null);
            var root = Temp("Wire Check (temporary)", null); // hidden, never saved: the open scene is left as it was
            try
            {
                // Scenes 5-6: random motion first, then the field. The cavern's atoms are placed on the same Awake.
                var wire = Make(root.transform, "Copper");
                wire.cavern = Temp("Copper Lattice", wire.transform).AddComponent<ParticleSystem>();
                Awake(wire);
                var atoms = new ParticleSystem.Particle[wire.cavern.main.maxParticles];
                int placed = wire.cavern.GetParticles(atoms);
                float axis = atoms.Take(placed).Where(a => Mathf.Abs(a.position.x) < wire.length / 2).Select(a => new Vector2(a.position.y, a.position.z).magnitude).DefaultIfEmpty(99f).Min();
                float lens = atoms.Take(placed).Select(a => Vector3.Distance(a.position, wire.cavernEye)).DefaultIfEmpty(99f).Min();
                int front = atoms.Take(placed).Count(a => a.position.z < -1f && a.position.y < 1.5f && a.position.x > -9f && a.position.x < 8f);
                Check(placed > 1500 && placed <= 3000, $"cavern: about 2700 atoms ({placed})");
                Check(axis > wire.tunnelRadius + 1f && lens > 2.5f, $"cavern: none inside the conduit (nearest {axis:0.00} m from the axis) or over the lens (nearest {lens:0.0} m)");
                Check(front == 0, $"cavern: the stage in front of the set is clear up to 1.5 m, so labels and Wattson read on the haze ({front} atoms there)");
                var home = Field<Vector3[]>(wire, "home").ToArray();
                Steps(wire, 3f, null);
                var after = Field<Vector3[]>(wire, "home");
                var offset = Field<Vector3[]>(wire, "offset");
                float moved = home.Select((h, i) => Mathf.Abs(after[i].x - h.x)).Max();
                float wander = offset.Average(o => o.magnitude);
                Check(moved < 1e-4f, $"field off: no drift (largest place shift {moved:0.00000} m)");
                Check(wander > 0.1f && wander < 0.8f, $"field off: random motion visible and bounded (mean wander {wander:0.00} m)");
                var position = Field<Vector3[]>(wire, "position");
                float radial = position.Max(p => new Vector2(p.y, p.z).magnitude);
                Check(radial <= wire.tunnelRadius + 1e-4f, $"electrons stay inside the glass (largest radius {radial:0.000} m)");

                wire.fieldOn = true;
                wire.counting = true;
                int wrong = 0, wraps = 0;
                float popAlpha = 0;
                var wrapped = Field<bool[]>(wire, "wrapped");
                var alpha = Field<float[]>(wire, "alpha");
                Steps(wire, 12f, t =>
                {
                    if (t >= 4f && wire.LastSecond != 3) wrong++;
                    for (int i = 0; i < wrapped.Length; i++) if (wrapped[i]) { wraps++; popAlpha = Mathf.Max(popAlpha, alpha[i]); }
                });
                Check(wrong == 0, $"field on, 0.6 m/s: the counter reads 3 per second every frame from 4 s on ({wrong} frames off, last {wire.LastSecond})");
                Check(wraps > 0 && popAlpha < 0.01f, $"electrons go round inside the terminals, invisibly ({wraps} wraps, brightest {popAlpha:0.000})");
                Check(wire.counter.text == "3", $"counter text shows the completed second ('{wire.counter.text}')");
                var xs = Field<Vector3[]>(wire, "home").Select(h => h.x).OrderBy(x => x).ToArray();
                float spacing = wire.length / wire.count, worst = 0;
                for (int i = 0; i < xs.Length; i++)
                {
                    float gap = i + 1 < xs.Length ? xs[i + 1] - xs[i] : xs[0] + wire.length - xs[i];
                    worst = Mathf.Max(worst, Mathf.Abs(gap - spacing));
                }
                Check(worst < 1e-3f, $"no gaps or pile-ups: places stay evenly spaced after wrapping (worst gap error {worst:0.00000} m)");
                float thermal = Field<Vector3[]>(wire, "velocity").Average(v => v.magnitude);
                Check(thermal > wire.driftSpeed, $"random motion faster than the drift ({thermal:0.00} vs {wire.driftSpeed:0.00} m/s)");

                wire.frozen = true;
                Steps(wire, 3.5f, null);
                Check(wire.LastSecond == 0, $"frozen: no charge passes (counter {wire.LastSecond})");

                // Scene 12: the drift follows the bench's current; a baseline at 1 V, then 2 V.
                var nichrome = Make(root.transform, "Nichrome");
                var rig = Temp("Rig", root.transform).AddComponent<CircuitRig>();
                rig.state = new CircuitState { keyClosed = true, voltage = 1f };
                rig.ammeterPlaced = rig.voltmeterPlaced = true;
                nichrome.rig = rig;
                nichrome.driftPerAmp = 2f;
                nichrome.fieldOn = true;
                nichrome.counting = true;
                nichrome.readoutVolts = Text(root.transform, "Volts");
                nichrome.readoutAmps = Text(root.transform, "Amps");
                var lattice = Temp("Lattice", nichrome.transform).AddComponent<ParticleSystem>();
                nichrome.lattice = lattice;
                Awake(nichrome);
                nichrome.latticeVisibility = 1;
                Steps(nichrome, 1f, null);
                float halfway = Field<float>(nichrome, "shownLattice");
                Steps(nichrome, 4f, null);
                Check(halfway > 0.3f && halfway < 0.9f && Field<float>(nichrome, "shownLattice") == 1f, $"lattice fades in (after 1 s {halfway:0.00}, then 1)");
                Check(nichrome.LastSecond == 2, $"1 V: counter reads 2 per second ({nichrome.LastSecond})");
                Check(nichrome.readoutVolts.text == "V = 1.0 V" && nichrome.readoutAmps.text == "I = 0.20 A", $"1 V readings '{nichrome.readoutVolts.text}' '{nichrome.readoutAmps.text}'");
                rig.SetVoltage(2f);
                Steps(nichrome, 4f, null);
                Check(nichrome.LastSecond == 4, $"2 V: counter doubles to 4 per second ({nichrome.LastSecond})");
                Check(nichrome.readoutVolts.text == "V = 2.0 V" && nichrome.readoutAmps.text == "I = 0.40 A", $"2 V readings '{nichrome.readoutVolts.text}' '{nichrome.readoutAmps.text}'");
            }
            finally
            {
                Object.DestroyImmediate(root);
                carry?.SetValue(null, null);
            }
            Debug.Log(failed == 0 ? "PASS wire: all checks" : $"FAIL wire: {failed} check(s) failed");
        }

        static GameObject Temp(string name, Transform parent)
        {
            var go = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave);
            if (parent) go.transform.SetParent(parent, false);
            return go;
        }

        static WireInterior Make(Transform parent, string name)
        {
            var go = Temp(name, parent);
            var ring = Temp("Cross Section", go.transform).transform;
            ring.localPosition = new Vector3(-1.2f, 0, 0);
            var wire = go.AddComponent<WireInterior>();
            wire.crossSection = ring;
            wire.counter = Text(go.transform, "Counter");
            return wire;
        }

        static TextMeshPro Text(Transform parent, string name) => Temp(name, parent).AddComponent<TextMeshPro>();

        static void Awake(WireInterior wire) => typeof(WireInterior).GetMethod("Awake", Any).Invoke(wire, null);

        static void Steps(WireInterior wire, float seconds, System.Action<float> each)
        {
            var step = typeof(WireInterior).GetMethod("Step", Any);
            var args = new object[] { Dt };
            int frames = Mathf.RoundToInt(seconds / Dt);
            for (int i = 1; i <= frames; i++)
            {
                step.Invoke(wire, args);
                each?.Invoke(i * Dt);
            }
        }

        static T Field<T>(WireInterior wire, string name) => (T)typeof(WireInterior).GetField(name, Any).GetValue(wire);
    }
}
