using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace OhmsLaw
{
    // The current you can hear (audio WP-E): one per scene, installed by ModuleFlow.EnterScene when the scene has a
    // WireInterior (Scenes 5, 6, 12) or a CircuitRig (Scenes 8-13); it lives in the scene and goes with it. A normalized
    // current intensity k in [0, 1] drives three library loops through their AudioBus gain, so the learner's sliders and the
    // AudioDucker apply as everywhere:
    //   wire scenes:  k = fieldOn && !frozen ? (rig ? I / 1 A : driftSpeed / 0.6 m/s) x (1 - 0.55 impede) : 0   (WireInterior.Drift)
    //   lab scenes:   k = keyClosed ? I / 1 A : 0,  I = ExperimentData.CurrentAt(V) = V / 5 ohm   (the current, not the voltage)
    //   LOOP_Electric_Charge_Hum   volume = gain x smoothstep(k), pitch 0.94 -> 1.08           (Electrical group, Sfx bus)
    //   LOOP_Electricity_Sparks    volume = gain x max(0, k - 0.35) / 0.65 x 0.7                (texture above k = 0.35)
    //   BGM_Conductor_Pulse        volume = gain x 0.5 x smoothstep(k), wire-only scenes (no bench): a layer under Airy, Music bus
    // k follows the live value with an exponential lerp (tau 1.0 s up, 0.4 s down: silent about 1.2 s after the current
    // stops, then the sources stop). Nothing is driven per particle; every source is 2D.
    public class ElectricalAudio : MonoBehaviour
    {
        const string HumClip = "LOOP_Electric_Charge_Hum", SparksClip = "LOOP_Electricity_Sparks", PulseClip = "BGM_Conductor_Pulse";
        const float FullAmps = 1f, FullDrift = 0.6f, RiseTau = 1.0f, FallTau = 0.4f, PulseCeiling = 0.5f;

        WireInterior wire;
        CircuitRig rig;
        AudioSource hum, sparks, pulse;
        AudioBus humBus, sparksBus, pulseBus;
        float humGain, sparksGain, pulseGain; // the clips' library gains
        float k;                              // the smoothed intensity
        bool leaving;
        static readonly HashSet<string> warned = new HashSet<string>(); // a missing clip is reported once per session

        // Called by ModuleFlow.EnterScene: finds the scene's wire interior / bench once and installs the component in the
        // active scene (destroyed with it); nothing when the scene has neither.
        public static ElectricalAudio Install()
        {
            var wire = FindFirstObjectByType<WireInterior>(FindObjectsInactive.Include);
            var rig = FindFirstObjectByType<CircuitRig>(FindObjectsInactive.Include);
            if (!wire && !rig) return null;
            var existing = FindFirstObjectByType<ElectricalAudio>();
            if (existing) return existing;
            var audio = new GameObject("ElectricalAudio").AddComponent<ElectricalAudio>();
            audio.wire = wire;
            audio.rig = rig;
            return audio;
        }

        void OnEnable() => ModuleFlow.Leaving += Leave;
        void OnDisable() => ModuleFlow.Leaving -= Leave;

        void Start()
        {
            var electrical = AudioBus.Group("Electrical");
            if (!electrical) electrical = AudioBus.Group("SFX");
            hum = Loop(HumClip, AudioBus.Kind.Sfx, electrical, out humBus, out humGain);
            sparks = Loop(SparksClip, AudioBus.Kind.Sfx, electrical, out sparksBus, out sparksGain);
            if (wire && !rig) pulse = Loop(PulseClip, AudioBus.Kind.Music, AudioBus.Group("Music"), out pulseBus, out pulseGain);
            AudioDucker.RescanSoon();
        }

        // A silent, stopped 2D loop on its own bus. The bus keeps its nominal gain of 1 and is driven below it, so AudioBus
        // never carries the loop into the next scene as a bed (only a bed at its full nominal gain is carried).
        AudioSource Loop(string clipName, AudioBus.Kind kind, AudioMixerGroup group, out AudioBus bus, out float gain)
        {
            bus = null;
            gain = 0;
            var clip = AudioLibrary.ClipOrNull(clipName);
            if (!clip)
            {
                if (warned.Add(clipName)) Debug.LogWarning("ElectricalAudio: no library clip '" + clipName + "' (Ohm's Law/Audio/Build library); skipped.", this);
                return null;
            }
            var go = new GameObject("Electrical " + clipName);
            go.transform.SetParent(transform, false);
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0;
            source.volume = 0;
            source.outputAudioMixerGroup = group;
            bus = go.AddComponent<AudioBus>();
            bus.kind = kind;
            bus.gain = 0;
            gain = AudioLibrary.GainOf(clipName);
            return source;
        }

        void Update()
        {
            float target = leaving ? 0 : Intensity();
            k = Mathf.Lerp(k, target, 1 - Mathf.Exp(-Time.unscaledDeltaTime / (target > k ? RiseTau : FallTau)));
            if (target <= 0 && k < 0.005f) k = 0;
            var level = Map(k);
            Drive(hum, humBus, humGain * level.hum, level.pitch);
            Drive(sparks, sparksBus, sparksGain * level.sparks, 1);
            Drive(pulse, pulseBus, pulseGain * level.pulse, 1);
        }

        // The live normalized current, before smoothing: 1 at 1 A, or at the wire's built 0.6 m/s drift when no bench drives it.
        float Intensity()
        {
            if (wire && wire.isActiveAndEnabled)
            {
                if (!wire.fieldOn || wire.frozen) return 0;
                float drive = wire.rig && wire.driftPerAmp > 0 ? Amps(wire.rig) / FullAmps : wire.driftSpeed / FullDrift;
                return Mathf.Clamp01(drive * (1 - 0.55f * wire.impede));
            }
            return rig ? Mathf.Clamp01(Amps(rig) / FullAmps) : 0;
        }

        // Ohm's law on the bench: the current the source voltage drives through the 5 ohm wire XY while the key is closed.
        static float Amps(CircuitRig rig)
        {
            if (!rig.state.keyClosed) return 0;
            var data = ExperimentData.Main;
            return data ? data.CurrentAt(rig.state.voltage) : rig.state.voltage / 5f;
        }

        static void Drive(AudioSource source, AudioBus bus, float gain, float pitch)
        {
            if (!source || !bus) return;
            bus.gain = gain;
            source.pitch = pitch;
            if (gain > 0 && !source.isPlaying) source.Play();
            else if (gain <= 0 && source.isPlaying) source.Stop();
        }

        // The scene hands off: the loops fade out with it and stop looping, so a long clip cannot run on under the next scene.
        void Leave(string next)
        {
            leaving = true;
            foreach (var source in new[] { hum, sparks, pulse }) if (source) source.loop = false;
        }

        void OnDestroy()
        {
            foreach (var source in new[] { hum, sparks, pulse }) if (source) source.Stop();
        }

        public struct Levels { public float hum, sparks, pulse, pitch; }

        // The mapping from the smoothed intensity to each loop's level, as a fraction of its library gain, and the hum's pitch.
        public static Levels Map(float k)
        {
            k = Mathf.Clamp01(k);
            float s = k * k * (3 - 2 * k); // smoothstep
            return new Levels
            {
                hum = s,
                sparks = Mathf.Clamp01((k - 0.35f) / 0.65f) * 0.7f,
                pulse = PulseCeiling * s,
                pitch = Mathf.Clamp(0.94f + 0.14f * k, 0.9f, 1.12f),
            };
        }

        // Self-check (no Unity objects): k = 0, 0.5, 1 give silence at rest, monotonic levels and pitch, and the ceilings hold.
        public static void Check()
        {
            Levels a = Map(0), b = Map(0.5f), c = Map(1);
            bool ok = a.hum == 0 && a.sparks == 0 && a.pulse == 0
                && a.hum < b.hum && b.hum < c.hum && c.hum <= 1
                && a.sparks <= b.sparks && b.sparks < c.sparks && c.sparks <= 0.7f
                && a.pulse < b.pulse && b.pulse < c.pulse && c.pulse <= PulseCeiling
                && a.pitch < b.pitch && b.pitch < c.pitch && a.pitch >= 0.9f && c.pitch <= 1.12f;
            if (!ok) throw new System.Exception("ElectricalAudio.Check: the intensity mapping is not monotonic or exceeds its ceilings");
            Debug.Log("PASS ElectricalAudio mapping: hum " + a.hum + " / " + b.hum + " / " + c.hum + ", pitch " + a.pitch + " -> " + c.pitch);
        }
    }
}
