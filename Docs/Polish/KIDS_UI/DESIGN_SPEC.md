# Ohm's Law — Kids UI design system (Figma first)

Status: **design phase, awaiting approval in Figma. No Unity UI has been changed.**
Figma file: https://www.figma.com/design/63sTcmNn0TYOn4lrxAgJgq (the existing UI v2 file, updated in place).

Visual authority: the user's sheet "Ohm's Law — Gameplay-matched Kids UI" (2026-10-13). It sets the style: glass navy
panels, cyan edge glow, blue bloom, glossy amber actions, rounded icon tiles, Nunito-rounded type.
Usability authority: UI UX Pro Max (`--design-system "kids educational science game glassmorphism dark"` and the
domain searches listed at the end). It sets hierarchy, accessibility, states, touch targets, layout and motion.
Content authority: the game itself. Every text line, value and question in the mockups comes from the current scenes
(`SceneNNBuilder`, `SceneDirector.Gates`), not from the reference sheet.

## 1. Tokens

Primitives come from the reference palette. Semantic tokens are what components bind to. In Figma these are the existing
`OHM / Primitives` and `OHM / Semantic` collections, updated in place; the `Acrylic` mode is glass and `Solid` is the
opaque fallback for low-end tablets.

| Primitive | Hex | Semantic use |
|---|---|---|
| Deep Navy | `#0B1626` | `color/bg`, `color/on-action` (ink on bright fills), scrim base |
| Panel Navy | `#1A2B45` | `color/surface` (glass at 84 % in Acrylic, 100 % in Solid) |
| Surface | `#2E3B55` | `color/raised` (inputs, value wells, inactive tabs) |
| Electric Blue | `#00D1FF` | `color/accent`, `color/border`, the voltage quantity, progress fill |
| Cyan Glow | `#7EE9FF` | `color/focus`, glow highlights, the current-reading in the experiment card |
| Warm Amber | `#FFB347` | `color/action` (the main button on every screen) |
| Gold | `#FFD166` | `color/action-hover`, step numbers, achievements, the current quantity |
| Success | `#2ECC71` | `color/success` |
| Error | `#FF6B6B` | `color/error` |
| Purple | `#A78BFA` | `color/achievement` |
| Text | `#F4FAFF` | `color/text`, `color/text-strong` |
| Text muted | `#B9CBE3` | `color/text-muted` |
| Coral | `#FF8C7A` | the resistance quantity |

The game's colours for quantities stay: **V blue, I gold, R coral**, matching the 3D words and glows in Scenes 4, 10 and
12. Colour is never the only signal: each quantity carries its letter and unit, and each state carries an icon and a
word.

### Contrast (WCAG 2.2, checked with glass at 84 % over black and over white)

| Pair | Over dark scene | Over bright scene | Rule |
|---|---|---|---|
| Text `#F4FAFF` on glass | 14.8 | 8.4 | body text |
| Muted `#B9CBE3` on glass | 9.4 | 5.4 | secondary text |
| Blue, Cyan, Gold, Amber on glass | 8.6–11.1 | 4.9–6.3 | values and labels |
| Success on glass | 7.4 | 4.2 | text at 24 px bold or larger only |
| Error, Purple, Coral on glass | 5.6–6.9 | 3.2–3.9 | large text only, or on their own tinted card at 92 % |
| Ink `#0B1626` on Amber, Gold, Green, Red, Purple, Blue fills | 6.5–12.6 | — | **every filled button** |
| White on those fills | 1.4–2.8 | — | **never** (the reference's white labels on red/purple are changed to ink) |

## 2. Type

The reference asks for "Nunito Rounded (or similar)". UI UX Pro Max's match for a children's educational game is
**Fredoka** for headings and numbers, with **Nunito** for body text. Both are on Google Fonts (OFL), and Figma has both.
Unity needs the two TTFs added to TextMesh Pro after approval; the project has only Nunito Sans Bold today.
Keep the DejaVu fallback for `Ω ∝ Δ`.

Desktop sizes are for the 1920×1080 reference canvas. Tablet sizes are for 1024×768 points.

| Style | Font | Desktop | Tablet | Use |
|---|---|---|---|---|
| Display | Fredoka Bold | 72/80 | 48/56 | title screen, Case Closed |
| Title | Fredoka SemiBold | 40/48 | 28/34 | pop-up and card titles, hero formulas (×2 inside the hero) |
| Heading | Fredoka SemiBold | 28/34 | 22/28 | panel titles |
| Readout | Fredoka SemiBold, tabular | 56/60 | 36/40 | meter values (`0.60 A`) |
| Body | Nunito Bold | 22/32 | 17/24 | instructions, dialogue, questions |
| Button | Nunito ExtraBold | 22/28 | 18/22 | button labels |
| Label | Nunito ExtraBold, +6 % tracking, caps | 15/20 | 13/16 | eyebrows (`YOUR TURN`, `QUICK CHECK`) |
| Caption | Nunito Bold | 17/24 | 14/20 | helper text, axis labels, legend |

No text smaller than 14 px on tablet or 15 px on desktop. Line length is at most 60 characters in dialogue panels.

## 3. Space, shape, depth

- **Spacing:** 4, 8, 12, 16, 24, 32, 48, 64. Card padding is 24 on desktop and 16 on tablet. The safe margin is 32 on
  desktop and 24 on tablet (plus `Screen.safeArea`).
- **Radius:** 12 for chips and wells, 16 for buttons and options, 20 for cards, 28 for pop-ups, and 999 for pills and
  progress.
- **Glass panel:**
  - Fill: a linear gradient from `#1F3456` at 88 % to `#0E1B30` at 84 %.
  - Border: a 1.5 px stroke in Electric Blue at 55 %.
  - Inner highlight: 1 px white at 22 % along the top edge.
  - Edge glow: Electric Blue, blur 18, at 35 %.
  - Depth: a soft shadow, y 12, blur 32, black at 45 %.
  - **No background blur.** Unity's overlay canvas does not blur cheaply; 84 % glass keeps text legible over the
    brightest lab frame.
- **Action button (amber):** a vertical gradient from Gold `#FFD166` through Amber `#FFB347` to `#F39A1E`, with an inner
  top highlight, an inner bottom shade, an amber glow (blur 20, 55 %), and ink text.
- **Effect styles:**
  - `Kids/Glow Cyan Edge`
  - `Kids/Bloom Blue`
  - `Kids/Glow Amber`
  - `Kids/Glow Success`
  - `Kids/Glow Error`
  - `Kids/Glow Purple`
  - `Kids/Glow Gold`
  - `Kids/Depth Soft`
  - `Kids/Focus Ring`
  - `Kids/Inner Highlight`

## 4. Components (`Kids /` on the components page; each a variant set with Auto Layout)

| Component | Variants and properties | Unity owner after approval |
|---|---|---|
| Button | Kind: Primary (amber), Secondary (glass), Success, Danger, Achievement. State: Default, Hover, Pressed, Focus, Disabled. Label text, optional icon | `HudKit` button builder, `LessonPanel`, `MCQPanel`, `HudScreens` |
| Icon Button | State: Default, Hover, Pressed, Active, Disabled. Icon swap, optional label | `ModuleFlow` toolbar (Pause, Replay, Captions, Volume, Skip) |
| Nav Tile | Selected: true or false. Icon swap, label | `HudScreens` chapters, pause and end menus |
| Lesson Bar | Module title, chapter, step `n of m`, progress | `ModuleFlow` progress pill |
| Objective Card | Kind: Objective, Try This, Done | `LessonPanel` "Your turn" card |
| Dialogue Panel | Size: Desktop, Tablet. Speaker, line, portrait | `SubtitleUI` (captions) plus the Ask Wattson portrait |
| Voltage Control | State: Default, Focus, Disabled. Value, steps 0–5 V | `LessonPanel` VoltStrip slider |
| Reading | Quantity: Voltage, Current, Resistance. Value, delta | `LessonPanel` top readings, `LiveReadingsPanel` |
| Checklist Item | State: Done, Current, To do | new: `LessonPanel` Scene 10 steps |
| Quiz Option | State: Default, Hover, Selected, Correct, Wrong, Disabled. Letter and answer | `MCQPanel` options |
| Question Header | Question `n of m`, dots | `MCQPanel` |
| Feedback Card | Kind: Correct, Well Done, Try Again, Hint, Achievement, Module Complete | `MCQPanel` result, `LessonPanel` Done, end card |
| Tabs | Selected: Table, Graph, Analysis | new: Scene 11 data view |
| Data Table | Rows with State: Default, New, Highlight | `ObservationTableUI` |
| Graph Card | V–I plot, points, line | `GraphUI` |
| Formula Card | `V = I × R` plus legend | `LessonPanel` hero and stage |
| Material Card | Material: Copper, Nichrome. Facts, `R ≈` chip | `LessonPanel` Popup3D (copper vs nichrome) |
| Hero Card | Big formula and a caption | `LessonPanel` hero |
| Ask Wattson | State: Default, Hover, Listening | Ask Wattson button |
| Title Card | Start Journey | `HudScreens` title |

Every interactive component has visible Default, Hover, Pressed, Focus (a 3 px Cyan Glow ring) and Disabled states
(40 % opacity, no glow, no action). Touch targets are at least 48×48 on tablet and 56 tall for main actions, with at
least 8 between targets.

## 5. Screens (desktop 1920×1080 and tablet 1024×768, real gameplay frames, HUD hidden)

| # | Mockup | Scene and frame | UI on it |
|---|---|---|---|
| 1 | Historical room | Scene 4, Wattson meets Ohm | Lesson Bar, Dialogue, Objective "Tap the glowing wire", toolbar |
| 2 | Electric conductor | Scene 5/6, inside the wire | Lesson Bar, Reading (current), charge counter card, Dialogue |
| 3 | Classroom experiment | Scene 10, the lab bench | Voltage Control, Reading (current), Record Reading, checklist, Dialogue |
| 4 | Data and graph | Scene 11 | Tabs, Data Table, Graph, Formula, analysis note |
| 5 | Copper vs nichrome | Scene 12, the pop-up | Material Cards with VS, Quick Check |
| 6 | Quiz | Scene 6, the adaptive question | Question Header, 3 options with one Selected, then Correct feedback |
| 7 | Completion | Scene 13, Case Closed | Module Complete, Achievement, summary of V, I and R, replay and exit |

**Before / After:** the same moment captured with the current v5 HUD and with the new UI, side by side, for each of the
seven screens.

## 6. Behaviour rules (UI UX Pro Max)

- **Progress:** always shows "Step n of m" and the chapter name. A dialogue panel never covers the apparatus being
  talked about; panels dock to the screen edges and the centre stays clear for the 3D scene.
- **Instructions:** one primary action per screen, in amber. Secondary actions are glass.
- **Feedback:**
  - **Success:** green with a check, a short reason, and the next step.
  - **Wrong:** red with a cross, a hint, and a "Try again" that keeps the child's answer visible.
- **Disabled controls:** 40 % opacity, no glow, and they say why (the helper text under the control).
- **Motion:**
  - Buttons 140 ms.
  - Panels 220 ms, easing out.
  - Exits faster than entries.
  - One or two animated elements per view.
  - Reduced motion uses fades only.
  - All UI animation runs on unscaled time, so pause works.
- **Charts:** direct labels on points, axis titles with units, the line plus point markers (not colour alone), and the
  table next to the graph as the text alternative.
- **Icons:** one vector family on a 24 grid, 2 px rounded strokes, set on rounded-square tiles. No emoji.
- **Captions:** on by default. Dialogue is at most two lines on desktop and three on tablet.

## 7. Open decisions for approval

1. **Graph axes:** the reference plots I on the vertical axis. The game plots V up and I across, because Chapter 9's
   narration says "the slope is the resistance". **Keep the game's axes.**
2. **Button text:** dark ink on every bright button instead of the reference's white on red and purple (contrast).
3. **Background blur:** none. Glass at 84 % is legible over the brightest lab frame.
4. **Fonts:** add Fredoka and Nunito (OFL) to the Unity project.
5. **Wattson:** stays the game's star detective, not the curly-haired boy on the sheet. That boy is Ohm in the game.

## UI UX Pro Max queries used

- `--design-system "kids educational science game glassmorphism dark"`: Glassmorphism, Fredoka + Nunito.
- `ux`:
  - touch target size
  - contrast
  - progress indicator steps
  - reduced motion
  - disabled state
  - success and error feedback
- `chart`: line chart accessibility.
- `references/pro-rules.md`: the pre-delivery checklist (icons, states, token-driven colour, 8 px rhythm, scrims).
