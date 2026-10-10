# Ohm's Law — UI v4 "Cinematic Case File" · Figma-first plan (2026-10-10)

Phase 1 (audit of the running module, the 10-page reference PDF and the gameplay recording) is done.
Phase 2 (the Figma file) is scripted and tested offline; it needs the Figma connector signed in.
Phase 3 is this document plus the Figma file. Nothing in Unity changes until you approve.

## 1. What the module has today (audit)

| Area | Reality in the project | Source |
|---|---|---|
| UI framework | uGUI + TextMeshPro on one persistent canvas built in code (no prefab-authored UI, no UI Toolkit, no DOTween) | `Shared/ModuleFlow.cs` → `LessonPanel`, `HudScreens`, `SubtitleUI`, `AskWattsonUI`, `PanelViews`, `GraphUI`, `ObservationTableUI`, `MCQPanel`, `HudKit` |
| Scaling | CanvasScaler 1920×1080, match 0.5; `HudScale` floor 0.75 (+15 % "Larger text"); `SafeAreaLayout`; landscape only | `ModuleFlow.Awake`, `HudScreens.HudScale` |
| Current look | v3 "Case File": ink `#0A1120` opaque cards, paper text, spark cyan `#BFF6FF` for guidance, V/I/R blue/amber/red | `Docs/Polish/HUD_DESIGN.md`, `Logs/Blockout/snapshots/*.jpg` |
| Title screen | Already reads OHM'S LAW · "A Journey of Curiosity, Current and Discovery" · START JOURNEY · Continue · meta line, over the study with the spark swirl (Ohm at the desk, Wattson at the right) | `HudScreens.TitleScreen` (lines 575–613) |
| HUD | Chapter chip "n / 10" + 10-step rail (top-left), capsule PAUSE · REPLAY · CC · VOLUME · SKIP (top-right, folds when idle), caption bar (bottom, centred in the free width), Ask Wattson avatar (bottom-right), 480/580 px dock of cards on the right | `LessonPanel.cs` |
| Cards in the dock | Turn (your turn / done / next step), Clue, Prediction (3 choices, one tap, never marked), Voltage steps (1–5 V), Readings, Table, Graph, Formula lines, Counter, Schematic, Copper/nichrome inset, Law tiles, Case closed | `LessonPanel.Order`, `PanelViews.cs` |
| Ask Wattson | Header (face, name, plain status, History, Close), bubbles, up to 3 suggested questions, Hold to talk · field · Ask/Stop; offline notes fallback | `Agent/AskWattsonUI.cs`, `WattsonAgent` |
| Pause menu | Where you are, Resume, Restart chapter, 10 chapters (locked until reached), unlock line, keys; settings column; Exit (inline "Tap again to leave") | `HudScreens.PauseMenu` |
| Settings that exist | Captions, Larger text, Reduce motion, Wattson / Effects / Music volumes. **No** graphics-quality or fullscreen option | `HudScreens.PauseMenu`, `LessonPanel.VolumePopup` |
| Fonts in the project | Nunito Sans Bold (the TMP asset), Source Sans 3 Regular/Semibold, DejaVu Sans Bold, Liberation Sans. No serif | `Assets/AdaptiveLearning/Art/Interface/Fonts`, `Assets/OhmsLaw/Shared/UI` |
| Chapters | 1 Meet Wattson · 2 Journey Inside the Wire · 3 Your Prediction · 4 Back to Georg Ohm · 5 From Ohm's Lab to Our Lab · 6 Build the Circuit · 7 Become the Experimenter · 8 The Pattern · 9 Discover Resistance · 10 Ohm's Law | `HudScreens.Chapters` |
| Graph | **I on X, V on Y** (the Chapter 9 narration: "the slope tells us the resistance") | `GraphUI.cs:59-60` |
| Recording | `ohms law.mp4` (9:18, 1280×720) shows the v3 HUD above, chapters 1–10; it matches the editor frames | — |

## 2. The PDF's 17 panels against the real game

| PDF panel | In the game? | v4 design | Unity object |
|---|---|---|---|
| 01 Start banner | yes (new banner, intro unchanged) | S01–S03, Title block, Button/CTA | `TitleScreen` |
| 02 In-game HUD | yes | S05–S10, Chapter chip, Control bar, Control | `LessonPanel` top controls, `ChapterBanner` chip |
| 03 Dialogue + CC | yes (captions; one speaker: Wattson) | Caption bar — Centred / Portrait | `SubtitleUI` |
| 04 Info / explanation card | yes (clue cards) | Clue card, S24 | `LessonPanel.Info` |
| 05 Circuit interaction prompt | yes (turn card + world glow) | Turn card, S14–S15 | `LessonPanel.Turn`, gates |
| 06 Voltage selection | yes (1–5 V steps) | Voltage step ×4 states, Voltage strip, S16 | `PanelViews.VoltageButtons` |
| 07 Measurement display | yes (dock readouts; 3D meters keep needles) | Readout, Readings card | `LiveReadingsPanel` |
| 08 Graph + observations | yes | Observation table, Graph container, S17, S19 | `ObservationTableUI`, `GraphUI` |
| 09 Quiz "Question 3 of 8" | **no** — only the one prediction | Prediction card, Choice ×4 states, S11–S12 | `MCQPanel` |
| 10 Correct / Not quite | **no** marking — Done confirmation + one wrong-tile hint | Turn card Done, Toast Success/Error, S21, S26 | `LessonPanel.Done`, `ShowHint` |
| 11 Final formula | yes | Formula card reveal, Case closed card, S27–S28 | `LessonPanel.Reveal`, `End` |
| 12 Pause menu | yes (richer than the mock) | Pause menu, Chapter row ×3, S29–S30 | `PauseMenu` |
| 13 Settings | partly (no graphics / fullscreen) | Settings rows, S31 | `SwitchRow`, `VolumeRow` |
| 14 Ask Wattson | yes | Ask Wattson panel, Chat bubble, Avatar ×6, S22–S23 | `AskWattsonUI` |
| 15 / 16 Clue or hint | yes | Clue card, Toast, S25 | `LessonPanel.Info`, `ShowHint` |
| 16 Chapter progress | yes (rail + chapter list) | Rail segment, Progress rail, Chapter banner, S04 | `ChapterBanner`, `PauseMenu` |
| 17 Notification toasts | hint pill only | Toast Info / Success / Error | `LessonPanel.ShowHint` |

## 3. The proposed system

**Colour** (Figma variables, collection "Ohm UI"; the PDF's starting values, one addition)

| Token | Value | Use |
|---|---|---|
| bg/primary · bg/secondary · bg/hover · bg/scrim | `#081A2B` · `#10283D` · `#17395A` · `#050F1C` | cards 92 %, rows, hover, pause scrim 72 % |
| accent/blue · accent/cyan · accent/gold | `#2EA7FF` · `#27D0F0` · `#FFC857` | selected / progress / graph line · interactive edges, eyebrows, Wattson · primary CTA, discoveries |
| accent/royal (added) | `#1557B8` | the START JOURNEY plate under its gold frame |
| state/success · state/error | `#27D07D` · `#FF5B5B` | Done · the wrong law tile |
| text/primary · secondary · muted · on-gold | `#F5F7FB` · `#AFC5D5` · `#6E8299` · `#081A2B` | |
| border/subtle · interactive · focus · gold | cyan 22 % · 55 % · 100 % · gold 80 % | card edge · buttons, chips · keyboard focus · CTA |
| quantity/voltage · current · resistance · charge | `#3FA9FF` · `#FFB830` · `#FF5A5F` · `#D6FAFF` | **unchanged project palette** (the 3D VOLTAGE / CURRENT signs use it) |

**Type**: Cinzel Black 132 (OHM'S LAW), Cinzel Bold 56 / 26 (chapter card, title subtitle); Nunito Sans Bold for headings, buttons, labels (32 / 28 / 24 / 20 / 16 / 15), Nunito Sans SemiBold for body and captions (21 / 18 / 17 / 32). Readouts 46, table 22, formula 64 / 34. Nothing under 15 px at 1080p. 22 Figma text styles, each carrying its TMP mapping in the description.

**Components** (Figma page 01): Button (4 styles × 4 states), Button/CTA, Control (5 states, icon swap), Control bar (full / folded), Rail segment + Progress rail, Chapter chip, Toggle, Slider, Key cap, Toast (3), Choice (4), Voltage step (4), Readout (3), Law tile (3), Chat bubble (2), Portrait (2), Ask Wattson avatar (6), Chapter banner, Caption bar (2), Turn card (3), Clue card (2), Prediction card, Voltage strip, Readings card, Observation table, Graph container, Formula card, Law card, Counter card, Diagram card, Comparison inset, Case closed card, Ask Wattson panel, Settings rows (switch / volume), Chapter row (3), Pause menu, Volume popup, Title block; 25 icons. Not built because the game has none: dropdown, quiz question, correct / incorrect modal.

**Layout**: unchanged from v3 (`HUD_DESIGN.md`): chip and capsule in the top corners, 480/580 dock on the right, captions bottom-centre in the free width, avatar bottom-right, banner top-left. What changes is the skin: navy glass cards with a cyan hairline and a soft shadow, serif cinematic titles, gold CTA / reveals, blue selection, cyan interactive edges and eyebrows, Wattson's portrait on live answers.

**Motion** (as today, re-tuned): press 90 ms, hover 150 ms with the glow, card enter 280 ms fade + 0.96 → 1 pop, one pulse per screen, formula reveal with a single gold glow; reduced motion = fades only. No new animation library.

**Responsive**: 1920×1080, 1920×1200 (×1.05), 2048×1536 (×1.23), 1366×768 (floor ×0.75) — page 08 shows the same dock composition at all four.

## 4. Decisions that need your word (say "approve" or change any line)

1. **Graph axes stay I on X, V on Y.** Your brief and the PDF say Voltage on X, Current on Y; the module plots the other way so that the slope is R, and Chapter 9's narration says so ("the slope tells us the resistance"). Swapping would make the slope 1/R and the narration wrong. Recommended: keep.
2. **Two fonts added** (both SIL Open Font Licence, already on Google Fonts for Figma): Cinzel (title, chapter card, subtitle) and Nunito Sans SemiBold (body, captions). Each is one new TMP font asset.
3. **Cards at 92 % navy with a cyan hairline.** v3 went opaque because 94–97 % read see-through in linear colour space; the implementation will test 92 % and fall back to 96 % where a card sits over a bright frame.
4. **Colour overlap accepted as in the PDF**: gold is both the CTA and the current amber's neighbour, red is both error and resistance. Quantity colours appear only on quantity glyphs, words, values and tiles.
5. **Title screen keeps the current composition** (study, spark swirl, Ohm and Wattson) and the current copy. The mock's Georg Ohm quote and "VOLTAGE / CURRENT / RESISTANCE" book spines are not in the game and are not designed. CC and VOLUME are the only controls before the case starts.
6. **Caption bar**: centred narration bar (no portrait) for scripted lines; the Portrait variant only while Ask Wattson answers live.
7. **Not designed because the game has no such feature**: quiz flow, Correct / Not quite cards, graphics quality, fullscreen, dropdowns, "New concept unlocked" toasts. Fullscreen on desktop is a one-line add if you want it on the settings column.
8. **Pause menu keeps today's content** (Resume, Restart chapter, ten chapters, settings column, inline exit confirm) rather than the mock's four buttons; "Exit to Menu" is the title screen on WebGL / iPad and quit on desktop, as today.

## 5. Figma build

Pages: 01 Design System · 02 Start Screen · 03 Gameplay HUD · 04 Investigation & Experiment · 05 AI & Information · 06 Feedback & Completion · 07 Settings & Pause · 08 Responsive Layouts · 09 Reference Board (the consolidated sheet, also exported as a JPG into `Docs/Polish/UI_V4/`).

The whole file is built by seven idempotent `use_figma` scripts plus one `upload_assets` call (18 HUD-free 1080p frames captured from the editor today with the `clean` play-test word, plus Wattson's face). The scripts were run against a local mock of the Plugin API (1 600 nodes, 55 variables, 22 text styles, 6 effect styles, 42 components, 36 screens) so each connector call should land first time. The Figma Starter plan allows 20 MCP calls a month; this build needs 8–10, so please sign in with the account you want the file on (the `shubham@metabookxr.com` Starter plan had ~14 left on 2026-10-07; the `kavikalashh@gmail.com` connector was out of allowance).

## 6. After approval — implementation outline

Skin only, handlers untouched: `LessonPanel` colour statics and `Card()` / `Pill()` / `Control()` skins, `HudKit` icons from the Figma SVG paths, `SubtitleUI` portrait variant, `HudScreens` title block / banner / pause menu styling, `PanelViews` voltage steps / inset / law tiles, `GraphUI` / `ObservationTableUI` palette, `AskWattsonUI` header and chips, two TMP font assets. Then the hands-free play test at 1920×1080, 1920×1200, 2048×1536 and 1366×768 with the `hud ask` probes, screenshots compared with the Figma frames, and a before / after sheet in `Docs/Polish/UI_V4/`.
