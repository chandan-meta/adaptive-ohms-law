# HUD v3 "Case File" — implementation and review report (2026-10-07)

Approved design: board https://claude.ai/artifact/3SU84Wr1ayCNskyzFGZmTK (rev. 2, UI UX Pro check), Figma
https://www.figma.com/design/s0BWWs7zT8csU6vhuCVSr2. System spec: `HUD_DESIGN.md`. Everything below was run in the Unity
6000.3.0f1 editor on Windows with the hands-free play test; nothing ran on a device or in a player build.

## Evidence

| Run | Result | Report / frames |
|---|---|---|
| Whole module, 1920×1080, 2× | RESULT PASS (every line, 21 gates incl. the four new ones) | `Logs/Blockout/play-report-v3-1080.txt`, `Logs/Blockout/shots/` |
| Whole module from Chapter 1, 1366×768 (75 % UI floor) | RESULT PASS | `Logs/Blockout/play-report-v3-1366.txt`, `Logs/Blockout/res/v3-1366/` |
| Whole module from Chapter 1, 2048×1536 (4:3), `hud ask` probes | RESULT PASS (8 HUD checks: pause holds the Timeline, resume, CC off/on, volume popup, replay, skip; Ask opened, answered offline, closed) | `Logs/Blockout/play-report-v3-2048.txt`, `Logs/Blockout/res/v3-2048/` |
| Whole module from Chapter 1, 1920×1080, `hud ask`, after the fixes below | RESULT PASS (same 8 HUD checks and the Ask probe; title screen, chip, Ask row, table and tiles confirmed in frames) | `Logs/Blockout/play-report-v3-1080b.txt`, `Logs/Blockout/res/v3-1080b/` |

Found in the 4:3 run and fixed: the chapter chip showed over the title screen and over the big chapter card (`HudButton`
wrote its CanvasGroup alpha every frame, undoing the banner's fade; the same fight undid the 45 % dim on unchosen prediction
choices) → it now writes alpha only while dimming; the title ran across Ohm → block moved low-left over the floor, display 132;
"Hold to talk" clipped in the Ask card → wider mic button; the send arrow showed next to "Stop" → hidden; the two-column table
sat left in its card → centred unless a row note needs the room; the case-closed V / I / R tiles had uneven heights → equal.
Found in the 1080 frames and fixed: Chapter 10 wrote "V = I R" (no ×) on the formula card, right after the learner built
"V = I × R" with the tiles → both beats now read "V = I × R" (Scene 13 rebuilt, Chapter 10 replayed: every line and the gate
pass; the run's only FAIL was the harness failing to save its first screenshot).
Not fixed: at 4:3 the finale camera crops Wattson at the left edge (the shot is composed for 16:9).
Snapshot sheets of this build: `Logs/Blockout/snapshots/screens.jpg`, `Logs/Blockout/snapshots/chapters.jpg`.

## Run 1 — gameplay

- Four watched chapters now ask for one action each (gates in the builders, handlers in `SceneDirector.Gates.cs`):
  Chapter 2 tap the + terminal to apply the voltage (the drift follows), Chapter 8 tap the graph to draw the line,
  Chapter 9 swap in nichrome yourself (copper shows alone first), Chapter 10 tap V, I, R to build V = I × R (wrong tile:
  "Start with V, the push."). 8 of 10 chapters are now interactive; 4 and 5 stay cinematic by design.
- Every turn states the step ("STEP 2 OF 3", "3 OF 5"), a hint, and confirms itself (Done card with a chime): "Ammeter placed,
  in series.", "Row 3 recorded: 3.0 V, 0.60 A.", "Prediction locked in.", "Voltage applied.", "Line drawn.", "That's the law."
- Enter acts on the current turn (taps its target); autoAnswer covers every new gate.

## Run 2 — UI (approved design implemented)

Control capsule with idle fold and disabled-tap hints; centred captions that hug the line; chapter banner (big card in
cinematic chapters, titled chip in activity chapters) with the 10-step rail; Turn and Clue cards; colour roles (spark for
guidance; V / I / R only for quantities, also in clue text via `LessonPanel.Quantities`); voltage steps with done / now /
next / later; table and graph as cards with hairlines; readings card steps aside while the table is up; Case Closed card;
pause menu with chapter list (locked until reached, replay returns you to where you were), settings (captions, larger text,
reduced motion, volumes) and inline-confirmed exit; title screen with Start / Continue; Ask Wattson card with plain status
and labelled buttons; `HudScale` (75 % floor, Larger text +15 %); `HudKeys` (Space, Esc, R, C, Enter).
Found while testing: cards read through in linear colour space even at 94–97 % (UI alpha blends in linear light: 3 % of a white
poster shows as ~20 % grey) → cards opaque; the one-line caption box was shorter than the font's line box (text paged out).

## Run 3 — cinematic

- Chapter 2: a push-in to a medium on the conduit as the learner applies the voltage (cause → effect), focus on the stream,
  back wide for the count. Chapter openings: the chapter title over the first shot. Finale: the case-closed card keeps the normal
  dock width so the shot is not pushed left (Wattson was cropped). New UI sounds: done (rising fifth), hint, stamp.
- Not done: Scenes 7 and 8 (hand-edited in the editor) were not rebuilt, so their cameras and the bright lamp in Scene 7's close
  shot are unchanged; no new VFX.

## Run 4 — QA

- `AudioBus` (uncommitted BGM change): only a music bus with a clip touches loop / play-on-awake (voice lines played with Play()
  looped before), default gain back to 1 (rebuilt scenes would have been silent), the editor auto-assign that stamped bgm.mp3
  onto every bus removed. Scene 4's music bus keeps the clip.
- Learner builds open on the title screen (the reviewers' scene list stays in the editor and development builds).
- The URP `ReflectionProbeManager.UpdateGpuData` NullReference appears while scenes are rebuilt (probe bakes), not in play.
- Not done: Tab focus navigation with a visible ring; a device, touch or WebGL pass; a player build; a human listen.

## Scores (honest; target 9)

| Area | Score |
|---|---|
| Gameplay | 7.5 |
| Engagement | 7.5 |
| UI/UX | 8.5 |
| Visual quality | 8 |
| Cinematography | 7 |
| Camera movement | 7 |
| Audio | 7 |
| User friendliness | 8.5 |
| Educational effectiveness | 8.5 |
| Technical stability | 8.5 |
| **Overall** | **7.8** |

Final board: Gameplay NEEDS IMPROVEMENT (long VO stretches in Chapters 9 and 12, two passive chapters); UI/UX NEEDS
IMPROVEMENT until the last fixes are seen in the editor and Tab focus exists; Cinematic NEEDS IMPROVEMENT (Scenes 7/8 untouched,
no new VFX, TTS voice); QA NEEDS IMPROVEMENT (no device or build pass).
