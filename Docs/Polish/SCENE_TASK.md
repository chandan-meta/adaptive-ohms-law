# Per-scene polish task (template for the scene agents)

You own exactly one file: `Assets/OhmsLaw/Editor/SceneNNBuilder.cs`. Read `Docs/Polish/BRIEF.md` first, then:

1. `Docs/STORYBOARD_SCENES_05_13.md` — your scene's section (beats, exact text, interactions). Nothing in Dialogue() changes;
   the Beats() order of lines, gates and hand-offs does not change.
2. `Docs/Polish/CRITIQUE.md` — your scene's numbered findings and the cross-scene list: fix every HIGH and MEDIUM item that lies
   inside your file; LOW where cheap.
3. `Docs/Polish/KIT_REPORTS.md` — what the shared kits now provide (study, rig, wire, text, ui) and what callers must change.
   Then read the kit source itself: `SceneBuilderBase.cs`, `.Look.cs`, `.Lab.cs`, `.Rig.cs`, `.Wire.cs`, `.Boards.cs`.
4. `Docs/Polish/STYLE_GUIDE.md` sections 3 (camera conventions) and 7 (VFX recipes); `Docs/Polish/TYPOGRAPHY.md` section 4 (sizes).
5. `Logs/Blockout/shots/<YourScene>/*.png` — what it looks like today.

## What to do in Stage()
- Use the kits: `StudySet(...)`/`OhmApparatus()` (Scenes 7–13), `Rig(...)` (8–13), `Boards(...)` (10–13), `WireStage(...)` (5, 6, 12),
  `Wattson(position, lookAt)` (Scene 4's prefab variant with the real Ohm-niscope), cards via `Card(...)` with the typography tiers,
  cameras via `Vcam(name, pos, look, fov, focus, lens, track, noise)` with the lens profiles (`lensWide/Medium/Shallow/Tracking/Dive`).
- The kids are the POV camera. No capsules, no name labels.
- Every world card faces the camera that reads it and is sized for that camera's depth (TYPOGRAPHY). Keep all on-screen text exact.
- Replace magenta `[VFX]` tags with real effects wherever a kit helper or a Scene 4 recipe (`Emitter`, `Glow`, `Polyline`, `PopIn`) makes
  it a few lines; when you keep a tag, keep its `BlockoutPlaceholder` note.
- Wattson: 2–3 m from the lens, lower-left or lower-right third, never between the camera and the subject; `moveTo` markers you create.
- Continuity: the last frame of the previous scene is your first frame (same camera constants, same visible objects, same readings).
  The match-cut pairs are 5→6, 7→8→9→10→11→12→13. Objects visible at your hand-off must exist at the next scene's start — check the
  next scene's builder to agree on names/positions and say in your report what the next scene must show.

## What to do in Beats()
- Keep every `Line`, `Gate`, `Handoff` and the cue semantics. You may change `Shot(...)` cameras, `Gesture(...)` targets/markers,
  `Show/Hide` of objects you renamed, `Visual(...)` durations within ±0.5 s, and add cues for new effects.

## Report
Files changed; new ref/marker names; camera list; what the previous/next scene must match; any kit request; compile status.
