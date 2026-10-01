# Shared systems (`Assets/OhmsLaw/Shared`, namespace `OhmsLaw`; editor code in `Assets/OhmsLaw/Editor`, `OhmsLaw.Editor`)

One paragraph per system and how Scenes 5–13 use it. Scene 4's systems (ModuleFlow, SubtitleTrack, WattsonController, the Ask Wattson agent) were kept and extended rather than duplicated.

## Runtime

**DialogueLine** (ScriptableObject, `DialogueLine.cs`) — `id`, `speaker`, `text` (exact subtitle), `ttsText`, `vo` clip, `fallbackDuration`; `Duration` is the clip length when a clip exists. One asset per line in `Assets/OhmsLaw/SceneNN/Dialogue/`; lines that are not in the storyboard live in `Dialogue/Proposed/`. Every subtitle clip, branch line and FAQ answer in Scenes 5–13 is one of these.

**SubtitleTrack / SubtitleClip / SubtitleUI** — custom Timeline track whose clips reference a `DialogueLine`; `SubtitleBehaviour` shows the exact text in the bottom subtitle box, re-asserting it every frame, and feeds the clip's waveform to Wattson's talking glow. Clip length = VO length or `fallbackDuration` (the builders re-time the Timeline after VO baking). `SubtitleTrack.LineStarted` fires for every line, Timeline or branch, and drives the play test and the F1 overlay.

**ModuleFlow** (persistent root prefab `Shared/ModuleRoot.prefab`) — scene order (`BlockoutAssets.SceneOrder`, Scene04 … Scene13), `CompleteScene()` (white fade → next), `Cut()` (match-cut, no fade), `JumpToScene()`, pause reasons (User / Ask / Hold) implemented as graph speed 0 so Cinemachine holds the shot, `UnitySectionComplete` event and the "→ Scene 14 (video)" end card (plays `scene14Video` with VideoPlayer if set). Hosts the shared canvas: SubtitleUI, CinematicControlsUI, AskWattsonUI, FlowOverlay.

**ModuleState.cs** — `ReviewMode` (`autoAnswer`, `autoDelay`, `timeScale`), `LearnerSession` (prediction, s10PatternAnswer + attempts, slopeAnswer, checkpoint results, tableRows) and `SceneHandoffState` (camera pose, CircuitState, fieldOn/driftSpeed, card positions, table rows, graph shown). Scene 6 writes the prediction; Scene 9's reminder card can read it; every scene's `SceneDirector.SaveHandoff()` fills the hand-off before its signal.

**ExperimentData** (`Shared/Resources/ExperimentData.asset`) — V = {1,2,3,4,5} V, I = {0.20 … 1.00} A, R = 5, with a `Consistent()` check (I = V/R). `CircuitRig`, `ObservationTableUI`, `GraphUI` and the V/I column all read `ExperimentData.Main`.

**ModulePalette / WorldText** — the palette asset adds `charge` (#D6FAFF) to Scene 4's colours; `WorldText.Card()` spawns a world-space TextMeshPro card (optional backing quad) using the module font with the DejaVu Sans symbol fallback (`Shared/UI/Symbols SDF.asset`: Ω ∝ Δ → ÷ × −). Every floating word, formula, label and VFX tag in Scenes 5–13 is a `WorldText` card.

**WattsonController (+ WattsonTrack/WattsonClip)** — Scene 4's body controller, extended with `MoveTo(marker)` and a `moveTo` reference on the gesture clip, so the Timeline flies him between markers (`Platform Point`, `Wire Point`, `Tray Point`, …) and poses him (Idle/Greet/Point/Flick/Reach/RaiseGadget/Aim/Listening/Thinking/Talking). Falls back to a labelled glowing sphere when the model is missing.

**Cue + InteractionGate (Timeline markers)** — `Cue` carries a state command (`wire.field 1`, `table.rows 3`, `graph.plot 5`, `show Card Voltage`, `fade 1`, `prompt …`, `handoff`, …); it is Retroactive so jumping around the Timeline re-applies state. `InteractionGate` names a learner interaction and lists the lines it may speak; it is TriggerOnce so a replay never re-asks. Both sit on the Timeline's own marker track and reach the `SceneDirector` on the director's GameObject.

**SceneDirector (+ SceneDirector.Gates.cs)** — one per scene: applies cues, holds the Timeline at a gate (`PauseReason.Hold`), runs the interaction coroutine (`S06_Prediction`, `S09_PlaceAmmeter/PlaceVoltmeter/CloseKey`, `S10_Set1V/Record/Pattern/Discover`, `S12_Slope`, `S13_Checkpoint`), speaks branch lines with `Say()` (subtitle + VO + talking glow, pauses while Ask Wattson is open), builds the on-screen prompt / voltage slider / tap hints, and performs the hand-off (`Complete()` fade or `CutToNext()` match-cut). Holds the review toggles: `buildMode`, `showLearnerPrediction`, `narrowingMetaphor`, `tableInWire`, `checkpointEnabled` + `checkpointPlacement`.

**MCQPanel** — question + 2–4 option buttons (≥ 88 px) on the module canvas; `WaitForPick(autoPick)` answers itself after `ReviewMode.autoDelay` when `autoAnswer` is on; `Mark`, `Hint`, `Shake` for retry feedback. Used by Scenes 6, 10, 12, 13 and the framework test.

**CircuitRig / CircuitState** — the modern bench circuit: source, key (tappable, lever), nichrome wire XY with X/Y terminals, ammeter and voltmeter with slots, parts tray, series current path (lights once the ammeter is placed, with the exact path label), voltmeter leads, slot glows. Readings: voltmeter = V, ammeter = V/R, needle and digit lerp. Drag-or-tap placement with snap/shake-back, mouse and touch. Scenes 8–13.

**ObservationTableUI / GraphUI** — world-space canvases. The table fills rows one by one, adds the V/I column with worked divisions (`1.0 ÷ 0.20 = 5`), highlights rows with tags (`1 V → 0.2 A`), sweeps a column, and reports taps. The graph has labelled axes (I horizontal, V vertical), plots the five points with coordinates, draws the line through the origin, highlights two points with `I → 2I / V → 2V`, draws the ΔV/ΔI triangle with values and pulses. Scenes 10–12.

**WireInterior** — the inside of a wire: carriers always in thermal motion, drifting together − → + when `fieldOn`, `frozen` for Scene 6, `impede`/`latticeVisibility`/`tunnelRadius` for Scene 12, cross-section flashes and a per-second counter with a one-second ring. Scenes 5, 6, 12.

**FlowOverlay** — F1 / three-finger-tap HUD: scene, current line, gate, Timeline time, FPS, Next scene / Replay line / autoAnswer / speed.

**BlockoutPlaceholder** — tags every stand-in with its intended final result; `Ohm's Law ▸ Blockout Report` lists them per scene in `Docs/BLOCKOUT_REPORT.md`.

## Editor

**SceneBuilderBase (+ .Stages.cs)** — the per-scene builders describe `Dialogue()`, `Agent()`, `Stage()` and `Beats()`; the base turns the beat list into a Timeline (Cinemachine shots with 1 s blends or cuts, Wattson gesture clips, subtitle + VO clips, Cue/InteractionGate markers, note markers for visual beats, the hand-off signal), writes dialogue/FAQ/context assets, instantiates the module root, adds the scene to Build Settings and prints the line table. Stage kits: `WireStage`, `LabGreybox`, `OhmTable`, `Rig`, `Boards`, `Wattson`, `Ohm`, `KidsAt`, `Card`, `Prop`, `Vfx`, `Cam`.

**BlockoutAssets** — palette + symbol font, ExperimentData, placeholder materials, Scene 4's synthesized SFX, VO lookup (`<id>.wav` real, `scratch_<id>.wav` OS TTS), tunnel mesh, charge glyph, scene order, builder discovery.

**BlockoutCommands** — menu items `Ohm's Law ▸ Build Scene 05 … 13`, `Build All`, and a request-file driver for an open editor (`Logs/ohm-build.req` = scene id | `all` | `vo <scene>` | `vo all` | `report` | `flow` | `inventory`; `Logs/ohm-play.req` = scene | `all [2x]`; results in `Logs/ohm-build.txt`, `Logs/Blockout/play-report.txt`).

**BakeBlockoutVO** — `Ohm's Law ▸ Bake VO ▸ <Scene> / All Scenes`: proxy `/tts` when the Wattson proxy answers `/health`, otherwise OS text-to-speech as `scratch_` clips (Windows System.Speech, macOS say, Linux espeak), then rebuilds the scene so the Timeline follows the clip lengths. Real clips named `<id>.wav|mp3` are never overwritten.

**BlockoutPlayTest** — `Ohm's Law ▸ Play Test`: plays a scene or the whole module hands-free with `autoAnswer`, logs every line/gate/hand-off with times, screenshots every beat to `Logs/Blockout/shots/`, verifies every Timeline line was spoken in order, fails on stalls/errors; starts through `playModeStartScene` so the open editor scene is untouched.

**FlowDocument / BlockoutReport / AssetInventory** — `Docs/FLOW.md` (beats per scene from the Timelines, totals, proposed lines, Mermaid chart), `Docs/BLOCKOUT_REPORT.md`, `Logs/Blockout/assets.md` (+ thumbnails).

**Tools/compile_check.sh** — compiles every script under `Assets/` with `dotnet build` outside Unity in ~4 s (same references and C# version as Unity's generated csproj).
