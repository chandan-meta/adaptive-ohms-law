# Hand-off — Ohm's Law Unity module (Scenes 4–13)

Last updated 2026-10-06. Repository: `github.com/chandan-meta/adaptive-ohms-law`, branch `main`. `Environment.fbx` is in Git LFS: run `git lfs install` before cloning/pulling, then `git lfs pull` to retrieve the model.

## Latest handoff — 2026-10-06 (module redesign)

- The module was redesigned around one right-hand lesson panel: see `Docs/Polish/REDESIGN_REPORT.md` (what changed, what was
  tested, what is left) and `Docs/Polish/REDESIGN_TEXT.md` (every line and panel text). Checkpoint before the work: git tag
  `checkpoint-2026-10-06-before-redesign`.
- Wattson's voice is now edge-tts `en-US-AnaNeural` (`Tools/edge_bake.py`, driven by `Ohm's Law ▸ Bake VO ▸ All Scenes` / `vo all`);
  every line of Scenes 4–13 has an `<id>.mp3`, the scratch Windows clips are gone. The ElevenLabs proxy path is kept: with keys in
  `Server/wattson-proxy/.env` the same menu bakes through the proxy instead.
- All UI lives in `Shared/LessonPanel.cs` (+ `PanelViews.cs`, `SubtitleUI`, `MCQPanel`, `LiveReadingsPanel`, `AskWattsonUI`). The
  blackboard table/graph kit (`SceneBuilderBase.Boards.cs`), `CinematicControlsUI` and `QuestionBank` were deleted.
- Sounds: `Editor/SfxBank.cs` synthesizes the bank (`Scene04/Audio/SFX/*.wav`); the plan is `Docs/Polish/AUDIO_CUES.md`.

## Previous handoff — 2026-10-05

- Latest asset commit: `53500d8` (`Update environment, Scene 04, materials and font assets`), successfully pushed to `origin/main`, including its LFS model. The working tree was clean before this documentation update.
- That commit contains 13 modified assets: the environment FBX, Scene 04 scene and Wattson prefab, seven shared materials, and the Nunito, Symbols and LiberationSans fallback font assets. The Wattson prefab removes its Follow Light and updates particle settings.
- Previous implementation commit: `22b5479`. Its wow-factor pass and review fixes are documented in `Docs/Polish/WOW_REPORT.md`; use that report for completed fixes, prior verification and remaining limitations. Earlier workflow notes below provide orientation but are not a fresh validation of the latest assets.
- Validation in this handoff session: `git diff --check` passed before the asset commit; Git confirmed the push and LFS upload. Unity compilation, scene playback, visual review and device builds were not run for these asset changes.
- Next: open with Unity 6000.3.0f1, allow imports to finish, inspect Scene 04 lighting and particles, the environment, shared material textures and font rendering, then run the existing module checks below. Review the saved scene before rebuilding it from editor scripts, since regeneration may overwrite scene/prefab edits.
- Continue from `Assets/OhmsLaw/Scenes/Scene00_ModulePlayer.unity` for the Scenes 4–13 module. The root README describes the older `Assets/AdaptiveLearning` entry point; this handoff and `Docs/Polish/WOW_REPORT.md` describe the newer module.
- External follow-up remains: live proxy/voice setup, manual interaction and pause/skip checks, tablet/WebGL validation, and the Scene 14 video assignment. Keep credentials in the ignored local configuration described by the proxy README.

## What exists
| Area | State | Where |
|---|---|---|
| Scene 4 "Meet Wattson" | polished, built by `Ohm's Law ▸ Build Scene 04` | `Assets/OhmsLaw/Editor/Scene04Builder*.cs`, `Assets/OhmsLaw/Scenes/Scene04_MeetWattson.unity` |
| Scenes 5–13 | **flow complete** (exact dialogue, VO, subtitles, every interaction, hand-offs, agent context); visuals being upgraded to the reference images | `Assets/OhmsLaw/Editor/Scene05Builder.cs` … `Scene13Builder.cs`, `Assets/OhmsLaw/Scenes/Scene05_*.unity` … |
| Module player (start screen, Play all, jump, autoAnswer, speed, subtitles) | built, first in Build Settings | `Scene00_ModulePlayer.unity`, `Shared/ModulePlayer.cs` |
| Shared runtime | see `Docs/SHARED_SYSTEMS.md` | `Assets/OhmsLaw/Shared/` |
| Voice-over | edge-tts `en-US-AnaNeural` mp3 for every line (`<id>.mp3`), baked by `Ohm's Law ▸ Bake VO ▸ All Scenes`; the proxy (ElevenLabs) voice replaces it when keys exist (see "Real VO") | `Assets/OhmsLaw/SceneNN/Audio/VO/` |
| Wattson proxy (Claude + ElevenLabs + OpenAI) | code present, needs API keys | `Server/wattson-proxy/` |
| Docs | storyboard, flow, blockout report, shared systems, polish research & art direction | `Docs/` |

## Daily workflow
- **Compile without Unity:** `bash Tools/compile_check.sh main` (≈4 s). Run after every C# change.
- **Build a scene (editor open):** menu `Ohm's Law ▸ Build Scene NN`, or from a shell `bash Tools/unity_request.sh Scene07` (the script
  toggles focus so the editor recompiles, waits for `Logs/ohm-reload.txt`, writes `Logs/ohm-build.req`, prints `Logs/ohm-build.txt`).
  Requests: `Scene05`…`Scene13`, `all`, `refresh` (asset refresh + recompile of an unfocused editor; the script sends it first), `vo all` /
  `vo Scene04` … `vo Scene13` (bake VO, re-time Timeline), `menu <path>` (runs any menu item, e.g. `menu Ohm's Law/Tests/Adaptive Questions`),
  `report` (BLOCKOUT_REPORT.md), `flow` (FLOW.md), `inventory` (asset inventory), `player`, `hierarchy <assetPath>`. Result: `Logs/ohm-build.txt`.
- **Self-checks (editor, no play mode):** menu `Ohm's Law ▸ Tests ▸ …`: `Voice Pipeline` (lexicon + bake text), `Wire Electrons`,
  `Lab Continuity` (match-cuts 4→5…8→9), `Wattson Pose`, `Ohm Pose` (hands never below the desk), `Wattson Actions` (Ask Wattson action
  layer, offline); plus `Ohm's Law ▸ Validate Scene 04 (play test)`. From a shell: `bash Tools/unity_request.sh "menu Ohm's Law/Tests/Lab Continuity"`.
- **Hands-free dry run:** menu `Ohm's Law ▸ Play Test ▸ Whole module`, or `echo "all 2x ask" > Logs/ohm-play.req` (`ask` also opens Ask Wattson in
  Scene 7, asks a question and closes it; the run ends with an `end_card` frame). Report: `Logs/Blockout/play-report.txt`; a screenshot of every
  beat: `Logs/Blockout/shots/<Scene>/`. autoAnswer picks the correct option after 3 s; do not click in the Game view.
- **Review HUD in play mode:** F1 (or three-finger tap): scene, line, gate, time, FPS, next scene, replay line, autoAnswer, speed.
- **Voice:** `Ohm's Law ▸ Bake VO ▸ All Scenes` (or `vo all` / `vo Scene04` through the request file) renders every line with edge-tts
  (`python Tools/edge_bake.py`, needs `pip install edge-tts` and internet) as `<id>.mp3`; a re-bake redoes only lines whose words or voice settings
  changed; a voice actor's `<id>.wav|ogg` is never touched. With keys in `Server/wattson-proxy/.env` and the proxy running (`/health` reports
  `tts: true`) the same menu bakes through the proxy (ElevenLabs) instead. Lines marked PROPOSED live in `SceneNN/Dialogue/Proposed/`.

## Toggles (on each scene's `SceneDirector`, or `ReviewMode`)
`buildMode` (Scene 9: LearnerPlaces / AutoPlace), `showLearnerPrediction` (Scene 9 reminder shows the Scene 6 answer), `ReviewMode.autoAnswer`,
`ReviewMode.timeScale`; the panel's MORE menu holds captions, reduced motion and the three volumes.

## Visual pass (in progress)
Target = the reference images in `Docs/Polish/References/`; spec = `Docs/Polish/ART_DIRECTION.md` (+ `USER_MASTER_PROMPT.md`, `STYLE_GUIDE.md`,
`TYPOGRAPHY.md`, `ASSETS.md`, `REUSE.md`, `CRITIQUE.md`). Kits: `SceneBuilderBase.Look.cs` (lighting/post/lenses/materials/models),
`.Classroom.cs` (room + teaching board), `.Callouts.cs` + `Shared/Callouts.cs` (screen-space callouts / VFX panels / concept panels),
`.Rig.cs` + `CircuitRig.cs` (bench, instruments, X→Y pulse), `.Wire.cs` + `WireInterior.cs` (electron space), `.Lab.cs` + `OhmWorker.cs`
(Ohm + 1826 apparatus), `.Boards.cs` (table/graph), `WorldText.cs` (typography tiers), `UI.cs`/`SubtitleUI`/`MCQPanel` (HUD).
Content guardrails that override the references are listed in ART_DIRECTION §9 (no spoilers on boards/panels).

## Known gaps / next steps
1. See `Docs/Polish/REDESIGN_REPORT.md` ("Remaining") for the redesign's open items.
2. Live Ask Wattson (proxy + keys: Claude chat, ElevenLabs voice, OpenAI speech-to-text); tablet device test; resolutions other than 1920×1080.
3. No student models exist (Kabir, Manav, Ishaan, Meera): the kids are the camera POV; audience silhouettes stand in for them in lecture shots.
4. Scene 14 video: set `ModuleFlow.scene14Video` on the module root prefab to play it after Scene 13.
