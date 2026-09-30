# Adaptive Ohm's Law — Scenes 06–08

## Play the continuous lesson

Open `Scenes/Scene_06_AdaptivePrediction.unity` and press Play. Scene 6 records a prediction, Scene 7 tests it with a fixed 10 Ω circuit, and Scene 8 plots the saved readings. Each transition waits for the learner's Continue button and fades into the next activity.

You can also open `Scenes/Scene_10_BecomeTheExperimenter.unity` or `Scenes/Scene_11_TurnDataIntoGraph.unity` directly. Scene 7 gives a neutral introduction if there is no prediction. Scene 8 uses explicitly labelled practice data if no compatible, completed experiment exists. It never presents sample values as the learner's own measurements. Sample-only practice is saved separately as Scene08GraphPractice and does not award assessed mastery. Scene 8 ends with a saved completion state and `OnScene08Completed`; Scene 9 is not implemented.

## Scene 7 review changes

- Full-screen apparatus view with compact overlays. Scenes 6–8 share the conductor chamber, cyan/amber palette, lesson header and compact dialogue strip. Wattson stays visible.
- Connected main-loop wires and a separate voltmeter branch across the resistor. Labelled source, meters, switch and resistor. The switch now visibly opens around the correct axis; its collider uses the shared Input System event path.
- Controls are gated during dialogue/conclusion and while chat is open. Recording is unavailable with an open circuit, at an already recorded voltage, or after all readings are collected.
- Readings are sorted by voltage and saved immediately. An interrupted experiment restores its unfinished session and table.
- The latest completed Scene 6 prediction is recalled. The evidence question appears only when ready to accept input. Dialogue remains visible above the answers.
- Only assessed voltage/current concepts receive evidence-question mastery; meter concepts remain independent.

## Rotary simulation controls

Close the circuit switch, then drag the source dial clockwise or anticlockwise. The knob adjusts voltage from 0–3 V in 0.1 V increments, with drag detents at 1, 2 and 3 V. Current follows I = V / 10 Ω; it is not independently set. Record each of 1 V, 2 V and 3 V, in any order. Intermediate settings remain available for exploration, but cannot be recorded for this activity.

Mouse and touch use the same rotary component. Left/right arrow keys adjust the selected dial; up/down moves focus; the mouse wheel also provides fine adjustment. The physical source knob and digital/analogue meter readouts follow the circuit model. Opening chat or entering a non-interactive stage disables circuit controls. Supported guidance highlights the dial.

Added components: `UI/RotaryVoltageControl`, `UI/RotaryDialGraphic`, and `Circuit/InstrumentReadouts`. Shared environment prefab: `Prefabs/Learning/ConductorChamber.prefab`. Apparatus geometry is locally generated placeholder art; no third-party models or licences were added. Desktop apparatus rendering is capped at 1920×1080; mobile rendering is capped at 1280×720, with 2× MSAA.

## Scene 8 graph activity

Scene 8 uses a dedicated composition: an opaque graph surface on the left, Wattson facing the learner on the right, and a small reference circuit below him. Subtle point markers retain large invisible touch targets. Simplified reading selectors sit directly below the graph. Shared conductor scenery is subdued, with secondary apparatus labels and the redundant status footer hidden. The saved editor preview also has readable labels instead of blank controls. `GraphPointGraphic` draws the markers; `CompanionViewport` keeps the companion/reference column separate at desktop and tablet aspect ratios.

Select a data row, then click or tap the matching graph intersection. Voltage is on the horizontal axis in volts and current on the vertical axis in amperes. Large targets support mouse, touch and standard Unity keyboard navigation. After three points, a line connects the evidence to the origin predicted by the ideal fixed-resistance model. The origin is explicitly distinguished from the three measured points.

The interpretation question asks what the straight line through the origin means. Wrong answers receive a clue with retry or guided explanation. Asking Wattson uses graph context, selected reading, progress, mastery and recent mistake; it uses local responses, with no external API.

`GraphActivityRecord` stores source experiment session, data provenance, observations, plotted indices, plotting mistakes, hints, explanation use, interpretation outcome and completion times. Interpretation attempts and response times use the shared question records. Reloading an unfinished graph restores its points.

## Adaptive behaviour

The ten concept IDs start at 50. Configurable values are in `ScriptableObjects/Concepts/PrototypeRules.asset`:

- Correct first unassisted answer: +10.
- Correct after a hint/explanation: +4.
- Incorrect question answer: −2.
- Increased difficulty: configurable additional gain.
- One failed attempt provides Guided support; repeated failures provide Supported help. Mastery thresholds also affect support.
- Help never subtracts mastery. Misplaced graph points are recorded and increase visual guidance without subtracting mastery.
- Scene 8 assesses GraphInterpretation and VoltageCurrentRelationship. Replaying completed Scene 7/8 activities records practice without repeatedly awarding mastery.

## Architecture

Question definitions and dialogue are ScriptableObjects. Wattson presentation is independent of adaptive scoring. `IStudentProfileStore` separates JSON storage from learning logic; `IWattsonResponseProvider` separates chat from the local provider.

New runtime components for this review and Scene 8:

- `Core/Scene08FlowManager`, `Core/Scene07To08Transition`
- `Graph/GraphPlotView`, `Graph/GraphGridGraphic`
- `UI/ExperimentPresentation`, `UI/CompanionViewport`, `UI/LearningViewport`, `UI/ViewportBackdrop`

Updated: Scene 6→7 transition, Scene 7 flow, question scoring options, learning profile, observation table, circuit controls, switch interaction and contextual chat. Existing Wattson and question prefabs remain available. Scene 8 reuses those scene components and shared systems; it does not need a runtime scene generator.

New assets: `ScriptableObjects/Questions/Scene08GraphQuestion.asset`, `ScriptableObjects/Dialogue/Scene08Dialogue.asset`, `Scenes/Scene_11_TurnDataIntoGraph.unity`.

Editor utilities: `LessonReviewTools`, `Scene07Refinement`, `Scene08Builder`, `LessonContinuityValidation`. Scene 8's builder refuses to overwrite an existing scene. Use the serialized scene for normal editing.

## Storage and validation

Profiles save under `Application.persistentDataPath/adaptive-learning-profile.json`, with a backup and preservation of malformed files. Existing profile data is retained and upgraded to version 2. No real profile is reset by the automated validation tools.

Use **Adaptive Learning → Validate Scenes 06 to 08** for the isolated Play-mode continuity test. It exercises real scene transitions, saved prediction recall, out-of-order readings, partial-session reloads, graph data transfer, mouse/touch input events, graph hints, chat, interpretation, JSON roundtrip and layouts at 1920×1080, 1920×1200, 1280×800, 2560×1600 and 1440×1080. Results and screenshots are written under `Logs/`.

Use **Adaptive Learning → Validate Scene 07 Rotary Controls** for mouse/touch drag-and-record, fine adjustment, chat gating, meter values and dial bounds across five resolutions.

Use **Adaptive Learning → Validate Scene 08 Graph** for the focused rendering, modal mouse input, data-row selection, guided/replay and first-attempt scoring checks. The 3D viewport uses a capped render texture so URP cannot draw over modal UI.

Use **Adaptive Learning → Build Scenes 06 to 08 Windows** to build the continuous lesson under `Builds/Scenes06-08/`.

## Prototype limits

Wattson, lab apparatus and materials are placeholders. All scripted dialogue now has bundled synthetic voice clips; dynamic chat uses device speech. Final character voice recordings and audio effects remain placeholders. The simulation is an ideal source, negligible wire resistance and a 10 Ω resistor at constant temperature, with ideal meters. Windows can use the Google Gemini proxy in Backend; cloud profile storage is not implemented. Editor-generated touch events and tablet aspect ratios are not a substitute for testing on a physical Android tablet.

Next suggested activity: Scene 9 can investigate resistance using the established circuit and the graph's slope, with a separate specification before implementation.






## Wattson conversation

Scenes 6–8 now share a scrolling text conversation, session history, contextual offline replies and a configurable backend adapter. See [WATTSON_CHAT.md](WATTSON_CHAT.md) for setup, request format and current limits. Windows is configured for the local Google Gemini proxy; provider capacity errors use clearly labelled built-in explanations. ASK WATTSON > SPEAK uses offline Windows recognition or Android device recognition; Wattson speaks replies through the device voice. See the setup guide for microphone and platform requirements.



