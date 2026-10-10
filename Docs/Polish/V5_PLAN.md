# v5 "Live Wire" — build contract (2026-10-10)

The user's brief, verbatim where it matters: *"The module is super boring. It needs to change a lot."* Everything below is
what changes, who changes it (one agent per area, no shared files), and the interfaces the areas agree on. Ponytail rules
apply to every agent: reuse the kits that exist (`SceneBuilderBase.*`, `LessonPanel`, `UI`, `Draw`, `WorldText`, `PopIn`,
`SfxBank`), shortest working diff, no new packages, `ponytail:` comment on any deliberate ceiling, one runnable check where
logic is non-trivial (the editor self-checks under `Ohm's Law/Tests` count).

## Ground rules for every agent

- Compile with `bash Tools/compile_check.sh <your-area>` after every change (≈5 s, no Unity). Zero errors before you report.
- **Never** send a Unity request (`Tools/unity_request.sh`, `Logs/ohm-*.req`), never open scenes, never bake voice. The
  orchestrator rebuilds the scenes, bakes the voice and runs the play tests after all agents report.
- Scene content lives in the builders (`Editor/SceneNNBuilder.cs`): a builder change is how a scene changes. The saved
  `.unity` files are rebuilt from them (Scenes 5, 6 get `CinematicPolish.Apply(5,6)` re-applied after the rebuild: keep
  `PolishMaterial` / volume / Wattson / Ohm patches working; only the pillar "Chamber Depth" goes, see area B).
- New spoken lines: `L("<id>", "<text>", fallbackSeconds, proposed: true)`; keep ids stable; the orchestrator bakes them
  (edge-tts "Ryan", per-line modulation already in `BakeBlockoutVO.EdgeVoice`).
- Keep every existing handler, gate id, UnityEvent, play-test word and the `autoAnswer` path working (the hands-free play
  test must still reach the end card). Keyboard: Enter still acts on the current turn, 1/2/3 pick options.
- Edited files are CRLF; keep them so. Match multi-line patterns after normalising if you script a patch.
- Report (your final message) = the JSON the schema asks for: files touched, cues/APIs added, new line ids, what the
  orchestrator should look at in the frames, anything you deliberately left out.

## Areas and file ownership

| Area | Agent owns | Must not touch |
|---|---|---|
| **A. HUD skin + new HUD surfaces + input** | `Shared/LessonPanel.cs`, `Shared/HudKit.cs`, `Shared/HudScreens.cs`, `Shared/SubtitleUI.cs`, `Shared/MCQPanel.cs`, `Shared/PanelViews.cs`, `Shared/LiveReadingsPanel.cs`, `Shared/GraphUI.cs`, `Shared/ObservationTableUI.cs`, `Shared/Agent/AskWattsonUI.cs`, `Shared/ModuleFlow.cs`, `Shared/SceneDirector.Experiment.cs` (cue verbs only), the `fade` case in `Shared/SceneDirector.cs`, new `Shared/LookAround.cs` | builders, gates, wire, Scene 4 |
| **B. Inside the wire (Scenes 5, 6) + the dive transition** | `Editor/SceneBuilderBase.Wire.cs`, `Editor/Scene05Builder.cs`, `Editor/Scene06Builder.cs`, `Shared/WireInterior.cs`, `Shared/SceneDirector.Wire.cs`, `Editor/CinematicPolish.cs` (`PolishWire` only), `Editor/WireChecks.cs`, `Scene04/Scripts/Scene04Director.cs` (`Complete()` only) | HUD files, other builders |
| **C. The experiment (Scenes 9–13): schematic in front, slider, readings strip, 3 V prediction, centre reveals, data stage, 3D copper/nichrome pop-up, longer resistance, quick check** | `Editor/Scene09Builder.cs` … `Editor/Scene13Builder.cs`, `Shared/SceneDirector.Gates.cs`, new `Shared/WireSample.cs`, new `Shared/WorldSchematic.cs`, new `Editor/SceneBuilderBase.Sample.cs`, `Shared/CircuitRig.cs` (only if the slider needs it), `Shared/ModuleState.cs` (LearnerSession fields) | HUD files (use area A's API below), wire, Scene 4/8 |
| **D. Intro cameras + Ohm (Scene 4), the lab transformation (Scene 8)** | `Editor/Scene04Builder.cs`, `Editor/Scene04Builder.Timeline.cs`, `Editor/Scene04Builder.Apparatus.cs`, `Scene04/Scripts/OhmWorker.cs`, `Editor/Scene08Builder.cs`, `Editor/SceneBuilderBase.Lab.cs`, `Editor/SceneBuilderBase.Classroom.cs`, `Shared/SceneDirector.Lab.cs`, `Editor/SfxBank.cs` (add recipes; never rename) | HUD files, Scenes 5–6, 9–13 |

`Editor/SceneBuilderBase.cs`, `.Look.cs`, `.Rig.cs`, `.Callouts.cs`, `Shared/SceneDirector.cs` (except A's `fade` line),
`Shared/WattsonController.cs`, `Shared/Cue.cs`, `Shared/InteractionGate.cs` are frozen for this pass: ask in your report if
you need a change there and work around it meanwhile (a `protected` helper added in a *new* partial file of your own is fine).

## The interface contract (area A builds it, B/C/D call it)

Cues are Timeline markers the builders write with `Cue("verb args")`; `SceneDirector.Run` → `ExperimentCue` handles
these. New in v5:

| Cue | Does | HUD API |
|---|---|---|
| `hero <big>\|<small>` | A centre-screen reveal plate: `big` at ~120 px (Symbols() colours V/I/R/Ω), `small` under it, translucent glass with a cyan/bolt frame, pops in with a glow, stays until `hero.clear` or the next scene. Never blocks input. | `LessonPanel.Hero(big, small)`, `HeroClear()` |
| `hero.clear` | removes it | |
| `readings.top 1\|0` | The live V / I (and R once `readings.r 1`) strip at **top-centre** of the screen, large (value 54 px), glass, instead of the dock readings card. While it is on the dock's Readings block stays hidden. | `LessonPanel.TopReadings(bool)` |
| `stage 1\|0` | **Data stage**: the dock's Table, Graph and Formula cards move to the centre of the screen (one wide glass plate, ~1180 px, scale 1.2) in front of the 3D view, Table left / Graph right / Formula under; the turn card and clue stay in the dock. `stage 0` returns them. `PanelFraming` keeps the dock reservation (no camera jump). | `LessonPanel.Stage(bool)` |
| `rig.slider 1\|0` | **Existing cue, new look**: `VoltageButtons.Create()` now builds a horizontal **battery slider** (0–5 V, snaps to whole volts, ticks + labels 0 … 5 V, the knob a bolt, the fill voltage blue) in a glass strip at **bottom-centre** above the captions (`UI_VoltageStrip`), not in the dock. Same fields (`set/open/current/max`), same `ControlsMax` cap (steps above the cap are dim and say why), tap on the track sets too, keyboard ←/→ steps. | `VoltageButtons.Create()` (class name kept) |
| `fade <alpha> [seconds]` | White fade with an optional length (`fade 1 0.15` = a flash) | `ModuleFlow.FadeWhite(alpha, seconds = -1)` |
| (no cue) | **Schematic on a world board**: `CircuitSchematic.CreateOn(RectTransform area)` builds the live schematic into any RectTransform (a world-space canvas the Scene 9 builder makes); `CircuitSchematic.Create()` (dock) keeps working. | `CircuitSchematic.CreateOn(area)` public static |
| (no cue) | **3D pop-up card**: `LessonPanel.Popup3D(RenderTexture rt, string title, string caption)` shows a centre card (~980 × 560) with the texture, a title row and a caption; `Popup3DClose()`. Area C renders copper/nichrome into the texture. The card forwards pointer drags to `LessonPanel.Popup3DDrag` (Action<Vector2>) so the learner can turn the sample. | `Popup3D(...)`, `Popup3DClose()`, `Popup3DDrag` |
| `quickcheck` | Nothing new in the HUD: area C uses `MCQPanel` (its eyebrow becomes "QUICK CHECK" when the question text starts with `[QC]`; A strips the tag). | `MCQPanel` eyebrow rule |

HUD skin (area A, applies everywhere): cards **translucent** navy glass (`#081A2B` at ~0.72, a 1 px cyan hairline at 45 %,
a soft inner top highlight), the chrome's accent stays Wattson's spark; an **electric motif**: `Icons.Get("bolt")` (a
lightning bolt SDF) on the turn card badge, the chapter chip and the Ask ring; the active turn card's edge carries a slow
"current" (an animated dashed cyan stroke marching along the outline, calm under reduced motion); the caption bar glass
with a small bolt before WATTSON; the title screen's START JOURNEY plate gets the bolt and a gold frame; pause menu and
volume popup follow the same glass. Nothing under 15 px at 1080p, contrast ≥ 4.5:1 for text on the glass over the
brightest frame (test the lab's white bench). V/I/R colours unchanged. Chapter chip, controls, Ask avatar: same places.

Input (area A): `LookAround` on the Main Camera (added by `ModuleFlow.EnterScene` next to `PanelFraming`): a left-drag that
starts on the 3D view (no UI under the pointer, no collider hit on press, so rig parts and the key still get their taps)
yaws the camera up to ±18° and pitches ±8° from the shot, eases back to the shot when released; off under reduced motion
and while the pause menu, title or Ask panel is open. Implemented on `CinemachineCore.CameraUpdatedEvent` after the brain
(like `PanelFraming`), never by moving a virtual camera.

## Narration (the words the areas write)

**Scene 5 (area B)** — replaces N01…N05, W02, W03, L06, L07 (keep L01 unused or delete; keep L08, L09 for the handoff):

| Id | Line |
|---|---|
| S05_V01 | Now you are inside the wire. |
| S05_V02 | These glowing charges are electrons, and every one of them is negative. |
| S05_V03 | Right now they just jiggle about at random. Lots of motion, but no overall flow in any direction. |
| S05_V04 | Let's connect a battery across the wire. This end goes to the negative terminal. |
| S05_V05 | And that end to the positive terminal. |
| S05_V06 | Between the two ends there is now a difference in electric potential. That push across the wire is the voltage, written V. |
| S05_V07 | Watch! Every charge still jiggles, but now they all flow the same way: from the negative end towards the positive end. That steady flow is electron drift. |
| S05_V08 | Now pick one cross-section of the wire, this ring. Count how much charge passes through this area in one second. |
| S05_V09 | This much charge, from negative to positive, through this area, every second. That rate of flow is the current, written I. |
| S05_V10 | Voltage pushes. Current flows. So, detective: how do you think the two are related? |

Beats: V01 on the entry push (see transition), V02–V03 free electrons; V04/V05 terminals light as he points (the terminal
discs toned down: no blown-out white, HDR ≤ 1.2×; labels readable; a wider "Wire Terminals" shot that holds BOTH
terminals fully in the free frame while they are named), V06 bracket + the **apply voltage** gate (tap the +), V07 drift
with the field wave + drift arrow, V08 the ring + counter, V09 current, V10 → Scene 6's question (Scene 6's S06_L02
becomes *"If we increase the voltage across the wire, with everything else the same, what happens to the current?"*).

**Scene 10 (area C)** — new gate `S10_Predict3V` between the 2 V row and the 3 V step: MCQ `[QC]At 3 volts, what will the
ammeter read?` A `0.40 A` · B `0.60 A` · C `0.80 A`; record `LearnerSession.answers.Add("S10_Predict3V <letter> <Correct|Incorrect>")`,
`LearnerSession.Record(Concepts.Relationship, grade)`; Wattson reacts: `S10_Predict3V_R` ("Locked in. Set three volts and
let the ammeter judge.") then after the 3 V row records a say gate `S10_After3V` with `S10_After3V_If_S10_Predict3V_B`
("Spot on. Three volts, zero point six amperes: the pattern holds.") / `_A` ("The ammeter says zero point six, not zero
point four. The current climbed with the voltage.") / `_C` ("Zero point six, not zero point eight: it rises in step, not
faster."). Slider hints: `S10_SetNV_H` become "Slide the battery to N volts."

**Scene 12 (area C)** — after S12_L03 and before S12_N01, with the 3D pop-up:

| Id | Line |
|---|---|
| S12_R01 | Zoom in. Inside any metal the atoms sit in a lattice, and the free electrons have to thread their way through it. |
| S12_R02 | In copper the way is easy. The electrons slip past with few collisions, so the same push gives a big current. |
| S12_R03 | In nichrome the lattice gets in the way far more. The electrons bump, scatter, lose headway. Same push, less current. |
| S12_R04 | Every collision hands a little energy to the lattice as heat. That's why a nichrome wire warms up, and why a heater glows. |
| S12_QC | Quick check, detective. Same voltage, less current. Which wire has the higher resistance? |
| S12_QC_R_If_S12_WhichResists_B | Nichrome it is. More opposition, less current, higher resistance. |
| S12_QC_R_If_S12_WhichResists_A | Careful: copper let more current through, so it opposes less. Nichrome has the higher resistance. |

Gate `S12_WhichResists`: MCQ `[QC]Same voltage. Which wire has the higher resistance?` A Copper · B Nichrome; record as
above under `Concepts.Resistance`.

## Per-area briefs

### A. HUD
1. Skin as above (`Card()`, `Pill()`, controls capsule, caption, chip, banner, pause menu, popup, Ask panel, title plate).
2. `Hero`, `TopReadings`, `Stage`, `Popup3D`, the slider, `CircuitSchematic.CreateOn`, `fade seconds`, `LookAround`, the
   `[QC]` eyebrow, and the cue verbs in `ExperimentCue` (`hero`, `hero.clear`, `readings.top`, `stage`).
3. `ResetForScene` clears hero / top readings / stage / popup / slider strip. `Shutdown()`-safe (a scene left mid-gate).
4. Keep `HUD_DESIGN.md`'s input sizes and keyboard map; update the doc's colour table at the end (short).

### B. Wire
1. Environment: the pillar hall goes. Inside a copper wire: a **copper lattice cavern**: a volume of soft, warm copper
   "atoms" (one static ParticleSystem, ~2500 particles on a slightly jittered hexagonal/cubic grid, 0.35–0.6 m, additive
   soft sprites in copper/amber, fading with the fog) all around and beyond the conduit, denser far away, none inside the
   conduit; a dark glossy floor stays (Wattson's shadow), the grid lines fainter; two warm copper point lights plus the
   cyan terminal glow; fog deep navy-copper; bloom moderate. It must read as "inside the metal" and give depth. Scenes 5
   and 6 share it (`WireStage`); Scene 12's nichrome variant tints grey-green (keep `lattice: true` behaviour).
2. Terminals: HDR of the discs and halos down so they never bloom to white; the "− negative" / "+ positive" labels larger
   (Px.Medium) and fully inside the frame; a new `Vcam("Wire Terminals", …)` that frames both terminals and the whole
   conduit in the free frame (dock 480 reserved); use it for V04–V06.
3. The dive transition: Scene 4 ends with `Flow.Cut()` (no white; `Scene04Director.Complete`), Scene 5 opens on the macro
   wire exactly as Scene 4 left it, pushes in ("Macro A" → "Macro B") with the streaks, then `fade 1 0.15` (a flash),
   cut to a new "Wire Enter" camera 3 m behind "Wire Inside" that **keeps moving** (blend 1.8 s into "Wire Inside") while
   the streak particles keep streaming for 0.6 s and `fade 0 0.5`; `Ambience("WireHum")` from the cut. The learner must
   feel one continuous dive. V01 plays as the push settles.
4. Narration and beats as in the table. Counter and clue cards keep working (`counter 1`, `callout show …`).
5. `CinematicPolish.PolishWire`: delete the chamber build (keep the floor material, fog values that the builder now owns,
   trail widths, probe re-bake). `WireChecks` still pass.

### C. Experiment
1. Scene 9: `WorldSchematic` = a world-space Canvas (≈1.4 × 0.6 m) floating 0.45 m above the back of the bench, tilted
   10° toward the camera, glass backing (`WirePanel` style or `UI.Panel` on the canvas), hosting
   `CircuitSchematic.CreateOn(area)`; shown by the existing `schematic 1` cue (make the scene use the board instead of the
   dock: `Cue("schematic.world 1")` handled in Gates.cs or a new partial of yours, or simply build it active and show/hide
   it with `Show("Schematic Board")`). Wattson's "ask the user to set up the circuit" line stays (S09_L05…). Dock keeps the
   turn card only. `dockWidth` stays as is (turn card needs it).
2. Scene 10: `Cue("readings.top 1")` instead of `readings 1`; `rig.slider 1` (now the bottom strip); the table and graph in
   the dock (graph "on the right": `Cue("graph.axes 1")` from the start so the empty graph is there, points plot as rows
   record); `S10_Predict3V` + `S10_After3V` as above; the "V × 2 → I × 2" lines go to `hero` ("V × 2  →  I × 2" | "double
   the voltage, double the current") for 3 s then `hero.clear`.
3. Scene 11: `S11_DrawLine` stays; the reveal becomes `hero V ∝ I|directly proportional, for this wire at constant
   temperature` and stays through S11_L05; `stage 1` while S11_N01 explains the pattern (table + graph in front), `stage 0`
   before the handoff (Scene 12 opens on the dock).
4. Scene 12: `stage 1` for the V ÷ I column and the slope (S12_L06 … S12_L13), `hero Ω|the ohm, unit of resistance` and
   `hero 1 Ω = 1 V ÷ 1 A|…`; the comparison = the **3D pop-up**: `WireSample` (a MonoBehaviour on a small stage far from the
   lab, e.g. y = −40: two short cut-away wire segments side by side, copper and nichrome, ion spheres in a lattice, 24 cyan
   electrons each drifting with scatter — nichrome scatters hard and drifts slow; a camera with its own culling layer
   rendering to a 960 × 540 RenderTexture; `LessonPanel.Popup3D(rt, "Copper vs nichrome", "same voltage · same length ·
   same thickness")`; drag rotates the stage; `S12_SwapWire` shows copper alone, the swap brings nichrome in). Lines R01–R04
   over it, then the quick check gate, then the existing ratio/slope beats on the stage.
5. Scene 13: `hero V = I × R|` after the law builds, `hero OHM'S LAW|` on the name; the case-closed card as is.
6. `SceneDirector.Gates.cs`: `S10_Predict3V`, `S12_WhichResists` (MCQ handlers like `Prediction`, graded, reaction line),
   `SetVoltage` works with the slider (it already reads `rig.state.voltage`; keep `ControlsMax`).
7. Builders stay match-cut consistent (9→10→11→12→13: same bench, same `Bench Study` camera, `SceneHandoffState`).

### D. Intro and transformation
1. Scene 4 cameras (`BuildCameras` + the shot plan in `BuildTimeline`): replace the held frames with slow moves that stay
   readable: SH01 a crane-down + drift in (two cams blended, 5 s); SH02 a tracking dolly that follows Wattson's flight
   (already two cams: blend them); SH04 a slow lateral dolly past the candle with Ohm in profile instead of the cut
   (`Move("SH04", …)`), foreground parallax; SH05/SH06 the slow 25° orbit around the words (cams exist, unused); SH08 the
   low-angle push (cams exist, unused); SH09 the push to the wire (exists). Keep the tap hold on SH11a. No orbit faster
   than 6°/s, no cut shorter than 1.5 s. Reduced motion still gets the calm version (the `ReducedMotionCamera` noise rule).
2. Ohm: `naturalPerformance = true` in Scene 4 (slower torso/head variation, asymmetric gestures) and one new beat:
   on SH04 Ohm looks up at Wattson (`Attend`) then back to the galvanometer; no new rig, no clips. The "cliché" the user
   means is the static genius-at-desk: give him the glance and the breathing, not more scribbling.
3. Scene 8 transformation: during `look 2.8 classroom` the camera moves (a slow 20° orbit + pull-back around the bench:
   two new vcams blended over the 2.8 s); a visible **time-wave ring** (a torus mesh scaled out from the bench, `Glow`
   material, 1.2 s) and a brief exposure lift at the wave; each instrument swap gets a spark burst (`Sparks(...)` kit) and a
   `Sfx("Transform")` already exists; Ohm's farewell: he turns to the camera (`ohm.look Main Camera`), a small bow
   (`ohm.pose Present` then `Examine`), gold dust; Wattson's `Greet`. Everything through existing cues (`look`, `fx`,
   `vanish`, `appear`, `glide`, `settle`, `ohm.*`), one new cue at most if needed (`SceneDirector.Lab.cs`).
4. Sounds: add to `SfxBank` only what the beats need (`TimeWave`, `Dive` if missing). Never rename a recipe.

## Verification the orchestrator runs afterwards

`compile_check`, `menu Ohm's Law/Build Scene 04`, `all` (Scenes 5–13), `menu Ohm's Law/Polish/Apply wire chamber (Scenes 5, 6)`,
`vo all`, `menu Ohm's Law/Tests/*`, play test `all 2x` at 1920×1080 (+ `hud ask`), frames reviewed per scene, then a
review workflow fixes what the frames show.
