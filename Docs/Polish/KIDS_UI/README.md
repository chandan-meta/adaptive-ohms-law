# Kids UI: Figma first

**Status:** the Figma build is ready to run in the existing file. Unity UI is untouched until the Figma design is approved.

## What runs in Figma

The build runs in https://www.figma.com/design/63sTcmNn0TYOn4lrxAgJgq and updates that file in place.

- **Tokens:**
  - `OHM / Primitives`, `OHM / Semantic` (Acrylic and Solid modes) and `OHM / Layout and Motion` are updated to the
    reference palette.
  - New tokens added: gold, purple, coral, amber-deep, tinted surfaces, radius 20/28/pill and touch sizes.
- **Text styles:** the `OHM/…` styles are restyled to Fredoka (headings and numbers) and Nunito (body). Tablet sizes are
  added.
- **Paint and effect styles:** `Kids/…` covers glass, tinted cards, button fills with Hover and Pressed versions, glows,
  bevel, focus, bloom and text glow. The UI v2 `OHM/FX/…` luminous styles are restyled to match, so the older v2 screens
  follow the new look.
- **Page "Kids UI · Design System":**
  - The Foundations board.
  - 34 icons.
  - 26 component sets, 257 variants, all Auto Layout:
    - Icon Tile, Button, Icon Button, Nav Tile, Glass Panel, Quiz Option, Checklist Item, Tabs, Reading
    - VS Badge, Lesson Bar, Toolbar, Objective Card, Dialogue, Voltage Control, Checklist, Question Card
    - Feedback Card, Table Row, Data Table, Graph, Formula, Material Card, Hero, Counter Card, Ask Wattson, Title Card
  - The independent component sheet, with the reference's 12 sections plus 2 more.
- **Page "Kids UI · Screens":**
  - 7 desktop (1920×1080) and 7 tablet (1024×768) mockups on HUD-free gameplay frames: the historical room, the
    conductor, the classroom experiment, data and graph, copper vs nichrome, the quiz, and completion.
  - 7 before/after pairs: the same frame with the current v5 HUD.
  - The reference sheet as the target.

The design rules are in [DESIGN_SPEC.md](DESIGN_SPEC.md), and [reference.png](reference.png) is the visual authority.

## Two ways to run it

**A · Development plugin (no MCP, no call limits)**

1. Optionally, run `python Docs/Polish/KIDS_UI/figma/receiver.py`. The plugin then sends its audit, build log and PNG
   renders to `Logs/UI_V5/figma/` for checking.
2. In the Figma **desktop app**, open the file.
3. Go to **Plugins → Development → Import plugin from manifest…** and pick `Docs/Polish/KIDS_UI/figma/plugin/manifest.json`.
4. Run **Ohm's Law Kids UI**. Click **1 · Audit this file**, then **2 · Build Kids UI** (about a minute).

**B · Figma connector (`use_figma`)**

1. Sign in once: `/mcp` → plugin:figma:figma → Authenticate.
2. Upload `figma/images/*.jpg` with `upload_assets`.
3. Run `build/mcp/1_foundations.js` … `6_sheet.js` in order, with `__IMG_HASHES__` replaced by the returned hashes. Each
   call is under 50 000 characters.

Re-running either path is safe:

- Tokens and styles are updated by name.
- Each part removes only what it made: shared plugin data `kidsui/part` and its board names.

## Source

The source is in `figma/`:

| File | Contents |
|---|---|
| `lib.js` | helpers |
| `1_foundations.js` | tokens, styles, foundations board |
| `2_icons.js` | icons |
| `3a_controls.js`, `3b_panels.js` | component sets |
| `4_screens.js` | mockups |
| `5_before_after.js` | before/after |
| `6_sheet.js` | component sheet |

Tooling in `figma/`:

- `python build.py` makes `plugin/`, `build/mcp/` and `images/`.
- `node mock.js` runs the whole build against a strict stand-in for the Figma API. It catches unloaded fonts, misused
  Fill/Hug/Absolute, malformed paints and effects, unknown variables, styles and properties, and edits inside instances.
  It also checks every mockup for off-frame or overlapping UI.

Gameplay frames are the play test's `clean` captures in `Logs/UI_V5/backdrops/` (desktop, tablet, and the "before"
frames with the HUD).

## After approval (not started)

1. Add Fredoka and Nunito to TextMesh Pro.
2. Map each component to its Unity owner (the table in DESIGN_SPEC §4).
3. Replace all UI in `LessonPanel`, `HudKit`, `HudScreens`, `PanelViews`, `LiveReadingsPanel`, `MCQPanel`, `SubtitleUI`,
   `GraphUI`, `ObservationTableUI` and the Ask Wattson UI.
4. Verify with the play test at 1080p, 16:10, 2048×1536 and 1366×768 against the Figma frames.
