using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

namespace OhmsLaw.Editor
{
    // Continuity of the historical lab across the built scenes (build 4, 7, 8, 9 first): Scene 4's wire, desk and Ohm land
    // on the same spots in Scene 7's study; Scene 8's first frame is Scene 7's; every Scene 8 cue names an object the
    // director knows; and Scene 8's cues, applied to their end state in order, leave the frame Scene 9 opens on (the room,
    // the bench, the rig parts, the camera, Wattson; no study, no Ohm). One PASS / FAIL line per check; scenes already open
    // in the editor are read but never changed or closed.
    public static class LabContinuityTest
    {
        static int passed, failed;
        const string Scenes = SceneBuilderBase.Root + "/Scenes/";

        [MenuItem("Ohm's Law/Tests/Lab Continuity")]
        public static void Run()
        {
            passed = failed = 0;
            var opened = new List<Scene>();
            try
            {
                Geometry();
                var s4 = Open("Scene04_MeetWattson", opened);
                var s7 = Open("Scene07_BackToOhm", opened);
                var s8 = Open("Scene08_OurLab", opened);
                var s9 = Open("Scene09_BuildCircuit", opened);
                if (s4.IsValid() && s7.IsValid()) Scene4To7(s4, s7);
                if (s7.IsValid() && s8.IsValid()) Scene7To8(s7, s8);
                if (s8.IsValid()) CueTargets(s8);
                if (s7.IsValid() && s8.IsValid() && s9.IsValid()) Polish(s4, s7, s8, s9);
                if (s8.IsValid() && s9.IsValid())
                {
                    if (opened.Contains(s8)) Scene8EndIs9(s8, s9);
                    else Debug.LogWarning("Lab Continuity: Scene 8 is open in the editor; skipping the end-state simulation (it would change the scene).");
                }
            }
            catch (System.Exception e) { Check("no exception", false, e.ToString()); }
            finally
            {
                foreach (var scene in opened) if (scene.IsValid() && scene.isLoaded) EditorSceneManager.CloseScene(scene, true); // discards the simulation
            }
            string summary = $"Lab Continuity: {passed} passed, {failed} failed";
            if (failed == 0) Debug.Log("PASS " + summary); else Debug.LogError("FAIL " + summary);
        }

        static void Check(string name, bool ok, string detail = "")
        {
            if (ok) { passed++; Debug.Log("PASS " + name); }
            else { failed++; Debug.LogError("FAIL " + name + (detail != "" ? " — " + detail : "")); }
        }

        static bool Near(Vector3 a, Vector3 b, float tolerance = 0.002f) => (a - b).magnitude <= tolerance;

        static Scene Open(string name, List<Scene> opened)
        {
            string path = Scenes + name + ".unity";
            var scene = SceneManager.GetSceneByPath(path);
            if (scene.isLoaded) return scene;
            if (!System.IO.File.Exists(path)) { Check(name + " is built", false, path + " missing"); return default; }
            scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            opened.Add(scene);
            return scene;
        }

        static Transform Find(Scene scene, string name) =>
            scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.name == name);

        static Bounds RenderBounds(Transform t)
        {
            var renderers = t.GetComponentsInChildren<Renderer>(true);
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return bounds;
        }

        // The placement itself: the desk centre, Scene 4's wire, and the bench clearing the window wall.
        static void Geometry()
        {
            Check("study desk top lands at (0.25, 0.80, 0)", Near(SceneBuilderBase.OhmDeskC, new Vector3(0.25f, 0.80f, 0f)), SceneBuilderBase.OhmDeskC.ToString("F3"));
            Check("Scene 4's wire lands at (0.54, 0.88, 0.13)", Near(SceneBuilderBase.WireStudyC, new Vector3(0.54f, 0.88f, 0.13f)), SceneBuilderBase.WireStudyC.ToString("F3"));
            float windowWall = SceneBuilderBase.StudyPoint(new Vector3(-3.215f, 0f, 0f)).x;
            Check("the 2.0 m bench clears the study's window wall", SceneBuilderBase.BenchWidth / 2 < windowWall, "wall x " + windowWall.ToString("F3"));
        }

        // Scene 7 is Scene 4's study: the wire, the desk, Ohm and the galvanometer in the same places (through StudyPoint).
        static void Scene4To7(Scene s4, Scene s7)
        {
            foreach (var name in new[] { "Copper Wire", "Georg Ohm", "Galvanometer Needle Pivot", "Ohm Notebook Target", "Thermoelectric Source" })
            {
                var a = Find(s4, name);
                var b = Find(s7, name);
                if (!a || !b) { Check($"4→7 {name} exists in both", false); continue; }
                var expected = SceneBuilderBase.StudyPoint(a.position);
                Check($"4→7 {name} on the same spot", Near(b.position, expected, 0.01f), $"Scene 7 {b.position:F3}, Scene 4 mapped {expected:F3}");
            }
            var wire4 = Find(s4, "Copper Wire");
            var wire7 = Find(s7, "Copper Wire");
            if (wire4 && wire7) Check("4→7 the wire keeps its size", Near(wire4.lossyScale, wire7.lossyScale, 0.0005f), wire7.lossyScale.ToString("F4"));
            var desk4 = Find(s4, "Victorian_Desk_Desk_0");
            var desk7 = Find(s7, "Victorian_Desk_Desk_0");
            if (desk4 && desk7) Check("4→7 the Victorian desk on the same spot", Near(RenderBounds(desk7).center, SceneBuilderBase.StudyPoint(RenderBounds(desk4).center), 0.01f));
            var ohm = Find(s7, "Georg Ohm");
            if (ohm) Check("Scene 7 Ohm faces the room (-Z)", Vector3.Dot(ohm.forward, Vector3.back) > 0.99f, ohm.forward.ToString("F2"));
            Check("Scene 7 has no classroom set", !Find(s7, "Classroom Back") && !Find(s7, "Teaching Board"));
            var board = Find(s7, "Board Question");
            Check("Scene 7's question chalk is on the study slate", board && board.IsChildOf(Find(s7, "Study Easel")));
        }

        // Scene 8's first frame is Scene 7's last: same poses for the set, the apparatus, Ohm, the slate and the camera.
        static void Scene7To8(Scene s7, Scene s8)
        {
            foreach (var name in new[] { "Victorian_Desk", "Copper Wire", "torsion galvanometer", "Thermoelectric Source", "Mercury Cups", "notebook",
                         "Georg Ohm", "Study Easel", "Board Voltage", "Board Question", "Wattson", "Cam Study Wide", "Fireplace", "Detective_Board" })
            {
                var a = Find(s7, name);
                var b = Find(s8, name);
                if (!a || !b) { Check($"7→8 {name} exists in both", false); continue; }
                Check($"7→8 {name} same pose", Near(a.position, b.position) && Quaternion.Angle(a.rotation, b.rotation) < 0.1f, $"{a.position:F3} vs {b.position:F3}");
            }
            foreach (var name in new[] { "Board Voltage", "Board Current", "Board Question Panel", "Board Question", "Board Circuit", "Page 1", "Page 2", "Page 3" })
            {
                var t = Find(s8, name);
                Check($"Scene 8 opens with {name} shown (Scene 7 ends with it)", t && t.gameObject.activeSelf);
            }
            foreach (var name in new[] { "Classroom Floor", "Classroom Back", "Classroom Left", "Classroom Right", "Classroom Ceiling", "Bench", "AMMETER", "VOLTMETER", "cells / controllable DC source", "NICHROME WIRE — XY", "switch / key" })
            {
                var t = Find(s8, name);
                Check($"Scene 8 opens with {name} hidden", t && !t.gameObject.activeSelf);
            }
            var director = s8.GetRootGameObjects().Select(r => r.GetComponentInChildren<SceneDirector>(true)).FirstOrDefault(d => d);
            Check("Scene 8 has the study and classroom looks", director && director.looks.Length == 2 && director.looks.All(l => l.lights.Length > 0 && l.volume),
                director ? string.Join(", ", director.looks.Select(l => l.name + ":" + l.lights.Length)) : "no director");
            if (director && director.looks.Length == 2)
                Check("Scene 8 starts in the study look (classroom lights off)", director.looks[1].lights.All(l => !l.enabled) && director.looks[0].lights.Any(l => l.enabled));
        }

        // The polish pass on the lab scenes (Logs/Blockout/wow_review.json pass1/pass2): the study flames match Scene 4's and
        // stay accents, every handheld camera and every show-driven effect root reads reduced motion, Scene 8's labels keep the
        // key and Wattson's hat clear, the transformation panels sit top-centre, and Scene 9 opens with the meters' plates
        // hidden and the learner's prediction on.
        static void Polish(Scene s4, Scene s7, Scene s8, Scene s9)
        {
            foreach (var name in new[] { "Oil Lamp", "Desk Candle" })
            {
                var a = s4.IsValid() ? Find(s4, name) : null;
                var b = Find(s7, name);
                var la = a ? a.GetComponent<Light>() : null;
                var lb = b ? b.GetComponent<Light>() : null;
                if (la && lb) Check($"4→7 {name} burns alike", Mathf.Approximately(la.intensity, lb.intensity) && Mathf.Approximately(la.range, lb.range), $"Scene 4 {la.intensity}/{la.range}, Scene 7 {lb.intensity}/{lb.range}");
            }
            var lamp = Find(s7, "Oil Lamp");
            var candle = Find(s7, "Desk Candle"); // the desk candle's light was removed from the study (2026-10-05 scene edit): none is fine
            var lampLight = lamp ? lamp.GetComponent<Light>() : null;
            var candleLight = candle ? candle.GetComponent<Light>() : null;
            Check("Scene 7 flames are accents (lamp ≤ 1.6, candle ≤ 0.7 or none): Ohm's face reads",
                lampLight && lampLight.intensity <= 1.6f + 1e-3f && (!candleLight || candleLight.intensity <= 0.7f + 1e-3f),
                $"lamp {(lampLight ? lampLight.intensity.ToString("0.00") : "missing")}, candle {(candleLight ? candleLight.intensity.ToString("0.00") : "none")}");
            foreach (var (scene, label) in new[] { (s7, "Scene 7"), (s8, "Scene 8"), (s9, "Scene 9") })
            {
                var noisy = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CinemachineBasicMultiChannelPerlin>(true)).ToArray();
                Check($"{label}: every handheld camera reads reduced motion", noisy.Length > 0 && noisy.All(p => p.TryGetComponent<ReducedMotionCamera>(out _)),
                    string.Join(", ", noisy.Where(p => !p.TryGetComponent<ReducedMotionCamera>(out _)).Select(p => p.name)));
            }
            var defs = s8.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<CalloutDef>(true)).ToList();
            var key = defs.FirstOrDefault(d => d.id == "key");
            var wire = defs.FirstOrDefault(d => d.id == "wire");
            Check("Scene 8 key label sits bottom-right (off the key and the meters)", key && key.anchor == CalloutDef.Anchor.BottomRight);
            Check("Scene 8 wire label sits top-left (clear of Wattson's hat)", wire && wire.anchor == CalloutDef.Anchor.TopLeft);
            foreach (var (scene, name) in new[] { (s7, "[VFX] historical apparatus starts transforming → modern circuit"), (s8, "[VFX] Ohm's apparatus becomes today's instruments") })
            {
                var fx = Find(scene, name);
                Check($"'{name}' exists (its note now lives in the lesson panel, no floating card)", fx);
                Check($"'{name}' reads reduced motion", fx && fx.TryGetComponent<ReducedMotionFx>(out _));
            }
            var director9 = s9.GetRootGameObjects().Select(r => r.GetComponentInChildren<SceneDirector>(true)).FirstOrDefault(d => d);
            Check("Scene 9 opens with the meters' digital plates hidden (needle move only on key-close)", director9 && director9.rig && !director9.rig.readoutsShown);
        }

        static readonly HashSet<string> Named = new HashSet<string> { "show", "hide", "vanish", "retract", "appear", "rise", "spread", "glide", "settle", "fx" };

        static List<string> Commands(Scene scene, out SceneDirector director)
        {
            director = scene.GetRootGameObjects().Select(r => r.GetComponentInChildren<SceneDirector>(true)).FirstOrDefault(d => d);
            var playable = director ? director.GetComponent<PlayableDirector>() : null;
            var timeline = playable ? playable.playableAsset as TimelineAsset : null;
            if (!timeline || !timeline.markerTrack) return new List<string>();
            return timeline.markerTrack.GetMarkers().OfType<Cue>().OrderBy(c => c.time).Select(c => c.command).ToList();
        }

        // "verb [seconds] name" → name.
        static string Target(string command)
        {
            var words = command.Split(' ');
            if (words.Length < 2) return null;
            int skip = words.Length > 2 && float.TryParse(words[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _) && words[0] != "show" && words[0] != "hide" && words[0] != "fx" ? 2 : 1;
            return string.Join(" ", words.Skip(skip));
        }

        // Every show/hide/morph/move/fx cue names an object in the director's refs; glide/settle have their goal; looks exist.
        static void CueTargets(Scene s8)
        {
            var commands = Commands(s8, out var director);
            Check("Scene 8 timeline has cues", commands.Count > 20, commands.Count + " cues");
            if (!director) return;
            var refs = new HashSet<string>(director.refs.Where(r => r).Select(r => r.name));
            foreach (var command in commands)
            {
                string verb = command.Split(' ')[0];
                if (verb == "look") { string look = Target(command); Check($"look '{look}' exists", director.looks.Any(l => l.name == look)); continue; }
                if (!Named.Contains(verb)) continue;
                string name = Target(command);
                Check($"cue '{command}' names a ref", refs.Contains(name));
                if (verb == "glide" || verb == "settle") Check($"cue '{command}' has its goal", refs.Contains(name + " Goal"));
            }
        }

        // Applies Scene 8's cues to their end state in timeline order (what REPLAY, skip-ahead or the retroactive markers
        // converge to), then compares with Scene 9's first frame.
        static void Scene8EndIs9(Scene s8, Scene s9)
        {
            var commands = Commands(s8, out var director);
            var byName = director.refs.Where(r => r).GroupBy(r => r.name).ToDictionary(g => g.Key, g => g.Last());
            foreach (var command in commands)
            {
                string verb = command.Split(' ')[0];
                if (!Named.Contains(verb) || verb == "fx") continue;
                if (!byName.TryGetValue(Target(command), out var go)) continue;
                if (verb == "glide" || verb == "settle")
                {
                    if (!byName.TryGetValue(go.name + " Goal", out var goal)) continue;
                    go.transform.position = goal.transform.position;
                    go.transform.localScale = goal.transform.localScale;
                }
                else go.SetActive(verb == "show" || verb == "appear" || verb == "rise" || verb == "spread");
            }

            bool Visible(Scene scene, string name) { var t = Find(scene, name); return t && t.gameObject.activeInHierarchy; }
            foreach (var name in new[] { "Historical Lab", "Georg Ohm", "Ohm Apparatus", "Study Easel" })
                Check($"Scene 8 ends without {name}", !Visible(s8, name));
            foreach (var name in new[] { "Classroom Floor", "Classroom Back", "Classroom Left", "Classroom Right", "Classroom Ceiling", "Bench", "AMMETER", "VOLTMETER",
                         "cells / controllable DC source", "NICHROME WIRE — XY", "switch / key", "Parts Tray", "Lead Key → Source", "Teaching Board" })
            {
                var a = Find(s8, name);
                var b = Find(s9, name);
                if (!a || !b) { Check($"8→9 {name} exists in both", false); continue; }
                Check($"8→9 {name} shown and in Scene 9's pose",
                    a.gameObject.activeInHierarchy == b.gameObject.activeInHierarchy && Near(a.position, b.position) && Near(a.lossyScale, b.lossyScale, 0.001f),
                    $"8: {a.gameObject.activeInHierarchy} {a.position:F3} {a.lossyScale:F3} / 9: {b.gameObject.activeInHierarchy} {b.position:F3} {b.lossyScale:F3}");
            }
            var cam8 = Find(s8, "Cam Bench Wide");
            var cam9 = Find(s9, "Cam Bench Wide");
            if (cam8 && cam9) Check("8→9 the last shot is Scene 9's Bench Wide", Near(cam8.position, cam9.position) && Quaternion.Angle(cam8.rotation, cam9.rotation) < 0.1f);
            var home = Find(s8, "Wattson Home");
            var wattson9 = Find(s9, "Wattson");
            if (home && wattson9) Check("8→9 Wattson ends where Scene 9 starts him", Near(home.position, wattson9.position, 0.01f), $"{home.position:F2} vs {wattson9.position:F2}");
            // Chalk items live under the board face; the slab, rails and wash are the board itself.
            var boardItems = s9.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Where(t => t.parent && t.parent.name == "Board Face" && t.gameObject.activeInHierarchy).Select(t => t.name).ToList();
            Check("Scene 9 opens on an empty classroom board", boardItems.Count == 0, string.Join(", ", boardItems));
        }
    }
}
