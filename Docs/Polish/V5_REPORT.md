# v5 "Live Wire" — report (2026-10-10)

The brief: *"The module is super boring. It needs to change a lot."* This pass answers it item by item. The contract the
build followed is `Docs/Polish/V5_PLAN.md`; the review findings are `Logs/v5_reviews.txt`, the fixes `Logs/v5_fixes.txt`.

## What changed, by the brief's own order

| The brief asked | What the module does now | Where |
|---|---|---|
| Change Wattson's voice and its modulation | Microsoft neural **Ryan** (en-GB, a lively detective) replaces Ana. Every line gets its own rate and pitch from its shape: questions lift, exclamations and short beats quicken, long explanations slow down. Every line re-baked, Ask Wattson's saved answers included. Five candidate voices were sent to compare; switching is one constant. | `Editor/BakeBlockoutVO.cs` (`EdgeVoice`), `Tools/edge_bake.py` |
| The UI looks bad: more translucent, related to electricity | Every card is navy **glass** (72 %) with a cyan hairline and a top shine, an inset ink plate keeps text readable over bright frames. The electric motif: a **lightning-bolt** icon on the turn card, chapter chip, Ask avatar, hero plates and START JOURNEY; the waiting turn card has a **current marching along its edge**. Controls read PAUSE · REPLAY · CAPTIONS · VOLUME · SKIP. | `Shared/LessonPanel.cs`, `HudKit.cs`, `HudScreens.cs` |
| The intro's camera moves are not attractive; Ohm's animation is a cliché | Scene 4's held frames became slow moves: a crane-down arrival, a tracking dolly on Wattson's flight, a lateral dolly past the candle to Ohm (no cut, Ohm in focus), a 25° orbit round VOLTAGE / CURRENT, a low push on the Ohm-niscope, a push to the wire. Ohm breathes and shifts (natural performance) and **looks up at Wattson** when he is named, straightening from the desk, then goes back to work. | `Editor/Scene04Builder*.cs`, `Scene04/Scripts/OhmWorker.cs` |
| Click the wire, then a smooth transition inside | The tap on the wire is now one continuous dive: Scene 4 cuts (no white) to Scene 5 on the same macro frame, the push into the copper with warm streaks, a 0.15 s flash, and the camera **keeps moving** for 1.8 s inside the wire while the streaks pass the lens. | `Scene04Director.Complete`, `Editor/Scene05Builder.cs`, cue `wire.dive` |
| The environment inside the wire is not good | The neon pillar hall is gone. Inside the wire is now a **copper lattice**: ~2700 warm atoms on a regular grid in every direction, rows converging into a copper-brown haze, a black glass floor that mirrors the conduit and the terminals. | `Editor/SceneBuilderBase.Wire.cs`, `Shared/WireInterior.cs` |
| The − and + terminals are too bright and not visible | The discs and halos are toned down (no bloom to white), the "− negative" / "+ positive" labels are bigger, and a new **Wire Terminals** shot holds both terminals and the whole wire in frame while Wattson names them. | `SceneBuilderBase.Wire.cs` |
| The voltage and current explanation is boring: start with "now you are inside the wire…" | New narration, in the brief's order: *Now you are inside the wire → the charges are electrons, all negative → random jiggle, no flow → connect a battery: this end − … that end + → the difference in potential is the voltage, V → (you tap +) everything drifts one way, − to +: electron drift → pick this ring: this much charge from − to + through this area every second is the current, I → voltage pushes, current flows: how are they related?* | `Editor/Scene05Builder.cs` (S05_V01–V10) |
| Ask how they are related, on screen | Scene 6's prediction card now uses the spoken words ("If we increase the voltage across the wire…"). Wattson freezes the electrons in a close two-shot first, so the magic is seen. | `Editor/Scene06Builder.cs` |
| Back to the lab, old instruments become new, goodbye to Ohm, the room change much more visual | Scene 7's method shot pushes in slowly instead of holding. Scene 8: each swap gets a spark burst on the instrument; Ohm turns to the class and **bows**, then dissolves into gold dust while Wattson waves facing the learner; then Wattson **casts the change**: a glowing **time-wave ring** stands up over the bench and expands through the walls, sparks fly in its plane, and the camera **sweeps 20° round the bench** while the study becomes the classroom. | `Editor/Scene07Builder.cs`, `Editor/Scene08Builder.cs` |
| Show the circuit in front, above the bench, and ask the learner to set it up | A **glass schematic board** floats over the back of the bench, in front of the camera, and lights up as the ammeter (series), the voltmeter (parallel) and the key go in. | `Shared/WorldSchematic.cs`, `Editor/Scene09Builder.cs` |
| A slider to set the battery to 1 V | A **battery slider** at the bottom of the frame (0–5 V, snaps to whole volts, drag or tap, ←/→ keys); steps above the current one are dim and say why. | `Shared/PanelViews.cs` (`VoltageButtons`) |
| Readings above, in front of the learner; the graph on the right | The live **V / I strip** sits top-centre in front of the scene (R joins once it is taught); the table and the graph fill in on the right as each row records. | `LiveReadingsPanel.cs`, `Scene10Builder.cs` |
| After 2 V, a prediction for 3 V, then the rest | A **quick check** after the 2 V row: *At 3 volts, what will the ammeter read?* (0.40 / 0.60 / 0.80 A). After the 3 V row Wattson gives the ammeter's verdict on the learner's pick. | gate `S10_Predict3V`, say gate `S10_After3V` |
| V ∝ I in the centre of the screen | Centre-screen **hero plates**: "V × 2 → I × 2", then **V ∝ I**, later Ω, 1 Ω = 1 V ÷ 1 A, V = I × R and OHM'S LAW. | cue `hero`, `LessonPanel.Hero` |
| Bring the table and graph in front and explain V / I | The **data stage**: the table, graph and formula move to the centre of the screen for the pattern (Scene 11) and for the V ÷ I column and the slope (Scene 12), then go back to the dock. | cue `stage`, `LessonPanel.Stage` |
| Copper vs nichrome looks 2D: 3D models in a small centre pop-up with the charges, longer resistance explanation | A **3D pop-up**: cut-away copper and nichrome wires with their lattice ions and cyan electrons drifting through them, rendered live; the learner can **drag to turn** them. Copper first; the learner swaps nichrome in; nichrome's electrons scatter and slow down, and its lattice **heats up red**. Four new lines explain the lattice, collisions and heat. | `Shared/WireSample.cs`, `Editor/SceneBuilderBase.Sample.cs`, S12_R01–R04 |
| Adaptive questions in between without breaking immersion | Two graded quick checks inside the dock while the 3D keeps running (the 3 V wager; *which wire has the higher resistance?*), plus the Scene 6 prediction and the Scene 13 build-the-law tiles. Answers feed `LearnerSession` (concepts, misconceptions), which Ask Wattson reads. | `SceneDirector.Gates.cs` (`QuickCheck`) |
| Use of AI | Ask Wattson (the avatar, bottom-right) is in every chapter, knows the chapter, the current step and the learner's answers. It answers from saved notes until the proxy has its API keys. | `Shared/Agent/*`, `Server/wattson-proxy` |
| The user should be able to move and look | **Look around**: drag on the 3D view to turn the camera up to 18° and tilt 8°; it eases back on release. Taps on parts, the key and the terminals still work. | `Shared/LookAround.cs` |
| Visuals, sound effects, the character doing magic | Wattson's gadget casts the freeze, the transformation and the 3D sample; sparks on every swap; the time-wave with its sweep sound; the existing synthesised bank (clicks, chimes, rises, zaps) on every action. | builders, `Editor/SfxBank.cs` |

## How it was checked

- **Build round (before the fixes):** whole-project compile clean; Scene 4 and Scenes 5–13 rebuilt from the builders; every
  line re-baked in the new voice (234 clips, 0 failed); hands-free play test of the whole module at 1920×1080 with the HUD
  and Ask Wattson probes: **RESULT PASS** (every line spoken in order, every gate taken, end card reached, no console errors).
  Self-checks: Ohm Pose, Wattson Pose, Wattson Actions, Lab Continuity, Voice Pipeline all pass; Wire Electrons failed one
  check (the cavern count read 0 in edit mode), fixed and re-run: all pass (2679 atoms, none inside the wire or over the lens).
- **Review round:** ten reviewers, one per chapter, scored the frames against this brief at 3–3.5 / 5 and filed 76 findings
  (`Logs/v5_reviews.txt`); four fixers applied them (`Logs/v5_fixes.txt`), the whole project compiles clean after them.
- **Not yet verified:** the fixed build has not been replayed. The editor was closed after it rebuilt Scene 4, so Scenes
  5–13 in the repository still come from the build before the review round, and S05_V07's new wording has no clip yet.
  One run of `bash Tools/v5_verify.sh` with the Ohm's Law editor open rebuilds them, re-bakes that line, re-applies the
  polish, runs every self-check and replays the module.

## Not done, or limited

- **Live AI answers** need `ANTHROPIC_API_KEY`, `ELEVENLABS_API_KEY`, `OPENAI_API_KEY` in `Server/wattson-proxy/.env` and the proxy running. Until then Ask Wattson answers from each chapter's saved notes.
- **Voice licence**: edge-tts is an unofficial client of Microsoft's read-aloud service. For a shipped product re-bake through the proxy (ElevenLabs) or record an actor.
- **Look around** turns the view only for rendering; the tap-hint hand lags while a drag is held.
- **No new music** was composed; the existing pads and the module theme play.
- Scenes 5–13 were rebuilt from the builders, so the editor hand edits of 2026-10-06 are gone (their important parts were already in code).
