# Implementation log — 2026-10-14

Checkpoint before any change: commit `28c9aa4`. Every entry says what was **Implemented** (code), and the
`Final_QA_Report.md` says what was then **Tested** (built, played) and **Verified** (seen in the after-run frames).

## WP-L — lab look (Scenes 8–13), Scene 4 flare — Implemented
- `Editor/CinematicPolish.cs` 74–91: the polished global profile re-reads `postExposure` from the builder's source
  profile on every Apply (deltas for bloom/grain/vignette/contrast unchanged). Classroom 0.2 → 0.45; Study 0.25,
  Electron 0.1, Finale 0.6 follow their builders. (Root cause of finding 1.)
- `Editor/SceneBuilderBase.Look.cs` 321–344: new `ShaftCone()` (four additive fins, bright apex, transparent base);
  376–399 `GlobalPost` returns its Volume.
- `Editor/SceneBuilderBase.Classroom.cs` 432–556 `LabSetLighting`: ambient sky .46/.56/.72 → .50/.58/.72, equator
  .33/.37/.45 → .46/.42/.38, ground .11/.12/.14 → .16/.15/.14; background .03/.05/.08 → .05/.08/.12; Lab Key
  (.93,.96,1) 2.6 → (1,.93,.8) 3.2; Cove 1–6 0.9 → 1.6; Board Wash (.95,.98,1) 1.8 → (1,.96,.9) 2.6; Display Glow
  1.2 → 1.8; Face Fill (1,.95,.88) 1.0 → (1,.9,.78) 1.5; Lab Rim unchanged. New "Finale Cove 1–6" gold points (off
  until the look), "Lab Shaft" cone (apex (0, 2.08, 0), 1.25 m, r 1.0, alpha 0.05) and "Lab Motes" (7/s, max 80,
  calm 0.35 under reduced motion) under "Classroom Ceiling" so Scene 8 reveals them with the ceiling; `LabLooks()`
  installs looks `classroom` + `finale` (finale: key ×1.25, blue coves ×0.6, gold coves 1.4, Face Fill ×1.3, Board
  Wash ×1.15, Display Glow ×1.2, warmer ambient/background, own `Finale_Global` volume: exposure 0.6, filter
  (1, .95, .86)).
- `Editor/BlenderSet.cs` 34–55, 101, 114: Rack_And_Frame glow 1.6 → 2.4; board "Material" emission (0.02, 0.06,
  0.035); per-material smoothness override, Round_Floor 0.5 → 0.7. Mural left at 0.9 (not in any bench shot).
- `Editor/Scene04Builder.cs` 616–617: NiscopeLens HDR 1.2 → 0.7, flare peak 2.5 → 1.6.
- Compile: clean (Editor + Runtime).
- Verify in the after run: Scene 9 entry no longer near-black; warm pool + faint column + motes over the table;
  blue coves against the warm key; board a lit green slab; table ring mirrored; Scene 8 reveal lands warm; Scene 4
  SH08 shows the gadget inside its rings. Risk: table top going pale under the 3.2 key / finale exposure 0.6.

## WP-U — HUD restyle to the kids reference — Implemented
- Files: `Shared/LessonPanel.cs` (tokens 63–96, Card/Band/Brackets 171–235, Pill 446–500, hint/turn/hero/end,
  PanelSeconds/ButtonSeconds 46), `Shared/HudKit.cs` 115–195 (HudButton), 210 (CurrentEdge), `Shared/HudScreens.cs`
  91, 129–133, 258, 328/426, 589–627 (title + Rule), `Shared/SubtitleUI.cs`, `Shared/MCQPanel.cs` 9–50, 127–200,
  217–240 (+ChipFade), `Shared/UI.cs` 86–109 (DisplayFont/Display), `Shared/LiveReadingsPanel.cs` 73–74,
  `Shared/PanelViews.cs` 584, `Editor/BlockoutAssets.cs` 20, 33, 39–66 (Fredoka SDF creation), new
  `Shared/UI/Fonts/Fredoka-SemiBold.ttf` / `Fredoka-Bold.ttf` + `Fredoka-OFL.txt` (SIL OFL 1.1, static instances
  made from Google's variable Fredoka with fontTools).
- Tokens before → after: Paper #F7F5EF → #F4FAFF; Muted #A9B6CC → #B9CBE3; Raised #141D30 → #2E3B55; Hover #1D2944 →
  #3A4A68; Glass #081A2B 72 % → #1A2B45 rim 80 % over a Deep #0B1626 plate (plate 75 % kept); card line Spark 45 %
  1 px → Accent #00D1FF 55 % 1.5 px; control line white 38 % → Accent 45 %; focus ring Spark → Glow #7EE9FF; Primary
  Paper/ink → Amber #FFB347/ink (hover Gold #FFD166, 6 px amber glow band + gloss); Secondary white 10 % → navy 92 %
  + cyan edge; chosen MCQ row Paper → Accent; Right/Wrong marks → Success #2ECC71 / Error #FF6B6B; title #FFC46B →
  Gold; stamp Paper → Gold. Ink, Body, V/I/R and Spark unchanged. Cards: two-tone + two outline bands (8 px @ 7 %,
  4 px @ 10 %) as a no-blur glow; corner brackets on the chapter banner, quick checks, end card and title block.
- Motion: panels 0.28 → 0.22 s, UIFade 0.25 → 0.22 in / 0.18 out, buttons ≈60 → 140 ms; reduced motion = fades only.
- Fredoka SemiBold (Nunito fallback) for: "OHM'S LAW", chapter titles (banner + pause menu), hero plate big text,
  readings values, CASE CLOSED, end-card and law-tile "V = I × R". All sizes unchanged.
- MCQ grading: `Gates.QuickCheck` already called `panel.Mark` for judged checks (S12_WhichResists) but HudButton
  re-lerped the fill every frame so the mark never showed; fixed, plus the "Correct!" / "Not quite" chip (1.5 s) and
  the shake when wrong. S10_Predict3V is `judge: false` by design (the ammeter settles the wager) and stays unmarked;
  an optional `correctIndex` parameter exists for later. Scene 6 prediction and the law tiles unchanged.
- Compile: clean. Caveat: the Fredoka SDF asset is created by `BlockoutAssets.Palette()` during the Scenes 05–13
  build, so it must exist before the play test (build order below); Figma not used (no OAuth).

## WP-C — camera and beats (Scenes 8–13) — Implemented
- `Shared/SceneDirector.Lab.cs` (header, LabCue ≈62–69, Move ≈124): new cue verb `push [s] <name>` = glide to
  "<name> Goal" that becomes a cut when `ReducedMotion.On` (the old `glide` only dropped its lift under reduced motion).
- `Editor/Scene09Builder.cs`: `Dolly()` helper; new cameras `Tray Close` (0.32, 1.45, −1.84) → aim (0.7, 1.2, 0.5),
  FOV 38, goal (0.40, 1.40, −1.35) during S09_L05; `Voltmeter Close` (0.1, 1.38, −1.94) → aim (0.4, 1.0, 0.3), goal
  (0.165, 1.30, −1.45) during S09_L07; each: shot at t (1 s blend), `push 3.2`, `Shot("Bench Study", 3.8)` so the
  gate framing is settled 0.4 s before the gate. `Ambience("CircuitHum")` after the key closes.
- `Editor/Scene10Builder.cs`: `Meter Close` (−0.62, 1.22, −0.74) → (−0.28, 0.95, 0.08), FOV 30, wide lens; cut to it
  right after row 1 records, 2.6 s hold, 1 s blend back, 1.2 s margin before the 2 V gate (the `_GO` line is spoken by
  the gate while the Timeline is paused, so the close-up precedes it). `LabTheme()/Bed()` build-time fallbacks to
  PadLab/RoomTone if the bank lacks the new clips.
- `Editor/Scene08Builder.cs`: time-wave ring holds 0.6 s longer before `look 2.8 classroom`; `Music("PadLabTheme")` +
  RoomTone at the reveal (scene +0.6 s).
- `Editor/Scene13Builder.cs`: `look 2.0 finale` and `push 11 Cam Finale` at Ohm's appear, goal (0, 1.31, −2.50)
  (0.4 m along the Finale axis; 0.6 m would clip Wattson at Front Point).
- Final poses of every scene unchanged (match cuts intact); Lab Continuity's Scene 8 checks still use `glide`.
- Compile: clean. Verify in the after run: Scene 9 ≈04m22–27 and 04m39–44 dolly then Bench Study by the gate;
  Scene 10 ≈05m24 the ammeter face reading 0.20 A; Scene 8 two ring frames before the reveal; Scene 13 09m54 → 10m05
  a ≈15 % tighter finale with Wattson inside. Deferred: "hum follows current" needs a rig-bound cue (Experiment.cs).

## WP-A — audio — Implemented
- `Shared/SceneDirector.cs` 198–236: pitched one-shots take an idle source from a pool (max 4) cloned beside `sfx`,
  each with its own AudioBus (same kind/gain → slider and ducker apply); the pitch stays until the clip ends. Plain
  calls still use `sfx`. `Shared/AudioDucker.cs` 40: `RescanSoon()` so a fresh clone is ducked at once.
- `Editor/SfxBank.cs` 15, 152–208 (+ helpers 276–300, 342–369): new loops `PadIntro` 24.0 s (Am9 pad, nine sparse
  bells), `PadLabTheme` 33.3 s (72 bpm, Cmaj7–Em7–Am7–Fmaj7–Gadd9, brightened pad, eighth-note plucks),
  `CircuitHum` 6.0 s (60 Hz + harmonics, slow wobble, mains sizzle). Rendered outside Unity: `SfxBank.Check` 0
  failures; levels PadIntro −23.8 / PadLabTheme −24.2 / CircuitHum −19.3 dBFS RMS next to PadLab −23.9. Wavs are
  written on the Scene 4 rebuild.
- Title music: `Editor/Scene04Builder.Timeline.cs` 204–210 (PadLab clip ease-in 0.8 s; Scene 4's Music bus carries
  `PadIntro` as `bgm`, playOnStart off); `Shared/ModuleFlow.cs` 32, 104–108, 139–195: while `TitleScreen.IsOpen` a
  "Title Music" bus (Music kind, gain 0.35, ducked, never carried) plays it and fades out over 0.6 s when the title
  closes; `TitleScreen.Again()` re-triggers it; missing clip → one warning, silent.
- Ducking: depths 30 % / 55 % and 0.15 s attack kept; release 0.6 → 1.0 s (`AudioDucker.cs` 18) so a rhythmic bed
  does not pump between consecutive lines.
- Compile: clean. Scenes 5–13 must be rebuilt after Scene 4 so their `sounds` arrays include the new wavs. Listen
  for: music under the title and no gap at START; VoltStep rising per volt; lab theme from the Scene 8 reveal; hum
  when the key closes.

## WP-W — wire interior (Scenes 5–6) — Implemented
- Files: `Editor/SceneBuilderBase.Wire.cs` (28, 36–44, 53, 56, 95, 107–116, 193, 199, 216, 221, 623, 630–640, 648,
  661), `Shared/WireInterior.cs` (33–34, 42–46, 65, 78, 158–177, 262–322, 404–417), `Editor/Scene05Builder.cs`
  (170, 201), `Editor/Scene06Builder.cs` (66–77, 117–120).
- Set emission (`WireSetGlow` by material name after `BlenderSet.Wire.Load()`): yellow strips ×2 → ×0.9; backdrop
  image ×1.2 → ×0.42; halo ring ×2 → ×2.4. Builder bloom 1.0 → 0.45; the polish's electron bloom 0.75 → 0.45 and
  its carrier size 0.22 → 0.30 (orchestrator edit in `CinematicPolish.cs` 81, 172) so Apply keeps the new look.
- Copper identity: Copper Light L/R (.72,.84,1) → (1,.66,.4); haze (fog/ambient/background) (.1,.12,.16) →
  (.13,.11,.105); "Conduit Rim" trio at x −3.2/0/3.2, y 1.1, z 1.5, 2.4, range 6; lattice ions visible 0 → 0.7,
  copper (1,.55,.28), 0.12 m, seeded jitter 0.03 m, static; carriers 0.22 → 0.30, body (.35,.85,1), new "Carrier
  Halos" additive system (×2.6, alpha 0.45, ice-blue under the freeze); conduit self-glow 0.12 → 0.30.
- Cameras (WireOrigin-relative): Wire Inside pos (−3.8, −0.1, −8.0) look (−0.6, −0.5, 0) FOV 68 → 64 (conduit 64 % of
  the free frame, − end 148 px tall; DoF focus marker at 4.2 m); Scene 6 entry pose identical. Wattson's PlatformPoint
  (−3.2, −1.25, −5.7) → (−3.65, −0.75, −5.55) (≈190 px, clear of labels and caption bar); old spot kept as "Terminal
  Point" for the Terminals shot. "− negative" label x −5.4 → −4.8. Scene 6: gauge ×1.6 at PlusBlock + (−0.6, 3.61,
  −0.35); the freeze/gadget beat plays in the "Wire Wattson" two-shot before S06_L01, then Wire Inside for the card.
- Compile: clean. Verify: calmer floor/backdrop, warm walls, orange ions in a faintly glowing tube, cyan haloed
  electrons, Wattson lower-left ≈190 px, labels inside both wides, gauge ≈100 px from the top.

## Orchestration — Tested
- Full-tree compile clean (`Tools/compile_check.sh final`); code committed as `e53ff6b` before any scene rebuild.
- Editor chain (open editor, PID 34148): `menu Ohm's Law/Build Scene 04` (51 s; wrote PadIntro / PadLabTheme /
  CircuitHum wavs) → `all` (first attempt failed: `BlockoutAssets.EnsureDisplayFont` null reference, the TMP
  fallback list is null in memory right after CreateAsset + SaveAssets; guarded, re-run clean, Scenes 05–13 built,
  Fredoka SDF created with Nunito as fallback) → `Polish/Apply later scenes` (9 scenes patched; Classroom polished
  exposure now 0.45, Finale_Global_Polished created) → self-checks Ohm Pose 21, Wattson Pose 119, Wattson Actions
  144, Lab Continuity 153, Voice Pipeline 20, Wire Electrons 18, HUD v5 11, Wire Sample 4: **490 pass, 0 fail**.
- After run `all record ask fresh @1920x1080`: **RESULT PASS**, 616.6 s, every line of every scene spoken in order,
  all gates taken, Ask Wattson probe passed, no FAIL / ERROR / STALL. Evidence `Reports/OhmsLawAudit/after/`.
- Perf (editor, no recorder, Scenes 9–13 at 2x): frame ms mean 17.5–19.6, p95 ≤ 25.9, batches ≤ 590 — the same as
  the pre-change baseline (the recorder's per-frame readback inflates the recorded runs to 42–45 ms in both runs).
- Audio of the after run: integrated −21.1 LUFS, LRA 8.7 LU, true peak −2.0 dBFS, no silence gap > 2.5 s.
