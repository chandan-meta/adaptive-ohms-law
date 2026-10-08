# HUD design system — v3 "Case File" (approved 2026-10-07)

Source of truth for the module's interface. Approved board: https://claude.ai/artifact/3SU84Wr1ayCNskyzFGZmTK (revision 2,
checked rule by rule against UI UX Pro). Figma: https://www.figma.com/design/s0BWWs7zT8csU6vhuCVSr2 (page *Design system*:
variables, text styles, icons, components with states; page *Screens*: user flow, layout zones, screens A–J).
Built in code on the persistent module canvas: `Shared/LessonPanel.cs` (controls, captions host, Ask avatar, dock, turn /
clue / case-closed cards), `Shared/HudScreens.cs` (chapter banner and chip, pause menu, title screen, `HudScale`, `HudKeys`),
`Shared/SubtitleUI.cs`, `Shared/HudKit.cs` (icons, `HudButton`, `UiSounds`), `Shared/PanelViews.cs`, `Shared/MCQPanel.cs`,
`Shared/Agent/AskWattsonUI.cs`.

## Principles

1. The 3D scene is the hero: HUD pieces live at the edges; the hero area (faces, apparatus, results) stays clear.
2. Colour means something. The chrome is ink and paper; Wattson's **spark** marks guidance and "done"; **blue, amber and
   red only ever mean V, I and R** (voltage blue was also the UI accent before v3).
3. Every action is said once in the card and once in the world (the target glows), and confirmed when it lands.
4. Nothing blocks: cards never steal input, a tap sends the chapter title away, animations never delay a control.

## Layout (1920 × 1080 reference px)

| Piece | Where | Size / behaviour |
|---|---|---|
| Chapter title (`UI_ChapterTitle`) | top-left, 56 / 50 px in, over a corner vignette | "CHAPTER n OF 10", title 54, purpose 23, 10-step rail; fades in 0.6 s, holds ~3.5 s, out 0.4 s; a tap sends it away |
| Chapter chip (`UI_ChapterChip`) | top-left, 32 / 28 px | 56 tall capsule: "n / 10", small rail, chapters icon; tap → pause menu |
| Controls (`UI_TopControls`) | top-right, 32 / 28 px | one 56 px capsule: PAUSE · REPLAY · CC · VOLUME · SKIP, 44 px items, 9 px between hit areas; idle in a cinematic chapter (3 s without input) → icons only, full contrast |
| Hint (`UI_Hint`) | under the controls | "Finish this step first" / "Close Ask Wattson first" for 2.5 s when a disabled control is tapped |
| Dock (`UI_Dock`) | right, top 104, bottom 196 | 480 wide, 580 with table / graph / inset / case closed; cards only while their beat runs |
| Captions (`UI_SubtitlePanel`) | bottom 40 px, centred in the free frame | hugs the line, max 1080 (and clear of the avatar when there is no dock), two lines then pages |
| Ask Wattson (`UI_AskWattson`) | bottom-right | 92 px face in a spark ring; idle / listening / thinking / speaking / error |
| Conversation (`UI_Conversation`) | dock bounds, 580 wide | header (face, name, plain status, History, Close), messages, chips, Hold to talk · field · Ask |
| Pause menu (`UI_PauseMenu`) | centre, over a 66 % scrim | 1140 wide card: where you are, Resume, Restart chapter, chapter list (10), unlock line, keys; settings column |
| Title screen (`UI_TitleScreen`) | full frame, left vignette, block low-left over the floor | kicker, "Ohm's Law" 132, lead 28, Start the case (primary), Continue · Chapter n, meta line |

Back to front: world labels · title screen · dock · captions · Ask · chapter banner · controls · popup · hint · pause menu ·
white fade. `HudScale`: the canvas never scales below 75 % (1366 × 768 and 1280 × 720 laptops get a larger canvas, not
smaller text); "Larger text" adds 15 %. Landscape only.

## Colour tokens

| Token | Value | Use |
|---|---|---|
| Ink | `#0A1120` | cards 94 %, caption 80 %, control bar 86 % (72 % folded), pause scrim (navy-black) 66 % |
| Raised | `#141D30` | option rows, steps, tiles, the text field; hover `#1D2944` |
| Paper | `#F7F5EF` | text, primary buttons, a chosen option (17.3:1 on ink) |
| Body | `#D3DAE6` | clue text |
| Muted | `#A9B6CC` | secondary text, eyebrows (9.2:1) |
| Spark | palette `wattsonAccent` `#BFF6FF` | Wattson, guidance, "done", the turn card, focus, progress done (16.0:1) |
| V / I / R | palette voltage `#3FA9FF`, current `#FFB830`, resistance `#FF5A5F` | the quantities only |
| Lines | white 10 % (cards), 38–40 % (interactive borders: 3:1) | |

## Type (Nunito Sans Bold, the module's TextMeshPro font; headings use TMP bold)

Display 132 · chapter 54 · caption 32 · turn 28 · question 26 · card title 24 · option 22 · card body 21 · hint 20 ·
button 20 · chip 18 · reading 46 · labels 15–16 caps, tracked. Nothing below 15 px at 1080p.

## Components and states

- **Control** (`LessonPanel.Control`): normal (bare), hover (+8 % white), pressed (0.97), on (white 14 %: CC on, PAUSE while
  paused, VOLUME while open), disabled (36 %).
- **Pill** (`LessonPanel.Pill`): Primary (paper / ink text, one per view), Secondary (white 10 %, 3:1 border), Ghost
  (muted text), Raised (option rows). Icon optional; the word is always there.
- **Turn card** (`LessonPanel.Turn(text, step, hint)` / `Done(text)`): waiting (spark-tinted badge with a target, spark
  edge, "YOUR TURN · STEP n OF m", pulses once on arrival) → done (spark badge with a check, "DONE", ~0.9 s, chime).
- **Clue card** (`LessonPanel.Info`): lens + "CLUE" (or "YOUR PREDICTION"), title 24, body 21 with quantity words coloured.
- **Choice** (`MCQPanel`): Raised row, letter badge; `Select(i)` → paper row / ink text, others 45 %; never marked.
- **Voltage step** (`VoltageButtons`): done (check, muted), now (paper, full contrast even while the controls are closed),
  next (spark outline, pulsing, live while Wattson asks for it), later (36 %; a tap says why). The count lives on the turn card.
- **Readings card** (`LiveReadingsPanel`): steps aside while the data table is up.
- **Data cards** (`ObservationTableUI`, `GraphUI`): card surface, hairline rules, paper values, coloured column heads and axis
  titles, neutral row highlight; table text 44 canvas px (≥ 15 px on screen); the two-column table centres in its card; slope
  triangle rise blue (ΔV), run amber (ΔI). A crowded dock never squeezes the turn card: the readings, then the formula, step aside.
- **Law tiles** (`LawTiles`, Chapter 10): R · V · I tiles; tap V, I, R to build V = I × R.
- **Case closed** (`LessonPanel.End`): tilted stamp, what you found (V / I / R tiles), V = I × R, the learner's numbers,
  Replay (primary), Chapters, Exit.
- **Rail** (`Rail`): done spark, now paper and longer (shape, not colour alone), ahead white 24 %.

## Motion

Press 90 ms (0.97) · hover 150 ms · card enter 280 ms decelerating (fade) + 0.96 → 1 pop · exit instant or 180 ms ·
one continuous pulse per screen (the world target; the turn card pulses once) · everything on unscaled time · reduced
motion: fades only, no pulse, no scale.

## Input

Targets ≥ 44 px (controls 44, options 64, steps 62, chips 52, avatar 108 hit area, slider rows 48, switch rows 48).
Keyboard (`HudKeys`): Space pause menu (Start on the title; push-to-talk while Ask is open), Esc menu (closes the popup or
Ask first), R replay line, C captions, Enter acts on the current turn (taps its target) or starts / resumes, 1 / 2 / 3 pick a
prediction, Tab / Shift+Tab move focus through what is in front (spark focus ring; sliders have none). A focused text field
keeps its keys. Idle 8 s in a turn: the card pulses once with the hint sound (and the step's recorded hint line, once).

## Sounds (`UiSounds`, synthesised)

hover tick, click, open, close, done (rising fifth), hint, stamp (soft thud) — effects bus, 35 %.
