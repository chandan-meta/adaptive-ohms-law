# Ohm's Law audio redesign — plan and contract (2026-10-14)

Brief: full BGM / SFX / ambience / character / electrical audio redesign with the downloaded CC0 + Mixkit assets, event
driven, narration first. Benchmark: the Photosynthesis module (two-source crossfades ≈2 s, music ducked −10 dB under the
voice with 0.2 s attack / 0.9 s release, synthesized UI cues, finale cadence).

## What already exists (reuse, do not duplicate)
- `Shared/AudioBus.cs` (Voice / Sfx / Music kinds, gain × learner slider, beds carried across scene loads, mixer group
  routing by name), `Shared/AudioDucker.cs` (music → 30 %, other → 55 % under any voice; 0.15 s attack, 1.0 s release),
  `Shared/VoiceChannel.cs`, mixer `Resources/OhmsLawMix.mixer` (Master → Music, Ambience, SFX, UI, Voice; the library
  builder adds **Character** and **Electrical**).
- `Shared/SceneDirector.cs`: Timeline cues `music <clip|off>` (2.5 s swap), `ambience <clip|off>` (1.5 s), `sfx <clip>`;
  `Sound(name)` resolves from the scene's serialized `sounds` bank; `Play` pools pitched one-shots. Gates play
  Select / Place / Switch / Reveal; the experiment plays Record / Plot / VoltStep (pitched per volt); the wire counter
  plays Count. `HudKit.UiSounds` synthesizes hover / click / open / close / done / hint / stamp. `ModuleFlow` plays the
  title bed (`titleMusic`, else the scene's carried bgm) and fades it out 0.6 s on START.
- Scene 4 has its own Timeline audio tracks (Music = PadLab loop, Ambience = StudyTone, SFX A/B clips from
  `Scene04Builder.Timeline.cs` `Sfx(name, time)`).

## New core (already written, compiles): `Shared/AudioLibrary.cs` + `Editor/AudioLibraryBuilder.cs`
- `AudioLibrary.Instance` (Resources `OhmsAudioLibrary`, built from `Audio/CueDefinitions/library.json` by menu
  `Ohm's Law/Audio/Build library`): `Clip(name)`, `Gain(name)` (0–1, levels the clips against each other), `Group(name)`
  (mixer group), `Resolve(name)` (alias: old bank names → new clips, e.g. `PadLab` → `BGM_Professor_Ohm_Laboratory`,
  `PadWire` → `BGM_Conductor_Airy`, `WireHum` → `LOOP_Electricity_Hum_Low`, `Select` → `SFX_Part_Select`, `ui.click` →
  `UI_Click` …), `Rule(cueText, now)` (first cue rule whose `cueStartsWith` matches, honouring its cooldown).
- Static safe helpers: `AudioLibrary.ClipOrNull(name)`, `GainOf(name)`, `GroupOf(name)`.
- 71 clips under `Assets/OhmsLaw/Audio/{Music,SFX}/…` (names in `Documentation/Ohms_Audio_Asset_Manifest.md`).
  Music: BGM_Professor_Ohm_Laboratory (82 s), BGM_Wattson_Magical_Discovery (122 s), BGM_Conductor_Airy / Pulse /
  Mystery, BGM_Electric_Current_Experiment. Success: SFX_Correct_Answer, SFX_Experiment_Complete,
  SFX_Scientific_Discovery, SFX_Final_Success, SFX_Discovery_Victory, SFX_Magic_Success. UI_*: Click, Select, Back, Open,
  Close, Confirm, Error, Toggle, Question, Hover, Stamp, Scroll, SFX_SciFi_UI_Hover. Character: SFX_Wattson_Appear /
  Whoosh / Wave / Pop / Settle / Magic, SFX_Magic_Gather, SFX_Magic_Poof. Electrical: SFX_Electric_Glitch / Buzz /
  Whoosh, SFX_Electronics_PowerUp, SFX_Voltage_Reveal, SFX_Current_Reveal, LOOP_Electric_Charge_Hum,
  LOOP_Electricity_Sparks, LOOP_Electricity_Hum_Intense, LOOP_Electricity_Hum_Low, SFX_Static_Crackles, SFX_Energy_Field
  (/ _Off), SFX_Terminal_Positive / Negative, SFX_Battery_Connect, SFX_Current_Marker, SFX_Drift_Chime. Scanner:
  SFX_Scanner_Ping / Lock, SFX_Scan_Charge, SFX_Apparatus_Click. Transitions: SFX_Whoosh_Fast, SFX_Tunnel_Woosh,
  SFX_SciFi_Sweep, SFX_Magic_Sweep, SFX_Tech_Alert, SFX_Impact_Low, SFX_Magic_Settle. Experiment: SFX_Voltage_Step,
  SFX_Record, SFX_Plot, SFX_Part_Select / Place, SFX_Key_Switch, SFX_Condition_Reached, SFX_Info_Chime, SFX_Fact_Chime.

## Hard rules
- **Scenes 5 and 6 and the wire set are the user's: never edit `Scene05Builder.cs`, `Scene06Builder.cs`,
  `SceneBuilderBase.Wire.cs`, `WireInterior.cs` (read only), never rebuild them.** Their audio comes from aliases
  (`music PadWire` → Airy, `ambience WireHum` → Hum_Low), the cue rules in `library.json` (`wire.field 1` →
  SFX_Battery_Connect, `show Drift Arrow` → SFX_Drift_Chime, `counter 1` → SFX_Current_Marker, …) and the runtime
  `ElectricalAudio` component (WP-E) that reads `WireInterior.fieldOn / frozen / driftSpeed` and `CircuitRig`.
- One editor; the orchestrator drives it. Agents edit code only and compile with `bash Tools/compile_check.sh <tag>`.
  No new voice lines. Reduced motion is irrelevant to audio; the learner's three sliders (Wattson / Effects / Music)
  and the ducker must keep covering every new source (every new AudioSource gets an `AudioBus`).
- Narration first: nothing louder than the voice; music beds at their library gain × the Music bus gain (0.35).

## Music states (event driven)
| Where | Bed | Trigger |
|---|---|---|
| Title | BGM_Professor_Ohm_Laboratory | title open (ModuleFlow title bed); fades 0.6 s on START |
| Scene 4 arrival (SH01) | BGM_Professor_Ohm_Laboratory (clipIn to a calm passage, eased in 1.5 s) | Timeline music track |
| Scene 4 Wattson enters (SH02) → V/C reveal | crossfade to BGM_Wattson_Magical_Discovery (Timeline clip overlap ≈2 s) | Timeline |
| Scene 4 hero → scanner → dive (SH08–SH11) | Wattson theme eases down over SH08–SH09, out by the dive | Timeline |
| Scene 5 | Airy (alias of the existing `music PadWire` at 1.8 s) + ElectricalAudio Pulse layer when the field is on | existing cues |
| Scene 6 | continues; `music off` at the exit (existing) | existing cues |
| Scene 7 (return) | BGM_Professor_Ohm_Laboratory (alias of `music PadLab`) | existing cue |
| Scene 8 reveal | BGM_Wattson_Magical_Discovery (alias of `music PadLabTheme`) | existing cue |
| Scene 9 build | Wattson theme continues | — |
| Scene 10 experiment | `music BGM_Conductor_Pulse` at scene start; ElectricalAudio hum follows the current | WP-S (cue) |
| Scene 11 pattern | Pulse continues; `music BGM_Wattson_Magical_Discovery` at the V ∝ I hero | WP-S |
| Scene 12 resistance | `music BGM_Conductor_Mystery` at the pop-up, back to Wattson theme at "You've already measured it" | WP-S |
| Scene 13 law → finale | `music BGM_Professor_Ohm_Laboratory` at L01; SFX_Final_Success at the end card | WP-S |
BGM_Electric_Current_Experiment is imported and auditioned by level only; it is **not** scheduled (too intense for the
classroom tone) — WP-S may use it for Scene 10 only if it keeps the gain ≤ 0.35 and says so.

## Work packages and ownership
| WP | Owner files | Deliver |
|---|---|---|
| **C** core hooks | `Shared/SceneDirector.cs` (`Sound()` falls back to the library with alias + gain; `Run()` plays the matching cue rule on the SFX/Character/Electrical group), `Shared/SceneDirector.Experiment.cs` + `.Gates.cs` (one-shots via the library gain), `Shared/HudKit.cs` (UiSounds use library clips `ui.hover/click/open/close/done/hint/stamp/wrong/start` when present, synth fallback), `Shared/MCQPanel.cs` (Correct → SFX_Correct_Answer, wrong → UI_Error), `Shared/LessonPanel.cs` (end card SFX_Final_Success + UI_Stamp; buttons), `Shared/HudScreens.cs` (START → UI_Confirm, pause open/close, chapter tiles), `Shared/ModuleFlow.cs` (title bed = `AudioLibrary.ClipOrNull("BGM_Professor_Ohm_Laboratory")` when `titleMusic` is empty; WebGL: the bed starts on the first pointer/key event if the AudioContext was blocked), `Shared/AudioBus.cs` (group by library), `Shared/AudioDucker.cs` only if needed | every UI/gate/experiment sound comes from the library, nothing missing, synth fallback kept |
| **E** electrical | new `Shared/ElectricalAudio.cs` (created by `ModuleFlow.EnterScene` or self-installing from the library: one per scene, finds `WireInterior` / `CircuitRig`), may add ≤ 5 lines in `ModuleFlow.cs` to install it (coordinate: only the install call) | normalized current intensity → LOOP_Electric_Charge_Hum (base, Electrical group) + LOOP_Electricity_Sparks (texture above ≈0.4) + BGM_Conductor_Pulse layer (Music group, wire scenes only, gain ≤ 0.5): volume, pitch ±8 %, smooth lerps (≈1.5 s), fade out at zero current, nothing per particle |
| **4** Scene 4 | `Editor/Scene04Builder.Timeline.cs`, `Editor/Scene04Builder.cs` (sound table / clips only) | Timeline beds per the table above; SFX at the real reveal frames: title shimmer (SFX_Magic_Gather at 0), camera whoosh SH01, SFX_Wattson_Appear at Wattson's entrance, SFX_Wattson_Whoosh on his flight, SFX_Wattson_Wave at the wave, SFX_Magic_Gather at the blue particles, SFX_Voltage_Reveal at the VOLTAGE word, SFX_Current_Reveal at CURRENT, SFX_Fact_Chime at the "?", SFX_Apparatus_Click + SFX_Scan_Charge at the niscope, SFX_Scanner_Ping at the reticle, SFX_Scanner_Lock at the tap, SFX_Electric_Whoosh rising into the dive, SFX_Impact_Low at the flash; replace Whoosh/Zip/Pop/Zap/Crackle/Shimmer/Click/Beam/Enter with library clips (keep the old names working as aliases if simpler) |
| **S** Scenes 7–13 | `Editor/Scene07Builder.cs` … `Scene13Builder.cs` | music cues per the table; SFX_Apparatus_Click / SFX_Magic_Poof on the Scene 8 swaps, SFX_Magic_Sweep + SFX_Impact_Low (gain 0.4) at the lab reveal, SFX_Discovery_Victory at "V × 2 → I × 2", SFX_Experiment_Complete at row 5, SFX_Scientific_Discovery at V ∝ I and at Ω, SFX_Final_Success + SFX_Magic_Success at OHM'S LAW; SFX_Wattson_Magic at Wattson's transformation cast; no sound per narration line |
| **W** Wattson | `Shared/WattsonController.cs` (gesture/move hooks only) | SFX_Wattson_Whoosh on a marker move ≥ 0.8 m (cooldown 2 s), SFX_Wattson_Wave on Greet, SFX_Wattson_Pop on Thinking, SFX_Wattson_Magic on Transform / RaiseGadget, SFX_Wattson_Settle when a move ends; Character group; never on blinks or idle |

Cue rules already in `library.json` (orchestrator): dive / flash / exits, clue cards, terminals, bracket, field wave,
battery connect, drift arrow, counter, freeze, gauge boost, spotlight, beam, niscope flash, hero plates, reveals,
stage, readings strip, schematic, drag slots, readouts, V/I column, slope triangle, highlights, vanish / appear, fx,
look, inset, end. WP-C makes `Run()` apply them; WP-S must not double a sound a rule already covers (check the list).

## Verification (orchestrator)
compile → `menu Ohm's Law/Audio/Build library` → `menu Ohm's Law/Build Scene 04` → `Scene07` … `Scene13` (never 05/06)
→ `menu Ohm's Law/Polish/Apply later scenes` is **not** run (it would touch 5/6) — instead `Apply lab (Scenes 8-13)` →
tests → play test `all record ask fresh @1920x1080` → audio analysis of the recording per section → report.
