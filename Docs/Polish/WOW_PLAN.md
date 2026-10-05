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

## 8. Gate contract (WP C runtime ↔ scene builders)

Runtime: `Shared/SceneDirector.Gates.cs` (questions), `SceneDirector.Experiment.cs` (bench activities, cues), question data and
grading in `Shared/QuestionBank.cs`. The builder declares each line with `L(id, text, fallback, proposed: true)` and lists it on
its gate (`Gate(id, description, lineIds...)`); a line listed on another gate of the same scene is found too. **Every line is
optional**: a missing one is skipped, so a gate never stalls. On-screen question and option texts come from `QuestionBank`
(storyboard ones word for word); the `_Q` line is spoken while the panel is up (cut short if the learner answers first;
autoAnswer waits for it). Suggested texts follow §2 (short, react to the learner, no claim the app did not produce: every value
a line quotes is guaranteed by the gate's own check).

**What every graded question does** (`Ask`): each pick is recorded (`LearnerSession.Record` + `LearnerSession.answers`) and
marked, and its `<G>_FA/_FB/_FC` line plays. Correct → the reasoning shows on the panel, done. Partial → acknowledged, the
correct option and the reasoning are shown, done. Incorrect → the option is disabled, the panel shakes and shows the hint,
`scaffold` runs if the learner is now `Struggling`, the spoken hint `<G>_H` plays (once per question; skipped for a learner who
came in `Confident`), retry. At most 2 retries, and only while two or more options are untried (with 3 options the second
wrong answer goes straight to the reveal). Reveal → correct option and reasoning on the panel, then Wattson shows it: `<G>_X`,
with the gate's visible experiment where one is listed (the panel closes first so the experiment is in view). Option A is
always the correct one (autoAnswer picks it).

**Bench activities**: `rig.controlsOpen` is true only inside S10_Set1V / S10_Set2V, S11_Target, S13_Apply and the RECORD
"wrong setting" path; `Hold()` closes it after every gate. Ask Wattson (WP B) may set the voltage only while it is open, and
reads `CircuitRig.Active.targetAmps` (≥ 0 during a target activity) for its hints. A setting counts once it has been held
0.8 s with no finger or button down.

| Gate (concept) | Builder · placement | Lines: id — purpose — suggested text |
|---|---|---|
| `S05_Drift` (ElectronDrift) | Scene05Builder (WP D) · after the organised drift has played a few seconds, before current is defined. Panel 640 px docked right: check both labelled ends stay in view | `S05_Drift_Q` — question — "Quick check, detectives: which way are the free electrons drifting now?" · `_FA` — right — "Negative charges, pulled toward plus. That's the drift." · `_FB` — the + → − misconception — "That's how lots of diagrams draw it, but these electrons are negative." · `_FC` — partial — "Half right: they still jiggle, but now they also creep toward plus." · `_H` — hint — "Look at the labelled ends. Which one would pull a negative charge?" · `_X` — reveal over the running drift — "Watch one electron: jiggle, jiggle, but always edging from minus to plus." |
| `S10_Set1V` (existing) | Scene10Builder (WP G) · unchanged | optional `S10_Set1V_H` — a wrong setting held — "Slide the battery knob to the 1 V tick." (the prompt also shows "N V now. Set the battery to 1 V.") |
| `S10_Record` (existing) | Scene10Builder · unchanged | now writes the meters' live values into row 1 and records only at 1 V (otherwise it opens the controls and prompts). Optional `S10_Record_GO`. |
| `S10_Predict2V` (Relationship) | Scene10Builder · after `S10_Record`, before the 2 V step (Bench Close; panel 740 px so the voltmeter stays clear) | `S10_Predict2V_Q` — question — "Detective prediction: 1 volt gave 0.20 amperes. What will 2 volts give?" · optional `_OK` — neutral, after the pick — "Locked in. Let's test it." · `_FA/_FB/_FC` — **played by S10_Record2** after row 2 is recorded at 2 V: A "You said 0.40, and the meter agrees. Twice the push, twice the current." · B "You said it would stay at 0.20, but the meter jumped to 0.40." · C "You expected less current, but it rose to 0.40. More push, more flow." |
| `S10_Set2V` | Scene10Builder · right after S10_Predict2V; replaces the `rig.voltage 2` auto step (3-5 V stay cue-driven) | `S10_Set2V_GO` — instruction — "Now set the battery to 2 volts." · `_H` — wrong setting held — "Slide the knob to the 2 V tick." (A `prompt` cue before the gate stays the builder's.) |
| `S10_Record2` | Scene10Builder · after S10_Set2V; replaces `table.rows 2` | optional `S10_Record2_GO` — "Record this reading too." Records row 2 from the meters (only at 2 V), then grades the prediction and speaks `S10_Predict2V_F<letter>`. |
| `S10_Pattern` (Relationship, adaptive) | Scene10Builder · unchanged place | `S10_R_WRONG` (existing) — hint · `S10_Pattern_FB` — "Check the I column: each row is bigger than the one above." · `S10_Pattern_FC` — "There is one! Read the amperes from top to bottom." · `S10_Pattern_E` — the simpler question for a struggling learner (rows 1-2 highlighted); text = panel text: "Rows 1 and 2: when the potential difference went up, what did the current do?" · `S10_Pattern_X` — spoken while the V then I columns sweep — "Watch the columns: the volts climb row by row, and so do the amperes." · **no `_FA`**: S10_L08 ("Good. …") follows. Confident learners skip the spoken hint. |
| `S10_Discover` (existing) | Scene10Builder · unchanged | callback to the Scene 6 prediction, after the reveal, before S10_L11: `S10_Discover_FA` — predicted increase — "Back in the wire you predicted the current would rise. Your data agrees." · `_FB` — predicted decrease — "You predicted the current would drop. The data says it rises. Good detectives update." · `_FC` — predicted no change — "You predicted no change. The data shows it rises with the push." |
| `S11_Target` (Graph) | Scene11Builder (WP G) · after S11_L04 / the `V ∝ I` card, before S11_L05; Boards shot. The gate brings up the slider (bottom-left, removed after), the readings strip with a target column, and the live point; cue `readings 1` + `graph.live 1` earlier if they should show before | `S11_Target_GO` — "Your turn: make the ammeter read 0.60 amperes with the battery slider." · `_H` — first miss (and after 15 s idle) — "Every volt adds 0.20 amperes. How many volts make your target?" · `_X` — 2nd miss; Wattson sets 3 V, point 3 lights — "Three steps of 0.20 make 0.60. So: 3 volts." · `_OK` — hit — "3 volts, 0.60 amperes, right on the line." · bonus for a learner who hit it first time: `_GO2` — "Bonus round: make it read 0.80 amperes." · `_X2` — "Four steps of 0.20 make 0.80. So: 4 volts." · `_OK2` — "4 volts, 0.80 amperes. You're reading the graph like a pro." |
| `S12_Ratio` (Resistance) | Scene12Builder (WP D) · board stretch: keep `table.work 0` / `table.work 1`; the gate replaces `table.work 2` (it shows the V / I column, highlights row 3 and fills "3.0 ÷ 0.60 = 5" at the end); continue with `table.result 3/4` | `S12_Ratio_Q` — "Your turn: row 3. What is 3.0 divided by 0.60?" · `_FA` — "Five again, same as rows 1 and 2." · `_FB` — "That's 0.60 divided by 3.0, flipped. Volts go on top." · `_FC` — "That's 3.0 times 0.60. We need to divide." · `_H` — "How many 0.60s fit into 3.0?" · `_X` — spoken while the cell fills — "Five lots of 0.60 make 3.0, so the answer is 5." |
| `S12_Slope` (Graph, existing) | Scene12Builder · unchanged place | all optional: `S12_Slope_Q` — "So what's the slope of our line?" · `_FA` — "5: the same number as V divided by I." · `_FB` — "That's ΔI over ΔV, upside down. Slope is ΔV over ΔI." · `_FC` — "Use the triangle's ΔV and ΔI, not a single reading." · `_H` — "Divide the rise, ΔV, by the run, ΔI." · `_X` — "ΔV is 2.0 volts, ΔI is 0.40 amperes. 2.0 divided by 0.40 is 5." A struggling learner also gets points 1 and 3 lit and the plot pulsed. |
| `S12_AQ1` (Relationship; `checkpointEnabled`) | Scene12Builder · after S12_L10 (resistance defined), back at the board with the rig active (the reveal turns the real source). Skipped for a learner already `Confident` with the relationship, so a hands-free autoAnswer run never shows it | `AQ1` — storyboard question · `AQ_EXPLAIN` — storyboard hint after a wrong answer · `AQ_EASY` — storyboard easier question (the retry) · `S12_AQ1_FA` — right — "Right: same wire, double the push, double the current." (falls back to `AQ_GOOD`) · `_FB` — "Halving would mean more push, less flow. Our table says otherwise." · `_FC` — "If it stayed the same, the I column would be flat. It isn't." · `_X` — Wattson sets 2 V then 4 V with the readings up, then puts the source back — "Watch the meter: 2 volts, 0.40 amperes. 4 volts: 0.80. Doubled." |
| `S13_AQ2` (Resistance; `checkpointEnabled`) | Scene13Builder (WP G) · after V = IR (S13_L05/L06 and the title); panel 640 px so `V = IR` stays visible | `AQ2` — storyboard question · `S13_AQ2_FA`, or `AQ_GOOD` (storyboard "Case closed, detective.") — right · `_FB` — "Careful: needing more push means it resists more, not less." · `_FC` — "Same current, different pushes, so their resistances can't match." · `_H` — "Think R = V divided by I. Same I for both, so compare V." · `_X` — "Same current, bigger push needed: that one opposes more. Bigger V, bigger R." A struggling learner gets the table's own numbers on the panel and the V / I column lit. |
| `S13_Apply` (OhmsLaw) | Scene13Builder · the ending, before S13_L07, in a shot that frames the board's graph (the readings strip and the live point are on it) | `S13_Apply_GO` — "Use V = IR: set the voltage so the current is 0.80 amperes." · `_H` — "R is 5 ohms. V = I times R: 0.80 times 5 is…?" · `_X` — Wattson sets 4 V — "0.80 amperes times 5 ohms is 4 volts. There: 0.80 amperes." · `_OK` — "4 volts gives 0.80 amperes. V = IR works." |
| `S13_Checkpoint` | Scene13Builder · **remove** the gate and its line list (AQ1 / AQ_EXPLAIN / AQ_EASY move to Scene 12; AQ2 / AQ_GOOD stay in Scene 13) | the runtime ignores an old marker |

**Cues** (`SceneDirector.Experiment.cs`, idempotent): `readings 1|0` — the live V / I strip in the free band above the graph's
chalk frame (board v ≈ 0.86-0.98, on the graph canvas, so it shows and hides with the graph); in a scene without a graph (Scene
10's Boards stretch) a HUD card bottom-right above the subtitles. It shares that band with Scene 12's `AboveGraph` cards (Slope
chalk, Ω) and Scene 13's carried-over Ω: cue `readings 0` before showing those. `readings.r 1|0` — adds R with its unit, only
after S12_L14 (ohms introduced). `graph.live 1|0` — the live point ("now") and its ghost on the graph. The target gates turn
the readings and the live point on and leave them on.
