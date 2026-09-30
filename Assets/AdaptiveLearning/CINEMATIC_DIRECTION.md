# Cinematic direction for Scenes 8–13

## Editing presentation

Open a saved scene under `Assets/AdaptiveLearning/Scenes`. The root `Cinematic Direction - edit targets` contains a `LessonCinematicDirector` and apparatus focus anchors. Move those anchors in Scene view, or edit each target's camera distance and Wattson side in the Inspector. `Shot Blend Seconds` controls the camera transition duration. The cyan `Focus highlight` child can be reshaped with its LineRenderer points; it indicates attention, not heat or current emission.

The existing equipment remains as editable prefab instances. Keep terminals, needle transforms, colliders and linked components when changing a mesh. No external models were downloaded for this pass; the existing equipment source information is in `IMPORTED_LAB_MODELS.md`.

Dialogue lives in the existing `ScriptableObjects/Dialogue` assets. Each line contains text, optional audio, duration and emotion, plus an optional explicit cinematic subject. Enable `Override Cinematic Subject` to select an apparatus deliberately. Without it, the director uses text matching; questions face the learner. Graph narration keeps the camera at the exploration composition because the graph is currently a screen-space display.

Wattson's `Movement Speed` and optional `Pointing Arm` are editable on `WattsonController`. The current model uses restrained procedural movement and arm rotation. These are placeholder performances, not authored skeletal animation clips.

## Playback and input

SKIP ends the current dialogue line and returns the camera. REPLAY repeats the current line, or speaks the last explanation after it has ended. Neither button awards learning mastery. Exploration stays locked until a camera return completes. Ask Wattson interrupts the apparatus shot; closing chat restores exploration and resumes lesson speech. Loss of application focus pauses the presentation.

Scenes 12 and 13 bind their saved joystick and Reset View controls at startup. The same camera controller is used for touch and desktop input. Reset is disabled during a cinematic handoff.

Saved lesson EventSystems use the Input System's `All Pointers As Is` mode, keeping mouse and touch pointer lifecycles independent. The voltage knob owns one drag at a time and releases it on deselection, application focus loss or pause. Its UI range is 0–5 V; the imported supply model retains its printed 0–12 scale, with the physical knob rotation mapped to that scale separately.

Recorded dialogue clips take priority. When a clip is missing, `DialoguePresenter` uses the existing Wattson device speech service and waits for it to finish before advancing. Subtitles remain available if device synthesis fails. Actual Android speech availability and microphone capture still require device testing.

## Validation

Use `Adaptive Learning > Cinematics > Validate Direction And Handoffs` for camera suspension, skip/replay and chat interruption checks. Use `Adaptive Learning > Validate Scenes 12 and 13` for the existing lesson acceptance suite. The latter now exercises saved scenes rather than rebuilding them, preserving manual model and camera edits. Automated validation uses separate learner profiles.

The isolated QA project is `Logs/LabQA`. Reports and Play-mode captures are stored in its `Logs` directory. Unity 6000.3's editor SearchDatabase startup exception is reported separately from lesson runtime failures.

## Measurement continuity

Scenes 10–12 now share the five-reading, 5-ohm investigation: 1–5 V produces 0.20–1.00 A at constant temperature. Scene 10 saves the actual readings; Scene 11 plots them with current horizontally and voltage vertically; Scene 12 passes the validated measurements into its ratio table and slope graph. Direct entry without a compatible completed experiment remains explicitly labelled practice. Older three-reading records are retained rather than rewritten as new evidence.

`LessonExperimentData` defines the shared protocol. The saved Scenes 10–11 contain five table rows and 25 graph targets, with a voltage dial extending to 5 V. Updated dialogue uses device speech where previous recordings no longer match the text. `Adaptive Learning > Validate Measurement Handoff 10 To 12` checks actual recording, rejected duplicates, graph placement and saved-data provenance using an isolated profile. The current tablet captures are `Logs/LabQA/Logs/Five-reading-experiment-1440x1080.png` and `Five-reading-graph-1440x1080.png`.

Scenes 10–11 now reuse the imported apparatus and a `SharedLaboratory` prefab derived from Scene 12. Expand `ExperimentStation` to edit the saved laboratory and imported equipment. Original placeholder renderers remain disabled for recovery; circuit controllers and profile references are retained. `ImportedExperimentMeters` updates the imported needles from the existing circuit. The imported voltmeter face has a 0–3 scale, so its 0–6 V range is explicitly labelled ×2 alongside the exact digital reading. The graph retains its compact reference-bench composition to leave room for plotting.

Device microphone, network AI responses and Android frame rate have not been certified by the editor tests. Wattson still uses a placeholder model and procedural gestures; this is functional presentation polish rather than a finished character-animation production.

## Latest verification

- 25 measurement-handoff checks passed, including open-switch rejection, duplicate prevention, all five readings, HUD agreement, graph placement and Scene 12 provenance.
- 19 cinematic checks passed, including actual Wattson movement, camera framing, skip/replay, chat interruption and return to exploration.
- The graph regression passed mouse/touch selection, five desktop/tablet resolutions, assisted and unassisted scoring, and replay without mastery inflation.
- The final rotary regression passed mouse/touch dragging through 5 V, keyboard/scroll, chat gating, recording and touch-target bounds at 1920×1080, 1920×1200, 1280×800, 2560×1600 and 1440×1080.
- The earlier saved Scenes 12–13 acceptance run passed its 20 acceptance points. The latest input-mode change has been exercised by the rotary regression; it has not been retested on physical Android hardware.

Play-mode screenshots were inspected after correcting missing graph labels, blank observation rows, stale HUD values and Wattson clipping. Reports are under `Logs/LabQA/Logs`. The final rotary run is `Rotary-validation.txt`; data handoff is `Measurement-handoff.txt`; cinematic interruption is `Cinematic-validation.txt`. Unity's separate SearchDatabase startup exception still occurs in the isolated editor, while these tested lesson paths report no runtime errors.
