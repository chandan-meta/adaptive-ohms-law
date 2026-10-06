# Cinematic HUD redesign — completion report (2026-10-06, second brief)

Brief: the reference screenshot (Ohm at his desk, controls top-right, subtitle bar at the bottom, round Ask Wattson
bottom-right) plus the written requirements (sections 1–31). Design system: `HUD_DESIGN.md`. This report says what was
implemented, what the review passes found and fixed, what the evidence is, and what is left.

## 1. What changed

The full-height right-hand panel from the morning's redesign is gone. The interface is now four anchored pieces built
once in code under the persistent module canvas (`Shared/LessonPanel.cs`, `Shared/HudKit.cs`, `Shared/SubtitleUI.cs`,
`Shared/Agent/AskWattsonUI.cs`), identical in every scene and state:

- **`UI_TopControls`** (top-right, 32 px from the edges): PAUSE · REPLAY · CC · VOLUME · SKIP as five compact pills,
  icon + caps label, translucent navy fill, hairline blue outline. Each has normal / hover (1.03, brighter) / pressed
  (0.97) / active (blue tint: CC ON, PAUSE while paused, VOLUME while its popup is open) / disabled (45 %) states,
  animated on unscaled time so they respond while the lesson is paused.
- **`UI_SubtitlePanel`** (bottom, 120 px from the left, 300 px from the right, 32 px from the bottom): the speaker in
  small cyan caps and the line in 32 px warm white, two lines per page, on a translucent bar that fades in and out.
- **`UI_AskWattson`** (bottom-right): Wattson's face in a 92 px circle with a cyan ring and a soft glow; "ASK /
  WATTSON" under it. Idle breathes gently while he can be asked; listening expands a ring; thinking shows three dots;
  speaking pulses the ring; an error turns the ring amber. It opens the existing conversation (same agent, actions,
  offline answers, push-to-talk) in a card on the right and the label becomes CLOSE.
- **`UI_Dock`** (right, under the controls, above the subtitle zone): compact cards, each present only while a beat
  needs it, in a fixed order: scene heading (activity screens), YOUR TURN instruction, concept note, battery buttons,
  readings, charge counter, circuit schematic, resistance inset, table, graph, formulas, prediction, end buttons. The
  dock is 480 px wide, 580 while the table, graph or inset is up, and never draws a background of its own.
- **`UI_VolumePopup`**: VOLUME opens a small card under the bar with Wattson / Effects / Music sliders and the
  reduced-motion toggle; any click elsewhere closes it.

Each scene says whether the dock may reserve part of the frame (`SceneDirector.dockWidth`, from the builder's
`DockWidth`): the cinematic scenes (4, 7, 8) reserve nothing and get the whole frame; the activity scenes (5, 6, 9–13)
reserve the dock strip and the camera composes into the rest (`PanelFraming`). The screen stays 100 % open in the
cinematic scenes and about 75 % open in the activity scenes, with the subtitle bar and the controls the only constant
overlays.

Removed: the full-height panel, its header title strip, its MORE menu (its settings moved into the volume popup), the
rectangular ASK WATTSON button, the hold-to-skip control (SKIP is now a single tap).

## 2. Control behaviour (as implemented)

| Control | Does | Guard |
|---|---|---|
| PAUSE / RESUME | Freezes the scene's Timeline (speed 0: VO, animation, cues, camera) and the gates read it; the label and icon switch to RESUME / play; RESUME continues from the same frame. | — |
| REPLAY | Jumps to the start of the line being spoken; the VO, caption and cues replay once (the old audio stops with the jump, nothing doubles). | Disabled during an interaction gate and while Ask Wattson is open. |
| CC ON / OFF | Shows or hides the subtitle bar (VO untouched); the setting persists. | — |
| VOLUME | Opens / closes the popup; its sliders write the three audio buses; MOTION toggles reduced motion. | — |
| SKIP | Jumps to the start of the next spoken line of the scene; if an interaction gate comes first it jumps to the gate instead (a required action is never skipped); past the last line it hands the scene off. Cues between are retroactive, so the scene's state is as if it had played. | Disabled during a gate and while Ask Wattson is open. |
| Ask Wattson | Pauses the lesson, opens the conversation card, resumes (restarting an interrupted line) on CLOSE. | Hidden in scenes whose context disables it. |

## 3. Files

| File | Change |
|---|---|
| `Shared/LessonPanel.cs` | rewritten: the four HUD pieces, the dock and its cards, the popup, the play-test hooks, `CardFade`, `PanelFraming` (reserve-aware) |
| `Shared/HudKit.cs` | new: `Icons` (pause, play, replay, skip, volume rasterised from signed-distance shapes), `HudButton` (states), `UiSounds` (hover / click / open / close, synthesised, effects bus) |
| `Shared/SubtitleUI.cs` | rewritten for the bottom bar (fade, speaker caps) |
| `Shared/Agent/AskWattsonUI.cs` | targets the avatar root |
| `Shared/PanelViews.cs`, `Shared/MCQPanel.cs`, `Shared/LiveReadingsPanel.cs` | sized to the dock's cards; the readings card waits for the meters |
| `Shared/SceneDirector.cs`, `Shared/ModuleFlow.cs` | `dockWidth` per scene, passed to the HUD on scene entry; the spotlight targets the avatar |
| `Editor/SceneBuilderBase.cs`, `Scene07Builder.cs`, `Scene08Builder.cs` | `DockWidth` (0 for the two cinematic lab scenes) |
| `Editor/BlockoutPlayTest.cs` | `@WxH` sets the Game view size for a run (`GameViewSize`), `hud` runs the control probe, the report notes the resolution |
| `Docs/Polish/HUD_DESIGN.md` | the design system |

**Prefabs.** The brief asks for reusable prefabs (`UI_TopControls`, `UI_ControlButton`, `UI_SubtitlePanel`,
`UI_AskWattson`, `UI_ContextInstruction`, `UI_VolumePopup`). The module builds its UI in code on one persistent canvas
that every scene shares, so there is exactly one implementation of each piece and nothing is duplicated between
scenes; the GameObjects carry those names. No `.prefab` assets were saved from them: a prefab of a code-built hierarchy
would be a second, inert copy that drifts from the code. If prefabs are wanted for a designer to edit in the
Inspector, the builders would need to instantiate them instead of constructing the pieces; that is a separate change.

## 4. Review passes

All runs were performed in the Unity 6000.3.0f1 editor on Windows (hands-free play test, 2× speed, autoAnswer),
with the Game view at the stated size; nothing ran on a device or in a player build. Frames: `Logs/Blockout/res/<size>/`
(33 kept in `Docs/Polish/Evidence/HUD/`).

**Pass 1 — layout.** Anchors: controls top-right, subtitle bar bottom-stretch, Ask Wattson bottom-right, dock right.
Checked at 1920×1080 (whole module), 1366×768 (Scenes 4, 8, 9 onward), 1280×720 (Scene 10 onward), 1600×900
(Scene 12 onward) and 2048×1536, a 4:3 tablet (Scene 5 onward): nothing clips, overlaps or leaves the screen; the
subtitle bar and the avatar never touch; the controls never cover a face or the apparatus. Fixed on the way: the
subtitle bar sat off-centre (a pivot set after the offsets), the control outlines took layout cells, the dock heading
was unreadable over the bright wall, and at 4:3 the 16:9 compositions lost their edges (the "− negative" terminal was
cut) until the camera kept its 16:9 horizontal field of view on narrower screens.

**Pass 2 — functionality** (`Scene05 2x hud`, which presses each control through its Button events and checks the
state): PAUSE holds the Timeline and reads RESUME; RESUME runs it again; CC OFF hides the captions and CC ON shows
them; VOLUME opens the popup and a click elsewhere closes it; REPLAY restarts the line being spoken; SKIP jumps to the
next line. All eight checks pass; the scene still reaches its hand-off with every line heard. Ask Wattson
(`all 2x ask`): opened in Scene 7, asked, received the offline answer, closed, and the lesson resumed. Fixed on the
way: the volume sliders' handles stretched into tall ovals (the Slider stretches its handle along the track), and the
note card read through the popup (now opaque, dock-wide, over a faint scrim).

**Pass 3 — cinematic review** (whole module at 1920×1080, every beat's frame): Scenes 4, 7 and 8 play full-frame with
only the controls, the captions and the avatar on screen; Wattson's and Ohm's faces, the wand, the meters and the
VOLTAGE / CURRENT reveal stay clear of the interface; captions appear with their lines and page with the voice;
cards appear only for their beat (notes, the counter, the prediction, the instruments' cards during the
transformation, the schematic during assembly, the buttons, table and graph during the experiment, the inset, the
formulas, the finale). Found and fixed: the scenes had never been rebuilt with the dock setting (the one build-all ran
during a play test and failed), so Scenes 7 and 8 were composed into the free area as if they had a dock; the dock
width now resolves at runtime from the scene's content, which also avoids rebuilding the three scenes that carry hand
edits (see §6).

**Pass 4 — polish.** Buttons: hover 1.03 / pressed 0.97 / active tint / disabled 45 %, eased on unscaled time; cards
fade and scale in (0.25 s); the subtitle bar fades; the avatar breathes, pulses when listening, dots when thinking,
pulses its ring when speaking; UI sounds (hover tick, click, open, close) at 35 % on the effects bus; the control pills
at 90 % opacity so bright scenery (the THINK poster) never reads through a label; the inset's "MORE / LESS current"
labels moved clear of their bars; one font (Nunito) and one corner radius family throughout.

**Regression:** editor self-checks after the changes: Ohm Pose 20/20, Wattson Pose 118/118, Wattson Actions all,
Lab Continuity 143/143 (its flame check now accepts a study without a desk-candle light, which the saved scenes no
longer have), Voice Pipeline all, Wire Electrons all; `Docs/FLOW.md` regenerated.

## 5. Evidence

| Frame | Shows |
|---|---|
| `hud_s04_intro`, `hud_s04_voltage_current` | Scene 4 cinematic: controls, caption bar, avatar only |
| `hud_s05_note_card`, `hud_s05_counter_card` | the dock's concept card and the charge counter card |
| `hud_s06_prediction_card` | YOUR TURN + the prediction card |
| `hud_s07_ask_open`, `hud_s07_ask_answer`, `hud_s07_ask_closed` | Ask Wattson opened, answered offline, closed |
| `hud_s07_cinematic_full_frame`, `hud_s08_cinematic_full_frame` | Scenes 7 and 8 full-frame |
| `hud_s09_select_ammeter`, `hud_s09_close_switch` | instruction, concept card, live schematic |
| `hud_s10_set_1v`, `hud_s10_table_graph` | voltage buttons, readings, table and graph |
| `hud_s11_v_proportional_i`, `hud_s12_inset`, `hud_s12_slope` | reveal, inset, slope marker |
| `hud_s13_finale`, `hud_s13_end_card` | V = IR / OHM'S LAW, REPLAY / EXIT |
| `probe_paused`, `probe_cc_off`, `probe_volume_popup`, `probe_skipped` | the control probe's states |
| `res_1366x768_*`, `res_1280x720_*`, `res_1600x900_*`, `res_2048x1536_*` | the other sizes |

Reports: `Logs/Blockout/play-report-<size>.txt` (each ends `RES WxH (captured frames)` and `RESULT PASS`).

## 6. Remaining

- **Background blur** behind the subtitle bar: not added. URP's overlay canvas has no cheap blur; the translucent
  fill at 82 % keeps the text readable without it.
- **Hover states** exist for mouse (and the play test exercises clicks); on touch there is no hover, as expected.
- **A human pass with mouse and touch** on a device is still needed: the controls were pressed by the test harness
  through the same Button events a tap raises, not by a finger.
- **Success sound**: not added (the placement, record and plot cues already confirm each action); hover / click /
  open / close exist.
- **Prefab assets**: see §3.
- **Hand-edited scenes.** `Scene05_InsideTheWire`, `Scene07_BackToOhm` and `Scene08_OurLab` were edited in the editor
  on 2026-10-06 at 13:34 (the desk-candle light removed, a light or camera moved, components added) after their last
  build; they were not rebuilt for this work and those edits are kept and committed as they are. The builders now omit
  the desk-candle light; the other edits are not in code, so rebuilding those scenes from the builders would discard
  them (diff against git first).
