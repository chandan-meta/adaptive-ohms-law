# Ohm's Law module redesign — completion report (2026-10-06)

Brief: `Docs/Polish/MASTER_PROMPT.md` (the user's 20-section master prompt). Checkpoint before the work: git tag
`checkpoint-2026-10-06-before-redesign` (commit `babd7e4`). Unity 6000.3.0f1, URP, mouse + touch, no XR: unchanged.

Companion documents: `REDESIGN_TEXT.md` (every revised line and panel text), `AUDIO_CUES.md` (the cue plan),
`../FLOW.md` (regenerated flow of every scene), `../HANDOFF.md` (workflow).

## 1. What was preserved, changed, shortened, removed

**Preserved.** The scene order and the code-built scene pipeline (`SceneNNBuilder` → Timeline); Scene 4's study,
characters and the VOLTAGE / CURRENT reveal; the first inside-wire experience; the Scene 6 prediction (one tap, never
marked, called back in Scenes 9 and 10); Ohm's study and the equipment → modern-instrument transformation and the
study → classroom sweep in Scene 8, with Ohm's "Present" moment toward the class; the bench rig (ammeter in series,
voltmeter in parallel, key); the project's own dataset (1–5 V, 0.20–1.00 A, 5 Ω); Ask Wattson's agent, actions and
proxy; reduced-motion, captions and volume settings.

**Changed.** All UI is one right-hand lesson panel (`Shared/LessonPanel.cs`): instruction, captions, notes, controls,
readings, counter, schematic, inset, table, graph, formulas, reveals, the prediction, Ask Wattson's conversation and the
end buttons. The 3D camera composes inside the free part of the frame (`PanelFraming`, a projection shift). Wattson's
voice is edge-tts `en-US-AnaNeural`. Scene 4's shot plan is calmer (one slow move, held shots, cuts). Scene 5 is
rebuilt in the brief's order (random motion → voltage as potential difference → drift − → + with a conventional-current
note → cross-section counter with a fixed one-second interval, labelled as simplified). Scene 9's assembly is stated
step by step in the panel with a synchronised schematic. Scene 10 uses five discrete voltage buttons with per-step
instructions; the table and graph live in the panel. Scene 12's resistance comparison is a magnified inset in the
panel (copper vs nichrome at the same voltage, length, thickness, temperature). Ohm's hands are constrained to the
desk top in `OhmWorker` (contact clearance, lift arcs), checked by the `Ohm Pose` self-test.

**Shortened.** Scene 7 (six lines, one establishing shot, one hero shot of the apparatus); Scene 8 (four lines around
the transformation); Scene 12 (inset + V/I column + slope + ohm, no second dive); Scene 13 (three pieces → V = IR →
OHM'S LAW → end, no slide section).

**Removed.** The blackboard table/graph kit (`SceneBuilderBase.Boards.cs`), the top-left effects/controls UI
(`CinematicControlsUI`), floating callout cards (`Callouts` is now a shim into the panel), the question bank and every
quiz/target gate except the Scene 6 prediction (S05_Drift, S10_Predict2V/Record/Pattern/Discover, S11_Target,
S12_Ratio/Slope/AQ1, S13_AQ2/Apply/Recap), the voltage slider and the "×2 click", the Scene 12 inside-wire resistance
dive, the Scene 13 slide section, the `AdaptiveQuestionsTest`.

## 2. Files changed

| Area | Files |
|---|---|
| Panel (new) | `Shared/LessonPanel.cs` (panel, blocks, header, controls, MORE settings, end block, `CanvasFit`, `UIPop`, `PanelFraming`), `Shared/PanelViews.cs` (voltage buttons, charge counter, circuit schematic, resistance inset, `Draw`) |
| Panel (rewritten onto it) | `Shared/SubtitleUI.cs`, `Shared/MCQPanel.cs`, `Shared/Callouts.cs`, `Shared/LiveReadingsPanel.cs`, `Shared/ObservationTableUI.cs`, `Shared/GraphUI.cs`, `Shared/Agent/AskWattsonUI.cs`, `Shared/Agent/WattsonAgent.cs`, `Shared/Agent/WattsonActions.cs` |
| Flow and cues | `Shared/ModuleFlow.cs` (creates the panel, `EndOfModule`), `Shared/SceneDirector.cs` + `.Gates.cs` + `.Experiment.cs` + `.Wire.cs` (new cues `sfx`, `ambience`, `music`, `counter`, `schematic`, `inset`, `expand`, `panel`, `formula`, `formula.clear`, `reveal`, `end`, `ohm.look`, `wire.ticks`; select-then-place gates; voltage-button gates; `panel <Block> 1|0`) |
| Runtime pieces | `Shared/CircuitRig.cs` (select → place, `Selected` event), `Shared/WireInterior.cs` (`Tally`, `SecondProgress`, `Counted`), `Scene04/Scripts/OhmWorker.cs` (desk-top contact, lift arcs, `Attend`), `Scene04/Scripts/Scene04Director.cs` (panel instruction, 220 px tap target) |
| Builders | `Editor/SceneBuilderBase.cs` (+ `.Rig`, `.Wire`, `.Classroom`, `.Lab`, `.Callouts`), `Editor/Scene04Builder.cs` + `.Timeline.cs`, `Editor/Scene05Builder.cs` … `Scene13Builder.cs` |
| Sound | `Editor/SfxBank.cs` (new: 23 synthesized recipes, loops included) merged into Scene 4's `BuildSfx`; clips in `Scene04/Audio/SFX/` |
| Voice | `Editor/BakeBlockoutVO.cs` (edge-tts path, `RemoveScratch`), `Tools/edge_bake.py` (new) |
| Tests / tooling | `Editor/BlockoutPlayTest.cs` (`Ended` detection, `end_card` frame, `ask` probe), `Editor/Tests/WattsonPoseTest.cs` (Ohm hand checks) |
| Deleted | `Shared/CinematicControlsUI.cs`, `Shared/QuestionBank.cs`, `Editor/SceneBuilderBase.Boards.cs`, `Editor/Tests/AdaptiveQuestionsTest.cs` |

Scenes rebuilt: `Scene04_MeetWattson` … `Scene13_OhmsLaw` (and their Timelines, re-timed to the new voice).

## 3. Panel instruction and dialogue text

See `REDESIGN_TEXT.md`. The panel's instructions, as implemented: "Click or tap the highlighted wire to explore what
happens inside." · "Tap your prediction." · "Select the ammeter." · "Click the highlighted position to place it." ·
"Select the voltmeter." · "Close the switch to complete the circuit." · "Set the voltage to N V." · "Observe the current
reading." The brief's "Select the battery control." and "Connect the highlighted terminals." are not used: the voltage
buttons are the battery control, and the voltmeter's leads connect themselves when it is placed (no separate action).

## 4. Audio cue list

`AUDIO_CUES.md` lists every bed and one-shot, where it plays and what plays it. All 23 clips are synthesized at build
time (licence-free). Not provided: recorded foley (none exists in the project), spatialization, and two reserved cues
(`Tap`, `Connect`) that no scene uses. Scene 4 keeps its original synthesized cues on its SFX tracks.

## 5. Evidence

All runs below were performed in the Unity 6000.3.0f1 editor (Windows, Game view 1920×1080, hands-free play test
at 2× speed with `ReviewMode.autoAnswer`). Nothing here was run on a tablet, in WebGL or in a player build.

**Play test, whole module** (`echo "all 2x ask" > Logs/ohm-play.req`, 13:01 on 2026-10-06): `RESULT PASS`. 92 line
checks passed (every Timeline line of Scenes 4–13 spoken in order, every clip a real edge-tts mp3, no scratch clip), all
11 gates passed (S06_Prediction, S09_PlaceAmmeter / PlaceVoltmeter / CloseKey, S10_Set1V … Set5V, S10_Callback), the
Ask Wattson probe opened the conversation in Scene 7, asked "What is electric current?", received the offline answer,
closed and the lesson resumed, the module reached the finale and the end card (REPLAY / EXIT) without a stall, and no
console error was logged. An earlier run of the same request at 12:41 also passed; the first run of the day (12:15)
found the one runtime error of the redesign (a panel view that could not be rebuilt twice), fixed since.

**Editor self-checks** (13:01–13:07): Ohm Pose 20/20 (hands never below the desk clearance in Work, Present, Examine;
finger curl; palms down; lift arc; attention turn), Wattson Pose 118/118 (incl. the Ask spot in the free frame),
Wattson Actions all passed (offline guidance quotes the readings and the voltage buttons), Lab Continuity 143/143
(match-cuts 4→5 … 8→9, reduced motion on every camera and VFX), Voice Pipeline all passed (lexicon, bake text),
Wire Electrons all passed (no drift with the field off, 3 per second at 0.6 m/s, counter doubles 1 V → 2 V).

**Four review passes, from the play-test frames** (`Logs/Blockout/shots/<Scene>/`, 30 of them kept in
`Docs/Polish/Evidence/`):

1. *Flow and science* — the sequence runs intro → prompted wire → inside the wire (random motion, voltage as potential
   difference, drift − → +, conventional-current note, counted section with a fixed second) → prediction → Ohm's study
   → transformation → guided assembly (series / parallel, schematic in step) → five voltage buttons → table and graph →
   V ∝ I → inset → V/I = k → slope → Ω → V = IR → end. `s05_*`, `s09_*`, `s10_*`, `s11_*`, `s12_*`, `s13_*`.
2. *UI and readability* — one panel, one instruction at a time, captions in the panel, notes one at a time, the data
   views shrink before any text overlaps (fixed in this pass: the table/graph/formula column used to overflow in
   Scenes 10–13, titles wrapped one letter per line, control rows stretched). `s09_select_ammeter`, `s10_set_1v`,
   `s12_v_over_i_chain`, `s13_end_card`.
3. *Characters, cameras, audio, FX* — Wattson inside the free frame in every scene (markers moved: Wattson Home, Source
   Side, Tray Point, Key Point, Front Point, Platform Point), Ohm's hands rest on the desk (`s07_ohm_hands_on_desk`),
   Ohm presents to the class (`s08_ohm_presents`), the finale holds (`s13_finale`), the dive streaks at half glare.
   Audio was verified structurally (every cue resolves to a clip, voice clips are non-silent at about −22 dB mean,
   durations match the re-timed Timelines); it was not listened to in this session.
4. *Regression* — repeated taps cannot duplicate a placement (select-then-place gates clear their state and the slot
   ignores a placed part), voltage buttons above the current step stay dim, Ask Wattson pauses and resumes the lesson,
   the end card offers REPLAY (module restart) and EXIT. Skip/pause/replay and a manual restart were not exercised by hand.

## 6. Implemented and tested / remaining

**Implemented and tested (play test + self-checks + frame review):** the single right-hand panel and every block in
it; the new Scene 4 shot plan and the prompted wire tap; the rebuilt inside-wire sequence with counter and polarity
markings; the prediction; the shortened Ohm segment with the hero shot; the transformation and classroom sweep; the
guided assembly with instructions, placement sound and synchronised schematic; the five-button voltage experiment with
table and graph; V ∝ I with line sweep and musical rise; the inset comparison; the V/I column, slope marker, the ohm,
the V = IR finale and the end card; Ohm's desk-contact hands; Wattson's edge-tts voice on all 193 lines with re-timed
Timelines and captions; the synthesized sound bank and beds; Ask Wattson in the panel with honest offline state.

**Remaining (not done, or not verifiable here):**
- Listening pass on the voice and mix (levels were measured, not heard); a human read of the Ana voice for "cute,
  playful, curious" is still a judgement call.
- Live Ask Wattson: needs the proxy keys (see §7). Speech input (push-to-talk) therefore untested.
- Manual interaction pass by a person: mouse and touch taps on the wire, the parts, the key, the buttons and the Ask
  Wattson field were exercised only by the automated gate answers and the probe.
- Other resolutions and devices (1366×768, tablets, WebGL): untested; the panel is designed in reference pixels with
  a canvas scaler, the data views shrink when short of height, but no device run was made.
- Lip-sync: Wattson's mouth is driven by clip loudness (as before); there is no phoneme lip-sync.
- "Maya": no such character exists (see §7); Ohm's reaction is toward the class.
- Scene 14 video and a foley/music pass by a sound designer (every cue is synthesized).

## 7. Missing assets and integrations (precise)

- **Live Ask Wattson.** `Server/wattson-proxy/` needs its `.env` (Anthropic key for chat, ElevenLabs key for the live
  voice, OpenAI key for speech-to-text) and must be running. Without it the panel says "OFFLINE MODE: saved answers
  only (the Wattson server needs its API keys)", answers from each scene's saved FAQ, and the microphone stays off.
  Nothing pretends to be connected.
- **Voice licence.** Wattson's clips come from edge-tts, an unofficial client of Microsoft Edge's read-aloud service
  (no key, no licence text). For a distributed product, re-bake through the proxy with an ElevenLabs voice (same menu,
  keys in `.env`) or record a voice actor (`<id>.wav` beside each mp3 is used first).
- **"Maya".** No character of that name exists in the project (only Wattson and Georg Ohm; the learners are the camera).
  "Ohm notices the class" (his Present pose in Scene 8) is what was preserved. A Maya character would need a model, rig
  and lines.
- **Figma.** Not available in this environment; the panel was designed in code (constants at the top of `LessonPanel.cs`).
- **Scene 14 video.** Unchanged: `ModuleFlow.scene14Video` is empty; the module ends on the panel's REPLAY / EXIT.
- **Device tests.** Only the desktop editor at 1920×1080 ran. Tablet/touch, WebGL and other resolutions were not run.
