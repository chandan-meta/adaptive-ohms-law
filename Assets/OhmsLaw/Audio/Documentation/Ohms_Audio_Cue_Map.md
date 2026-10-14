# Ohm's Law — audio cue map (2026-10-14)

Every sound is triggered by a real gameplay event: a Timeline marker placed by the scene builder, a cue rule matched
at runtime when a Timeline cue runs (`SceneDirector.Run` → `AudioLibrary.Rule`), a code hook (gates, HUD, Wattson),
or the simulation state (`ElectricalAudio`). No video timestamps are used. Clip names are the library names in
`Ohms_Audio_Asset_Manifest.md`; aliases (old bank name → clip) and rules live in `CueDefinitions/library.json` and in
the inspectable `Resources/OhmsAudioLibrary.asset`. Scenes 5 and 6 were **not** rebuilt (user-owned): everything they
play comes from aliases, rules and runtime components.

## Music states
| Trigger | Bed | Where it is triggered |
|---|---|---|
| Title screen opens | BGM_Professor_Ohm_Laboratory (0.35 × 0.75) | `ModuleFlow.TitleBed` (library clip when `titleMusic` is empty); fades 0.6 s on START |
| Scene 4 0–6 s (SH01 arrival) | BGM_Professor_Ohm_Laboratory, clipIn 11.13 s (bar 4), ease-in 1.5 s | Timeline Music track, `Scene04Builder.Timeline.cs` `Bed()` |
| Scene 4 SH02 Wattson enters (4–6 s blend) → SH08 | BGM_Wattson_Magical_Discovery (downbeat on SH02) | Timeline Music track |
| Scene 4 SH08–SH10 | Wattson theme to 40 %, out before SH11 (dive) | Timeline Music track |
| Scene 5 1.8 s (after the flash) | BGM_Conductor_Airy (alias of the existing `music PadWire`) + `ambience WireHum` → LOOP_Electricity_Hum_Low | existing Scene05 cues + alias |
| Scene 5 after the + tap | BGM_Conductor_Pulse layer rises with the current (≤ 0.5 × gain) | `ElectricalAudio` (wire scenes only) |
| Scene 6 exit | `music off`, `ambience off` (existing) | existing Scene06 cues |
| Scene 7 | BGM_Professor_Ohm_Laboratory (alias of `music PadLab`) | `Scene07Builder` cue |
| Scene 8 lab reveal | BGM_Wattson_Magical_Discovery (alias of `music PadLabTheme`) | `Scene08Builder` cue at the `look 2.8 classroom` |
| Scene 9 | Wattson theme continues (carried bed) | — |
| Scene 10 start | BGM_Conductor_Pulse (2 s equal-power crossfade) | `Scene10Builder` `Music()` at 0 |
| Scene 11 V ∝ I hero | BGM_Wattson_Magical_Discovery | `Scene11Builder` `Music()` −3.0 s into S11_L04 |
| Scene 12 pop-up (`inset 1`) | BGM_Conductor_Mystery; back to the Wattson theme before S12_L07 | `Scene12Builder` `Music()` cues |
| Scene 13 S13_L01 | BGM_Professor_Ohm_Laboratory | `Scene13Builder` `Music()` |
| End card | SFX_Final_Success once | cue rule `end` |

Bed changes are two-source equal-power crossfades (music 2.0 s, ambience 1.5 s; `SceneDirector.Crossfade`); the same
clip is a no-op; beds carry across chapter loads (`AudioBus.Carry/TakeOver`). Every bed is on a Music-kind `AudioBus`
(ducked to 30 % under the voice, 0.15 s attack, 1.0 s release; Music slider).

## Section cues (the brief's sections → real events)
| Brief section | Event | Clip(s) | Trigger location |
|---|---|---|---|
| 01 Start banner | title opens / "OHM'S LAW" shimmer | SFX_Magic_Gather (0.35) + SFX_Whoosh_Fast (0.5) at Timeline 0 | `Scene04Builder.Timeline.cs` placements |
| 01 | START JOURNEY pressed | UI_Confirm (`ui.start`) | `HudScreens.cs` 545 → `UiSounds.Play("start")` |
| 02 Wattson entrance | flight start 5.30 s / flight 6.00 s / wave 10.20 s | SFX_Wattson_Appear 0.9 / SFX_Wattson_Whoosh 0.57 / SFX_Wattson_Wave 0.9 | Timeline SFX lanes (gesture hooks muted in Scene 4 by `Scene04Director.Start`) |
| 03 V/C reveal | blue sparks 21.86 s / VOLTAGE 22.56 s / CURRENT 23.56 s / arc 27.06 s / "?" 28.76 s | SFX_Magic_Gather / SFX_Voltage_Reveal / SFX_Current_Reveal / SFX_Static_Crackles / SFX_Fact_Chime | Timeline SFX lanes at the reveal activations |
| 04 Ohm's experiment | niscope 34.07 s / reticle 41.37 s / tap-to-dive 47.27 s / rising whoosh 47.37 s / peak 50.37 s | SFX_Apparatus_Click + SFX_Scan_Charge / SFX_Scanner_Ping / SFX_Scanner_Lock / SFX_Electric_Whoosh / SFX_Impact_Low | Timeline SFX lanes; the tap gate holds the Timeline so Lock fires on the tap |
| 05 Arrival in the chamber | `wire.dive 1.8` / flash `fade 1 0.15` | SFX_Tunnel_Woosh 0.9 / SFX_Impact_Low 0.7 | cue rules (Scene 5 Timeline untouched) |
| 05 | clue cards (`callout show …`) | SFX_Fact_Chime 0.5 (1 s cooldown) | cue rule |
| 05 | random motion trails (`wire.trails 1`) | SFX_Electric_Glitch 0.5 | cue rule |
| 06 Battery and terminals | `show Card Terminal Label −` / `… +` / `show Bracket` / `show [VFX] field wave` | SFX_Terminal_Negative / SFX_Terminal_Positive / SFX_Electric_Whoosh / SFX_Electronics_PowerUp | cue rules |
| 06 | the + tap applies the voltage (`wire.field 1`) | SFX_Battery_Connect 0.9; hum + pulse fade in | cue rule + `ElectricalAudio` (k from `WireInterior.fieldOn/driftSpeed`) |
| 07 Electron drift | `show Drift Arrow` | SFX_Drift_Chime 0.7 | cue rule |
| 08 Current and measurement | `counter 1` / each counted charge / Scene 6 gauge `boost Card Terminal +` | SFX_Current_Marker 0.7 / UI_Hover tick (alias `Count`) / SFX_Voltage_Reveal 0.6 + SFX_Voltage_Step (alias `VoltStep`) | cue rules + `SceneDirector.Wire` Count + `SceneDirector.Gates` |
| 08 | freeze (`wire.freeze 1/0`) / beam / niscope flash | SFX_Energy_Field / SFX_Energy_Field_Off / SFX_Scan_Charge / SFX_Magic_Poof; SFX_Wattson_Magic from the RaiseGadget gesture hook | cue rules + `WattsonController` |
| 09 Voltage experiment | slider step / row records / point plots / 5 V row done / condition reached | SFX_Voltage_Step (pitched per volt, pooled source) / SFX_Record / SFX_Plot / SFX_Experiment_Complete / SFX_Condition_Reached | `SceneDirector.Experiment` (aliases VoltStep/Record/Plot) + `Scene09/10Builder` cues |
| 09 | current changes | LOOP_Electric_Charge_Hum volume = smoothstep(I / 1 A), pitch 0.94–1.08; LOOP_Electricity_Sparks above 0.35; fades out at I = 0 | `ElectricalAudio` (`CircuitRig.keyClosed`, `ExperimentData.CurrentAt(V)`, V = I × R) |
| 10 Discovery / reward | "V × 2 → I × 2", V ∝ I, Ω, "V = I × R" hero plates / graph line / V/I column | SFX_Scientific_Discovery (rule `hero `, 2 s cooldown) / SFX_Magic_Sweep (alias `Rise`) / SFX_Info_Chime (`table.ratio 1`) | cue rules + existing builder sfx |
| 11 Return to the lab | Scene 6 exit `fade 1 0.3` / Scene 7 pull-back / Scene 8 reveal | SFX_Whoosh_Fast 0.6 / SFX_Magic_Settle / SFX_Magic_Sweep (`look` rule) + SFX_Impact_Low 0.4 ("Lab Impact" source) | cue rule + `Scene07/08Builder` |
| 12 Question / classroom | quick-check opens / pick / correct / wrong / prediction card / schematic board | UI_Open (`stage 1`, `inset 1`) / UI_Click / SFX_Correct_Answer / UI_Error / UI_Question (`spotlight`) / SFX_Scanner_Lock (`schematic 1`) | `MCQPanel.Mark`, `HudKit.UiSounds`, cue rules |
| 12 | instrument swaps, Ohm dissolves, voltmeter appears, sparks | SFX_Apparatus_Click / SFX_Magic_Poof (`vanish `) / SFX_Magic_Gather (`appear `) / SFX_Static_Crackles (`fx `) | `Scene08Builder` + cue rules |
| 13 Conclusion | OHM'S LAW hero / end card / stamp | SFX_Scientific_Discovery + SFX_Magic_Success (+2.0 s) / SFX_Final_Success (`end` rule) / UI_Stamp | `Scene13Builder`, cue rule, `LessonPanel.End` |

## Persistent UI and game states (`HudKit.UiSounds` → library `ui.<name>` at its gain, UI mixer group, synth fallback)
START / Continue → UI_Confirm · buttons, pills, chapter tiles → UI_Click · hover → UI_Hover (mouse only, 80 ms throttle)
· popups, pause menu, Ask Wattson, data stage, 3D pop-up open → UI_Open · close → UI_Close · gate done → SFX_Correct_Answer
(`ui.done`) · hint / idle nudge / Ask spotlight → UI_Question · wrong pick → UI_Error · end-card stamp → UI_Stamp ·
scene change → SFX_Whoosh_Fast 0.35 (`fade 1` rule, 4 s cooldown) · experiment activation (key closes) → SFX_Key_Switch
(alias `Switch`) + hum.

## Wattson (`WattsonController`, Character group, 2D, once per event)
marker move ≥ 0.8 m → SFX_Wattson_Whoosh (2 s cooldown) and SFX_Wattson_Settle on arrival · Greet → SFX_Wattson_Wave ·
Thinking → SFX_Wattson_Pop · Transform / RaiseGadget action moment → SFX_Wattson_Magic · never on blinks or idle ·
`gestureSounds = false` in Scene 4 (its Timeline carries them at the exact frames).

## Mixer
`Resources/OhmsLawMix.mixer`: Master → Music, Ambience, SFX, UI, Voice, **Character**, **Electrical** (unity gain; the
learner's three sliders and the ducker work on the `AudioBus` gains, so a missing group falls back to SFX).
