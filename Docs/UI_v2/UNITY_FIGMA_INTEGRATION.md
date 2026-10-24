# Unity integration of Figma artwork — 24 October 2026

The existing Unity interface now uses **43 PNG exports directly from [OhmsLaw_UI_v2](https://www.figma.com/design/63sTcmNn0TYOn4lrxAgJgq/OhmsLaw_UI_v2?node-id=65-3)**, file `63sTcmNn0TYOn4lrxAgJgq`, in the Amber & Ink theme. `unity-figma-assets.json` records each source Figma node, exported filename and dimensions.

The PNGs live in `Assets/OhmsLaw/Shared/Resources/FigmaUI/`; runtime loading uses `Resources.Load<Sprite>("FigmaUI/<name>")` through `FigmaSkin`. Shared cards, button states, measurement surfaces, icons and slider artwork use these exports. Their colors are preserved with white Image tint. Resizable surfaces use sliced sprites; icons and title artwork use unsliced sprites. The importer preserves source alpha, sRGB, FullRect geometry and original dimensions up to 4096px, with bilinear filtering and no mipmaps or compression.

The start frame uses `TitleBanner.png`, `TitleBannerTablet.png`, `TitleStart.png`, `TitleStartTablet.png` and `TitleShade.png` over the live Unity environment. Static banner lettering comes from Figma. Start remains a real Unity Button. Prompts, narration captions, measurements, tables, graphs and other changing text retain their existing live bindings and interactions. Inter and Space Grotesk load from `FigmaUI/Fonts/`, with the module font retained for scientific glyph fallback, including Ω.

To update the artwork, re-export the corresponding Figma node to the **same PNG filename** and preserve its existing `.meta` file. Update the provenance manifest if the source node or dimensions change, then reimport and run these editor menu entries:

- `Ohm's Law/Figma/Configure and verify exported sprites`
- `Ohm's Law/Tests/Figma imported skin`
- `Ohm's Law/Figma/Run integration review (preserve preferences)`

Import verification and **87 runtime binding checks passed**, including selected/off/disabled controls and desktop/tablet banner selection. The full gameplay harness also **passed**, including HUD controls, synthetic mouse/touch input, all lesson scenes and the free lab. That run's evidence is preserved in `Logs/FigmaUI/Gameplay-full/`. The final Scene10–13 gameplay check after the toggle correction also **passed**, with evidence in `Logs/FigmaUI/Gameplay-final/`. Import, binding and integration results are in `Logs/FigmaUI/import-verification.txt`, `runtime-binding-checks.txt` and `integration-review.txt`. The integration runner restored the specified learner preferences; the previous Blockout and pointer reports were preserved and restored.

The corrected `Logs/FigmaUI/Unity-experiment.png` and `Unity-finale.png` previews have been visually checked: exported colors, selected captions, panel placement and controls render correctly over the actual 3D classroom. The capture appends Unity's unchanged ScreenSpaceOverlay UI after world postprocessing, matching the runtime order; it does not move the canvas or pass it through the scene's grading. Older `Camera-route-*` captures in the full-run archive are explicitly marked invalid for visual acceptance.

Active secondary controls reuse the exported gold primary surface and switch their label/icon to ink; off controls restore the exported secondary surface and ivory foreground. Figma's current normal/hover/pressed secondary surfaces are identical, so pointer feedback also retains the existing scale animation and exported keyboard focus perimeter.

Existing scenes were never rebuilt or saved for this integration. Device builds, physical touch hardware and device performance remain untested.
