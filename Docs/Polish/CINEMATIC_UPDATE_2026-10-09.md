# Cinematic update — 9 October 2026

This update works in the existing Unity module. The supplied gameplay video and project assets take precedence over the concept boards. No exterior, gate, replacement character, replacement apparatus, or replacement introduction was added.

## Scope and preservation

- Recovery reference: `codex/pre-cinematic-polish-20261009`.
- `Scene00_ModulePlayer`, `Scene04_MeetWattson`, and the original introductory Timeline remain unchanged on disk. The active project calls its first story chapter Scene04; it does not contain a separate Scene01 introduction in this flow.
- Patched saved scenes: Scene05_InsideTheWire, Scene06_AdaptiveQuestion, Scene07_BackToOhm, Scene08_OurLab, Scene09_BuildCircuit, Scene10_Experiment, Scene11_Graph, Scene12_Resistance, Scene13_OhmsLaw.
- Original narration assets, narration text, Timeline sequence, scientific data, circuit interactions, graph, formula, and completion sequence are retained.
- Original models and prefabs are retained. Per-scene prefab overrides carry the character settings; the shared character prefabs were not replaced.

## Implementation

| Area | Change |
| --- | --- |
| START banner | Existing live lab background, amber title, two-line subtitle, navy/amber START JOURNEY button, existing keyboard/hover support. The original Wattson is temporarily presented beside the title. A short curtain transition restores his original position/path before the existing introduction resumes. |
| Inside the wire | Original tube, electrodes, cable, charges, grid floor, and Wattson retained. Layered background piers and inset light strips add depth without introducing machines or scientific claims. Two shadowless lights illuminate the background. |
| Glass | Small URP transparent shader with a clear centre, Fresnel edge, narrow highlight, reflection-probe sampling, and fog. No screen refraction or distortion of teaching labels. |
| Materials | Later scenes use copied material variants so the original introduction's materials remain intact. Cooler classroom walls, warmer bench surface, darker board, steel highlights, and smoother wire-floor reflections. |
| Post processing | Separate later-scene volume profiles; restrained bloom, reduced grain, softer vignette, and modest contrast/color adjustment. |
| Wattson movement | Swept furniture-footprint checks and short spring integration steps prevent desk crossings and large-frame movement instability. Existing desk meshes provide the bounds. |
| Conversations | Later scenes can reuse their authored wide Cinemachine camera during Ask Wattson, then return to the paused Timeline camera. This avoids bringing the character into an extreme macro close-up. |
| Ohm | Optional slower torso/head variation and asymmetric presenting gestures on the existing rig, with reduced-motion support. No replacement Animator controller or new animation package. |
| Audio | Native AudioMixer routes for Voice, Music, SFX, UI, and Ambience. Existing music, sounds, user volume controls, and narration ducking are reused. Crossfades use the bus's nominal gain so replay during a fade cannot progressively reduce its volume. |
| Transitions | Repeated scene-load requests share one transition rather than starting overlapping fades/loads. |
| Tooling | An in-place scene patcher, saved-prefab-override checks, input-device probe, screenshot capture correction, and per-scene Editor performance observations. |

No third-party assets, dependencies, or packages were added. Background dressing uses Unity primitives and the existing render pipeline. Audio clips remain the project's existing resources; this update does not claim newly composed music.

## Modified code

- `Shared/HudScreens.cs`: title composition and handoff.
- `Shared/WattsonController.cs`: furniture clearance and conversation camera.
- `Scene04/Scripts/OhmWorker.cs`: opt-in performance variation, enabled only on later scene instances.
- `Shared/ModuleFlow.cs`: duplicate-load guard.
- `Shared/SceneDirector.cs`, `Shared/AudioBus.cs`, `Shared/HudKit.cs`: nominal crossfade gain and mixer routing.
- `Shared/AudioDucker.cs`: comment updated to describe routing; ducking behavior retained.
- `Shared/Art/WireGlass.shader`: transparent wire material.
- `Editor/CinematicPolish.cs`: saved-scene patches, regression checks, mixer creation and Windows build command.
- `Editor/CinematicPointerProbe.cs`: synthetic mouse/touch input through the actual Input System UI module.
- `Editor/BlockoutPlayTest.cs`: title capture, optional input probe and Editor performance logging.

Paths above are relative to `Assets/OhmsLaw`.

## Four passes

1. Inspect the actual assets/flow, inspect the supplied footage, create the recovery branch, and run the unmodified module to collect baseline evidence.
2. Implement scene/material/UI/character/audio changes in place and run compile and regression checks.
3. Review full-run screenshots and interactions; correct weak background visibility, title framing, saved prefab overrides, and conversation framing. Rerun affected sections.
4. Final full-speed recording, player build/smoke check, evidence comparison, and final QA report. Results are recorded in the accompanying QA report after execution.

## Reapplying and reviewing

Use `Ohm's Law > Polish > Apply later scenes` to reapply the scene patch. Do not rebuild the hand-edited scenes from their older scene builders. `Regression checks` reopens the saved later scenes and checks that character overrides survived serialization.

Evidence and raw logs are under `Logs/CinematicPolish`. The Figma layout study is at https://www.figma.com/design/D3HOj9L28f5o60n22Yr7QY; the actual Unity captures are the implementation evidence.
