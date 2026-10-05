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
| `S09_*` placement (existing) + `S09_Conventional` (new say gate) | 9; the say gate after S09_L06, back on Bench Present with the chalk heading "Conventional current: + → −" going up over the lit path (key still open, callouts down) | — | placement gates unchanged; the say gate holds the Timeline for ONE PROPOSED line: `S09_Conventional`, or `S09_Conventional_If_S05_Drift_B` for a learner who picked "+ to −" at S05_Drift (even if corrected later). Texts in §8 |
| `S10_Predict2V` (new) | 10, after row 1 (1 V, 0.20 A) is recorded, before the 2 V step | Relationship | "At 1 V the ammeter read 0.20 A. What will it read at 2 V?" A. 0.40 A · B. 0.20 A (misconception "current does not depend on voltage") · C. 0.10 A (misconception "more voltage, less current"). Graded AFTER the learner sets 2 V (`S10_Set2V`) and records (`S10_Record2`): Wattson compares the prediction with the meter |
| `S10_Set2V`, `S10_Record2` (new) | 10, right after S10_Predict2V (Bench Close); each is introduced by a PROPOSED Timeline line (`S10_Set2V_GO`, `S10_Record2_GO`) and the builder's prompt cue | — | learner sets 2 V (slider or Ask Wattson) and records row 2 from the live meter (only at 2 V); S10_Record2 then grades the prediction against the meter and speaks `S10_Predict2V_FA/_FB/_FC` |
| `S10_Pattern` (existing) | 10 | Relationship | adaptive: wrong → `S10_Pattern_FB/_FC` + S10_R_WRONG and re-ask; Struggling → highlight rows 1-2 and ask the simpler question (`S10_Pattern_E`); Confident → skip the spoken hint; out of retries → V then I column sweep with `S10_Pattern_X` |
| `S10_Discover` (existing) | 10 | — | then a PROPOSED callback to the Scene 6 prediction (matched / revised) |
| `S11_Target` (new) | 11, after "V ∝ I" and before S11_L05 | Graph | live readings beside the graph + slider (or Ask Wattson): "Make the ammeter read 0.60 A." Hints by attempt (0.20 A per volt → which voltage?); after 2 misses Wattson sets it and explains; Confident learners get a second target (0.80 A) |
| `S12_Ratio` (new) | 12, in the board stretch, instead of Wattson working row 3 | Resistance | "3.0 ÷ 0.60 = ?" A. 5 · B. 0.2 (misconception "divides I by V") · C. 1.8 (misconception "multiplies V by I") |
| `S12_Slope` (existing) | 12 | Graph | unchanged, adaptive hint |
| `S12_AQ1` (moved from S13 checkpoint) | 12, after resistance is defined (S12_L10), Boards shot | Relationship | storyboard AQ1 text and options; skipped when `Confident(Relationship)`; wrong → `S12_AQ1_FB/_FC`, AQ_EXPLAIN, retry on AQ_EASY; out of tries → `S12_AQ1_X` while Wattson steps the source 2 V → 4 V on the readings strip |
| `S13_AQ2` (moved) | 13, in the "Question" frame (the Cards frame slid 0.75 m right) after V = IR and the OHM'S LAW title, which stay lit on the left; panel 640 px docked right over the wall | Resistance | storyboard AQ2 text and options; wrong → `S13_AQ2_FB/_FC`, hint `S13_AQ2_H`, retry (Struggling(Resistance) gets the table's own numbers on the panel); out of tries → `S13_AQ2_X` |
| `S13_Apply` (new) | 13, in the "Apply" frame (bench meters, the graph's line without its recorded points, the readings strip V · I · R above it, V = IR on the board), after the PROPOSED line `S13_A01` and the Ask Wattson spotlight | OhmsLaw | hands-on application: "Use V = IR: set the voltage so the current is 0.80 A" (4 V) with live readings; Wattson helps on request (hint `_H`; after two misses he sets 4 V with `_X`); then the `S13_Recap` say gate and the closing line |
| `S13_Recap` (new say gate) | 13, after S13_Apply, before S13_L07 | — | one PROPOSED recap line keyed on the learner record: `S13_Recap_If_Struggling_OhmsLaw`, else `S13_Recap_If_Struggling_Resistance`, else `S13_Recap` (texts in §8) |

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
a line quotes is guaranteed by the gate's own check). **The text a builder declares with `L()` wins** over the suggestions
below (the final new-line report is built from the declarations).

**Voice and reactions** (all gates): a gate line (`Say`) first waits for its turn on the voice channel
(`VoiceChannel.WaitTurn`, at most 10 s), so it never starts over an Ask Wattson answer, also after the Ask panel closes. A
question line counts as being spoken during that wait (an answer cuts it before it starts; autoAnswer waits for it). Wattson
reacts (`WattsonController.React`: hop / tilt / shrug) to every graded pick in `Ask`, to the S10_Predict2V comparison at
S10_Record2, and to a target hit (S11_Target, S13_Apply). Ungraded picks (S06_Prediction, S10_Predict2V itself) get none.

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
0.8 s with no finger or button down. **Cap:** `SceneDirector.ControlsMax` (volts, normally 5) is the highest setting the
learner and Ask Wattson may make while the controls are open. S10_Set1V and S10_Record set it to 1 V, so nobody reads 2 V
before S10_Predict2V; `Hold()` puts it back to 5 V after every gate. The slider stops at the cap (ticks above it dim); a
learner who pushed past it sees "The knob stops at 1 V for this reading." on the prompt (until the builder's next `prompt`
cue). Ask Wattson must refuse a request above the cap before applying anything (open request to WP B); a setting above it
that reaches the rig anyway is undone the next frame.

**Say gates (branch lines keyed on the learner record).** A gate with no handler of its own whose listed lines are all
`<G>` or `<G>_If_<condition>` holds the Timeline while Wattson says ONE line: the first `_If_` line, in the order listed,
whose condition holds, else `<G>`, else nothing. TriggerOnce like every gate (a replay or skip never re-speaks it); autoAnswer
runs answer A everywhere, so they hear `<G>`. Builders need no runtime change: declare the lines (`proposed: true`) and
`Gate("<G>", "description", "<G>_If_...", "<G>")`. A gate listing any other line id keeps the "no handler" warning.
Conditions (`LearnerSession.Holds`, checked by the Adaptive Questions self-test):
- `<GateId>_<Letter>`: the learner picked that option at that gate, at any attempt, even if corrected later
  (`S05_Drift_B`; `AQ_EASY` picks count as `S12_AQ1`, the easier S10 question as `S10_Pattern`; the predictions
  `S06_Prediction_<L>` and `S10_Predict2V_<L>` count too).
- `Struggling_<Concept>` / `Confident_<Concept>`: the concept by its `Concepts` field name (ElectronDrift, Voltage, Current,
  Relationship, Graph, Resistance, OhmsLaw), e.g. `S13_Recap_If_Struggling_Resistance`.

| Gate (concept) | Builder · placement | Lines: id — purpose — suggested text |
|---|---|---|
| `S05_Drift` (ElectronDrift) | Scene05Builder (WP D) · after the organised drift has played a few seconds, before current is defined. Panel 640 px docked right: check both labelled ends stay in view | `S05_Drift_Q` — question — "Quick check, detectives: which way are the free electrons drifting now?" · `_FA` — right — "Negative charges, pulled toward plus. That's the drift." · `_FB` — the + → − misconception — "That's how lots of diagrams draw it, but these electrons are negative." · `_FC` — partial — "Half right: they still jiggle, but now they also creep toward plus." · `_H` — hint — "Look at the labelled ends. Which one would pull a negative charge?" · `_X` — reveal over the running drift — "Watch one electron: jiggle, jiggle, but always edging from minus to plus." |
| `S09_Conventional` (say gate) | Scene09Builder (WP G) · after S09_L06: back on Bench Present with the callouts down, the chalk heading "Conventional current: + → −" goes up over the lit path (key still open) and Wattson points at it from Tray Point. Lines listed in this order: `S09_Conventional_If_S05_Drift_B`, `S09_Conventional` | `S09_Conventional` — everyone (autoAnswer runs hear it) — "Plot twist: we draw conventional current plus to minus. Electrons drift minus to plus!" · `S09_Conventional_If_S05_Drift_B` — a learner who picked "+ to −" at S05_Drift (even if corrected) — "Your plus-to-minus answer in the wire? That's conventional current; electrons drift minus to plus." |
| `S10_Set1V` (existing) | Scene10Builder (WP G) · unchanged | `S10_Set1V_H` — a wrong setting held (once) — "Slide the battery knob to the 1 V tick." (the prompt also shows "N V now. Set the battery to 1 V."). Controls capped at 1 V (see Cap above). |
| `S10_Record` (existing) | Scene10Builder · unchanged | now writes the meters' live values into row 1 and records only at 1 V (otherwise it opens the controls, capped at 1 V, and prompts). No `_GO` declared: the storyboard's S10_L03 and the builder's prompt cue introduce it. |
| `S10_Predict2V` (Relationship) | Scene10Builder · after `S10_Record`, before the 2 V step (Bench Close; panel 740 px so the voltmeter stays clear) | `S10_Predict2V_Q` — question — "Detective prediction: 1 volt gave 0.20 amperes. What will 2 volts give?" · `_OK` — neutral, after the pick — "Locked in. Let's test it." · `_FA/_FB/_FC` — **played by S10_Record2** after row 2 is recorded at 2 V (they name the prediction and the meter, not the doubling: S10_L08-L10 reveal that): A "You said 0.40, and the meter agrees. Your hunch survived the test." · B "You said it would stay at 0.20, but the meter jumped to 0.40." · C "You expected less current, but the meter climbed to 0.40." |
| `S10_Set2V` | Scene10Builder · right after S10_Predict2V; replaces the `rig.voltage 2` auto step (3-5 V stay cue-driven) | `S10_Set2V_GO` — a Timeline line right before the gate, with the prompt "Set the battery to 2 volts." taking over as it ends — "Now set the battery to 2 volts." · `_H` — wrong setting held (once) — "Slide the knob to the 2 V tick." |
| `S10_Record2` | Scene10Builder · after S10_Set2V; replaces `table.rows 2` | `S10_Record2_GO` — a Timeline line right before the gate, with the prompt "Record the current reading in ammeter." taking over — "Moment of truth! Record the ammeter reading." Records row 2 from the meters (only at 2 V), then grades the prediction and speaks `S10_Predict2V_F<letter>`. |
| `S10_Pattern` (Relationship, adaptive) | Scene10Builder · unchanged place | `S10_R_WRONG` (existing) — hint · `S10_Pattern_FB` — "Check the I column: each row is bigger than the one above." · `S10_Pattern_FC` — "There is one! Read the amperes from top to bottom." · `S10_Pattern_E` — the simpler question for a struggling learner (rows 1-2 highlighted); text = panel text: "Rows 1 and 2: when the potential difference went up, what did the current do?" · `S10_Pattern_X` — spoken while the V then I columns sweep — "Watch the columns: the volts climb row by row, and so do the amperes." · **no `_FA`**: S10_L08 ("Good. …") follows. Confident learners skip the spoken hint. |
| `S10_Discover` (existing) | Scene10Builder · unchanged | callback to the Scene 6 prediction, after the reveal, before S10_L11: `S10_Discover_FA` — predicted increase — "Back in the wire you predicted the current would rise. Your data agrees." · `_FB` — predicted decrease — "You predicted the current would drop. The data says it rises. Good detectives update." · `_FC` — predicted no change — "You predicted no change. The data shows it rises with the push." |
| `S11_Target` (Graph) | Scene11Builder (WP G) · after S11_L04 / the `V ∝ I` card (hidden for the activity, back after it), before S11_L05; Boards shot. The builder cues `readings 1` + `graph.live 1` and the Ask Wattson spotlight before the gate; the gate brings up the slider (bottom-left, removed after) and the target column | `S11_Target_GO` — "Your turn: make the ammeter read 0.60 amperes. Use the slider, or ask me!" · `_H` — first miss (and after 15 s idle; also serves the bonus round, so it names no target) — "Each volt adds 0.20 amperes. So how many volts reach your target?" · `_X` — 2nd miss; Wattson sets 3 V, point 3 lights — "Let me show you: three steps of 0.20 make 0.60. So, 3 volts." · `_OK` — hit — "3 volts, 0.60 amperes. Your point sits right on the line!" · bonus for a learner `Confident(Graph)` after the first round: `_GO2` — "Bonus round, detective: now make it read 0.80 amperes." · `_X2` — "Watch: four steps of 0.20 make 0.80. So, 4 volts." · `_OK2` — "4 volts, 0.80 amperes. Every volt, another 0.20. The pattern holds!" |
| `S12_Ratio` (Resistance) | Scene12Builder (WP D) · board stretch: keep `table.work 0` / `table.work 1`; the gate replaces `table.work 2` (it shows the V / I column, highlights row 3 and fills "3.0 ÷ 0.60 = 5" at the end); continue with `table.result 3/4` | `S12_Ratio_Q` — "Your turn, detective. What is 3.0 divided by 0.60?" · `_FA` — "Five again. The same as rows one and two." · `_FB` — "That's 0.60 divided by 3.0. Flip it: volts divided by amps." · `_FC` — "That's 3.0 times 0.60. We need to divide: volts by amps." · `_H` — "Think: how many times does 0.60 go into 3.0?" · `_X` — spoken while the cell fills — "Watch: 3.0 divided by 0.60 is 5. Five, again." |
| `S12_Slope` (Graph, existing) | Scene12Builder · unchanged place | all optional: `S12_Slope_Q` — "So, what is the slope? ΔV divided by ΔI." · `_FA` — "Five. The same five as V divided by I." · `_FB` — "That's ΔI divided by ΔV, upside down. Volts go on top." · `_FC` — "Careful: use the two differences, 2.0 volts and 0.40 amps." · `_H` — "ΔV is 2.0 volts, ΔI is 0.40 amps. Divide them." · `_X` — "2.0 divided by 0.40 is 5. The slope is 5." A struggling learner also gets points 1 and 3 lit and the plot pulsed. |
| `S12_AQ1` (Relationship; `checkpointEnabled`) | Scene12Builder · after S12_L10 (resistance defined), back at the board with the rig active (the reveal turns the real source). Skipped for a learner already `Confident` with the relationship, so a hands-free autoAnswer run never shows it | `AQ1` — storyboard question · `AQ_EXPLAIN` — storyboard hint after a wrong answer · `AQ_EASY` — storyboard easier question (the retry) · no `S12_AQ1_FA` declared: a right pick falls back to `AQ_GOOD` ("Case closed, detective.") · `_FB` — "More push, but less flow? That's not what our wire did." · `_FC` — "If the current stayed put, the I column would never change. It does!" · `_X` — Wattson sets 2 V then 4 V with the readings strip up (Boards shot: the bench meters are off screen), then puts the source back — "Watch the readings: 2 volts gives 0.40 amperes, 4 volts gives 0.80." |
| `S13_AQ2` (Resistance; `checkpointEnabled`) | Scene13Builder (WP G) · asked in the "Question" frame (the Cards frame slid 0.75 m right) after V = IR and the OHM'S LAW title (not dimmed); the 640 px panel docks right over the wall so `V = IR` and the title stay lit on the left | `AQ2` — storyboard question · no `S13_AQ2_FA` declared: a right pick plays `AQ_GOOD` (storyboard "Case closed, detective.") · `_FB` — "Careful: needing more push means it resists more, not less." · `_FC` — "Same current, different pushes, so their resistances can't match." · `_H` — "Think R = V divided by I. Same I for both, so compare V." · `_X` — "Same current, bigger push needed: that one opposes more. Bigger V, bigger R." A struggling learner gets the table's own numbers on the panel and the V / I column lit. |
| `S13_Apply` (OhmsLaw) | Scene13Builder · the "Apply" frame (bench meters, the graph's line without its recorded points, the readings strip V · I · R above it, the live point, V = IR on the board), after the Timeline line `S13_A01` ("Laws are for using. Your turn, and ask me if you get stuck.") and the Ask Wattson spotlight; before the `S13_Recap` say gate | `S13_Apply_GO` — "Use V = IR: set the voltage so the current is 0.80 amperes." · `_H` — "R is 5 ohms. V = I times R: 0.80 times 5 is…?" · `_X` — Wattson sets 4 V — "0.80 amperes times 5 ohms is 4 volts. There: 0.80 amperes." · `_OK` — "4 volts gives 0.80 amperes. V = IR works." |
| `S13_Recap` (say gate) | Scene13Builder · after S13_Apply, before S13_L07 (Apply frame, Wattson pointing at V = IR on the board). Lines listed in this order: `S13_Recap_If_Struggling_OhmsLaw`, `S13_Recap_If_Struggling_Resistance`, `S13_Recap` | `S13_Recap_If_Struggling_OhmsLaw` — e.g. two S13_Apply misses — "The trick: amperes times ohms gives the volts you need. Every time." · `S13_Recap_If_Struggling_Resistance` — S12_Ratio / S13_AQ2 misses — "Remember: more push for the same current means more resistance. R = V ÷ I." · `S13_Recap` — everyone else (autoAnswer runs hear it) — "Push, flow, opposition: know any two, and V = IR gives you the third." |
| `S13_Checkpoint` | Scene13Builder · **removed** (done): AQ1 / AQ_EXPLAIN / AQ_EASY moved to Scene 12's `S12_AQ1`, AQ2 / AQ_GOOD to `S13_AQ2` | the runtime ignores an old marker |

**Cues** (`SceneDirector.Experiment.cs`, idempotent): `readings 1|0` — the live V / I strip in the free band above the graph's
chalk frame (board v ≈ 0.86-0.98, on the graph canvas, so it shows and hides with the graph); in a scene without a graph (Scene
10's Boards stretch) a HUD card bottom-right above the subtitles. It shares that band with Scene 12's `AboveGraph` cards (Slope
chalk, Ω) and Scene 13's carried-over Ω: cue `readings 0` before showing those. `readings.r 1|0` — adds R with its unit, only
after S12_L14 (ohms introduced). `graph.live 1|0` — the live point ("now") and its ghost on the graph. The target gates turn
the readings and the live point on and leave them on.

### §8 notes added 2026-10-05 (phase 2b)

- **Cap and Ask Wattson.** While `SceneDirector.ControlsMax` is lowered (S10_Set1V / S10_Record: 1 V) the action layer refuses any request
  above it BEFORE applying anything: "For this reading the knob stops at 1 V. Higher settings come after your prediction." Its example
  commands and the offline guidance never suggest a voltage above the cap.
- **One-volt rule.** In Scene 10 until row 2 is recorded (`LearnerSession.tableRows < 2`) the AI context drops facts that quote readings above
  1 V or the V–I direction, withholds the chip "Why does the current change…", and adds the rule "the learner has measured only 1 V so far…".
- **App rules.** Unity's `state` carries per-moment rules as `[App rule: …]` first; the proxy keeps only segments whose text starts with a
  prefix in `Server/wattson-proxy/index.js` `APP_RULE_PREFIXES` (7 today) and strips every other bracketed rule from every field; rules can
  only restrict (withhold, hint, stay on case), never override. A new Unity rule needs its prefix added there.
- **Cues.** `rig.drag ammeter|voltmeter|none` (slot glow + draggable until placed; a placed part clears it), `rig.path 1|0`
  (`CircuitRig.showPath`; the lit path reads at 40 % alpha while the key is open). Scene 9 cues `rig.drag` with the part callouts and
  `rig.path 0` at its Boards shot; Scenes 10–13 start with the path off.
- **REPLAY** is disabled while a gate is open (its lines play outside the Timeline). autoAnswer never picks while Ask Wattson is open.
- Accepted residual: the 11→12 cut switches the table cells from `1 | 0.2` to `1.0 V | 0.20 A` (a `table.units` cue would remove it).
