# Ohm's Law — audio implementation report (2026-10-14)

Labels: **Observed** (seen/measured), **Implemented** (code or assets), **Tested** (built and played hands-free),
**Verified** (confirmed in the recording by measurement). Nothing below claims a listening sign-off: no human has
listened to the mix yet; levels were measured.

## 1. Initial audit (Observed, before this pass)
- Every sound was synthesized in code (`Editor/SfxBank.cs`: pads, hum, clicks, chimes) plus one mp3 (`bgm.mp3`) at the
  finale; the HUD synthesized its own clicks at runtime. No downloaded music or effects.
- Music: one pad for Scenes 7–13 (`PadLab`), a darker pad for 5–6 (`PadWire`), a bank-made title pad from the
  morning pass; no theme per chapter, no discovery jingles, no character sounds, no scanner/transition design.
- Bed changes were a one-source dip (fade out, swap, fade in) — an audible hole between beds.
- Ducking existed (music 30 %, other 55 %, 0.15 s / 1.0 s) and worked; voice always intelligible (measured −21 LUFS
  integrated, LRA 8.7, peaks −1.7 dBFS; no silence gap > 2.5 s).
- Electrical audio did not follow the simulation (a hum cue at the key close only); per-volt pitch steps had been
  silent until the morning fix.
- Mixer had Master → Music, Ambience, SFX, UI, Voice; no Character / Electrical groups.
- Not broken: no missing references, no overlapping beds, no clipping.

## 2. Downloaded and applied (Implemented)
See `Ohms_Audio_Asset_Manifest.md` (71 clips, source file names, authors, licences, URLs, measured levels, gains):
- OpenGameArt CC0: Intro Music (RonyDkid) → `BGM_Professor_Ohm_Laboratory`; Magic Puzzle In-Game 1 (MintoDog) →
  `BGM_Wattson_Magical_Discovery`; Dark Sci-Fi Audio Pack (SRG774): airy / pulse / sector / victory / hover →
  `BGM_Conductor_Airy` / `BGM_Conductor_Pulse` / `BGM_Conductor_Mystery` / `SFX_Discovery_Victory` /
  `SFX_SciFi_UI_Hover`; Claimed by the Void (vitalezzz) loop → `BGM_Electric_Current_Experiment` (imported,
  auditioned by level, **not scheduled**: at −9.9 LUFS it is the most intense track and would compete with narration;
  the Pulse bed carries the experiment instead).
- Kenney CC0: Music Jingles (PIZZI01 / STEEL07 / PIZZI07 / STEEL14 → Correct / Experiment Complete / Scientific
  Discovery / Final Success), Interface Sounds (UI family + part select/place, key switch, chimes), Digital Audio
  (terminal tones, current marker, drift chime, scanner, pops), Sci-Fi Sounds (force field, low impact, scan charge).
- Mixkit sound effects (SFX free licence; no Mixkit music): electricity (glitch, buzz, whoosh, power-ups, charge hum,
  crackles, sparks, hums), magic (sparkle whoosh, fairy sparkle, wand sparkle, light moving, sweep, poof, success),
  transitions (fast whoosh, tunnel woosh, sci-fi sweep, tech alert).
- All imports: Vorbis; music and loops stream (quality 0.6, load in background); effects compressed in memory, < 2 s
  decompressed on load (`Editor/AudioLibraryBuilder.cs`).

## 3. Architecture (Implemented)
- `Shared/AudioLibrary.cs` (ScriptableObject `Resources/OhmsAudioLibrary.asset`, inspectable: every entry shows its
  clip, group, gain, loop, source, licence; aliases; cue rules) built by `Ohm's Law/Audio/Build library` from
  `Audio/CueDefinitions/library.json` (written by `Tools/audio_organize.py`, hand-editable).
- `Shared/SceneDirector.cs`: `Sound()` resolves library-first (alias → clip, gain, mixer group), bank fallback;
  one-shots pooled (6) with per-call group and pitch; **two-source equal-power crossfades** for `music` (2.0 s) and
  `ambience` (1.5 s) that keep the bed carry across chapter loads; `CueSound()` plays the matching cue rule at the
  end of every `Run()` (silent during REPLAY/SKIP jumps).
- `Shared/AudioBus.cs`: library trim under the bus gain; group by name with SFX fallback. Mixer: Character and
  Electrical groups added by reflection (same route as the existing mixer creation).
- `Shared/ElectricalAudio.cs` (installed by `ModuleFlow.EnterScene` in scenes with a `WireInterior` or `CircuitRig`):
  normalized current k = I / 1 A (lab: `keyClosed ? ExperimentData.CurrentAt(V) : 0`, V = I × R with R = 5 Ω; wire:
  field on and not frozen → drift-based), hum volume smoothstep(k), pitch 0.94–1.08, sparks above 0.35, Pulse layer
  ≤ 0.5 in wire scenes, exponential lerps τ 1.0 s up / 0.4 s down, sources stop at k = 0; `Check()` self-test.
- `Shared/WattsonController.cs`: a 2D Character-group source; whoosh/settle on marker moves ≥ 0.8 m (2 s cooldown),
  wave on Greet, pop on Thinking, magic at the Transform/RaiseGadget action moment; `gestureSounds` off in Scene 4.
- `Shared/HudKit.cs` UiSounds: library `ui.*` clips at their gains on the UI group, synth fallback; hover mouse-only.
  `MCQPanel`: correct → jingle, wrong → gentle error. `HudScreens`: START → confirm. `ModuleFlow`: title bed =
  lab theme, 0.6 s fade on START, WebGL guard (bed restarts on the first tap/key if the context was blocked).
- Scene 4: Timeline audio tracks (music clips with clipIn at bar boundaries, ease/blend 1.5–6 s; 16 effects at the
  reveal activations). Scenes 7–13: music-state cues and the few one-shots the rules do not cover.
- Scenes 5–6: untouched by request; their beds and effects come from aliases, 39 cue rules and the runtime components.

## 4. Testing (Tested / Verified)
- Compile: whole tree clean (`Tools/compile_check.sh`). Editor chain on the open editor: `Ohm's Law/Audio/Build library`
  (71 clips, 31 aliases, 39 cue rules; mixer groups Character + Electrical added), `Build Scene 04`, `Scene07` …
  `Scene13` one by one (Scenes 5/6 never rebuilt — `git status` confirms their files unchanged), `Polish/Apply study +
  lab (Scenes 7-13)` (a new menu that skips 5/6), eight self-checks: **492 pass, 0 fail**.
- Hands-free full run `all record ask fresh @1920x1080` (first audio run, 17:25): **RESULT PASS**, every line of every
  scene in order, all gates, Ask Wattson probe, no console errors (the only logged exception is the offline Ask
  Wattson proxy, as before). **Measured**: title bed present before START (−32 dB mean in the first 1.5 s), no silence
  gap > 2.5 s, but true peak **+0.5 dBFS** with 22 windows above −1.5 dBFS stacked at the dive flash, the lab reveal,
  the key close, the hero stings and the end card.
- Fix (data only): one-shot reference levels lowered 4 dB for SFX and 2 dB for Character/UI, the hottest cue rules
  (dive, flash, hero, end, look, field wave, battery connect) lowered; library rebuilt; **second full run (17:40):
  RESULT PASS, −20.8 LUFS integrated, LRA 5.9 LU, true peak −1.8 dBFS, one window above −2 dBFS (the Scene 9 key
  close at −1.8), no gap > 2.5 s.** Per scene (`Reports/OhmsLawAudit/audio2/audio_report.md`): voice 8.5–13.9 dB above
  the bed in every chapter except the sweep-heavy Scene 8 reveal (6.6 dB); integrated −19.7 … −22.8 LUFS.
- Verified in the recording by measurement: beds present under every chapter (20th-percentile levels −27 … −37 dB,
  never silent), no bed dip at the chapter changes (no near-silence windows), ducking keeps the voice on top. The
  evidence run: `Reports/OhmsLawAudit/audio2/Gameplay_Full_Run.mp4` (+ `Deliverables/ohms_law_AUDIO_2026-10-14_720p.mp4`).
- Not verified by ear: the musical fit of each bed passage, the character family's charm, whether any one-shot still
  reads as a double at the OHM'S LAW hero (jingle + Magic_Success 0.2 s apart). A listening pass is still required.
- WebGL: not built this session; the title bed's first-gesture restart guard and the START gesture are in code only.

## 5. Outstanding
1. Listening sign-off (headphones, laptop, tablet) and per-clip gain tweaks in `library.json` → `Build library`.
2. `BGM_Electric_Current_Experiment` imported but unused (too intense for the classroom tone); drop or schedule later.
3. Scene 4's Timeline clip volumes were baked with the first (hotter) gains; its peak stayed at −2.5 dBFS, so it was
   left as built — rebuild Scene 4 once if the family should be 4 dB lower there too.
4. Scenes 5/6 beds are aliases of the old cue names (PadWire → Airy, WireHum → Hum_Low); if the user rebuilds them
   from their builders nothing changes, but any new wire beats need a cue rule, not a builder edit.
5. A WebGL build test (autoplay unlock, streaming clips, tab visibility) and the hum-follows-current feel on a device.
6. `BGM_Conductor_Mystery` (sector) plays only for the Scene 12 pop-up; `SFX_SciFi_UI_Hover` and `UI_Scroll` are imported
   for the sci-fi panel hover / slider drag but not wired (hover is mouse-only and already uses UI_Hover).
