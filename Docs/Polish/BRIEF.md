# Polish pass — working brief for implementation agents

Read this first, then your own task. Everything here applies to every agent.

## What this is
Unity 6 (6000.3), URP, project root `D:\AI unity project\AI OHM'S LAW` (path has a space and an apostrophe — quote it).
An Ohm's Law learning module for Class-10 students. Scenes 5–13 are built from code by **scene builders**
(`Assets/OhmsLaw/Editor/Scene05Builder.cs` … `Scene13Builder.cs`) on top of shared kits
(`Assets/OhmsLaw/Editor/SceneBuilderBase*.cs`); runtime behaviour lives in `Assets/OhmsLaw/Shared/`.
The current build is a greybox BLOCKOUT. This pass makes it look finished: real models, Scene 4's lighting and
post-processing, properly sized text, real effects. **Dialogue, line order, interactions, gates, timing and the
accuracy rules stay exactly as they are.**

Reference material (read what your task needs):
- `Docs/STORYBOARD_SCENES_05_13.md` — the storyboard: beats, exact on-screen text, interactions (source of truth).
- `Docs/Polish/STYLE_GUIDE.md` — Scene 4's visual language with exact values (lighting, post, materials, cards, cameras, Wattson, VFX, UI).
- `Docs/Polish/ASSETS.md` — which real asset to use for every prop, with scale factors, moving-part names, material status, caveats.
- `Docs/Polish/REUSE.md` — reusable systems from the older `Assets/AdaptiveLearning` implementation.
- `Docs/Polish/TYPOGRAPHY.md` — the exact TextMeshPro size formula and the size table to use.
- `Docs/Polish/CRITIQUE.md` — shot-by-shot problems in the current blockout (screenshots in `Logs/Blockout/shots/<Scene>/`).
- `Logs/Blockout/assets.md` + `Logs/Blockout/assets/*.png` — inventory and thumbnails; `Logs/Blockout/hierarchy_Lab_Scene04.txt` — every part of the study with bounds.
- `Docs/SHARED_SYSTEMS.md` — what each shared system does.

## The shared kit you build on (`SceneBuilderBase*.cs`)
- `SceneBuilderBase.Look.cs` — **Scene 4's frame and look, already lifted**: constants `FloorY`, `DeskY`, `Group`, `Hover`, `OhmFeet4`,
  `OhmHead`, `Wire4`, `Notebook4`, `Galvanometer4`, `Pile4`, `DeskTop`, `BenchCentre` (2.0 × 0.9 m bench where the desk was),
  study-frame cameras `StudyWideCam/Look`, `StudyBenchCam/Look`, `StudyCloseCam/Look`, `StudyBoardsCam/Look`, `StudyWattsonHome`,
  `BoardsAnchor`; helpers `Study()` (Scene 4's dressed room prefab at the origin; desk/drawer/chair parts registered as
  refs `Study/<name>`), `StudyLighting()` (Scene 4 rig + global ACES/bloom volume), `WireLighting(tint)`, `GlobalPost`,
  `Lens`/`Lenses()` + `lensWide/Medium/Shallow/Tracking/Dive`, `Vcam(name, pos, look, fov, focus, lens, track, noise)`,
  `Lit`, `Glow` (additive HDR), `Translucent`, `EmissiveCopy`, `Hdr`, `MeshAsset`, `Torus`, `Model(path, name, parent, pos, euler, scale, collider)`
  (instantiates a project prefab and strips old `AdaptiveOhmsLaw` scripts), `FitCollider`, `FindChild`, `Emitter` (Scene 4 particle base), `Polyline`.
  Module-wide generated assets go under `Assets/OhmsLaw/Shared/Art/{Materials,Meshes,Post}`.
- `SceneBuilderBase.cs` — the builder core: `Node`, `Ref`, `Marker`, `Prim`, `Prop`, `Placeholder`, `Vfx`, `Card`, `Capsule`, `KidsAt`,
  `Ohm`, `Wattson`, `Audio`, `Materials`, `FloorGrid`, `MainCamera` (post on, SMAA, Cut default blend), `Cam`, and the Timeline
  generator (`Timeline()`), beat DSL (`Line`, `Wait`, `Visual`, `Cue`, `Show`, `Hide`, `Shot`, `Gesture`, `Gate`, `Handoff`).
- `SceneBuilderBase.Wire.cs` (inside the wire), `.Lab.cs` (room + Ohm's apparatus), `.Rig.cs` (modern circuit), `.Boards.cs` (table + graph canvases).
- Runtime: `Assets/OhmsLaw/Shared/SceneDirector.cs` (+ `.Gates.cs`) applies Timeline **cues** by name: `show <ref>`, `hide <ref>`,
  `boost <ref>`, `fade 0|1`, `wire.*`, `rig.*`, `table.*`, `graph.*`, `prompt …`, `reminder`, `lab`, `ohm.freeze`, `handoff`, `spotlight`.
  Anything a cue names must be registered with `Ref(go)` (looked up by GameObject name). Cards are registered as `Card <name>`.

## Rules
1. **Edit only the files your task lists.** If you need a change elsewhere, write it in your final report under "Requests for other files" — do not make it.
2. After every substantial edit run `bash Tools/compile_check.sh <your-agent-name>` from the project root (≈4 s; it compiles every script
   under `Assets/` with Unity's references). Finish with zero errors. You cannot run Unity, build scenes or play; nobody can see your result
   until the editor rebuilds the scenes, so be precise about positions, scales and orientations (use the bounds in ASSETS.md / hierarchy dumps).
3. Keep the public API that other code already calls unless your task says otherwise (check callers with grep before renaming).
4. Text: use the size table in `Docs/Polish/TYPOGRAPHY.md`; never invent font sizes. World text must face the camera that will look at it
   (TextMeshPro reads correctly from a camera on its −Z side looking toward +Z; use `Face(card, cameraPos)` or `LookRotation(card.pos − cam.pos)`).
5. Every remaining stand-in keeps a `BlockoutPlaceholder` note; everything you make real loses its magenta `[VFX]` tag / placeholder note.
6. Accuracy rules in the storyboard are non-negotiable (electrons drift − → +, never still; the nichrome wire never glows; readings from `ExperimentData`; …).
7. Style: match the surrounding code (short comments that say why, no boilerplate, no speculative options). C# 9, Unity 6 APIs (`FindObjectsByType`, Cinemachine 3 `CinemachineCamera`).
8. Your final message is your report: what you changed (files, new public members with signatures), what callers must do, requests for other files, and anything you could not verify.
