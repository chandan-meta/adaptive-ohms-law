# Wow-factor pass — report (2026-10-05)

Brief: `Docs/Polish/WOW_PLAN.md` §1 (decisions), §5 (questions), §6 (action layer), §8 (gate contract). New dialogue:
`Docs/Polish/NEW_LINES.md`. Flow: `Docs/FLOW.md`. Status of the four review passes: see §6 below.

## 1. What changed

**Opening preserved; Ohm's lab is now one place.** Scene 4 keeps its shots, timing and lines; the 1826 apparatus
(copper–bismuth thermoelectric source, torsion galvanometer, mercury cups, copper test wires, notebook) replaces the
voltaic pile on Ohm's bench from Scene 4 on, with the same copper wire Wattson's beam enters. Scene 7 returns to that
study: same bench, same apparatus, Ohm at his spot, Scene 4's room and lighting, a chalk slate by the fireplace for the
two clues. The unexplained classroom switch is gone.

**Scene 8 transformation is visible.** Wattson performs a Transform gesture (anticipation → thrust → settle) in frame;
glowing dust travels along the desk; each historical component shrinks out while its modern equivalent grows in, in its
own place (galvanometer → ammeter, thermoelectric source → DC source, copper wires → nichrome wire XY on its posts,
mercury cups → key, then the voltmeter joins); the desk becomes the bench; Wattson ties it to the lesson ("The instruments
changed… The relationship we're testing is still the same."); Ohm presents, is thanked and dissolves back to 1826; then the
study sweeps away from the bench into the modern classroom lab on camera (walls, furniture, lighting, post and reflections
crossfade). Cues are idempotent; REPLAY/SKIP land on the final state; Scene 8's last frame equals Scene 9's first
(`Ohm's Law/Tests/Lab Continuity`, 130 checks).

**Inside the wire (Scenes 5, 6, 12).** Fewer, followable electrons with short trails; "− negative" / "+ positive" labelled
ends; the six teaching stages in order (where we are and what the particles are → the two ends → voltage → organised drift,
which now starts after voltage is named → current as charges per second through the highlighted cross-section, whose
counter shows the completed per-second value → voltage change → current change, shown in Scene 12's dive after the bench
experiment so the Scene 6 prediction is not spoiled). A "electron drift" arrow (never called current); conventional
current appears only in Scene 9, labelled "+ → −" with a line distinguishing it from electron drift (a remediation line
plays for learners who picked "+ to −" in Scene 5). Scene 12's dive returns to the same bench and wire before the board.

**Wattson (the "wind character").** Rig: headless Blender 5.1 inspection of `LB_FK.fbx` (52 joints, 0 unweighted vertices,
3,286 vertices with > 4 influences); repair limited influences to 4 and removed 1,640 stray far-bone weights, renormalised
7,722 vertices, re-exported with identical bone names (rewrite without changes reproduced the file byte for byte).
Performance (`WattsonController`, procedural — the rig has no clips): eased flight with lean and settle, body turn toward
targets, arm-side pointing (no fold across the chest), elbow/wrist bend, bent floating legs with secondary motion, head and
eye look, breathing/sway idle, pupil-squash blink, reactions on graded answers (fist pump / so-so / shrug), Transform gesture.
Ohm: clavicles, neck and finger curl so Work/Present poses are not stiff; never T-posed.

**Ask Wattson is a grounded companion.** Conversation panel (top-left, clear of the graph and apparatus) with labelled
learner/Wattson bubbles, history, listening / thinking / speaking / error states, microphone (push-to-talk) and text input.
Every request carries live state (scene, activity, controls open/cap, voltage, current, key, recorded rows, whether
resistance has been taught) and a learner summary (concept record). Offline answers are labelled "Offline answer"; an STT
failure keeps text input working. New question interrupts the old answer; stale answers are cancelled on scene change.

**Wattson operates the controls.** `WattsonActions`: request → intent (set / increase / decrease / double / halve / reset /
max; number words and digits) → validation (controls open, cap `SceneDirector.ControlsMax`, whole volts 0–5, relative from
the real value; out-of-range explained, resistance change declined with an explanation) → `CircuitRig.Active.SetVoltage`
(the same path the slider uses) → confirmation from the APPLIED value and `Amps` ("Done: 4 V. The ammeter now reads 0.80 A.").
Runs before any network call, so it works offline and never waits on latency. The AI never drives the scene.

**Live readings beside the graph.** `LiveReadingsPanel` on the board next to the graph: V, I (and R + "R = V ÷ I" once
Scene 12 introduces the ohm), previous value for comparison, subtle change highlight; `GraphUI` live point ("now") distinct
from recorded points. One state model: `CircuitRig.state` via `SetVoltage`; RECORD records the live meter values.

**Adaptive questions distributed.** Graded MCQs (correct / partial / incorrect per option, misconception tags, hint + retry,
show-me after the last retry, `LearnerSession.Record`, Struggling/Confident branches): S05_Drift · S06_Prediction ·
S09_Conventional (say gate) · S10_Predict2V → Set2V → Record2 · S10_Pattern (adaptive) · S10_Discover + Scene 6 callback ·
S11_Target (reach 0.60 A; second target when confident) · S12_Ratio · S12_AQ1 (skipped when confident) · S12_Slope ·
S13_AQ2 · S13_Apply (V = IR: reach 0.80 A) · S13_Recap (reacts to the record). The end-of-module checkpoint cluster is gone;
Scene 13 ends with the application and recap.

**Voice.** Proxy `/tts` now uses a youthful ElevenLabs voice by default (env-overridable), expressive model and tuned
`voice_settings`; one bake covers every scene incl. Scene 4 and proposed lines, prefers real mp3 over scratch, stamps text +
voice so re-bakes are incremental, throttles to the proxy limit; lexicon + delivery punctuation go to `ttsText` only.
Without keys the bake made 193 scratch clips with the clearer local voice (Microsoft Mark). `VoiceChannel` turn-taking and
`AudioDucker` ducking of music/SFX while any voice plays.

**Accessibility / presentation.** Reduced-motion toggle (bottom-left; flashes, camera noise, particles, pulses read it),
labels never colour-only, touch targets ≥ 88 px, HUD row kept at its reference width, callouts and panels re-anchored.

## 2. Affected assets

Scripts (runtime): `Assets/OhmsLaw/Shared/` CircuitRig, SceneDirector (+ .Gates/.Experiment/.Wire/.Lab/.Cues), QuestionBank,
LiveReadingsPanel, GraphUI, ObservationTableUI, MCQPanel, ModuleState, ModuleFlow, VoiceChannel, AudioDucker, ReducedMotion,
ReducedMotionFx, CinematicControlsUI, FlowOverlay, SubtitleUI, WorldText, PopIn, WattsonController, WireInterior,
`Agent/` WattsonAgent, WattsonActions, AskWattsonUI, AgentServices, LocalFAQ, PronunciationLexicon;
`Assets/OhmsLaw/Scene04/Scripts/OhmWorker.cs`.
Scripts (editor): every `SceneNNBuilder.cs` (04–13, + Scene04Builder.Apparatus), `SceneBuilderBase.{cs,Lab,Look,Wire,Classroom}`,
BakeBlockoutVO, BakeWattsonVO, BlockoutAssets, BlockoutCommands, BlockoutPlayTest, FlowDocument, WireChecks,
`Editor/Tests/` AdaptiveQuestionsTest, WattsonActionsTests, WattsonPoseTest, LabContinuityTest.
Scenes/prefabs: all `Assets/OhmsLaw/Scenes/*.unity` rebuilt; `Scene04/Prefabs/{Lab,Ohm,Wattson}_Scene04.prefab`; every
`SceneNN/Timeline/*.playable`; `SceneNN/Dialogue/**` (+78 proposed lines); `SceneNN/Agent/*` contexts.
Model: `Assets/AdaptiveLearning/Art/Wattson/LB/LB_FK.fbx` (skin weights only; original in git history).
Audio: `SceneNN/Audio/VO/scratch_*.wav` (193 clips). Settings: `ProjectSettings/QualitySettings.asset` (Mobile skinWeights 4).
Server: `Server/wattson-proxy/{index.js,systemPrompt.txt,check.test.js,README.md,.env.example}`.

## 3. How Wattson's control actions reach the experiment

`AskWattsonUI` (typed or transcribed text) → `WattsonActions.TryHandle` → validated intent → `CircuitRig.Active.SetVoltage(v)`
(clamped; fires `VoltageChanged`) → slider knob, source knob, meters, readouts, `LiveReadingsPanel`, `GraphUI` live point and
the wire readout all read the same `CircuitRig.state` → reply built from `state.voltage` and `Amps` after applying → spoken
through the voice channel and shown in the history. The manual slider calls the same `SetVoltage`. Gates open/close
`controlsOpen` and lower `ControlsMax` (1 V during the first reading) and the action layer honours both.

## 4. Where the adaptive questions now appear

| Moment | Gate | Concept |
|---|---|---|
| Scene 5, after the drift has played | S05_Drift | electron drift direction |
| Scene 6 | S06_Prediction | prediction (ungraded, called back in Scene 10) |
| Scene 9, when the lit path appears | S09_Conventional (say) | conventional vs electron flow (remediation if needed) |
| Scene 10, after row 1 | S10_Predict2V → S10_Set2V → S10_Record2 | V–I relationship (graded by the meter) |
| Scene 10 | S10_Pattern (adaptive), S10_Discover + Scene 6 callback | relationship |
| Scene 11, after V ∝ I | S11_Target (0.60 A; 0.80 A when confident) | reading the graph / controls |
| Scene 12 | S12_Ratio, S12_AQ1 (skipped when confident), S12_Slope | resistance, relationship, graph |
| Scene 13 | S13_AQ2, S13_Apply (V = IR → 0.80 A), S13_Recap (say) | resistance, Ohm's law |

## 5. What was actually tested

Executed:
- `bash Tools/compile_check.sh` (every .cs, Runtime + Editor): clean.
- `node --test` in `Server/wattson-proxy` (mocked providers, no network): 15 pass.
- In-editor self-checks (open editor, rebuilt scenes, after the review fixes): Voice Pipeline 20 PASS · Wattson Actions 145 PASS ·
  Adaptive Questions 44 PASS · Wire Electrons 15 PASS · Lab Continuity 145 PASS (Scenes 4/7/8/9 geometry, lighting and cut
  continuity) · Wattson Pose 119 PASS · Ohm Pose 13 PASS, 0 FAIL.
- Hands-free playthroughs (ReviewMode.autoAnswer) Scene 4 → end card: 2× and 1× before the review passes and 1× after the
  review fixes, RESULT PASS each time, all 20 gates completed, every Timeline line spoken in order, frames captured per
  line/gate/scene entry/exit and reviewed against the references (`Logs/Blockout/compare/*.png`); Scenes 8–13 re-captured
  after the last Scene 8 change. The harness pins MOTION FULL for captures (`calm` in the request captures reduced motion).
- Headless Blender inspection/repair of the Wattson rig with pose renders (`scratch/rig/`).

Static only (not executed here): live Ask Wattson chat, speech-to-text and ElevenLabs voice (no API keys on this machine);
manual input paths (drag, slider, taps) beyond autoAnswer; REPLAY/SKIP/reset edge cases (reviewed in code, pass 4);
tablet/WebGL builds and device performance.

## 6. Review passes

Each pass was a read-only review of the rebuilt scenes, the 1× play-test frames/report and the code; every HIGH/MEDIUM finding was then
handed to an independent skeptic who tried to refute it from the evidence. Full text: `Logs/Blockout/wow_review.json`.

**Pass 1 — learning flow and scientific clarity** — verdict: PASS with reservations. 2 confirmed HIGH/MEDIUM, 1 refuted, 3 LOW.
- [MEDIUM] Scene 9/10 – pre-experiment reading leak (S10_Predict2V): Closing the key puts the meters at 0.5 V → 0.10 A with legible numeric plates; the learner then sets 1 V → 0.20 A and is asked to predict the 2 V reading. They…
- [MEDIUM] Pacing – passive stretches > 45 s (from play-report.txt): Three stretches with no learner input: (a) module start → S05_Drift ≈ 90 s; (b) S06_Prediction → S09_PlaceAmmeter ≈ 105–110 s (Scenes 7 and 8 have no interactio…
- refuted: Scene 12 – question cluster for non-confident learners (S12_Ratio → S12_AQ1 → S12_Slope)

**Pass 2 — characters, transformations and visuals** — verdict: PASS WITH FIXES. 6 confirmed HIGH/MEDIUM, 1 refuted, 5 LOW.
- [HIGH] Scene 5 — S05_Drift gate framing: The learner is asked which way electrons drift between the two labelled ends while the + end is hidden under the question panel.…
- [HIGH] Scenes 7–8 — Ohm's face / study exposure: Ohm's face is unreadable in every Study Wide / Study Desk frame of Scene 7 and the first half of Scene 8; the lamp and candle bloom into white discs.…
- [MEDIUM] Scene 8 — callout layout over the held composition: During S08_P01 ('The instruments changed…') the key — one of the four instruments just introduced — is covered by the voltmeter's label; Wattson's hat is under…
- [MEDIUM] Scene 8 — modern bench clips to white under the study look: The 'desk becomes today's bench' hold reads as a clipped white rectangle rather than a lit surface; detail on the top (wire, cables) loses contrast.…
- [MEDIUM] Reduced motion — camera noise, white flashes, Niscope flash (code): With MOTION REDUCED the learner still gets handheld shake in the wide shots, the full-screen white flashes between scenes and the Ohm-niscope burst at full stre…
- [MEDIUM] Reduced motion — Scene 8 (and other) particle effects lack ReducedMotionFx (code): Under reduced motion the Scene 8 transformation, room wave and farewell dust, Scene 7's shimmer, Scene 11's axis flashes and Scene 13's title ignite play at ful…
- refuted: Scene 9 — L06 Bench Close callouts vs Wattson and the wire

**Pass 3 — AI, controls, UI and audio** — verdict: PASS 3 mostly holds up. 2 confirmed HIGH/MEDIUM, 0 refuted, 6 LOW.
- [HIGH] Ask Wattson action layer: intent parsing (WattsonActions.Parse): While the controls are open (S10_Set2V, S11_Target, S13_Apply) these learner sentences, typed or transcribed, move the source, Wattson says "Done: ..." with a r…
- [MEDIUM] Ask Wattson panel: dock and size vs. Wattson and the table: In the Boards shots (Scenes 11-13) and the Apply frame the 95 %-opaque panel sits exactly over Wattson, so the Listening/Thinking/Talking poses the agent switch…

**Pass 4 — complete playthrough and regression** — verdict: PASS with one HIGH regression risk. 3 confirmed HIGH/MEDIUM, 0 refuted, 5 LOW.
- [HIGH] SKIP / NEXT SCENE during a gate (all scenes with gates): Holding SKIP (or F1 > NEXT SCENE) while S05_Drift, S06_Prediction, S10_Predict2V, S10_Pattern, S12_Ratio, S12_Slope, S12_AQ1 or S13_AQ2 is up leaves the MCQ pan…
- [MEDIUM] Scene 5, S05_Drift question panel vs the labelled ends: While Wattson asks 'Which way are the free electrons drifting now?' with options 'From the − end toward the + end' / 'From the + end toward the − end', only the…
- [MEDIUM] PAUSE while a gate is running (every gate; S11_Target, S13_Apply, S10_Set1V/Set2V, all MCQs): Pressing PAUSE during a gate flips the label to PLAY but nothing stops: a gate line (S05_Drift_Q, S11_Target_GO, S12_Ratio_Q, S13_Recap...) keeps speaking with…

Fix status after the review-fix pass (verified read-only against the diff, `Logs/Blockout/wow_review_fixes.json`):
- Closed: readout leak before the 1 V baseline (plates hidden until the baseline, chat state does not quote hidden numbers); S05_Drift
  panel now top-centre with the VOLTAGE card hidden for the gate, both labelled ends and Wattson in view; Scene 8 callout anchors;
  reduced motion for camera noise, the Ohm-niscope flash, scene fades and the show-driven effect roots (Scenes 4, 7, 8, 11, 13);
  intent parsing (statements and questions are never executed as actions); Ask panel no longer covers Wattson in board shots;
  SKIP / NEXT SCENE during a gate sweeps the gate UI, stops its coroutines and voice, and goes through the director so the hand-off
  state survives; the end card is silent; PAUSE holds gate lines, subtitles, timers, options, slider, RECORD, key and drag input;
  Scene 12 "One volt on the bench" settles before the line; the Scene 7 sketch arrow is captioned; chip/FAQ spoilers before the
  V/I column; reset uses the scene's start voltage; proxy app rules matched word for word; honest RESTING vs OFFLINE; mic and
  Space edge cases; truncated streamed answers marked.
- Closed after re-capture: the modern bench no longer clips to white under the study look (it appears in a darker copy of its
  top material and the look crossfade lerps it to the classroom top); Ohm's face reads in the Scene 7/8 study frames after the
  lamp/candle cut. Left as is: the lamp chimney still blooms a little (a warm rim, no longer a white disc).
- Deliberately not applied: the two refuted findings (Scene 12 question cluster; Scene 9 L06 callouts), the pacing change to
  Scenes 7-8, an automated PAUSE step in the play-test harness (manual verification listed in §5).

## 7. Remaining limitations and external setup

- **Keys**: put `ELEVENLABS_API_KEY`, `ANTHROPIC_API_KEY`, `OPENAI_API_KEY` in `Server/wattson-proxy/.env` (see its README),
  start the proxy, set `ProxyConfig.baseUrl`, then run `Ohm's Law/Bake VO/All Scenes` (TTS_LIMIT=400 for the first full
  bake). Until then Wattson speaks with the local Windows voice and Ask Wattson answers offline (labelled).
- The default ElevenLabs voice id is a premade voice; confirm it is available on your account (README lists alternatives
  and a Voice Design prompt for a younger timbre).
- Rig: deformation limits remain in the mesh itself (collar on big head turns, coat/leg seam on deep knee bends, no neck
  joint); the controller keeps joints inside the range that holds up. A re-skin in Maya (`Lighting Boat rigging 02.ma`,
  ngSkinTools2 missing here) would be needed for more.
- Resistance has no physical control (fixed nichrome wire, 5 Ω); "lower the resistance" is declined with an explanation.
- Hindi answers are not enabled (no Devanagari font in the project).
- Accepted residual: the 11 → 12 cut switches table cells from `1 | 0.2` to `1.0 V | 0.20 A`.
- Learner record is session-only (not persisted).
