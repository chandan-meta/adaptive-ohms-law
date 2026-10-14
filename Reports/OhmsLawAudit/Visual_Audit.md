# Visual audit — Ohm's Law module, dry run of 2026-10-14 (BEFORE)

Run: commit `28c9aa4`, editor play test `all record ask fresh @1920x1080`, 1080p30, 612 s, RESULT PASS, every line of
every scene spoken in order, all 16 gates taken, no console errors. Evidence in `before/`: `Gameplay_Full_Run.mp4`,
`Shots/<Scene>/` (one PNG per beat), `Captures/<Scene>_sheet.jpg` (1 fps), `Cinematic_Frames/sheet_t*.jpg` (10 fps round
each scene change). Scene starts in the video: 04 0:00 · 05 0:56 · 06 2:29 · 07 2:56 · 08 3:37 · 09 4:10 · 10 5:13 ·
11 6:26 · 12 7:16 · 13 9:33. Labels: **Observed** = seen in these frames; **Suggested** = the fix proposed; status is
tracked in `Frame_Audit.csv` and `Implementation_Log.md`.

## Scene-by-scene

### Scene 4 — Meet Wattson (0:00–0:56)
- **Composition/environment (Observed):** the 1826 study is the module's best set: fireplace, oil lamp, globe, map,
  warm wood; the title holds over a crane shot with spark motes; Wattson's entry, the over-shoulder on Ohm, the
  VOLTAGE / CURRENT orbit and the "?" all read. Focal points are clear in every frame.
- **Lighting (Observed):** warm key from the lamp/fire with cool window fill, god-ray mesh, baked probe; exposure 0.25,
  bloom 0.3 after polish. One failure: the Ohm-niscope hero (SH08, 0:38–0:41, `Shots/Scene04_MeetWattson/08_S04_L05.png`)
  blooms to a white blob over Wattson's face.
- **Materials (Observed):** PBR wood/brass/paper fine; Wattson's cloth/felt good; Ohm's skin slightly waxy in close-up.
- **Camera (Observed):** eleven shots in 51 s, long blends, nothing static; the tap gate frames the wire with a
  reticle; the dive (0:54–0:58, `Cinematic_Frames/sheet_t0053.jpg`) is continuous: streaks → flash → hall.
- **Suggested:** niscope flare peak down; title music (none plays); otherwise keep.

### Scene 5 — Inside the wire (0:56–2:29)
- **Observed:** the user's Blender hall (Tripo machines as − / + terminals, wall panels, cove arcs, yellow floor strips)
  is atmospheric, but the yellow strips at emission ×2 plus bloom own every frame (`Bugs/03_*.png`); the Earth
  backdrop "SMALL CHARGES BIG IDEAS" competes with the conduit; the conduit is a thin glass tube with small white
  "−" discs, so the teaching subject is the least visible object; Wattson stands on the far-left floor at ~75 px.
  The − / + labels, the drift arrow, the counting ring and the clue cards are legible. Nothing says "copper".
- **Lighting:** cool blue key, haze 0.012, bloom 0.75 (electron profile) — the bloom is what makes the strips bleed.
- **Camera:** Wire Inside (wide) → Wire Terminals → Wire Drift (closer, good) → back; 93 s in three framings.
- **Suggested:** strips ×0.45, backdrop ×0.35, copper tint on the fills + haze, lattice ions inside the conduit,
  larger cyan carriers with halo, closer "Wire Inside", Wattson on the platform beside the − end.

### Scene 6 — Your prediction (2:29–2:56)
- **Observed:** exact match cut from Scene 5; the frozen electrons and the "V" gauge on the + terminal are tiny; the
  two-shot "Wire Wattson" (`02_S06_L01.png`) is the only frame in which the gadget and Wattson are big; the prediction
  card is clear and deliberately unmarked. Exit whiteout 0.6 s (`sheet_t0173.jpg`) is clean.
- **Suggested:** gauge ×1.6 and clear of the label; show the freeze in the two-shot.

### Scene 7 — Back to Georg Ohm (2:56–3:37)
- **Observed:** macro wire → study wide → slate ("Voltage → V, Current → I", the question) → hero push on Ohm's
  hands and galvanometer (`08_S07_L05.png`, excellent) → wide. Ask Wattson probe opens/answers/closes cleanly over the
  wide. Warm and legible throughout; the slate reads as a dark LED board rather than chalk (minor).

### Scene 8 — From Ohm's lab to our lab (3:37–4:10)
- **Observed:** instrument swaps with cyan sparks, the round table grows in the study under a white spot (odd but
  transitional), Ohm presents, bows, dissolves to gold dust; the time-wave ring is visible for ~1 frame
  (`Captures/Scene08_OurLab_sheet.jpg` 04m04); then the lab appears: **dim, cold navy, flat dark-green board, no
  warmth** (`Bugs/01_lab_dim_cold_S08.png`). The reveal reads as a downgrade from the study.
- **Root cause (code):** `Classroom_Global_Polished` keeps exposure 0.2 (stale copy; the builder intends 0.45).
- **Suggested:** exposure fix, warm key + shaft, coves up, board wash/emission, rack glow, dust, floor reflection.

### Scene 9 — Build the circuit (4:10–5:13)
- **Observed:** one static "Bench Study" frame for 63 s (`Captures/Scene09_BuildCircuit_sheet.jpg`); the glass
  schematic board over the bench is a strong teaching device and lights A → V → loop correctly; the tap-hand hints, the
  X→Y pulse and the clue cards work; the meters are small in the wide; the lab is dark (`Bugs/02_*.png`).
- **Suggested:** push-ins during the ammeter and voltmeter explanations, back to the gate framing before each gate.

### Scene 10 — Become the experimenter (5:13–6:26)
- **Observed:** battery slider bottom-centre, V/I strip top-centre with "was" deltas, 3D meter plates, table and
  graph filling on the right, the 3 V wager and the verdict, the "V × 2 → I × 2" hero: functionally complete and
  readable (`UI_Captures/before_Scene10_*.png`). Still one static camera; the first reading is never seen close.
- **Science (Observed):** 1 V → 0.20 A … 5 V → 1.00 A, R = 5 Ω; correct and consistent across meters, strip, table, graph.
- **Suggested:** meter close-up as the first reading lands; per-volt slider tone (pitch bug).

### Scene 11 — The pattern (6:26–7:16)
- **Observed:** tap-the-graph gate, the line sweeps through the origin, the data stage brings table + graph centre,
  V ∝ I hero plate; clear. Static camera; the stage hides Wattson's lower half (known).

### Scene 12 — Resistance (7:16–9:33)
- **Observed:** the 3D copper-vs-nichrome pop-up (lattice balls, electrons, "same voltage · same length · same
  thickness"), the swap gate, the quick check, the V/I column working out to 5, the slope triangle, the Ω and
  "1 Ω = 1 V ÷ 1 A" plates, R joining the top strip: the richest chapter, accurate. The pop-up's lattice reads as
  orange blobs; the lab stays dark behind it.

### Scene 13 — Ohm's law (9:33–10:12)
- **Observed:** V/I/R recap tiles, the build-the-law gate, "V = I × R", OHM'S LAW; Ohm reappears behind the bench in
  the dark lab (`Bugs/06_finale_dark_S13.png`) with gold sparks; the CASE CLOSED card with replay / chapters / exit.
  "The lab warms" (the storyboard beat) is not visible.
- **Suggested:** `finale` look + slow glide on Ohm's return.

## HUD (all scenes)
Observed: consistent navy glass; captions 32 px, two lines, never clipped; controls, chapter chip and rail, Ask
Wattson, dock cards, hero plates and the end card all present and legible at 1080p; contrast fine. Monochrome and
corporate next to the kids reference (`Docs/Polish/KIDS_UI/reference.png`): no colour accents beyond V/I/R, Paper
primary buttons, no success/error feedback on the graded quick checks, a single sans weight for everything.

## Audio (recording analysis)
Integrated −21.1 LUFS, LRA 8.7 LU, true peak −1.7 dBFS, no silence gap > 2.5 s; voice ducks music to 30 %. No music on
the title screen; one synth pad for Scenes 7–13; `SceneDirector.Play` resets pitch in the same call so the per-volt
slider tones and per-row chimes all sound the same pitch.

## Performance (editor, while recording at 1080p)
Frame ms mean 27–31 (Scene 4: 45 under the recorder's per-frame readback); batches ≤ 572; triangles ≤ 2.0 M (wire
hall). Not a player benchmark; no scene exceeds the budget the previous reports set (≤ 1.2 M tris in the lab).

## Scores — BEFORE (out of 10)

| Scene | Visual | Environment | Lighting | Materials | Camera | Character anim | VFX | Sound | UI/UX | Flow | Engagement | Accuracy | Perf | Overall |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 04 Meet Wattson | 8 | 8 | 8 | 7 | 8 | 7 | 7 | 6 | 7 | 8 | 8 | 9 | 8 | 7.7 |
| 05 Inside the wire | 5 | 6 | 5 | 6 | 6 | 6 | 6 | 6 | 7 | 7 | 6 | 9 | 8 | 6.4 |
| 06 Prediction | 6 | 6 | 6 | 6 | 6 | 6 | 6 | 6 | 7 | 8 | 7 | 9 | 8 | 6.7 |
| 07 Back to Ohm | 8 | 8 | 8 | 7 | 8 | 7 | 6 | 6 | 7 | 8 | 7 | 9 | 8 | 7.5 |
| 08 Lab transform | 6 | 5 | 4 | 6 | 7 | 7 | 6 | 6 | 7 | 7 | 7 | 9 | 8 | 6.5 |
| 09 Build circuit | 5 | 5 | 4 | 6 | 4 | 6 | 6 | 6 | 7 | 8 | 7 | 9 | 9 | 6.3 |
| 10 Experiment | 6 | 5 | 4 | 6 | 4 | 6 | 6 | 6 | 8 | 8 | 8 | 10 | 9 | 6.6 |
| 11 Pattern | 6 | 5 | 4 | 6 | 5 | 6 | 6 | 6 | 8 | 8 | 7 | 10 | 9 | 6.6 |
| 12 Resistance | 7 | 5 | 4 | 6 | 6 | 6 | 7 | 6 | 8 | 8 | 8 | 10 | 8 | 6.8 |
| 13 Ohm's law | 6 | 5 | 4 | 6 | 5 | 6 | 6 | 7 | 8 | 8 | 7 | 10 | 9 | 6.7 |
| **Module** | 6.3 | 5.8 | 5.1 | 6.2 | 5.9 | 6.3 | 6.2 | 6.1 | 7.4 | 7.8 | 7.2 | 9.4 | 8.4 | **6.8** |
