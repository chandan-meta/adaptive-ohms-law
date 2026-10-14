# Ohm's Law — Improvement plan (2026-10-14)

Source of truth for the implementation pass. Evidence: `Reports/OhmsLawAudit/before/` (the recorded dry run of commit
`28c9aa4`: `Gameplay_Full_Run.mp4`, `Shots/<Scene>/*.png` per beat, `Captures/<Scene>_sheet.jpg` at 1 fps,
`Cinematic_Frames/sheet_*.jpg` at 10 fps round every scene change, `Frame_Audit.csv`). Benchmark: the Photosynthesis
module (`D:\AI unity project\AI PHOTOSYNTHESIS`, see `Logs\uishots\33_title.png`, `30_cinematic_hud.png`).

## Ranked findings → work packages

| # | P | Where | Finding (observed) | Fix (suggested) | WP |
|---|---|---|---|---|---|
| 1 | P0 | Scenes 8–13 | Lab is dim and cold: the polished grade keeps exposure 0.2 while the builder intends 0.45; walls/floor near black, the green board a flat rectangle, no warmth after the warm study (`Shots/Scene08_OurLab/13_*.png`, `Scene09_BuildCircuit/01_enter.png`) | Polish copies exposure from the source profile; warm key + shaft, brighter coves, board wash + soft board emission, rack/screen glow up, dust motes, floor smoothness for the ring reflection, a `finale` look | L |
| 2 | P0 | Audio | `SceneDirector.Play` sets and resets pitch in the same call: VoltStep per-volt pitch, Record/Plot per-row pitch never heard | Play the clip through a pooled one-shot source with the pitch, or `PlayClipAtPoint`-style per-call source | A |
| 3 | P1 | Title | No title music; the title sits on a paused Timeline whose pad only starts after START | Title pad (new synth loop) from `ModuleFlow` while `TitleScreen` is open, 0.6 s fade-out on Start | A |
| 4 | P1 | Scenes 9–13 | One static camera ("Bench Study") for ~4.5 min; no close-ups when a meter, the key or the wire is explained (`Captures/Scene09_BuildCircuit_sheet.jpg`) | Slow push-ins / glides to Goal markers at the explanation beats; return to the gate framing before every gate (tap targets must stay visible); meter close-up as the first reading lands; finale glide with the `finale` look | C |
| 5 | P1 | Scenes 5–6 | Yellow floor strips (emission ×2, bloom) dominate every frame; the Earth backdrop screen distracts; the conduit is thin and the electrons are small white discs; Wattson 75 px (`Shots/Scene05_InsideTheWire/02_S05_V01.png`, `10_S05_V07.png`) | Strips ×0.45, backdrop ×0.35, copper haze + rim light on the conduit, carriers larger/cyan with halo, lattice ions visible inside the conduit, closer "Wire Inside"/"Wire Drift" framing with Wattson on the platform next to the conduit; Scene 6 gauge ×1.6 and the freeze VFX in a two-shot | W |
| 6 | P1 | HUD | Navy glass HUD is readable but monochrome; the kids reference (`Docs/Polish/KIDS_UI/reference.png`) asks for rounded cyan-edged panels, amber primary buttons, Fredoka display type, V/I/R in electric blue / gold / coral, success/error feedback | Restyle tokens + card/button/caption/MCQ/title/end card; Fredoka SDF for display text; graded quick checks get correct/wrong states (Scene 6 prediction stays unmarked); every position, size and binding kept | U |
| 7 | P1 | Scene 8 | Study → lab change drops from warm to cold navy (`Captures/Scene08_OurLab_sheet.jpg` 04m04–04m06); the time-wave ring is only an arc for one frame | L's warmer lab look makes the reveal an upgrade; C keeps the ring upright and holds it 0.6 s longer, and switches the bed to the lab theme at the reveal | L, C |
| 8 | P2 | Scenes 7–13 | Same synth pad (PadLab) for 6 minutes; no circuit hum when current flows | New `PadLabTheme` (pad + soft arpeggio, 70 bpm) and `CircuitHum` loop; hum level follows the current in Scene 10 | A (clips), C (cues) |
| 9 | P2 | Scene 4 | Ohm-niscope flash still blooms to a white blob at SH08 (`Shots/Scene04_MeetWattson/08_S04_L05.png`) | Niscope flarePeak 2.5 → 1.6 in the saved Scene 4 (builder constant) | L (small) |
| 10 | P2 | Scene 13 | Ohm returns behind the bench in the dark lab; "the lab warms" is not visible (`Shots/Scene13_OhmsLaw/08_S13_N01.png`) | `look 2.0 finale` + glide on Ohm's return; Finale glow particles kept | L, C |
| 11 | P3 | Scene 13 | No free-play after the law (change V or R, watch I) | Later: reuse the battery slider + top readings with a copper/nichrome toggle on the end card | — |
| 12 | P3 | Wattson | "Clips under the table" (reported): not reproduced in this run; furniture clearance includes the round table's 18 renderers | Re-check in the after run at 1 fps | QA |

Science check (observed, no change needed): electron drift − → +, conventional current + → −, voltage = potential
difference, current = charge per second, R = V/I with 5 V / 1 A = 5 Ω, Ω defined, "temperature constant" stated; no
speed-of-light claims. Units V / A / Ω throughout.

## Work packages and file ownership (one editor: agents edit code only, compile with `bash Tools/compile_check.sh <tag>`)

| WP | Owner files | Must not touch |
|---|---|---|
| **L** lab look | `Editor/SceneBuilderBase.Classroom.cs`, `Editor/CinematicPolish.cs`, `Editor/BlenderSet.cs`, `Editor/SceneBuilderBase.Look.cs`, `Editor/Scene04Builder.cs` (niscope peak only) | scene builders 05–13, Wire.cs, Shared/UI |
| **W** wire look | `Editor/SceneBuilderBase.Wire.cs`, `Shared/WireInterior.cs`, `Editor/Scene05Builder.cs`, `Editor/Scene06Builder.cs` | BlenderSet.cs (override set-material emission in Wire.cs after the set loads), Classroom.cs |
| **C** camera + beats | `Editor/Scene07Builder.cs` … `Scene13Builder.cs`, `Editor/SceneBuilderBase.cs` (only if a shot helper needs a new optional parameter), `Shared/SceneDirector.Lab.cs` (new cue verbs only) | Classroom.cs, Wire.cs, UI files, SceneDirector.cs |
| **A** audio | `Editor/SfxBank.cs`, `Shared/AudioBus.cs`, `Shared/AudioDucker.cs`, `Shared/ModuleFlow.cs`, `Shared/SceneDirector.cs` (the `Play` pitch fix only), `Editor/Scene04Builder.Timeline.cs` (music bed only) | builders 05–13 (ask C for cues), HudScreens.cs |
| **U** UI | `Shared/LessonPanel.cs`, `HudKit.cs`, `HudScreens.cs`, `SubtitleUI.cs`, `MCQPanel.cs`, `PanelViews.cs`, `UI.cs`, `LiveReadingsPanel.cs`, `ObservationTableUI.cs`, `GraphUI.cs`, `Agent/AskWattsonUI.cs`, `Editor/BlockoutAssets.cs` (font asset), `Shared/UI/Fonts/` | ModuleFlow.cs, SceneDirector*.cs, builders |

Cross-WP contracts: A ships clips `PadIntro` (title), `PadLabTheme` (lab bed), `CircuitHum` (loop); C uses them via the
existing `Music()` / `Ambience()` helpers in Scenes 8–13. L defines a SceneDirector look named `finale`; C calls
`look 2.0 finale` in Scene 13. No new voice lines (no re-bake). Reduced motion must still disable every new move or pulse.

## Verification order (orchestrator)

compile → `menu Ohm's Law/Build Scene 04` (Scene 4 touched) → `all` → `menu Ohm's Law/Polish/Apply later scenes` →
`menu Ohm's Law/Tests/*` → play test `all record ask fresh @1920x1080` → `python Tools/audit_frames.py after` → compare.
