# Per-scene task (one agent per `Assets/OhmsLaw/Editor/SceneNNBuilder.cs`)

You own exactly one file. Read, in order: `Docs/Polish/BRIEF.md` (rules), `Docs/Polish/USER_MASTER_PROMPT.md` (the target),
`Docs/Polish/ART_DIRECTION.md` (buildable spec + content guardrails §9 — binding), your scene's reference image(s) in
`Docs/Polish/References/` (use the Read tool on the PNGs; Scenes 10–13 have none: extend the Scene 7/8/9 language — classroom,
teaching board, callouts), `Docs/STORYBOARD_SCENES_05_13.md` (your scene; dialogue/beats/interactions are untouchable),
`Docs/Polish/CRITIQUE.md` (your scene's findings; many are already fixed by the kits — re-check against the kit code),
`Docs/Polish/KIT_REPORTS_2.md` (classroom, callouts, bench, electrons, ohm kits: API, caller changes, ref names) and
`Docs/Polish/KIT_REPORTS.md` sections "text" and "ui" (typography tiers, WorldText.Cap/Wrap, Card styles, boards, HUD).
Then read the kit source you call: `SceneBuilderBase.cs` (beat DSL, Card/Vfx/Wattson/Cam), `.Look.cs` (Vcam, lenses, Lit/Glow,
Model, Emitter, Polyline), `.Classroom.cs` (ClassroomSet, Board/BoardLine/BoardPanel/BoardSurface, Audience, constants *C),
`.Callouts.cs` + `Shared/Callouts.cs` (Callout/Concept helpers, cues `callout show|hide|hideall <id>`, VfxPanel auto-shown for
"[VFX] ..." objects), `.Rig.cs` + `Shared/CircuitRig.cs` (Rig, markers, PlayPulse), `.Wire.cs` + `Shared/WireInterior.cs` (WireStage,
markers, effects), `.Lab.cs` + `Scene04/Scripts/OhmWorker.cs` (OhmApparatus / ClassroomApparatus, modes Work/Present/Examine via cue
`ohm.pose <Mode>`, `ohm.freeze 1|0`), `.Boards.cs` (Boards() mounts on the teaching board when ClassroomSet ran),
`Shared/SceneDirector.cs` (every cue verb). Today's screenshots of your scene: `Logs/Blockout/shots/<YourScene>/`.

## Stage()
- Scenes 7–13: `ClassroomSet(modernBench, audience)` first (7: oak desk + audience; 8: starts oak, swaps to the bench via the existing
  `Hide("Old Table"); Show("Bench")` cues, audience on; 9–13: bench, audience off), `ClassroomApparatus()` for 7–8, `Rig(...)` for 8–13,
  `Boards(...)` for 10–13 (they land on the teaching board), `Wattson(WattsonHomeC, BenchWideCamC)`. Use the classroom camera names
  ("Bench Wide", "Bench Close", "Bench Present", "Table Close", "Boards") and add scene-specific Vcam shots only where a reference asks
  for a different framing. Scenes 5, 6, 12: `WireStage(...)` (the electron space); Scene 5's first 3 s is Scene 4's copper wire
  (Study() + the Scene 4 wire position) before the dive; Scene 12 starts on the bench, dives, returns to the boards on the board.
- World text: only the storyboard's on-screen text; cards via `Card(...)` with `WorldText.Cap.*` tiers (glow style for concept/hero/title
  words, panel style for data/prose); board text via `Board(...)` (chalk). Everything that was a floating prop label becomes a
  `Callout(id, target, title, description, anchor)` shown/hidden by cues (`callout show <id>`), with the storyboard's label text.
  Concept cards in the electron space = `Concept(...)` panels (Scene 6 text per ART_DIRECTION §9).
- Delete `KidsAt(...)`, `FloorGrid`, `Back Wall`, greybox `Prop/Prim` stand-ins that the kits replaced. Keep `Vfx("...")` objects ONLY
  where no real effect exists; the VfxPanel appears automatically for any shown object named "[VFX] …" (also real effects named that way).
- Every world card faces the camera that reads it (`Face(card, camPos)`); Wattson never between camera and subject; nothing floats
  (use `ContactShadow` for new props).

## Beats()
Keep every `Line`, `Gate`, `Handoff` and the cue semantics. Change `Shot(...)` to the classroom cameras / two-camera blends for motion
(a push-in = `Shot("A")` then `Shot("B")` a few seconds later: the Timeline blends over the overlap), `Gesture(...)` targets/markers,
`Show/Hide`/`callout` cues for the renamed objects, `Visual(...)` durations within ±0.5 s, `ohm.pose`/`ohm.freeze` cues where the
references show Ohm presenting/examining. Match-cut continuity: your first frame = the previous scene's last frame (same camera name,
same visible objects, same readings): check the neighbouring builders and state in your report what they must show.

## Verify and report
`bash Tools/compile_check.sh <agent>` must be clean. Report: files changed; refs/markers/callout ids/cameras added; what the previous
and next scene must match; any kit request (exact and minimal); anything unverified.
