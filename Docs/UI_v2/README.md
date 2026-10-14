# OhmsLaw UI v2 — Figma design review

Status: awaiting design approval. Unity UI integration has not started.

Corrective luminous pass (12 October 2026): the existing experiment and reusable components now have layered gradients, cyan rims, soft halos and illuminated buttons. See [Before / After / Target](https://www.figma.com/design/63sTcmNn0TYOn4lrxAgJgq?node-id=37-2) and [luminous handoff](LUMINOUS_HANDOFF.md). The component-sheet link below now opens a compact index of eight readable independent sections. Read `polish-state.json` for the latest IDs/styles and `polish-validation.json` for this pass's checks. Earlier flat-style contrast results do not validate the new gradients.

[Editable Figma file](https://www.figma.com/design/63sTcmNn0TYOn4lrxAgJgq)

- [Independent component sheet](https://www.figma.com/design/63sTcmNn0TYOn4lrxAgJgq?node-id=3-597)
- [Desktop experiment](https://www.figma.com/design/63sTcmNn0TYOn4lrxAgJgq?node-id=5-2489)
- [Landscape tablet experiment](https://www.figma.com/design/63sTcmNn0TYOn4lrxAgJgq?node-id=7-2518)
- [Desktop prototype start](https://www.figma.com/proto/63sTcmNn0TYOn4lrxAgJgq?node-id=5-1954)
- [Tablet prototype start](https://www.figma.com/proto/63sTcmNn0TYOn4lrxAgJgq?node-id=7-1931)
- [Scene migration map](https://www.figma.com/design/63sTcmNn0TYOn4lrxAgJgq?node-id=14-3459)
- [Unity handoff](https://www.figma.com/design/63sTcmNn0TYOn4lrxAgJgq?node-id=11-2)

## Deliverables

Six named Figma pages; 33 desktop and 34 landscape tablet learner/utility screens; three UI-only desktop views; compact desktop 1280×720, target tablet 1280×800, and long-caption checks. The library contains 15 editable outline icons, 11 component sets with interaction states, tablet components, semantic colour aliases, layout tokens, type styles, acrylic/solid opacity modes and a panel surface asset. Screens consist of editable text, native shapes and component instances. Only discrete character/environment assets use raster fills.

The supplied main UI sheet is stored on the references page. Wattson is the existing star detective portrait, not the illustrative Nova robot. UI-free lab and conductor backgrounds come from existing export previews. Those previews illustrate composition; they are not newly captured Unity frames. Historical cinematic screens reuse a context preview and require validation over actual historical shots during integration.

`migration-inventory.csv` maps 29 actual scene/shared UI entries. `serialized-ui-audit.json` includes 44 serialized scenes/prefabs, inactive UI objects and 61 runtime-construction scripts with callback evidence. Diagnostic/sample scenes are explicitly identified rather than silently removed.

`Assets/` contains 15 SVG icons and `PanelSurface.svg/png`; the panel is suitable for a 16 px nine-slice border. All final labels remain editable/runtime text. `Fonts/` contains the actual Google Fonts files, OFL licences, source URLs and cmap evidence. Inter and Space Grotesk support Ω but lack ∝; retain the existing DejaVu scientific-symbol fallback. Only English glyph coverage is scoped here.

`design-tokens.json`, `figma-state.json`, and `handoff-export-bundle.json` retain token values, aliases, styles, node IDs and phase evidence. Figma opacity variables use percentage values (96/100), whereas Unity normalized alpha should use 0.96/1.0.

## Interaction and content

Prototype links cover lesson progression, quiz retry, pause/settings navigation, back actions, loading recovery and the tablet graph/measurements tab. The prototype demonstrates navigation, not the live circuit simulation. Some supporting controls are specification-only; exact voice, saved-data and runtime gating behaviour must be connected in Unity.

Question examples use existing `Scene06Prediction` and `Scene08GraphQuestion` assets. No curriculum asset has been rewritten. The neutral OhmsLaw prediction and assessed AdaptiveLearning prediction retain their existing distinct controller behaviour after integration.

Examples use 1–5 V, 0.20–1.00 A and 5 Ω, satisfying V = I × R. Both graph axes and slope explanation use V vertically and I horizontally. The old README's three-reading/10 Ω description and some old scene-sequence notes disagree with current shared data and graph code. Treat these as content audit findings, and inspect serialized circuit/controller values before binding each activity. `ResistanceGraphView` also carries practice defaults; saved observations must take precedence.

The primary fill is darker than the starting #3682F6 token to meet normal-text contrast. Electric blue remains an accent/large voltage-readout colour. Supporting details collapse on tablets so they do not cover the source dial, terminals or scales. No blur is required.

## Design validation

- 18 contrast checks across white, warm, black and bright backgrounds in sRGB and linear-light compositing; minimum ratio 5.42:1 for tested normal text/primary actions.
- Font cmaps inspected directly; scientific fallback verified.
- Sample values and graph slope relationship asserted.
- Final bounds audit and screenshots cover desktop/tablet compositions, component sheet, foundations, handoff, compact/target resolutions and long captions.
- Prototype reactions and native layer types inspected through Figma. Interactive prototype clicking by a human and accessibility assistive-technology behaviour remain untested.

Run `python Docs/UI_v2/validate_design.py` for the export checks. Figma construction scripts are provenance, not a Unity migration utility: do not rerun them on populated pages without reading the ledger and checking their preconditions.

## Next checkpoint

The attached brief requires one design approval before Unity UI replacement. After approval, integrate into the existing uGUI frameworks scene by scene. Preserve controllers, GUIDs, hand-edited scenes, simulation, narration and saved profiles. Keep legacy assets until equivalent behaviour and visual match pass. The three Unity validation reports below are explicitly pending.
