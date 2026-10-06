# HUD design system (2026-10-06, cinematic layout)

One interface for every scene and state, built once in code by `Shared/LessonPanel.cs` (with `HudKit.cs`,
`SubtitleUI.cs`, `PanelViews.cs`, `MCQPanel.cs`, `LiveReadingsPanel.cs`, `Agent/AskWattsonUI.cs`) under the persistent
`ModuleFlow` canvas. The 3D scene owns the screen; the interface sits around it. Reference layout: the screenshot the
brief supplied (top-right controls, bottom subtitle bar, Ask Wattson bottom-right).

## Canvas

Screen Space Overlay · Canvas Scaler *Scale With Screen Size* · reference 1920 × 1080 · match 0.5 · everything under a
`SafeAreaLayout` rect. All numbers below are reference px; nothing is placed in absolute screen coordinates.

## Pieces and anchors

| GameObject | Anchor | Position / size | Holds |
|---|---|---|---|
| `UI_TopControls` | top-right | 32 px from the right, 28 px from the top, 46 px tall, content-sized | `UI_ControlButton` × 5: PAUSE · REPLAY · CC · VOLUME · SKIP |
| `UI_VolumePopup` | top-right, under the bar | 340 px wide, 10 px below the controls | Wattson / Effects / Music sliders, MOTION toggle; a transparent catcher closes it on any click outside |
| `UI_SubtitlePanel` | bottom-stretch | left 120, right 300, bottom 32, content-sized (≈ 110) | speaker (20 px cyan caps) + line (32 px warm white, two lines per page) |
| `UI_AskWattson` | bottom-right | 40 px from the right, 26 px from the bottom, 180 × 156 | round avatar (92 px) in a cyan ring with glow, "ASK / WATTSON" label |
| `UI_Dock` | right, between the controls and the subtitle zone | 480 px wide (580 with the table/graph/inset), top 88, bottom 196 | compact cards in a fixed order; no background of its own |
| `UI_Conversation` | right, same bounds as the dock (580 wide) | shown while Ask Wattson is open | status strip, mode line, messages, suggestions, mic / field / send |

Dock card order (top to bottom): heading (scene title, activity screens only) · Instruction · Info (concept card) ·
Volt (battery buttons) · Readings · Counter · Schematic · Inset · Table · Graph · Formula · MCQ · End. A card exists
only while a beat needs it; the data views (table, graph) shrink before any text can overlap (`CanvasFit`, min 70 %).

A scene declares how much of the frame the dock may reserve (`SceneDirector.dockWidth`, set by its builder; −1, the
default for scenes built before the field existed, decides from the scene: a circuit rig or an electron wire reserves
the dock, a cinematic scene does not): cinematic scenes (4, 7, 8) reserve nothing and get the full frame; activity
scenes (5, 6, 9–13) reserve 480 px, 580 while the table and graph are up, and `PanelFraming` on the camera composes the
shot into the rest of the frame. On screens narrower than 16:9 (4:3 tablets) `PanelFraming` also keeps the shots' 16:9
horizontal field of view, so compositions authored for 16:9 keep their edges.

## Colours

| Token | Value | Use |
|---|---|---|
| card fill | rgba(8, 13, 23, 0.74) | every card, the control pills (0.92 for the popup, 0.9 for the conversation, 0.70 for the subtitle bar) |
| card line | accent at 22 % | 1.5 px outline on cards and pills |
| accent | palette *voltage* blue `#3FA9FF` | outlines, active states, the instruction heading, reveals |
| Wattson accent | palette `#BFF6FF` | speaker name, avatar ring |
| text | `#FAF7F0` / white | subtitle line, card text |
| muted | `#A9BDDB` | dock heading, slider names, secondary labels |
| note | `#FFB830` | concept-card titles, error ring |
| success / warning | the palette's current amber `#FFB830` and resistance red `#FF5A5F` | readings, the ohm |

No gradients, one glow (the avatar's, 10–18 % alpha), no neon.

## Type (TextMeshPro, the palette's Nunito)

| Role | Size | Style |
|---|---|---|
| control label | 18 | bold caps, +2 tracking |
| speaker | 20 | bold caps, +4 tracking |
| subtitle line | 32 | regular, warm white, two lines per page |
| instruction | 22 | bold; heading "YOUR TURN" 15 bold caps accent |
| concept card | 20 title / 19 body | |
| dock heading | 15 | bold caps muted, +5 tracking |
| formula lines | 34 (reveal 64, sub 22) | bold, one line each, auto-shrink to fit |
| buttons in cards | 20 | bold |

## Button states (`HudButton`)

Normal → hover: fill +7 % white, outline to accent 75 %, scale 1.03 → pressed: scale 0.97, fill +12 % → active
(CC ON, PAUSE while paused, VOLUME while its popup is open, the chosen voltage): fill pulled 28 % toward the accent,
outline accent 80 % → disabled (REPLAY / SKIP during an interaction or while Ask Wattson is open): 45 % alpha.
All eased on unscaled time (works while paused); no scale change under reduced motion. Cards scale in (0.96 → 1,
0.28 s) when they appear; the subtitle bar fades 0.25 s.

## Behaviour of the controls

- **PAUSE / RESUME** freezes the scene's Timeline (speed 0: cinematic, VO, animation, cues) and the gates read it;
  RESUME continues from the same frame.
- **REPLAY** jumps to the start of the line being spoken (`SubtitleTrack.LastLineStart`): the VO, caption and cues
  replay once, nothing doubles.
- **CC ON / OFF** shows or hides the subtitle bar; VO is untouched.
- **VOLUME** opens the popup (three buses, the motion toggle); it closes on any click elsewhere.
- **SKIP** jumps to the start of the next spoken line of this scene, or into the next interaction gate if that comes
  first (a required action is never skipped), or hands the scene off past its last line. Cues between are retroactive,
  so the state is as if it had played.
- **Ask Wattson** opens the conversation in place of the dock, pauses the lesson, and resumes it (restarting an
  interrupted line) on close. Avatar states: idle (cyan ring, a slow breath while he can be asked), listening
  (expanding ring), thinking (three dots), speaking (ring pulse), error (amber ring).

## Sounds

`UiSounds`: hover tick, click, open, close, synthesised at runtime, on the effects bus at 35 % gain, under the voice.

## Checking a resolution

`echo "Scene09 2x @1366x768" > Logs/ohm-play.req` sets the Game view to that size for the run (the report notes
`SIZE` when the view took it and `RES WxH (captured frames)` from the first frame's header) and restores 1920×1080
afterwards; frames land in `Logs/Blockout/shots/`. `Scene05 2x hud` presses every top control and checks the result.
