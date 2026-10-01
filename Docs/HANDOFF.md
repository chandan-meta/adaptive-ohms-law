# Hand-off — Ohm's Law Unity module (Scenes 4–13)

Last updated 2026-10-01. Repository: `github.com/chandan-meta/adaptive-ohms-law`, branch `main` (the 226 MB `Environment.fbx` is in Git LFS: run `git lfs install` before cloning/pulling).

## What exists
| Area | State | Where |
|---|---|---|
| Scene 4 "Meet Wattson" | polished, built by `Ohm's Law ▸ Build Scene 04` | `Assets/OhmsLaw/Editor/Scene04Builder*.cs`, `Assets/OhmsLaw/Scenes/Scene04_MeetWattson.unity` |
| Scenes 5–13 | **flow complete** (exact dialogue, VO, subtitles, every interaction, hand-offs, agent context); visuals being upgraded to the reference images | `Assets/OhmsLaw/Editor/Scene05Builder.cs` … `Scene13Builder.cs`, `Assets/OhmsLaw/Scenes/Scene05_*.unity` … |
| Module player (start screen, Play all, jump, autoAnswer, speed, subtitles) | built, first in Build Settings | `Scene00_ModulePlayer.unity`, `Shared/ModulePlayer.cs` |
| Shared runtime | see `Docs/SHARED_SYSTEMS.md` | `Assets/OhmsLaw/Shared/` |
| Voice-over | Windows scratch TTS (`scratch_*.wav`) for Scenes 5–13; Scene 4 has real clips | `Assets/OhmsLaw/SceneNN/Audio/VO/` |
| Wattson proxy (Claude + ElevenLabs + OpenAI) | code present, needs API keys | `Server/wattson-proxy/` |
| Docs | storyboard, flow, blockout report, shared systems, polish research & art direction | `Docs/` |

## Daily workflow
- **Compile without Unity:** `bash Tools/compile_check.sh main` (≈4 s). Run after every C# change.
- **Build a scene (editor open):** menu `Ohm's Law ▸ Build Scene NN`, or from a shell `bash Tools/unity_request.sh Scene07` (the script
  toggles focus so the editor recompiles, waits for `Logs/ohm-reload.txt`, writes `Logs/ohm-build.req`, prints `Logs/ohm-build.txt`).
  Requests: `Scene05`…`Scene13`, `all`, `vo all` / `vo Scene07` (bake VO, re-time Timeline), `report` (BLOCKOUT_REPORT.md), `flow` (FLOW.md),
  `inventory` (asset inventory), `player`, `hierarchy <assetPath>`.
- **Hands-free dry run:** menu `Ohm's Law ▸ Play Test ▸ Whole module`, or `echo "all 2x" > Logs/ohm-play.req`. Report: `Logs/Blockout/play-report.txt`;
  a screenshot of every beat: `Logs/Blockout/shots/<Scene>/`. autoAnswer picks the correct option after 3 s; do not click in the Game view.
- **Review HUD in play mode:** F1 (or three-finger tap): scene, line, gate, time, FPS, next scene, replay line, autoAnswer, speed.
- **Real VO:** start the proxy (`Server/wattson-proxy`, needs ELEVENLABS_API_KEY) so `/health` answers, then `vo all` replaces the scratch clips
  (clips named `<id>.wav|mp3` are never overwritten). Lines marked PROPOSED live in `SceneNN/Dialogue/Proposed/`.

## Toggles (on each scene's `SceneDirector`, or `ReviewMode`)
`buildMode` (Scene 9: LearnerPlaces / AutoPlace), `showLearnerPrediction` (Scene 9 reminder shows the Scene 6 answer), `narrowingMetaphor`
(Scene 12), `tableInWire` (Scene 12 boards inside the wire), `checkpointEnabled` + `checkpointPlacement` (Scene 13), `ReviewMode.autoAnswer`,
`ReviewMode.timeScale`.

## Visual pass (in progress)
Target = the reference images in `Docs/Polish/References/`; spec = `Docs/Polish/ART_DIRECTION.md` (+ `USER_MASTER_PROMPT.md`, `STYLE_GUIDE.md`,
`TYPOGRAPHY.md`, `ASSETS.md`, `REUSE.md`, `CRITIQUE.md`). Kits: `SceneBuilderBase.Look.cs` (lighting/post/lenses/materials/models),
`.Classroom.cs` (room + teaching board), `.Callouts.cs` + `Shared/Callouts.cs` (screen-space callouts / VFX panels / concept panels),
`.Rig.cs` + `CircuitRig.cs` (bench, instruments, X→Y pulse), `.Wire.cs` + `WireInterior.cs` (electron space), `.Lab.cs` + `OhmWorker.cs`
(Ohm + 1826 apparatus), `.Boards.cs` (table/graph), `WorldText.cs` (typography tiers), `UI.cs`/`SubtitleUI`/`MCQPanel` (HUD).
Content guardrails that override the references are listed in ART_DIRECTION §9 (no spoilers on boards/panels).

## Known gaps / next steps
1. Finish the visual pass per scene (P05–P13), rebuild all, run the three dry runs (UI, visuals, full), compare screenshots with references.
2. Real Wattson voice (proxy + keys); tablet device test; resolutions other than 1920×1080 (1600×900, 1366×768 to check).
3. No student models exist (Kabir, Manav, Ishaan, Meera): the kids are the camera POV; audience silhouettes stand in for them in lecture shots.
4. Scene 14 video: set `ModuleFlow.scene14Video` on the module root prefab to play it after Scene 13.
