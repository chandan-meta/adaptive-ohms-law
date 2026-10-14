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
            // Slow, readable moves (v5): every shot that carries a beat drifts for its whole length as a two-camera blend
            // (the Timeline's ease in / out makes each a dolly, push or orbit); the cuts between them are all 1.5 s or
            // longer and no orbit turns faster than 6 deg/s. Reduced motion keeps the same moves: ReducedMotionCamera only
            // drops the handheld noise.
            Move("SH01", "SH01a Arrival", "SH01b Arrival");                      // crane down and drift in as the group lands
            Move("SH02", "SH02a Wattson Enters", "SH02b Wattson Enters");        // tracking dolly on Wattson's flight
            Shot("SH03 Welcome", start["SH03"], end["SH03"]);
            Move("SH04", "SH04a Over Shoulder", "SH04b Rack To Ohm");            // lateral dolly past the lamp and candle to Ohm, Ohm in focus; no cut
            Shot("SH06a Orbit", start["SH05"], end["SH06"] - 0.05);              // the 25-degree orbit round the words over both shots, ~2.7 deg/s
            Shot("SH06b Orbit", start["SH05"] + 0.05, end["SH06"]);
            Shot("SH07 Before", start["SH07"], end["SH07"]);
            Move("SH08", "SH08a Hero", "SH08b Hero");                            // the low-angle push on the Ohm-niscope
            Move("SH09", "SH09a Aim", "SH09b Aim");                              // the push to the wire: the lock-on
            Shot("SH10 Everybody In", start["SH10"], end["SH10"]);
            Shot("SH11a Dive", start["SH11"], end["SH11"] - 1.0);                 // holds here for the tap, then the dive
            Shot("SH11b Dive", T("SH11", 0.4), end["SH11"]);
            var shots = cameras.GetClips().OrderBy(c => c.start).ToArray();
            for (int i = 1; i < shots.Length; i++)
            {
                double overlap = shots[i - 1].end - shots[i].start;
                if (overlap <= 0) continue;
                shots[i].blendInDuration = overlap;
                shots[i - 1].blendOutDuration = overlap;
            }
            // The plan's two rules, checked on the built track so a bad edit fails at build time: every clip lasts 1.5 s or
            // more (no shorter cut) and no blend turns the view faster than 6 deg/s on average.
            foreach (var clip in shots)
                if (clip.duration < 1.5) Debug.LogError($"Scene04 shot plan: {clip.displayName} lasts {clip.duration:F2} s (under 1.5 s)");
            for (int i = 1; i < shots.Length; i++)
            {
                double overlap = shots[i - 1].end - shots[i].start;
                if (overlap <= 0) continue;
                float turn = Quaternion.Angle(s.cams[shots[i - 1].displayName].transform.rotation, s.cams[shots[i].displayName].transform.rotation);
                if (turn / overlap > 6) Debug.LogError($"Scene04 shot plan: {shots[i - 1].displayName} to {shots[i].displayName} turns {turn / overlap:F1} deg/s (over 6)");
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
            // SH04 is over his shoulder: for L02's first 2 s (the line starts at 0.3) he keeps his look on the camera, so his face
            // turns to it instead of the hat brim filling the frame, then glances at Ohm, then back to the group.
            Pose(start["SH04"], T("SH04", 2.3), WattsonController.Gesture.Point, s.camera.transform, s.ohmPoint);
            Pose(T("SH04", 2.3), T("SH04", 3.2), WattsonController.Gesture.Point, s.ohmPoint, s.ohmPoint);
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

            // --- Sound effects and music from the audio library (Audio_Plan.md, work package 4). Every clip plays at its library
            // gain (which levels the clips against each other) times the cue's own level; the synthesized bank is the fallback
            // for a library clip that is missing. Effects take as many lanes as they overlap, so none ever crossfades or slips.
            var sfxTracks = new List<AudioTrack>();
            var busyUntil = new List<double>();
            void Sfx(string name, double time, float level = 1f)
            {
                var clip = Library(name, out float gain);
                if (!clip && !sfx.TryGetValue(name, out clip)) return;
                int lane = busyUntil.FindIndex(until => until <= time);
                if (lane < 0)
                {
                    lane = sfxTracks.Count;
                    var track = timeline.CreateTrack<AudioTrack>(null, "SFX " + (char)('A' + lane));
                    director.SetGenericBinding(track, s.sfx);
                    sfxTracks.Add(track);
                    busyUntil.Add(0);
                }
                var placed = sfxTracks[lane].CreateClip(clip);
                placed.displayName = name;
                placed.start = time;
                placed.duration = clip.length;
                ClipVolume(placed, gain * level);
                busyUntil[lane] = placed.end + 0.05;
            }

            var musicTrack = timeline.CreateTrack<AudioTrack>(null, "Music");
            director.SetGenericBinding(musicTrack, s.music);
            TimelineClip Bed(AudioClip clip, string label, double from, double to, double clipIn, float volume)
            {
                var bed = musicTrack.CreateClip(clip);
                bed.displayName = label;
                bed.start = from;
                bed.duration = to - from;
                bed.clipIn = clipIn;
                ((AudioPlayableAsset)bed.asset).loop = true;
                ClipVolume(bed, volume);
                return bed;
            }
            // Professor Ohm's laboratory theme under the arrival (START fades the title bed, the same track, out over 0.6 s while
            // this one comes up over 1.5 s), in from bar 4: the track runs at 70 bpm with its downbeats at 0.85 + n x 3.43 s, and
            // 11.13 s opens its calm steady passage (-18 dB, well before the louder section at 35 s), so it never sounds like the
            // title starting over. At SH02 a 2 s crossfade hands over to Wattson's theme, whose second downbeat (110 bpm,
            // downbeats at 0.52 + n x 2.18 s) lands on his entrance.
            var laboratory = Library("BGM_Professor_Ohm_Laboratory", out float laboratoryGain);
            if (!laboratory) laboratory = sfx["PadLab"];
            var discovery = Library("BGM_Wattson_Magical_Discovery", out float discoveryGain);
            const double crossfade = 2.0, laboratoryIn = 11.13, discoveryDownbeat = 2.70;
            double swap = T("SH02");
            if (!discovery) Bed(laboratory, "Laboratory", 0, total, laboratoryIn, laboratoryGain).easeInDuration = 1.5;
            else
            {
                var lab = Bed(laboratory, "Laboratory", 0, swap + crossfade / 2, laboratoryIn, laboratoryGain);
                lab.easeInDuration = 1.5;
                lab.blendOutDuration = crossfade;
                // Full through SH07; over SH08 (the Ohm-niscope hero) it eases down to 40 %: two clips of the same passage,
                // sample-aligned, crossfade from full to 0.4, so the dip is one continuous bed. The quieter bed holds through
                // SH09's lock-on and goes out over SH10, silent before SH11's dive so Scene 5's Airy enters clean.
                double dipFrom = start["SH08"], dipTo = end["SH08"], outFrom = start["SH10"], outTo = start["SH11"];
                double discoveryIn = discoveryDownbeat - crossfade / 2;
                var theme = Bed(discovery, "Wattson theme", swap - crossfade / 2, dipTo, discoveryIn, discoveryGain);
                theme.blendInDuration = crossfade;
                theme.blendOutDuration = dipTo - dipFrom;
                var quiet = Bed(discovery, "Wattson theme 40%", dipFrom, outTo, discoveryIn + (dipFrom - theme.start), discoveryGain * 0.4f);
                quiet.blendInDuration = dipTo - dipFrom;
                quiet.easeOutDuration = outTo - outFrom;
            }
            // The title bed rides on the music bus without playing from it: ModuleFlow plays the carried clip (the laboratory
            // theme, from its start) on a bus of its own while the title screen holds this Timeline at 0 (a music bus's bgm with
            // Play On Start off only carries the clip).
            var musicBus = s.music.GetComponent<AudioBus>();
            musicBus.bgm = laboratory;
            musicBus.playOnStart = false;
            var ambienceTrack = timeline.CreateTrack<AudioTrack>(null, "Ambience");
            director.SetGenericBinding(ambienceTrack, s.ambience);
            var ambience = ambienceTrack.CreateClip(sfx["StudyTone"]);
            ambience.start = 0;
            ambience.duration = total;
            ((AudioPlayableAsset)ambience.asset).loop = true;

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
            // The same, calling Ohm's Attend(target): he glances at `target` until a plain Signal(..., s.ohm.Attend) clears it.
            void SignalLook(string name, double time, Transform target)
            {
                var asset = Asset(S4 + "/Timeline/" + name + ".signal", ScriptableObject.CreateInstance<SignalAsset>);
                signals.CreateMarker<SignalEmitter>(time).asset = asset;
                var reaction = new UnityEvent();
                UnityEventTools.AddObjectPersistentListener<Transform>(reaction, s.ohm.Attend, target);
                s.receiver.AddReaction(asset, reaction);
            }

            // SH01 arrival and SH02 entrance: the title's charge streams shimmer under the crane's whoosh; Wattson sparkles in
            // as his flight starts (0.3) and whooshes toward the lens as he closes on it (he arrives at 3.5).
            Active("Charge Streams Blue", 0, 5);
            Active("Charge Streams Gold", 0, 5);
            Active("Landing Dust", 0.7, 4.5);
            Sfx("SFX_Magic_Gather", 0, 0.6f);
            Sfx("SFX_Whoosh_Fast", 0, 0.5f);
            Sfx("SFX_Wattson_Appear", T("SH02", 0.3), 0.9f);
            Sfx("SFX_Wattson_Whoosh", T("SH02", 1.0), 0.8f);
            // SH03: the wave, as the Greet gesture lifts his hand.
            Sfx("SFX_Wattson_Wave", T("SH03", 0.2), 0.9f);
            // SH04: Ohm looks up at Wattson as he is pointed out (Wattson glances back at 2.3), then back to the galvanometer.
            SignalLook("Scene04_OhmAttend", T("SH04", 1.2), s.wattson.transform);
            Signal("Scene04_OhmAttendOff", T("SH04", 4.6), s.ohm.Attend);
            // SH05 words (his flick's blue spark trail gathers from the shot's start, the words pop at 0.7 and 1.7), SH06 arc and
            // question mark, SH07 drift
            double wordsOff = T("SH11", 1.0);
            Sfx("SFX_Magic_Gather", start["SH05"], 0.8f);
            Active("VOLTAGE", T("SH05", 0.7), wordsOff);
            Sfx("SFX_Voltage_Reveal", T("SH05", 0.7), 0.9f);
            Active("CURRENT", T("SH05", 1.7), wordsOff);
            Sfx("SFX_Current_Reveal", T("SH05", 1.7), 0.9f);
            Active("Arc", T("SH06", 0.2), end["SH06"] - 0.2);
            Sfx("SFX_Static_Crackles", T("SH06", 0.2), 0.5f);
            Active("?", T("SH06", 1.9), end["SH07"]);
            Sfx("SFX_Fact_Chime", T("SH06", 1.9), 0.9f);
            Signal("Scene04_DriftWords", T("SH07", 0.2), scene04.DriftWords);
            // SH08 Ohm-niscope: the gadget comes out with the RaiseGadget pose at the shot's start (the click), its rings charge
            // through the slow motion and flash at about 2.2 (inside the charge's tail). SH09 lock-on: the reticle at 1.3.
            Signal("Scene04_SlowMotion", T("SH08", 0.15), scene04.SlowMotion);
            Sfx("SFX_Apparatus_Click", start["SH08"], 0.9f);
            Sfx("SFX_Scan_Charge", T("SH08", 0.1), 0.5f);
            Active("Reticle", T("SH09", 1.3), T("SH11", 1.2));
            Sfx("SFX_Scanner_Ping", T("SH09", 1.3), 0.9f);
            Active("Wire Highlight", T("SH09", 1.8), total);
            // SH11: the tap (the instruction stays in the panel until the wire is tapped; the hold at 0.1 freezes the graph and
            // its audio with it), then the dive: the lock confirms the tap the moment the Timeline resumes, the electric whoosh
            // rises into SH11b's dive, and the low impact lands on the match cut to Scene 5 (Complete, total - 0.7).
            Signal("Scene04_WaitForTap", T("SH11", 0.1), scene04.WaitForTap);
            Sfx("SFX_Scanner_Lock", T("SH11", 0.2), 0.9f);
            Sfx("SFX_Electric_Whoosh", T("SH11", 0.3), 0.9f);
            Active("Beam", T("SH11", 0.45), total);
            Active("Dive Streaks", T("SH11", 0.8), total);
            Sfx("SFX_Impact_Low", total - 0.7, 0.6f);
            Signal("Scene04Complete", total - 0.7, scene04.Complete);

            EditorUtility.SetDirty(timeline);
        }

        // ------------------------------------------------------------------ the audio library (Assets/OhmsLaw/Audio)

        [Serializable] class LibraryEntry { public string name, path; public float gain = 1f; }
        [Serializable] class LibraryFile { public LibraryEntry[] entries; }
        static Dictionary<string, LibraryEntry> libraryEntries;

        // A downloaded library clip by its name in Ohms_Audio_Asset_Manifest.md, with the gain that levels it against the
        // others. Read from library.json at build time (the Resources asset may not be built yet); a name the json lacks is
        // looked up by file name under Audio at gain 1. Null when there is no such clip, so the caller can fall back.
        static AudioClip Library(string name, out float gain)
        {
            gain = 1f;
            if (libraryEntries == null)
            {
                libraryEntries = new Dictionary<string, LibraryEntry>();
                var file = File.Exists(AudioLibraryBuilder.Json) ? JsonUtility.FromJson<LibraryFile>(File.ReadAllText(AudioLibraryBuilder.Json)) : null;
                foreach (var entry in file?.entries ?? new LibraryEntry[0])
                    if (!string.IsNullOrEmpty(entry.name)) libraryEntries[entry.name] = entry;
            }
            libraryEntries.TryGetValue(name, out var found);
            string path = found?.path;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                path = AssetDatabase.FindAssets(name + " t:AudioClip", new[] { Root + "/Audio" }).Select(AssetDatabase.GUIDToAssetPath)
                    .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p) == name);
            var clip = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (!clip)
            {
                Debug.LogWarning("Scene04: no library clip named " + name + " under " + Root + "/Audio; the synthesized bank stands in");
                return null;
            }
            if (found != null) gain = Mathf.Clamp01(found.gain);
            return clip;
        }

        // A Timeline audio clip's own volume (its AudioClipProperties are not public): the library gain times the cue's level.
        static void ClipVolume(TimelineClip clip, float volume)
        {
            var asset = new SerializedObject(clip.asset);
            asset.FindProperty("m_ClipProperties").FindPropertyRelative("volume").floatValue = Mathf.Clamp01(volume);
            asset.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------ placeholder sound, synthesized

        static Dictionary<string, AudioClip> BuildSfx()
        {
            libraryEntries = null; // library.json is read again on every build
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

            foreach (var recipe in SfxBank.Recipes()) recipes[recipe.Key] = recipe.Value; // the designed bank (SfxBank.cs)
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
