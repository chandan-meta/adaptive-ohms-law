# Final QA report — Ohm's Law module, 2026-10-14

**Scope:** full gameplay dry run → frame-by-frame audit → five implementation work packages → rebuild → full
regression run → comparison. Commits: checkpoint `28c9aa4` (before), code `e53ff6b`, final (scenes + reports) see
`git log`. Labels: Observed / Suggested / Implemented / Tested / Verified are used literally; nothing below is
claimed verified unless it was seen in an after-run frame or measured in the after-run recording.

## 1. Regression runs (Tested)

| Run | Request | Result | Length | Lines | Gates | Errors |
|---|---|---|---|---|---|---|
| Before (`28c9aa4`) | `all record ask fresh @1920x1080` | PASS | 612.5 s | 79 / 79 in order | 16 / 16 | 0 |
| After (final) | `all record ask fresh @1920x1080` | PASS | 616.6 s | 79 / 79 in order | 16 / 16 | 0 |
| After, perf (no recorder) | `Scene09 2x` | PASS | — | — | — | 0 |

Editor self-checks after the rebuild: Ohm Pose 21, Wattson Pose 119, Wattson Actions 144, Lab Continuity 153, Voice
Pipeline 20, Wire Electrons 18, HUD v5 11, Wire Sample 4 → **490 pass, 0 fail**. Title → START → ten chapters → finale
→ CASE CLOSED card (replay / chapters / exit) all reached; Ask Wattson opened, answered and closed in Scene 7; captions
paged with every line; no missing references or console exceptions in either run.

Performance (editor, Scenes 9–13 at 2x, no recorder): frame ms mean 17.5–19.6, p95 ≤ 25.9, batches ≤ 590, triangles
≤ 0.49 M — unchanged from the pre-change baseline (the recorded runs show 42–45 ms in both before and after because of
the recorder's per-frame readback; not a player benchmark).

Audio (after recording): integrated −21.1 LUFS, LRA 8.7 LU, true peak −2.0 dBFS, no silence gap > 2.5 s; identical
loudness to the before run.

## 2. Findings → status

| # | Finding | Status | Evidence (after) |
|---|---|---|---|
| 1 | Lab dim/cold (polished exposure 0.2) | **Verified** — exposure 0.45 in the polished profile; lab lit, green board, key shaft and motes visible | `after/Shots/Scene09_BuildCircuit/01_enter.png`, `Before_After/compare_S08_13_*.jpg`, `compare_S09_01_enter.jpg` |
| 2 | Play pitch reset (slider tones) | **Implemented, Tested** (no errors; pooled sources) — the per-volt pitch could not be isolated in the mixed recording (voice and pad mask the blip), so **not verified by ear** | `Implementation_Log.md` WP-A |
| 3 | No title music | **Implemented, Tested** — the title bed is a Music-kind bus at gain 0.35 while the title is open; the title-hold level in the recording is identical to before (−21.5 dB mean over 2–9 s), so the bed is not audibly louder than the old Timeline pad that was already playing under the title; **listening sign-off pending** | `after/Gameplay_Full_Run.mp4` 0:00–0:10 |
| 4 | Static camera Scenes 9–13 | **Verified** — Scene 9 dolly-ins at L05 and L07 return to Bench Study before each gate; Scene 10 meter close-up with 0.20 A / 1.0 V at 05m26–05m28; Scene 13 finale push tighter on Ohm | `after/Beat_Sheets/Scene09_BuildCircuit.jpg` (05, 08), `after/Captures/Scene10_Experiment_sheet.jpg`, `after/Shots/Scene13_OhmsLaw/08_S13_N01.png` |
| 5 | Wire hall: strips dominate, conduit small | **Verified** — strips calm, backdrop dim, conduit spans the frame with cyan haloed electrons and copper ions, Wattson ≈190 px on the platform; − / + labels inside the frame; Scene 6 two-shot shows the freeze, gauge visible | `after/Shots/Scene05_InsideTheWire/02_S05_V01.png`, `Before_After/compare_S05_10_S05_V07.jpg`, `after/Beat_Sheets/Scene06_AdaptiveQuestion.jpg` |
| 6 | HUD monochrome vs kids reference | **Verified** — Fredoka gold title with cyan rule + bolt, amber START JOURNEY, bracketed cards with cyan edges, Wattson's face on the caption bar, gold CASE CLOSED stamp, amber Replay button | `after/Shots/Scene04_MeetWattson/02_title_screen.png`, `after/Shots/Scene10_Experiment/07_gate_S10_Predict3V.png`, `after/Shots/Scene13_OhmsLaw/09_end_card.png` |
| 6b | Graded quick-check mark (Correct / Not quite) | **Verified** — at 08m22 the chosen Nichrome row is green with the "Correct!" chip and the turn card reads DONE · Correct | `UI_Captures/after_S12_quickcheck_correct_08m22.jpg`, `after_S12_quickcheck_1fps_sheet.jpg` |
| 7 | Cold cut study → lab; ring one frame | **Verified** (reveal lands lit) / **Implemented** (ring hold +0.6 s; two frames expected, checked on the 1 fps sheet at 04m04–04m05) | `after/Captures/Scene08_OurLab_sheet.jpg` |
| 8 | One pad for six minutes; no circuit hum | **Implemented, Tested** — PadLabTheme from the Scene 8 reveal, CircuitHum after the key closes; both present in the rebuilt Timelines (`Docs/FLOW.md`); listening sign-off pending | `Docs/FLOW.md` Scene08/09 |
| 9 | Niscope white blob | **Verified** — SH08 shows the gadget inside its rings | `after/Shots/Scene04_MeetWattson/08_S04_L05.png` |
| 10 | Finale dark | **Verified** — warm finale look, Ohm and Wattson lit, gold sparks, push-in | `after/Shots/Scene13_OhmsLaw/08_S13_N01.png`, `Before_After/compare_S13_09_end_card.jpg` |
| 11 | Free-play V/R → I | **Not done** (P3, deferred) | — |
| 12 | Wattson under the table (reported) | **Not reproduced** in either run at 1 fps (furniture clearance covers the round table's renderers) | `before/after Captures/*_sheet.jpg` |

## 3. Student-experience pass (Observed in the after run)
- Objective always visible: turn cards ("YOUR TURN · Set the battery to 2 V", "Build the law"), the chapter chip and
  rail, Wattson's line in the caption bar.
- Interaction intuitive: tap-hand hints on parts and slots, the slider with dimmed future steps, numbered answers,
  keyboard equivalents (1/2/3, Enter, Space).
- Feedback: chime + reaction line per gate; the Scene 12 quick check is judged (green row + "Correct!" chip, Verified at 08m22 in
  `UI_Captures/after_S12_quickcheck_correct_08m22.jpg`); the Scene 10 wager is deliberately judged by the
  ammeter ("Spot on. Three volts, zero point six amperes") and stays unmarked; the Scene 6 prediction is never marked
  (by design, the data decides).
- Pacing: 10:17 hands-free; no explanation repeats; the only long chapter (Resistance, 2:17) is the densest.
- Science: unchanged and correct (drift − → +, conventional + → −, R = V/I = 5 Ω, Ω defined, temperature constant).

## 4. Scores — AFTER (out of 10; before in brackets)

| Scene | Visual | Environment | Lighting | Materials | Camera | Char anim | VFX | Sound | UI/UX | Flow | Engagement | Accuracy | Perf | Overall |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 04 | 8 (8) | 8 | 8 | 7 | 8 | 7 | 8 (7) | 7 (6) | 8 (7) | 8 | 8 | 9 | 8 | 7.9 (7.7) |
| 05 | 8 (5) | 7 (6) | 7 (5) | 7 (6) | 7 (6) | 7 (6) | 8 (6) | 7 (6) | 8 (7) | 7 | 8 (6) | 9 | 8 | 7.6 (6.4) |
| 06 | 7 (6) | 7 (6) | 7 (6) | 7 (6) | 7 (6) | 7 (6) | 7 (6) | 7 (6) | 8 (7) | 8 | 7 | 9 | 8 | 7.4 (6.7) |
| 07 | 8 | 8 | 8 | 7 | 8 | 7 | 6 | 7 (6) | 8 (7) | 8 | 7 | 9 | 8 | 7.6 (7.5) |
| 08 | 7 (6) | 7 (5) | 7 (4) | 6 | 7 | 7 | 7 (6) | 7 (6) | 8 (7) | 7 | 7 | 9 | 8 | 7.2 (6.5) |
| 09 | 7 (5) | 7 (5) | 7 (4) | 6 | 7 (4) | 6 | 6 | 7 (6) | 8 (7) | 8 | 7 | 9 | 9 | 7.2 (6.3) |
| 10 | 7 (6) | 7 (5) | 7 (4) | 6 | 7 (4) | 6 | 6 | 7 (6) | 8 | 8 | 8 | 10 | 9 | 7.4 (6.6) |
| 11 | 7 (6) | 7 (5) | 7 (4) | 6 | 5 | 6 | 6 | 7 (6) | 8 | 8 | 7 | 10 | 9 | 7.2 (6.6) |
| 12 | 7 | 7 (5) | 7 (4) | 6 | 6 | 6 | 7 | 7 (6) | 8 | 8 | 8 | 10 | 8 | 7.3 (6.8) |
| 13 | 7 (6) | 7 (5) | 8 (4) | 6 | 7 (5) | 6 | 7 (6) | 7 | 8 | 8 | 8 (7) | 10 | 9 | 7.6 (6.7) |
| **Module** | 7.3 (6.3) | 7.2 (5.8) | 7.3 (5.1) | 6.4 (6.2) | 6.9 (5.9) | 6.5 (6.3) | 6.8 (6.2) | 7.0 (6.1) | 8.0 (7.4) | 7.8 | 7.5 (7.2) | 9.4 | 8.4 | **7.4 (6.8)** |

The 9/10 target is not met and is not claimed. Honest ceiling of this pass: materials (6.4) and character animation
(6.5) were not touched; the lab's walls are still a flat navy; camera work in 11–12 is still mostly static behind the
data stage; sound is synthesized throughout.

## 5. Remaining limitations (Observed / not done)
1. **Listening sign-off**: no human has listened to the after mix (title bed, lab theme, circuit hum, per-volt tones).
   The per-volt pitch fix is implemented and runs without errors but was not isolated in the recording.
2. **Materials / textures** untouched (lab walls flat navy; wire-set machines keep their Tripo textures; the pop-up
   lattice reads as blobs at 1080p).
3. **Camera** in Scenes 11–12 still mostly static behind the data stage; Scene 7's hero is a push, nothing else moves.
4. **Device pass** (mouse/touch by hand, tablet, WebGL, player build) not done, as before.
5. **Free-play** V/R → I after the law (P3) not built.
6. **Figma** not used (connector needs the user's OAuth); the UI was implemented Unity-first from the reference sheet.
7. **Voice licence** (edge-tts) and live Ask Wattson keys: unchanged open items from earlier reports.
8. **Reported Wattson-under-table clip** not reproduced; if it recurs, note the scene and beat.

## 6. Deliverables
- Videos: `Deliverables/ohms_law_BEFORE_2026-10-14_720p.mp4`, `Deliverables/ohms_law_AFTER_2026-10-14_720p.mp4`
  (+ `ohms_law_AFTER_part0..2.mp4`), raw 1080p30 runs in `Logs/Blockout/recording` (after) — the before raw was
  superseded, its 720p mux is kept.
- Audit: `Visual_Audit.md`, `Gameplay_Flow_Report.md`, `Frame_Audit.csv`, `Improvement_Plan.md`,
  `Implementation_Log.md`, this report; `Bugs/`, `Before_After/compare_*.jpg`, `UI_Captures/`.
- Per-run evidence: `before/` and `after/` (video, `Shots/`, `Captures/<Scene>_sheet.jpg`, `Cinematic_Frames/`,
  `Beat_Sheets/`, `play-report.txt`, `Frame_Index.csv`, `scene_times.txt`).
- Changed Unity assets: Scenes 04–13, their Timelines, `Shared/Art/Cinematic/*_Polished.asset`, LabSet/WireSet
  materials, new `Glow_LabMote`, `Glow_LabShaft`, `Glow_WireCarrierHalo` materials, `Meshes/LabShaft.mesh`,
  `Scene04/Audio/SFX/{PadIntro,PadLabTheme,CircuitHum}.wav`, `Shared/Resources/Fredoka SDF.asset`,
  `Shared/UI/Fonts/Fredoka-*.ttf` (OFL); scripts listed in `Implementation_Log.md`.
- Tooling: `Tools/audit_frames.py` (run → evidence), `Tools/before_after.py` (pairs).
