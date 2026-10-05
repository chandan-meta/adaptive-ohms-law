# Wow-factor pass — plan and contracts (2026-10-05)

The user's brief (clearer explanations, stronger characters, visible transformations, engaging experiments, a grounded AI
companion) applied to the existing module, Scenes 4-13. Inspection results: `Logs/Blockout/inspection_wow.json`.
Open review findings from the previous pass: `Logs/Blockout/review_part{1,2,3}.json` (still valid where code is unchanged).

## 1. Decisions

User (2026-10-05):
- **"Wind character" = Wattson** (the lightning-bolt detective, `Assets/AdaptiveLearning/Art/Wattson/LB/LB_FK.fbx`).
- **Voice**: the user adds keys to `Server/wattson-proxy/.env` (ElevenLabs TTS, Anthropic chat, OpenAI STT). Build all tooling
  now; the re-bake runs when keys exist. Until then lines play the existing Windows voice (scratch clips).
- **Lab**: Scene 7 returns to **Scene 4's study**, same bench, same camera direction. The **1826 apparatus** (copper-bismuth
  thermoelectric source, torsion galvanometer, mercury cups, copper test wires, notebook) sits on that bench from Scene 4 on,
  replacing the voltaic pile and simple galvanometer. Scene 8 transforms that study into the modern classroom lab (Scenes 9-13).
- **Dialogue**: every storyboard line stays word for word. New lines are allowed, added with `L(id, text, fallback,
  proposed: true)` (they land in `SceneNN/Dialogue/Proposed/`) and listed in the final report.

Defaults (no user input needed; deviations from the brief are deliberate and reported):
- **Spoiler rules win.** The Scene 6 prediction must stay unanswered until the bench experiment, so the "voltage change →
  current change" stage is shown inside the wire in **Scene 12's dive** (after the experiment), not in Scenes 5-6. R and Ω
  appear only from Scene 12; the live readings show V and I before that, and add R from Scene 12.
- **Resistance is fixed** (nichrome wire XY, 5 Ω). "Lower the resistance" is a recognised but declined request: Wattson
  explains that this experiment keeps the wire, and so its resistance, the same.
- **Learner record is session-only** (`LearnerSession`), sent to the AI as a short summary.
- **Platform unchanged**: Standalone Win64 (Android/WebGL tiers exist); new Input System only; touch targets ≥ 88 px.
- Electron flow vs conventional current: electrons drift − → + (Scenes 5, 6, 12). Wherever the bench shows a current direction
  (Scene 7 chalk sketch arrow, Scene 9 lit path, the X → Y pulse), it is labelled **conventional current (+ → −)** once, with a
  short line distinguishing it from electron drift.

## 2. Ground rules for every change

- Storyboard lines, order and exact on-screen strings stay. Scientific accuracy rules in `Docs/STORYBOARD_SCENES_05_13.md` stay.
- Wattson's new lines: youthful, curious, playful, short (mostly ≤ 14 words), reacts to what the learner just did, explains
  with what is on screen. Never repetitive praise (no two consecutive lines starting with praise; prefer naming the reasoning).
  Light humour at most once per scene. No line longer than ~2 sentences except the storyboard's own.
- Never claim an action or a reading the app did not produce. Confirmations quote the value actually applied.
- Cues must be idempotent state setters (Timeline cue markers are Retroactive; REPLAY and closing Ask Wattson re-evaluate them):
  replaying or skipping must land on the same final state.
- Unity "fake null": never `??` on `GetComponent` results.
- The editor is open: agents do not run Unity; the orchestrator builds and play-tests (`Tools/unity_request.sh`).
  Compile with `bash Tools/compile_check.sh <name>`.

## 3. Foundation (done, use it)

- `CircuitRig.SetVoltage(float) → float applied` (clamps 0-5 V; fires `VoltageChanged`; remembers `PreviousVoltage`),
  `ResetVoltage()` (back to `StartVoltage`), `CircuitRig.Active` (the live rig), `controlsOpen` (learner/Wattson may change V),
  `Amps` (live current, computed from `ExperimentData`), `MinVolts/MaxVolts`. The slider, `rig.voltage` cue and autoAnswer all
  call `SetVoltage`; the slider follows `VoltageChanged`.
- Cue hooks: `SceneDirector.Run` hands unknown verbs to `WireCue` / `LabCue` / `ExperimentCue` in
  `SceneDirector.Wire.cs` / `.Lab.cs` / `.Experiment.cs` (one owner each).
- `LearnerSession.Record(concept, Grade, misconception)`, `Struggling(concept)`, `Confident(concept)`, `Summary()`;
  concept ids in `Concepts.*`.
- `VoiceChannel.Register(AudioSource)` / `Busy(except)` / `IsVoice(source)`: Wattson's scripted voice and the Ask Wattson voice
  register; nobody starts speaking while another voice plays (WP A adds ducking of every other source).
- `ReducedMotion.On` (PlayerPrefs `ohm.reducedMotion`): effects, flashes and camera noise read it (WP B adds the HUD toggle).
- `WattsonController.Gesture.Transform` (appended to the enum) with `TransformActionDelay` (0.55 s, the action moment) and
  `TransformSettleSeconds`; WP F implements the motion, WP E times the transformation cues to the action moment.
- Editor self-checks: an agent may add a `[MenuItem("Ohm's Law/Tests/...")]` method; the orchestrator runs it with the
  request `menu Ohm's Law/Tests/...` (`Tools/unity_request.sh`), result in `Logs/ohm-build.txt`.

## 4. Work packages

| WP | Owner files | Deliverable |
|---|---|---|
| A Voice & audio | `Server/wattson-proxy/*`, `Editor/BakeBlockoutVO.cs`, `Editor/BakeWattsonVO.cs`, `Editor/BlockoutAssets.cs`, `Shared/Agent/PronunciationLexicon.cs`, `Shared/ModuleFlow.cs`, new `Shared/AudioDucker.cs`, `Scene04/Audio/VO/*` | Youthful voice config + delivery, bake that covers Scene 4 + proposed lines and prefers real clips, ducking, voice turn-taking, grounded chat on the server |
| B Ask Wattson | `Shared/Agent/*` (not PronunciationLexicon), new `Shared/Agent/WattsonActions.cs`, `Shared/CinematicControlsUI.cs` | Conversation UI, states, mic + text, live-state grounding, validated action layer, honest offline fallback, reduced-motion toggle |
| C Experiment & questions runtime | `Shared/SceneDirector.cs`, `SceneDirector.Gates.cs`, `SceneDirector.Experiment.cs`, `MCQPanel.cs`, `GraphUI.cs`, `ObservationTableUI.cs`, `CircuitRig.cs`, `ModuleState.cs`, new `Shared/LiveReadingsPanel.cs` | Live readings beside the graph, live/previous point, graded adaptive questions, the new gates in §5 |
| D Electron space | `Shared/WireInterior.cs`, `Editor/SceneBuilderBase.Wire.cs`, `Shared/SceneDirector.Wire.cs`, `Editor/Scene05Builder.cs`, `Scene06Builder.cs`, `Scene12Builder.cs` | Decluttered, staged wire teaching; Scene 5 drift question; Scene 12 voltage→current dive beat, exit back to the bench, Scene 12 question placement |
| E Lab continuity & transformation | `Editor/Scene04Builder*.cs`, `SceneBuilderBase.Lab.cs`, `SceneBuilderBase.Look.cs`, `SceneBuilderBase.Classroom.cs`, `Scene07Builder.cs`, `Scene08Builder.cs`, `Shared/PopIn.cs`, `Shared/SceneDirector.Lab.cs` | 1826 apparatus in Scene 4, Scene 7 in the study, Scene 8 component-by-component apparatus transformation then room transformation, Ohm's exit |
| F Wattson character | `Shared/WattsonController.cs`, `Scene04/Scripts/OhmWorker.cs`, `Editor/SceneBuilderBase.cs` (Wattson/Ohm helpers only), Blender work in `scratch/` | Rig inspection (Blender headless), repairs where evidence shows a defect, richer procedural performance, transformation gesture with anticipation / action / settle |
| G Scene builders 9-11, 13 (phase 2) | `Editor/Scene09Builder.cs`, `Scene10Builder.cs`, `Scene11Builder.cs`, `Scene13Builder.cs` | Wire the new gates, activities and reactions into the beats; conventional-current note; recap + application ending |

Cross-file needs: request them in the agent report (file, member, change); the orchestrator applies them.

## 5. Questions and activities (Observe → Predict → Experiment → Explain → Continue)

Every graded question uses per-option grades (Correct / Partial / Incorrect), a feedback line per option, a misconception
tag on wrong options, a hint + retry on Incorrect (max 2 retries, then Wattson shows it with the visible experiment), and
records `LearnerSession.Record`. autoAnswer picks the Correct option. Option order may stay fixed (correct = A) for the play test.

| Gate id | Scene / moment | Concept | Content |
|---|---|---|---|
| `S05_Drift` (new) | 5, after the organised drift has played a few seconds, before current is defined | ElectronDrift | "Which way are the free electrons drifting now?" A. From the − end toward the + end (Correct: electrons are negative, the + end attracts them) · B. From the + end toward the − end (Incorrect, misconception "electrons flow + to −"; hint: look at the labelled ends — which one attracts negative charge?) · C. Every which way, with no overall direction (Partial: they still jiggle randomly, and now they all drift toward + too) |
| `S06_Prediction` (existing) | 6 | Relationship (prediction, ungraded) | unchanged; reactions stay; later scenes call back the prediction |
| `S09_*` placement (existing) | 9 | — | unchanged gates; when the lit path / X → Y pulse first appears, label it conventional current (+ → −) with one PROPOSED line; if `ElectronDrift` has the "+ to −" misconception, a short remediation line instead |
| `S10_Predict2V` (new) | 10, after row 1 (1 V, 0.20 A) is recorded, before the 2 V step | Relationship | "At 1 V the ammeter read 0.20 A. What will it read at 2 V?" A. 0.40 A · B. 0.20 A (misconception "current does not depend on voltage") · C. 0.10 A (misconception "more voltage, less current"). Graded AFTER the learner sets 2 V (`S10_Set2V`) and records (`S10_Record2`): Wattson compares the prediction with the meter |
| `S10_Set2V`, `S10_Record2` (new) | 10 | — | learner sets 2 V (slider or Ask Wattson) and records row 2 from the live meter |
| `S10_Pattern` (existing) | 10 | Relationship | adaptive: Struggling → highlight rows 1-2 and ask the simpler AQ_EASY-style question; Confident → skip the extra hint |
| `S10_Discover` (existing) | 10 | — | then a PROPOSED callback to the Scene 6 prediction (matched / revised) |
| `S11_Target` (new) | 11, after "V ∝ I" and before S11_L05 | Graph | live readings beside the graph + slider (or Ask Wattson): "Make the ammeter read 0.60 A." Hints by attempt (0.20 A per volt → which voltage?); after 2 misses Wattson sets it and explains; Confident learners get a second target (0.80 A) |
| `S12_Ratio` (new) | 12, in the board stretch, instead of Wattson working row 3 | Resistance | "3.0 ÷ 0.60 = ?" A. 5 · B. 0.2 (misconception "divides I by V") · C. 1.8 (misconception "subtracts") |
| `S12_Slope` (existing) | 12 | Graph | unchanged, adaptive hint |
| `S12_AQ1` (moved from S13 checkpoint) | 12, after resistance is defined | Relationship | storyboard AQ1 text and options; skipped when `Confident(Relationship)`; AQ_EASY + AQ_EXPLAIN remediation when wrong |
| `S13_AQ2` (moved) | 13, after "And the opposition." beat or after V = IR | Resistance | storyboard AQ2 text and options; Struggling(Resistance) → AQ_EXPLAIN-style hint tied to the table, retry |
| `S13_Apply` (new) | 13, ending | OhmsLaw | hands-on application: "Use V = IR: set the voltage so the current is 0.80 A" (4 V) with live readings; Wattson helps on request; success → recap and the closing line |

`S13_Checkpoint` is removed from Scene 13 (its items moved above). `checkpointEnabled` keeps gating the moved AQ items.

## 6. Ask Wattson action layer (contract)

Learner text (typed or STT transcript) → `WattsonActions.TryHandle(text, out reply)` BEFORE any AI call:
- Intents: set voltage to N; increase / decrease by N (default 1); double / halve the voltage; reset; resistance up/down
  (declined, explained); set current to X (declined: current follows the voltage — suggest the voltage, or hint during a
  target activity); anything else → not an action, goes to the AI.
- Validation: `CircuitRig.Active != null`, `controlsOpen`, numbers in volts, whole volts 0-5 (the panel has 1 V ticks), relative
  changes from the real current value; out of range → explain the limit, apply nothing.
- Execution: `CircuitRig.Active.SetVoltage(v)` only. Reply built by the app from the APPLIED value and `Amps` after applying
  ("Done — 4 V. The ammeter now reads 0.80 A."), spoken through the normal voice path, shown in the history.
- The AI never executes anything; the server prompt says actions are done by the app and the model must not claim them.

Chat request (Unity → proxy) gains two plain-text fields, `state` (live experiment/activity state: scene, activity/gate,
controlsOpen, voltage, current, keyClosed, recorded rows) and `learner` (`LearnerSession.Summary()`); the server appends them to
the system prompt as authoritative context.

## 7. Review passes (after implementation)

1. Learning flow & science · 2. Characters, transformations & visuals · 3. AI, controls, UI & audio · 4. Full playthrough &
regression (replay, skip, reset, rapid input, wrong answers, interrupted dialogue, unsupported commands, AI offline).
Each pass: build all, hands-free play test (`Logs/Blockout/play-report.txt` + shots), targeted runtime checks, review agents,
fixes, re-test. Static inspection and executed tests are reported separately.
