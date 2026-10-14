Audit and polish pass (14 October 2026): read `Reports/OhmsLawAudit/Final_QA_Report.md` first (regression runs, verified findings, honest scores, remaining limitations), then `Implementation_Log.md` (every change by work package) and `Improvement_Plan.md` (file ownership). Evidence pipeline: play test `all record ask fresh @1920x1080` → `python Tools/audit_frames.py <run>`.

# Hand-off — Ohm's Law Unity module (Scenes 4–13)

UI v2 design checkpoint (12 October 2026): editable [OhmsLaw_UI_v2](https://www.figma.com/design/63sTcmNn0TYOn4lrxAgJgq) is ready for review. Read [UI v2 handoff](UI_v2/HANDOFF.md) and [design review](UI_v2/README.md) before continuing. The audit covers both OhmsLaw and AdaptiveLearning UI, including runtime/hidden/shared views. Unity UI integration awaits the requested design approval; no gameplay scenes or scripts were modified in this design phase. Migration inventory, exported assets, font licences/glyph evidence and explicitly pending Unity validation reports are in `Docs/UI_v2`.

Last updated 2026-10-07 (HUD v3 "Case File": `Docs/Polish/V3_REPORT.md` and `Docs/Polish/HUD_DESIGN.md` supersede the HUD
parts below; new: title screen, chapter banner, pause menu with chapters, turn / clue / case-closed cards, four new gates
S05_ApplyVoltage, S11_DrawLine, S12_SwapWire, S13_BuildLaw). Repository `github.com/chandan-meta/adaptive-ohms-law`,
branch `main`; the HUD v3 work is committed on 2026-10-08 (`git log -1`). Unity 6000.3.0f1, URP, Cinemachine 3, Timeline, TextMeshPro, new Input System;
mouse + touch; no XR. `Environment.fbx` is in Git LFS: `git lfs install` before cloning, `git lfs pull` after.

## 1. What the module is now

Ten code-built scenes play in order from `Assets/OhmsLaw/Scenes/Scene00_ModulePlayer.unity` (first in Build Settings):

| Scene | What happens | Interaction |
|---|---|---|
| 04 Meet Wattson | Ohm's study, 1826; VOLTAGE / CURRENT reveal; "Click or tap the highlighted wire" | tap the wire |
| 05 Inside the wire | random electron motion → voltage as potential difference → drift − → + (conventional-current note) → the counted cross-section, one second at a time | — |
| 06 Your prediction | "If the potential difference increases…?" | one tap (never marked) |
| 07 Back to Georg Ohm | the same study, the 1826 apparatus, why he repeated the measurement | — |
| 08 Ohm's lab becomes our lab | each instrument transforms, the study sweeps into the classroom, Ohm presents and leaves | — |
| 09 Build the circuit | select → place the ammeter (series), select → place the voltmeter (parallel), close the key; live schematic | three guided steps |
| 10 The experiment | 1 V … 5 V on five buttons; readings, table and graph fill in | five taps |
| 11 The pattern | the line through the points, V ∝ I | — |
| 12 Resistance | copper vs nichrome inset, V/I column, slope, the ohm | — |
| 13 Ohm's law | V, I, R → V = IR → OHM'S LAW; Ohm returns; REPLAY / EXIT | end |

The interface is one HUD built in code on the persistent module canvas: PAUSE · REPLAY · CC · VOLUME · SKIP top-right, the
subtitle bar at the bottom, Wattson's round avatar (Ask Wattson) bottom-right, and a dock of compact cards on the right that
exist only while a beat needs them. Cinematic scenes (4, 7, 8) play full-frame; activity scenes reserve the dock strip and the
camera composes into the rest. Wattson speaks every line (edge-tts "Ana"); the sound bank is synthesized in code; Ask Wattson
answers from each scene's saved FAQ until the proxy has its keys.

Read, in this order: `Docs/Polish/HUD_REPORT.md` (the UI: what, how it was tested, what is left), `Docs/Polish/HUD_DESIGN.md`
(the design system: anchors, sizes, colours, type, states, control behaviour), `Docs/Polish/REDESIGN_REPORT.md` (the learning
sequence, voice, sounds, characters; its "panel" is now the dock), `Docs/Polish/REDESIGN_TEXT.md` (every line and panel text),
`Docs/Polish/AUDIO_CUES.md` (every sound cue), `Docs/FLOW.md` (generated: every cue, line and shot per scene with its time),
`Docs/SHARED_SYSTEMS.md` (the runtime). Evidence frames: `Docs/Polish/Evidence/` and `Evidence/HUD/`.

## 2. Verified on 2026-10-06 (editor only, Windows)

- Whole module, hands-free, 1920×1080: `RESULT PASS` (every line of every scene spoken in order, all 11 gates passed, Ask
  Wattson opened / answered offline / closed in Scene 7, finale and end card reached, no console errors).
- HUD control probe (Scene 5): PAUSE, RESUME, CC off / on, VOLUME open / close, REPLAY, SKIP all pass.
- Other Game view sizes, single scenes onward: 1366×768, 1280×720, 1600×900, 2048×1536 (4:3) all pass; frames reviewed.
- Editor self-checks: Ohm Pose 20/20, Wattson Pose 118/118, Wattson Actions all, Lab Continuity 143/143, Voice Pipeline all,
  Wire Electrons all.
- Not done: any run on a device, tablet or player build; a human mouse / touch pass; listening to the mix (levels were measured).

## 3. Read before touching a scene file

**Scenes 5, 7 and 8 carry hand edits** saved in the editor on 2026-10-06 at 13:34, after their last build: the desk-candle
light removed, a light or camera moved, components added. They were not rebuilt and are committed as they are. The builders
now omit the desk-candle light (Scene 4's does too), but the other edits are not in code: **diff a scene against git before
rebuilding it**, or port the edit into its builder first. The same happened on 2026-10-05 to Scene 4 and the Wattson prefab
(Follow Light removed; the builder follows).

## 4. Daily workflow

- **Compile without Unity:** `bash Tools/compile_check.sh <tag>` (≈5 s, dotnet). Run after every C# change.
- **Drive the open editor:** `bash Tools/unity_request.sh "<request>"`. It makes the editor recompile first (a `refresh`
  request, then a focus toggle as fallback; the editor only recompiles on focus or refresh), writes `Logs/ohm-build.req`,
  prints `Logs/ohm-build.txt`. Requests: `Scene05` … `Scene13` (build one scene), `all` (build 5–13), `menu Ohm's Law/Build
  Scene 04`, `vo all` / `vo Scene07` (bake the voice and re-time the Timeline), `menu <menu path>` (any menu item, e.g. the
  tests), `flow` (FLOW.md), `report`, `inventory`, `player`, `hierarchy <assetPath>`, `refresh`.
  **Never send a build while a play test runs** (it fails with "cannot be used during play mode" and the scenes stay stale).
- **Hands-free play test:** `echo "all 2x ask" > Logs/ohm-play.req` (menu: `Ohm's Law ▸ Play Test ▸ Whole module`). Words:
  a scene name or id (`Scene09`, or `Scene04_MeetWattson` for Scene 4), `all`, `2x` (speed), `ask` (opens Ask Wattson in
  Scene 7, asks, closes), `hud` (Scene 5: presses every control and checks the result), `calm` (reduced motion),
  `@1366x768` (the Game view takes that size for the run, back to 1920×1080 after). Report `Logs/Blockout/play-report.txt`
  (ends with `RES WxH (captured frames)` and `RESULT PASS|FAIL`); a frame per beat in `Logs/Blockout/shots/<Scene>/`.
  autoAnswer answers every gate after 3 s: do not click in the Game view during a run.
- **Self-checks (no play mode):** menu `Ohm's Law ▸ Tests ▸ Ohm Pose | Wattson Pose | Wattson Actions | Lab Continuity |
  Voice Pipeline | Wire Electrons`, or `bash Tools/unity_request.sh "menu Ohm's Law/Tests/Lab Continuity"`.
- **Voice:** `Ohm's Law ▸ Bake VO ▸ All Scenes` (or `vo all`) renders every line with edge-tts (`python Tools/edge_bake.py`;
  needs `pip install edge-tts` and internet) as `<id>.mp3`, re-bakes only lines whose words or voice settings changed, never
  touches a voice actor's `<id>.wav|ogg`, then rebuilds the scene to re-time it. With keys in `Server/wattson-proxy/.env` and
  the proxy running (`/health` reports `tts: true`) the same menu bakes through the proxy (ElevenLabs) instead. Voice: `EdgeVoice`
  in `Editor/BakeBlockoutVO.cs` (`en-US-AnaNeural`, +10 %, −4 Hz). Proposed lines live in `SceneNN/Dialogue/Proposed/`.
- **Review HUD in play mode:** F1 (or three-finger tap): scene, line, gate, time, FPS, replay line, autoAnswer, speed.

## 5. Where things live

| Area | Files |
|---|---|
| HUD | `Shared/LessonPanel.cs` (controls, subtitle bar, avatar, dock, popup, `PanelFraming`), `Shared/HudKit.cs` (icons, button states, UI sounds), `Shared/PanelViews.cs` (voltage buttons, counter, schematic, inset), `Shared/SubtitleUI.cs`, `Shared/MCQPanel.cs`, `Shared/LiveReadingsPanel.cs`, `Shared/ObservationTableUI.cs`, `Shared/GraphUI.cs`, `Shared/Callouts.cs` (shim into the dock) |
| Flow and cues | `Shared/ModuleFlow.cs` (scene order, pause reasons, fades, end of module), `Shared/SceneDirector.cs` + `.Cues` + `.Gates` + `.Experiment` + `.Wire` (Timeline cues and interaction gates), `Shared/Cue.cs`, `Shared/InteractionGate.cs`, `Shared/SubtitleTrack.cs` |
| Runtime pieces | `Shared/CircuitRig.cs` (bench, select → place, one voltage path), `Shared/WireInterior.cs` (electrons, counter), `Shared/WattsonController.cs`, `Scene04/Scripts/OhmWorker.cs` (Ohm's procedural hands, desk contact), `Scene04/Scripts/Scene04Director.cs`, `Shared/Agent/*` (Wattson agent, actions, Ask Wattson UI, proxy client) |
| Builders | `Editor/SceneBuilderBase.cs` (+ `.Rig`, `.Wire`, `.Look`, `.Lab`, `.Classroom`, `.Callouts`), `Editor/Scene05Builder.cs` … `Scene13Builder.cs`, `Editor/Scene04Builder*.cs` (Scene 4, its own Timeline), `Editor/SfxBank.cs` (the sound bank), `Editor/BakeBlockoutVO.cs`, `Editor/BlockoutPlayTest.cs`, `Editor/BlockoutCommands.cs` (the request file), `Editor/Tests/*` |
| Proxy | `Server/wattson-proxy/` (Node 22; `/chat` Claude, `/tts` ElevenLabs, `/stt` OpenAI; keys in its ignored `.env`, names in its README) |
| Assets | `Assets/OhmsLaw/SceneNN/{Audio/VO,Dialogue,Agent,Timeline}`, `Scene04/Audio/SFX/` (the bank), `Shared/Art/`, scenes in `Assets/OhmsLaw/Scenes/` |

Toggles: `SceneDirector.buildMode` (Scene 9: LearnerPlaces / AutoPlace), `showLearnerPrediction` (Scene 9 reminder shows the
Scene 6 answer), `dockWidth` (px the dock reserves; −1 = from the scene's content, 0 = cinematic), `ReviewMode.autoAnswer`,
`ReviewMode.timeScale`; in the HUD, CC toggles captions and the VOLUME popup holds the three volumes and reduced motion.

## 6. Open items

1. **Live Ask Wattson.** Put `ANTHROPIC_API_KEY`, `ELEVENLABS_API_KEY`, `OPENAI_API_KEY` in `Server/wattson-proxy/.env`
   (never in Unity, never committed), `npm start`, check `/health`. Until then the conversation says so and answers from the
   saved FAQ; push-to-talk is therefore untested.
2. **Voice licence.** The clips come from edge-tts, an unofficial client of Microsoft Edge's read-aloud service. Before
   distribution, re-bake through the proxy (ElevenLabs) or record a voice actor; a human listen of the mix is also pending.
3. **Device pass.** Mouse and touch by hand (the harness pressed the same Button events), a tablet, WebGL, a player build.
4. **Not in the project:** a "Maya" character (the brief names one; only Wattson and Georg Ohm exist, the learners are the
   camera), student models, a Scene 14 video (`ModuleFlow.scene14Video`), foley recordings (every cue is synthesized),
   UI prefab assets (the HUD is built once in code; see HUD_REPORT §3), background blur behind the subtitle bar.
5. **Rebuilding Scenes 5, 7, 8** means porting their hand edits first (§3).

## 7. Gotchas worth knowing

- Cue markers at the same Timeline time fire in no guaranteed order: offset a cue that must follow another by 0.05 s
  (the builders' `Cue(..., offset)`); `formula.clear` keeps lines revealed in the same frame.
- `PanelFraming` shifts the projection, so the free frame is the central part of the authored shot: keep Wattson's and the
  apparatus' markers inside it (see the builders' `SourceSide`, `TrayPointC`, `KeyPointC`, `FrontPoint`).
- Dock views build under a child root (`LessonPanel.Fresh`) so a card can be cleared and rebuilt; a card that must keep
  running while hidden fades and leaves the layout (`LiveReadingsPanel`) instead of deactivating.
- A `UI.Outline` child in a layout group needs `LayoutElement.ignoreLayout`; set a RectTransform's pivot before its offsets.
- The play test reads the run's resolution from the first captured PNG (editor `Screen.width` is unreliable from update
  callbacks); `@WxH` re-targets every Game view through its own size selection and waits a few editor updates.
- `Scene04` is not a play-test scene id (Scene 4 has no builder): use `Scene04_MeetWattson`.
