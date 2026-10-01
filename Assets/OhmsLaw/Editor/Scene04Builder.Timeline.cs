using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace OhmsLaw.Editor
{
    // Timeline, placeholder sound effects and preview renders for Scene 4.
    public static partial class Scene04Builder
    {
        // ------------------------------------------------------------------ timeline

        static void BuildTimeline(Stage s, Dictionary<string, DialogueLine> lines, Dictionary<string, AudioClip> sfx)
        {
            // Shot plan from the storyboard. A shot stretches when its recorded line is longer than planned,
            // and every later shot moves with it.
            var plan = new (string shot, float length, string line, float lineAt)[]
            {
                ("SH01", 5f, null, 0), ("SH02", 5f, null, 0), ("SH03", 2f, "S04_L01", 0.1f), ("SH04", 9f, "S04_L02", 0.3f),
                ("SH05", 5f, null, 0), ("SH06", 4f, "S04_L03", 0.5f), ("SH07", 3f, "S04_L04", 0.1f), ("SH08", 6f, "S04_L05", 1.8f),
                ("SH09", 4f, null, 0), ("SH10", 3f, "S04_L06", 0.6f), ("SH11", 4f, null, 0),
            };
            var start = new Dictionary<string, double>();
            var end = new Dictionary<string, double>();
            double clock = 0;
            foreach (var p in plan)
            {
                float length = p.line == null ? p.length : Mathf.Max(p.length, p.lineAt + lines[p.line].Duration + 0.2f);
                start[p.shot] = clock;
                end[p.shot] = clock += length;
            }
            double total = s.total = (float)clock;
            double T(string shot, double offset = 0) => start[shot] + offset;

            var timeline = Asset(S4 + "/Timeline/Scene04_Timeline.playable", ScriptableObject.CreateInstance<TimelineAsset>);
            foreach (var old in timeline.GetRootTracks().ToArray()) timeline.DeleteTrack(old);
            timeline.editorSettings.frameRate = 30;

            var host = Node("Scene04 Director");
            var director = s.director = host.AddComponent<PlayableDirector>();
            director.playableAsset = timeline;
            director.playOnAwake = true;
            director.extrapolationMode = DirectorWrapMode.Hold;
            s.receiver = host.AddComponent<SignalReceiver>();
            var scene04 = s.director04 = host.AddComponent<Scene04Director>();
            scene04.wire = s.wire;
            scene04.words = s.words;
            scene04.wattson = s.wattson;
            scene04.niscope = s.niscope;
            scene04.wordsDrift = Vector3.up * 0.28f + Vector3.ProjectOnPlane(Hover - WordsCamera, Vector3.up).normalized * 0.3f;

            // --- Cinemachine
            var cameras = timeline.CreateTrack<CinemachineTrack>(null, "Cameras");
            director.SetGenericBinding(cameras, s.camera.GetComponent<CinemachineBrain>());
            void Shot(string cam, double from, double to)
            {
                var clip = cameras.CreateClip<CinemachineShot>();
                clip.displayName = cam;
                clip.start = from;
                clip.duration = to - from;
                var shot = (CinemachineShot)clip.asset;
                shot.DisplayName = cam;
                var reference = shot.VirtualCamera;
                reference.exposedName = cam;
                shot.VirtualCamera = reference;
                director.SetReferenceValue(cam, s.cams[cam]);
            }
            // Two overlapping clips blend for the whole shot: that blend is the dolly, push-in or orbit.
            void Move(string shot, string a, string b)
            {
                Shot(a, start[shot], end[shot] - 0.05);
                Shot(b, start[shot] + 0.05, end[shot]);
            }
            Move("SH01", "SH01a Arrival", "SH01b Arrival");
            Move("SH02", "SH02a Wattson Enters", "SH02b Wattson Enters");
            Shot("SH03 Welcome", start["SH03"], end["SH03"]);
            Shot("SH04a Over Shoulder", start["SH04"], T("SH04", 2.6));          // focus on Wattson
            Shot("SH04b Rack To Ohm", T("SH04", 1.0), end["SH04"] - 0.5);        // rack focus and push in
            Shot("SH04a Over Shoulder", end["SH04"] - 1.8, end["SH04"]);          // and back to Wattson
            Shot("SH05 Words", start["SH05"], end["SH05"]);
            Move("SH06", "SH06a Orbit", "SH06b Orbit");
            Shot("SH07 Before", start["SH07"], end["SH07"]);
            Move("SH08", "SH08a Hero", "SH08b Hero");
            Move("SH09", "SH09a Aim", "SH09b Aim");
            Shot("SH10 Everybody In", start["SH10"], end["SH10"]);
            Shot("SH11a Dive", start["SH11"], end["SH11"] - 1.0);                 // holds here for the tap, then dolly-zooms
            Shot("SH11b Dive", T("SH11", 0.4), end["SH11"]);
            var shots = cameras.GetClips().OrderBy(c => c.start).ToArray();
            for (int i = 1; i < shots.Length; i++)
            {
                double overlap = shots[i - 1].end - shots[i].start;
                if (overlap <= 0) continue;
                shots[i].blendInDuration = overlap;
                shots[i - 1].blendOutDuration = overlap;
            }

            // --- Wattson: flight along the spline, then gestures
            var flight = Asset(S4 + "/Timeline/Wattson_Flight.anim", () => new AnimationClip());
            flight.ClearCurves();
            flight.SetCurve("", typeof(WattsonController), "pathT", new AnimationCurve(
                new Keyframe(0, 0, 0, 0),
                new Keyframe((float)T("SH02", 0.3), 0, 0, 0),
                new Keyframe((float)T("SH02", 3.5), 1, 0.6f, 0),   // arrives fast, so the spring overshoots and settles
                new Keyframe((float)total, 1, 0, 0)));
            EditorUtility.SetDirty(flight);
            var flightTrack = timeline.CreateTrack<AnimationTrack>(null, "Wattson Flight");
            director.SetGenericBinding(flightTrack, s.wattson.GetComponent<Animator>());
            var flightClip = flightTrack.CreateClip(flight);
            flightClip.start = 0;
            flightClip.duration = total;

            var gestures = timeline.CreateTrack<WattsonTrack>(null, "Wattson Gestures");
            director.SetGenericBinding(gestures, s.wattson);
            void Pose(double from, double to, WattsonController.Gesture gesture, Transform look, Transform point = null, bool gadget = false)
            {
                var clip = gestures.CreateClip<WattsonClip>();
                clip.displayName = gesture.ToString();
                clip.start = from;
                clip.duration = to - from;
                var pose = (WattsonClip)clip.asset;
                pose.gesture = gesture;
                pose.gadgetOut = gadget;
                pose.lookAt = new ExposedReference<Transform> { exposedName = "look " + look.name };
                director.SetReferenceValue(pose.lookAt.exposedName, look);
                if (!point) return;
                pose.pointAt = new ExposedReference<Transform> { exposedName = "point " + point.name };
                director.SetReferenceValue(pose.pointAt.exposedName, point);
            }
            var G = WattsonController.Gesture.Idle;
            Pose(0, end["SH02"], G, s.groupPoint);
            Pose(start["SH03"], end["SH03"], WattsonController.Gesture.Greet, s.groupPoint);
            Pose(start["SH04"], T("SH04", 3.2), WattsonController.Gesture.Point, s.ohmPoint, s.ohmPoint);
            Pose(T("SH04", 3.2), end["SH04"], WattsonController.Gesture.Point, s.groupPoint, s.ohmPoint);
            Pose(start["SH05"], T("SH05", 2.6), WattsonController.Gesture.Flick, s.groupPoint);
            Pose(T("SH05", 2.6), end["SH06"], G, s.groupPoint);
            Pose(start["SH07"], end["SH07"], WattsonController.Gesture.Reach, s.groupPoint);
            Pose(start["SH08"], end["SH08"], WattsonController.Gesture.RaiseGadget, s.groupPoint, null, true);
            Pose(start["SH09"], end["SH09"], WattsonController.Gesture.Aim, s.wirePoint, s.wirePoint, true);
            Pose(start["SH10"], end["SH10"], WattsonController.Gesture.Aim, s.groupPoint, s.wirePoint, true);
            Pose(start["SH11"], total, WattsonController.Gesture.Aim, s.wirePoint, s.wirePoint, true);

            // --- Dialogue: subtitles always, recorded voice when the line has a clip
            var subtitles = timeline.CreateTrack<SubtitleTrack>(null, "Subtitles");
            var voice = timeline.CreateTrack<AudioTrack>(null, "VO");
            director.SetGenericBinding(voice, s.wattson.voice);
            foreach (var p in plan.Where(p => p.line != null))
            {
                var line = lines[p.line];
                var clip = subtitles.CreateClip<SubtitleClip>();
                clip.displayName = line.id + "  " + line.text;
                clip.start = T(p.shot, p.lineAt);
                clip.duration = line.Duration;
                ((SubtitleClip)clip.asset).line = line;
                if (!line.vo) continue;
                var spoken = voice.CreateClip(line.vo);
                spoken.start = clip.start;
                spoken.duration = line.vo.length;
            }

            // --- Sound effects (two tracks so close-together effects never crossfade) and music
            var sfxTracks = new[] { timeline.CreateTrack<AudioTrack>(null, "SFX A"), timeline.CreateTrack<AudioTrack>(null, "SFX B") };
            foreach (var track in sfxTracks) director.SetGenericBinding(track, s.sfx);
            var busyUntil = new double[2];
            void Sfx(string name, double time)
            {
                int lane = busyUntil[0] <= time ? 0 : 1;
                var clip = sfxTracks[lane].CreateClip(sfx[name]);
                clip.start = Math.Max(time, busyUntil[lane]);
                clip.duration = sfx[name].length;
                busyUntil[lane] = clip.end;
            }
            var musicTrack = timeline.CreateTrack<AudioTrack>(null, "Music");
            director.SetGenericBinding(musicTrack, s.music);
            var music = musicTrack.CreateClip(sfx["Pad"]);
            music.start = 0;
            music.duration = total;
            ((AudioPlayableAsset)music.asset).loop = true;

            // --- VFX activation and signals
            var vfxGroup = timeline.CreateTrack<GroupTrack>(null, "VFX");
            void Active(string fx, double from, double to)
            {
                var track = timeline.CreateTrack<ActivationTrack>(vfxGroup, fx);
                track.postPlaybackState = ActivationTrack.PostPlaybackState.Inactive;
                director.SetGenericBinding(track, s.fx[fx]);
                var clip = track.CreateDefaultClip();
                clip.displayName = fx;
                clip.start = from;
                clip.duration = to - from;
            }
            var signals = timeline.CreateTrack<SignalTrack>(null, "Signals");
            director.SetGenericBinding(signals, s.receiver);
            void Signal(string name, double time, UnityAction call)
            {
                var asset = Asset(S4 + "/Timeline/" + name + ".signal", ScriptableObject.CreateInstance<SignalAsset>);
                signals.CreateMarker<SignalEmitter>(time).asset = asset;
                var reaction = new UnityEvent();
                UnityEventTools.AddPersistentListener(reaction, call);
                s.receiver.AddReaction(asset, reaction);
            }

            // SH01 arrival and SH02 entrance
            Active("Charge Streams Blue", 0, 5);
            Active("Charge Streams Gold", 0, 5);
            Active("Landing Dust", 0.7, 4.5);
            Sfx("Whoosh", 0);
            Sfx("Zip", T("SH02", 0.3));
            // SH05 words, SH06 arc and question mark, SH07 drift
            double wordsOff = T("SH11", 1.0);
            Active("VOLTAGE", T("SH05", 0.7), wordsOff);
            Sfx("Pop", T("SH05", 0.7));
            Sfx("Zap", T("SH05", 0.8));
            Active("CURRENT", T("SH05", 1.7), wordsOff);
            Sfx("Pop", T("SH05", 1.7));
            Sfx("Zap", T("SH05", 1.8));
            Active("Arc", T("SH06", 0.2), end["SH06"] - 0.2);
            Sfx("Crackle", T("SH06", 0.2));
            Active("?", T("SH06", 1.9), end["SH07"]);
            Signal("Scene04_DriftWords", T("SH07", 0.2), scene04.DriftWords);
            // SH08 Ohm-niscope, SH09 lock-on
            Signal("Scene04_SlowMotion", T("SH08", 0.15), scene04.SlowMotion);
            Sfx("Shimmer", T("SH08", 0.2));
            Active("Reticle", T("SH09", 1.3), T("SH11", 1.2));
            Sfx("Click", T("SH09", 1.75));
            Active("Wire Highlight", T("SH09", 1.8), total);
            // SH11 dive
            Signal("Scene04_WaitForTap", T("SH11", 0.1), scene04.WaitForTap);
            Active("Beam", T("SH11", 0.45), total);
            Sfx("Beam", T("SH11", 0.45));
            Active("Dive Streaks", T("SH11", 0.8), total);
            Sfx("Dive", T("SH11", 0.8));
            Signal("Scene04Complete", total - 0.7, scene04.Complete);

            EditorUtility.SetDirty(timeline);
        }

        // ------------------------------------------------------------------ placeholder sound, synthesized

        static Dictionary<string, AudioClip> BuildSfx()
        {
            const int hz = 44100;
            const float tau = Mathf.PI * 2;
            var random = new System.Random(4);
            float Noise() => (float)(random.NextDouble() * 2 - 1);

            // f(t seconds, u 0..1) -> sample
            float[] Make(float seconds, Func<float, float, float> f)
            {
                var samples = new float[(int)(seconds * hz)];
                for (int i = 0; i < samples.Length; i++) samples[i] = f(i / (float)hz, i / (float)samples.Length);
                return samples;
            }
            float Bell(float u) => Mathf.Sin(u * Mathf.PI) * Mathf.Sin(u * Mathf.PI);

            float low = 0, phase = 0, burst = 0;
            float Sweep(float from, float to, float u) { phase += tau * Mathf.Lerp(from, to, u) / hz; return Mathf.Sin(phase); }
            float Wind(float openness) { low += (Noise() - low) * openness; return low; }

            var recipes = new Dictionary<string, Func<float[]>>
            {
                ["Whoosh"] = () => Make(1.8f, (t, u) => Wind(0.02f + 0.3f * Bell(u)) * Bell(u) * 1.6f),
                ["Zip"] = () => Make(0.9f, (t, u) => Wind(0.05f + 0.4f * u) * Bell(u) + Sweep(500, 1900, u) * Bell(u) * 0.12f),
                ["Pop"] = () => Make(0.18f, (t, u) => Sweep(300, 1200, u) * Mathf.Exp(-u * 5) * 0.6f),
                ["Zap"] = () => Make(0.3f, (t, u) => Sweep(1500, 250, u) * (0.6f + 0.4f * Noise()) * Mathf.Exp(-u * 4) * 0.45f),
                ["Click"] = () => Make(0.07f, (t, u) => Mathf.Sin(tau * 2400 * t) * Mathf.Exp(-u * 9) * 0.55f + Noise() * Mathf.Exp(-u * 30) * 0.3f),
                ["Chirp"] = () => Make(0.28f, (t, u) => Sweep(900, 1900, u + 0.06f * Mathf.Sin(tau * 30 * t)) * Mathf.Sin(u * Mathf.PI) * 0.45f),
                ["Crackle"] = () => Make(3.4f, (t, u) => { if (random.NextDouble() < 0.004) burst = 1; burst *= 0.93f; return Noise() * burst * Mathf.Sin(u * Mathf.PI) * 0.5f; }),
                ["Shimmer"] = () => Make(2.6f, (t, u) => new[] { 1047f, 1319f, 1568f, 2093f }.Sum(f => Mathf.Sin(tau * f * t)) * (0.5f + 0.5f * Mathf.Sin(tau * 6 * t)) * Mathf.Sin(u * Mathf.PI) * 0.1f),
                ["Beam"] = () => Make(1.4f, (t, u) => { float s = Sweep(180, 1700, u * u); return (s + 0.4f * Mathf.Sin(phase * 2)) * Mathf.Min(1, u * 8) * Mathf.Min(1, (1 - u) * 6) * 0.35f; }),
                ["Dive"] = () => Make(2.2f, (t, u) => (Wind(0.03f + 0.5f * u * u) * 1.4f + Sweep(110, 1000, u * u) * 0.2f) * u * Mathf.Min(1, (1 - u) * 10)),
                // Soft looping pad: every partial completes whole cycles in 16 s, so the loop point is silent.
                ["Pad"] = () => Make(16f, (t, u) => new[] { 110f, 164.8125f, 220f, 261.625f, 329.625f }.Select((f, i) => Mathf.Sin(tau * f * t) * (0.6f + 0.4f * Mathf.Sin(tau * t / 8 + i))).Sum() * 0.09f),
            };

            foreach (var recipe in recipes)
            {
                string path = S4 + "/Audio/SFX/" + recipe.Key + ".wav";
                if (File.Exists(path)) continue; // a sound designer's replacement with the same name is kept
                low = phase = burst = 0;
                File.WriteAllBytes(path, Wav.Encode(recipe.Value(), hz));
            }
            AssetDatabase.Refresh();
            return recipes.Keys.ToDictionary(name => name, name => AssetDatabase.LoadAssetAtPath<AudioClip>(S4 + "/Audio/SFX/" + name + ".wav"));
        }

        // ------------------------------------------------------------------ previews

        // Renders every shot to Logs/Scene04 for a framing check, and Wattson's portrait for the Ask button.
        static void RenderPreviews(Stage s)
        {
            Directory.CreateDirectory("Logs/Scene04");
            var shown = new[] { "VOLTAGE", "CURRENT", "?", "Reticle", "Wire Highlight" }.Select(name => s.fx[name]).Append(s.wattson.gadget.gameObject).ToArray();
            foreach (var go in shown) go.SetActive(true);
            foreach (var label in s.words.GetComponentsInChildren<TextMeshPro>()) label.ForceMeshUpdate();

            var go2 = Node("Preview Camera");
            var cam = go2.AddComponent<Camera>();
            cam.CopyFrom(s.camera);
            cam.scene = scene; // ignore whatever other scene is open beside this one
            go2.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;

            void Render(int width, int height, string file)
            {
                var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                // URP's supported way to render one camera on demand, also from inside a running editor.
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
                RenderPipeline.SubmitRenderRequest(cam, request);
                RenderTexture.active = target;
                var image = new Texture2D(width, height, TextureFormat.RGB24, false);
                image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                File.WriteAllBytes(file, image.EncodeToPNG());
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(target);
                Object.DestroyImmediate(image);
            }

            foreach (var shot in s.cams)
            {
                cam.transform.SetPositionAndRotation(shot.Value.transform.position, shot.Value.transform.rotation);
                cam.fieldOfView = shot.Value.Lens.FieldOfView;
                Render(960, 540, "Logs/Scene04/" + shot.Key + ".png");
                if (shot.Key.StartsWith("SH05")) Render(800, 600, "Logs/Scene04/" + shot.Key + " 4x3.png");
            }

            // Portrait for the magnifier on the ASK WATTSON button.
            Vector3 face = s.wattson.transform.position + Vector3.up * 0.1f;
            cam.transform.position = face + s.wattson.transform.forward * 0.62f;
            cam.transform.LookAt(face);
            cam.fieldOfView = 30;
            cam.backgroundColor = new Color(0.08f, 0.12f, 0.2f);
            s.wattson.gadget.gameObject.SetActive(false);
            const string portrait = Shared + "/UI/WattsonFace.png";
            Render(256, 256, portrait);
            AssetDatabase.ImportAsset(portrait);
            palette.wattsonFace = AssetDatabase.LoadAssetAtPath<Texture2D>(portrait);
            EditorUtility.SetDirty(palette);

            Object.DestroyImmediate(go2);
            foreach (var go in shown) go.SetActive(false);
        }
    }
}
