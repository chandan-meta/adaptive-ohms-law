# Corrective luminous finish — 12 October 2026

Status: corrected Figma styling ready for design review. Unity integration remains pending approval.

[Corrected experiment](https://www.figma.com/design/63sTcmNn0TYOn4lrxAgJgq?node-id=5-2489) · [Before / After / Target](https://www.figma.com/design/63sTcmNn0TYOn4lrxAgJgq?node-id=37-2) · [Compact component index](https://www.figma.com/design/63sTcmNn0TYOn4lrxAgJgq?node-id=3-597)

## What changed in Figma

Shared component mains now use editable navy/blue gradient paints, cyan strokes, a narrow cyan halo, a wider soft blue halo, restrained depth/inner shadows, and separate upper reflection and edge-highlight shapes. Paint and effect styles are reusable and exported in `polish-state.json`. Native text and icons remain above the decoration; no layer blur is applied to body text or readouts.

Measurements have blue, cyan or amber surfaces and rims rather than only coloured numbers. Primary buttons have illuminated blue gradients; hover has stronger edge/halo, pressed has a recessed dark surface, focus has a separate perimeter, disabled removes decorative glow, and selected voltage has a check mark. Quiz selected/focus/disabled cues are preserved. Graph illumination is limited to the line and selected point; grid/ticks remain sharp.

The linked screen's 24 original text values—including 3.00 V, 0.60 A, 5 Ω, selected 3 V, progress and Wattson dialogue—match the frozen baseline exactly. The gameplay image hash is identical. Background image opacity changed from 0.78 to 1 and the full-frame tint from 0.14 to 0.04. Modal scrims remain in place. Record reading sits closer to the voltage panel.

Eight independent neutral-background review sheets replace the tall combined-strip presentation; the old catalogue remains archived. The before-state is an archival frozen clone; live production components and corrected screen instances remain linked. Target crops are reference material only.

The file's available shader list was empty. This is an exportable glow-layer treatment, not shader Bloom or Figma Glass. No full-frame blur, Glass/background-blur combination, or environment asset alteration was used.

## Unity mapping after approval

The actual project is Unity 6000.3.0f1, URP 17.3.0 and uGUI 2.0. Both module HUD and AdaptiveLearning scenes use overlay UI. Retain that architecture and existing controllers/bindings. Scene-camera bloom cannot provide this overlay finish by itself.

Use an existing/native uGUI hierarchy per card: decorative halo Image behind the panel; nine-sliced surface Image; optional reflection Image; crisp border; editable text/icons/controls above. Decorative Images have `raycastTarget=false` and must not introduce extra EventSystems. Keep masks on inner scrolling/reflection content, not around outer halos.

`FX/PanelSurface.png` is a 128×128 surface with a 16 px corner. Import as a Sprite with a 16 px slicing border. `PanelCyanHalo`, `PanelBlueHalo`, and `PanelAmberHalo` are 256×256 RGBA assets containing a 128×128 core with 64 px transparent padding. Their corresponding nine-slice corner region is 80 px (64 padding + 16 corner); position the expanded halo RectTransform 64 reference pixels outside the core. Verify corners/halo falloff at the approved sizes.

`FX/ButtonSurface.png` is 200×56 with a 12 px corner; use a 12 px slicing border. `ButtonHalo.png` is 328×184 with a 200×56 core and 64 px padding; use a 76 px corner region (64 + 12). `PanelReflection.png` is a separate translucent upper reflection. Keep text entirely out of all texture exports.

The PNGs carry their own alpha. Use white Image tint/alpha 1 rather than multiplying the surface's translucency again through an always-dim CanvasGroup. Parent CanvasGroups should control intended transition/pause state only. Exported SVGs from the earlier phase are not assumed to preserve the new blur/halo appearance; use the verified RGBA assets for these effects.

Animate existing button/panel state using unscaled time: approximately 140 ms button feedback and 220 ms panel transitions. Use sprite/alpha changes for default, hover, pressed, focus and disabled states; keep input immediately responsive. No continuously animated glow is required.

For lower-cost tablets, use the solid fallback paint/panel, remove broad halos and sheen from secondary panels, and retain one primary/selected glow. Reuse an atlas/material and avoid runtime background blur or per-panel offscreen rendering. Physical-device performance must be measured after integration; no device result is inferred from Figma.

## Validation and remaining differences

`polish-validation.json` confirms exact experiment text/content preservation, identical background asset, zero foreground overflow/overlap in the desktop/tablet screen audits, no text layer blur, and four halo exports with completely transparent outer edges. Rendered primary label gradient bands meet 4.5:1; minimum sampled ratio is 4.528:1 across enabled states.

The corrected finish is visibly closer to the reference but is not pixel-perfect. The project retains its actual laboratory, Wattson portrait, larger vertical measurement layout and existing typography/spacing. Primary centre colours are controlled for white-label contrast. Drop-shadow halos approximate the reference's bloom-like light; no optical/shader bloom or refraction is claimed. The target crops are enlarged low-resolution reference pixels, while the corrected UI stays editable and sharp.

Unity visual comparison, complete gameplay flow, assistive-technology behaviour, native touch, and device performance remain unexecuted. Obtain the requested Figma design approval before replacing Unity UI.
