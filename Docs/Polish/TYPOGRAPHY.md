## 1. TextMeshPro world-size formula (derivation from the package source)

Source: `Library/PackageCache/com.unity.ugui@4ee0fac0c6e4/Runtime/TMP/`

- `TextMeshPro.cs:2232-2235` (3D) and `TextMeshProUGUI.cs:2581-2584` (UGUI) are identical:
  ```
  float orthographicMultiplier = m_isOrthographic ? 1 : 0.1f;
  float baseScale = m_fontSize / m_fontAsset.m_FaceInfo.pointSize * m_fontAsset.m_FaceInfo.scale * orthographicMultiplier;
  float currentEmScale = m_fontSize * 0.01f * orthographicMultiplier;   // unit for lineSpacing/charSpacing/paragraphSpacing (em/100)
  ```
- Every glyph is placed with `currentElementScale = baseScale * m_fontScaleMultiplier (1, except sub/superscript) * character.m_Scale (1) * glyph.scale (1)` (`TextMeshPro.cs:2553, 2618`).
- Line advance (`TextMeshPro.cs:2299, 3226, 3993`): `lineGap = faceInfo.lineHeight - (ascentLine - descentLine)`; new-line offset = `(ascender - descender + lineGap) * baseScale + lineSpacing * currentEmScale` = `faceInfo.lineHeight * baseScale` when `lineSpacing = 0`.
- The only difference between the two components: `TMP_Text.cs:1002` defaults `m_isOrthographic = false` (3D TextMeshPro → factor 0.1), and `TextMeshProUGUI.cs:802` forces `m_isOrthographic = true` in Awake (factor 1, units = canvas pixels). Nothing compensates for `transform.lossyScale`; the mesh is in local units and the Transform scales it.

**Nunito SDF face** (`Assets/OhmsLaw/Shared/UI/Nunito SDF.asset`, `m_FaceInfo`): pointSize 90, scale 1, unitsPerEM 1000, lineHeight 122.76, ascentLine 90.99, capLine 64, meanLine 44, descentLine −31.77 (note lineHeight == ascent − descent, so lineGap = 0).

With `F = fontSize`, transform scale 1:

| quantity | 3D `TextMeshPro` (metres) | `TextMeshProUGUI` (canvas px) | UGUI on a 0.0015 world canvas (m) |
|---|---|---|---|
| em | 0.1 F | F | 0.0015 F |
| **cap height** | 64/900 F = **0.07111 F** | 0.7111 F | 0.0010667 F |
| x-height | 44/900 F = 0.04889 F | 0.4889 F | 0.000733 F |
| **line advance** (lineSpacing 0) | 122.76/900 F = **0.1364 F** (1.364 em = 1.92 cap) | 1.364 F | 0.002046 F |
| 1 line of textBounds | 0.1364 F | 1.364 F | 0.002046 F |
| lineSpacing unit | 0.001 F per unit | 0.01 F | — |

Inverse: `F = cap_m × 14.0625` (3D) — i.e. `10 × pointSize / capLine`; `F = cap_px × 1.40625` (UGUI). `WorldText.Card` uses `height * 14f` → cap = 0.9956 × height: correct to 0.4%. A 3D fontSize is 66.7× smaller than the UGUI px value that gives the same metres on a 0.0015 canvas.

**Screen projection** (what actually matters): px per metre at depth `d` along the camera axis, vertical FOV φ, 1080 rows = `540 / (d·tan(φ/2))`; a vertical label seen from a camera pitched θ is further shortened by cos θ.

| camera (builder constants) | FOV | px/m | typical read depth | px/m there |
|---|---|---|---|---|
| Bench Close (0.2,1.6,−1.7)→(0,0.95,0), pitch 21° | 38 | 1568/d | props 1.2–2.1 m | 750–1300 (≈1000) |
| Bench Wide (0,2.1,−3.2)→(0,0.9,0) | 42 | 1407/d | bench 3.4 m | 414 |
| Boards (1.4,1.9,−3.4)→(1.4,1.6,0) | 44 | 1337/d | boards 3.6–3.8 m / cards 3.5 m / **kids 1.2 m** | 352–371 / 382 / **1114** |
| Cards S13 (0,2.1,−3.6)→(0,2.2,0.3) | 40 | 1484/d | cards 3.9 m / **kids 1.4 m** | 380 / **1060** |
| Table Close S07 | 40 | 1484/d | 1.5–2.0 m | 740–990 |
| Wire Inside (FOV 60, looks along +X) | 60 | 935/d | VOLTAGE card 3.35 m, CURRENT card 7.7 m, counter 4.85 m, question 6.45 m | 279 / 122 / 193 / 145 |

## 2. Measured vs intended (1920×1080 frames; letter heights measured per-glyph with a pixel scan)

| shot | element | code: cap m → fontSize | depth → px/m | predicted cap px (×cos pitch) | measured | |
|---|---|---|---|---|---|---|
| S08_L02 Bench Close | Prop label "NICHROME WIRE — XY" (SetParent(true) onto rotated, non-uniform cylinder) | 0.06 → 0.84 | 1.55 → 1012 | 56 | H 56, N 55, I 54 | ✓ |
| S08_L02 | Prop label "cells / controllable DC source" | 0.06 → 0.84 | 2.12 → 740 | 41 | x-height 31 → cap ≈ 45 (frame edge) | ✓ (+10%) |
| S08_L02 | Card "X" | 0.12 → 1.68 | 1.55 → 1012 | 113 | 115 | ✓ |
| S10 Set1V | Meter caption "Measures potential difference — V — volt (V)" | 0.05 → 0.70 | 1.19 → 1320 | 61 | V 63 | ✓ |
| S10 Set1V | Path label "Source → Ammeter → …" | 0.08 → 1.12 | 2.38 → 659 | 49 | A 50 | ✓ |
| S10 Set1V | Voltmeter reading "0.0 V" (child with 1/0.28,1/0.18,1/0.2 compensation) | 0.06 → 0.84 | 1.18 → 1333 | 75 | 70–71 | ✓ (−6%) |
| S10 Set1V | Prop label "switch / key" | 0.06 → 0.84 | 1.99 → 788 | 44 | ≈42 | ✓ |
| S10 Set1V | Screen Prompt (UGUI 38) / slider ticks (24) | cap 27 px / 17 px | — | 27 / 17 | 26 / 17 | ✓ |
| S13_L06 Cards | Card "V = IR" | 0.5 → 7.0 | 3.9 → 380 | 189 | 186 | ✓ |
| S13_L06 | Kid name (Capsule label, scale-compensated) | 0.25 → 3.5 | **1.4 → 1060** | 264 | x-height 190 → cap ≈ 276 | ✓ |
| S12_L09 Boards | Card "V / I = CONSTANT" | 0.2 → 2.8 | 3.5 → 382 | 76 | C 77, T 75 | ✓ |
| S12_L09 | Kid names | 0.25 → 3.5 | **1.2 → 1114** | 277 | x-height 197 → cap ≈ 286 | ✓ |
| S12_L09 | Table cells (UGUI 34, canvas 0.0015 → 0.0363 m) / header (26 → 0.0277 m) | | 3.64 → 367 | 13.3 / 10.2 | 12–13 / 10 | ✓ but illegible |
| S12_L09 | Graph ticks & point labels (22 → 0.0235 m) | | 3.78 → 354 | 8.3 | 8–10 | ✓ but illegible |
| S05_L08 Wire Inside | Card CURRENT (3 lines) | 0.3 → 4.2 | 7.67 → 122 | 37 | T 37 | ✓ |
| S05_L08 | Card VOLTAGE (Face()'d, 35° off-axis, cropped by frame) | 0.3 → 4.2 | 3.35 → 279 | 84 | V 71, A 70 | ≈ |
| S05_L08 | Counter "6" / "charges per second" (not Face()'d) | 0.5 / 0.14 | 4.85 → 193 | 97 / 27 | ≈100 tall × 12 wide | ✗ edge-on (78° off the view ray) |

**Verdict:** every rendered size agrees with the code's metre intent within ±6 % (the two outliers are frame-edge perspective). The `×14` constant, the `SetParent(worldPositionStays)` label on the 90°-rotated non-uniform wire, and the `1/scale` compensation on capsule names and meter readings all yield lossyScale = 1. There is no unit bug.

## 3. Root causes of the bad result

1. **Sizes fixed in metres without the depth/FOV conversion.** One constant per category (prop 0.06, caption 0.05, terminal 0.12, name 0.25, cards 0.2–0.9) regardless of shot. Bench shots are 1.2–2.1 m deep at FOV 38 (≈1000 px/m) while card shots are 3.5–4 m at FOV 40–44 (≈380 px/m): a 2.7× difference. So "small" 6 cm labels render at 55–75 px (2–2.7× the 27 px subtitle cap), 12 cm terminals at 115 px, and 25 cm names at 1.2 m depth at 280 px (26 % of the frame). The polished Scene 4 hero words VOLTAGE/CURRENT are fontSize 1.3 = 9.2 cm at 1.84 m/FOV 44 ≈ 67 px: that is the project's reference size for a hero word, and the blockout's incidental labels are already at that size.
2. **NoWrap + 20 m rect + textBounds-sized backing.** 30–50-character labels at 6–8 cm caps are 1.2–1.9 m ribbons (em = 1.4 × cap, ≈0.5 em per char) that cross the whole bench and each other; caption backings become 2 m slabs that occlude the props behind them (S08/S10).
3. **Facing.** Labels face world −Z; the bench cameras are pitched 21° and yawed, so baselines slope and glyphs are ×0.93 shorter; the wire-interior `Counter` is never `Face()`d and the Wire camera looks along +X → "charges per second" is unreadable. The "− terminal" card sits behind the Wire Inside camera (depth −2.7 m).
4. **Blocking.** `KidsAt(z = −2.2)` puts the four capsules 1.2–1.4 m in front of the Boards/Cards cameras (z −3.4/−3.6): 0.9 m-wide capsules and ≈1 m-wide names (5 letters at 0.25 m cap) at 0.55 m pitch overlap each other and cover the boards.
5. **World canvases 3× too small.** 0.0015 scale with 22–34 px fonts = 2.3–3.6 cm caps at 3.6–3.8 m → 8–13 px. Legibility floor on a tablet is ~20 px cap (2.4 mm).
6. **Multi-line concept cards use one size for all lines** (0.3 m × 3 lines × 1.92 cap = 1.7 m tall) and are placed off-axis near the frame edge → cropped (S05 VOLTAGE); inside the wire the VOLTAGE card is at 3.35 m and the CURRENT card at 7.7 m, so the "same" card renders at 84 vs 37 px.
7. Scene 7/9 prose cards wrap at 5 m: at ≈400 px/m that is 2000 px, wider than the frame.

## 4. Proposed typography scale

Target screen cap heights at 1080p (1 reference px ≈ 0.12 mm on a 10" tablet; ergonomic floor ≈ 2.5 mm = 21 px, comfortable primary ≈ 3.5 mm = 30 px):

| tier | cap px | use |
|---|---|---|
| XS | 20 | graph ticks, point coordinates |
| S | 24 | table cells, meter captions, slider ticks, graph axis titles |
| M | 28–30 | prop names, subtitles, prompts |
| L | 36 | terminal X/Y, meter digits, concept sub-lines (Symbol / Unit), question cards |
| XL | 56 | data/formula cards (R = V/I, V âˆ? I, V/I = CONSTANT), V/I/R letter cards |
| XXL | 64 | concept word (VOLTAGE, CURRENT, RESISTANCE) = Scene 4 hero-word size |
| Hero | 100–115 | formula reveal V = IR, Ω |
| Title | 120 | OHM'S LAW |

Conversion: `cap_m = cap_px × 2·d·tan(φ/2) / 1080`, `fontSize = cap_m × 14.0625`.

**World text (3D TextMeshPro, Nunito SDF), sized for the shot in which the text is read:**

| element (where) | shot, px/m | target px | cap m | fontSize | wrap width | now |
|---|---|---|---|---|---|---|
| Prop name label (`Prop`) | Bench Close ≈1000 | 28 | **0.03** | **0.42** | 0.45 m (2 lines) | 0.06 / 0.84 |
| Meter caption (`Meter`) | Bench Close 1320 | 24 | **0.025** | **0.35** | 0.45 m | 0.05 / 0.70 |
| Meter reading digits | Bench Close 1330 | 36 | **0.04** | **0.56** | — | 0.06 / 0.84 |
| Terminal X / Y cards | Bench Close 1012 | 36 | **0.04** | **0.56** | — | 0.12 / 1.68 |
| Path label | Bench Close 659 | 28 | **0.045** | **0.63** | 1.2 m (2 lines) | 0.08 / 1.12 |
| Data/formula cards (CONSTANT, RESISTANCE â€” R, R = V/I, VÃ—2â†’IÃ—2, Slope, 1 Î© = 1 V/1 A, V âˆ? I) | Boards 382 | 56 | **0.15** | **2.1** | 2.4 m | 0.2â€“0.3 |
| Ω symbol card | Boards 382 | 150 | **0.4** | **5.6** | — | 0.9 (340 px) |
| V / I / R letter cards S13 | Cards 380 | 100 letter + 36 name | **0.26** + `<size=36%>` | **3.7** | — | 0.2 one line (3.4 m wide, overlapping at 1.4 m pitch) |
| V = IR hero | Cards 380 | 115 | **0.3** | **4.2** | — | 0.5 (189 px) |
| OHM'S LAW title | Cards 371 | 120 | **0.32** | **4.5** | — | 0.55 (204 px) |
| Concept cards in the wire (VOLTAGE / Symbol / Unit), both at matched depth ≈3.5 m | Wire Inside 275 | 64 / 36 | **0.23** headline, **0.13** sub-lines (`<size=56%>`) | **3.25** / 1.85 | 2.6 m | 0.3 ×3 lines (1.7 m tall, cropped) |
| Question card "WHAT MAKES CHARGE FLOW?" | Wire Inside 145 | 48–60 | 0.35–0.4 | 4.9–5.6 | 6 m ok | 0.45 (65 px) ok |
| Counter "6" / caption | Wire Inside 193 | 96 / 28 | 0.5 / 0.14 (keep) **+ `Face(counter)`** | 7.0 / 2.0 | — | not faced |
| Prose cards S07 Question, S09 Reminder (+ "Your prediction") | Bench Wide ≈405 | 44 | **0.11** (0.09) | **1.55** (1.27) | **2.7 m** (≈1100 px, 3 lines) | 0.18/0.16, wrap 5 m (> frame) |
| S07 "Voltage → V" / "Current → I" | Bench Wide 592 | 56 | 0.095 | 1.34 | — | 0.14; also at x = ±2.2 m → outside the 1.64 m half-width: move to |x| ≤ 1.3 |
| Kid name (Capsule), if kept | 1.2 m, 1114 | 28 | **0.025–0.04** | 0.35–0.56 | — | 0.25; and move `KidsAt` out of the Boards/Cards frustum (z ≤ −1.0 or beside the camera) |
| VFX tag text | any | 28–36 | fixed **0.035** | 0.49 | width×0.9 | width×0.06 (0.07–0.14) |
| Notebook "Ink" on 0.26 m page | Table Close | — | **0.012** | 0.17 | 0.22 m | 0.08 (1.35 m string on a 0.26 m page) |

Line spacing (TMP `lineSpacing`, em/100 on top of the natural 1.364 em = 1.92 cap): **−20 for all-caps / single-word multi-line cards** (→ 1.16 em ≈ 1.64 cap), **0 for mixed-case prose** (question cards, subtitles, MCQ, table). `paragraphSpacing 0`.

Backing panel padding (cap-relative; textBounds already includes the 1.92-cap line box): **0.8 cap horizontal, 0.5 cap vertical per side** (now 0.6 / 0.45), minimum 0.02 m, panel width clamped to wrap width + 1.6 cap; corner radius ≈0.4 cap once a rounded sprite/quad is used. `rectTransform.sizeDelta.y = 6·cap` only fits 3 lines — set it from the line count (`lines × 1.92 cap × (1 + lineSpacing/100·0.71)` + 1 cap).

**World-space canvases** (ObservationTableUI / GraphUI, Boards cam 354–371 px/m):

| | now | proposed | screen cap |
|---|---|---|---|
| `WorldCanvas` scale | 0.0015 | **0.002** | |
| Table cell font / header / tag | 34 / 26 / 30 | **44 / 36 / 40** (cap 0.063 / 0.051 / 0.057 m) | 23 / 19 / 21 px |
| Table RowHeight / ColumnWidth / TagWidth | 78 / 300 / 260 | **84 / 360 / 280**, header row 100 with 2-line headers ("Potential Difference\nV (V)") | |
| Table canvas sizeDelta | 900×600 (1.35×0.9 m) | **1380×504** (2.76×1.0 m incl. tag column; 2.16 m without) — re-place table/graph so they no longer overlap, or pull `BoardsCam.z` to −2.6 (477 px/m) and keep 0.0015 with the same px | |
| Graph tick / point label | 22 / 22 | **40 / 40** | 20 px |
| Graph axis titles / tags / ΔV ΔI | 30 / 28 / 28 | **48 / 48 / 48** | 24 px |
| Graph text rect, tick offsets, axis-title offsets | 240×40, −28/−34, −72/−96 | 320×60, −40/−50, −100/−130 | |

**Screen UI (CanvasScaler 1920×1080, match 0.5; 1 px ≈ 0.12 mm on a 10" tablet):**

| | now (cap) | proposed (cap, physical) |
|---|---|---|
| Subtitle body / speaker | 38 (27 px) / 26 | **44 (31 px ≈ 3.7 mm) / 28**; box offsetMax.y 214 → **236** so two 60 px lines fit with the 10/44 insets |
| MCQ question / option / hint; Row | 36 / 32 / 28; 104 | **40 / 36 (26 px, 3.1 mm) / 30; Row 108** (buttons stay ≥ 88 px) |
| Prompt; rect | 38; 1100×96 | **42 (30 px); 1200×108** |
| Slider tick labels; tick rect | 24 (17 px, 2.0 mm); 80×36 | **30 (21 px, 2.5 mm); 96×44** |
| Screen buttons / `UI.Button` default / TAP ring | 34 / 30 / 30 | **36 / 34 / 30** |

## 5. Concrete constants

```csharp
// WorldText.cs
public const float CapToFontSize = 10f * 90f / 64f;           // 14.0625 for Nunito SDF (3D TMP: 0.1 world unit per fontSize em)
public static float FontSizeForCap(TMP_FontAsset f, float capMetres) => capMetres * 10f * f.faceInfo.pointSize / f.faceInfo.capLine;
public const float LineAdvancePerCap = 122.76f / 64f;          // 1.918 cap per line at lineSpacing 0
public const float PadX = 0.8f, PadY = 0.5f;                   // backing padding per side, in caps (was 0.6 / 0.45)
public const float CapsLineSpacing = -20f, ProseLineSpacing = 0f;
// Card(): label.fontSize = FontSizeForCap(label.font, height); label.lineSpacing = allCaps ? CapsLineSpacing : ProseLineSpacing;
//         sizeDelta.y = lines * height * LineAdvancePerCap + height; quad scale = bounds.size + height * new Vector2(2*PadX, 2*PadY)
//         optional: static float CapForScreenPx(float px, float depth, float fovDeg) => px * 2f * depth * Mathf.Tan(fovDeg * 0.5f * Mathf.Deg2Rad) / 1080f;

// SceneBuilderBase.cs / .Stages.cs  (cap heights in metres)
const float PropLabelCap = 0.03f, PropLabelWrap = 0.45f;       // was 0.06, NoWrap
const float MeterCaptionCap = 0.025f, MeterCaptionWrap = 0.45f;// was 0.05
const float MeterReadingCap = 0.04f;                           // was 0.06
const float TerminalCap = 0.04f;                               // was 0.12
const float PathLabelCap = 0.045f, PathLabelWrap = 1.2f;       // was 0.08
const float DataCardCap = 0.15f, DataCardWrap = 2.4f;          // Boards cam; was 0.2–0.3
const float OmegaCap = 0.4f;                                   // was 0.9
const float LetterCardCap = 0.26f;  /* "V\n<size=36%>Potential Difference" */
const float HeroFormulaCap = 0.3f, TitleCap = 0.32f;           // was 0.5 / 0.55
const float ConceptWordCap = 0.23f, ConceptSubCap = 0.13f, ConceptWrap = 2.6f, ConceptDepth = 3.5f; // wire cards, both at equal depth
const float ProseCardCap = 0.11f, ProseCardWrap = 2.7f;        // S07/S09; was 0.16–0.18, wrap 5 m
const float NameLabelCap = 0.04f;                              // capsule names; was 0.25 (and move KidsAt out of the Boards/Cards frustum)
const float VfxTagCap = 0.035f;                                // was width * 0.06
const float InkCap = 0.012f, InkWrap = 0.22f;                  // notebook pages; was 0.08
// WireStage: Face(counter) ; Face(minus/plus) from the camera that actually sees them.
const float WorldCanvasScale = 0.002f;                         // was 0.0015

// ObservationTableUI.cs
const float ColumnWidth = 360f, RowHeight = 84f, HeaderRow = 100f, TagWidth = 280f;  // was 300 / 78 / — / 260
const float HeaderFont = 36f, CellFont = 44f, TagFont = 40f;                         // was 26 / 34 / 30
// GraphUI.cs
const float TickFont = 40f, PointFont = 40f, AxisTitleFont = 48f, TagFont = 48f;      // was 22 / 22 / 30 / 28

// UI.cs / SubtitleUI.cs / MCQPanel.cs / SceneDirector.cs  (reference px)
public const float SubtitleFont = 44f, SpeakerFont = 28f, SubtitleTop = 236f;        // was 38 / 26 / 214
public const float MCQQuestionFont = 40f, MCQOptionFont = 36f, MCQHintFont = 30f, MCQRow = 108f;  // was 36 / 32 / 28 / 104
public const float PromptFont = 42f; /* rect 1200×108 */                             // was 38, 1100×96
public const float SliderTickFont = 30f; /* tick rect 96×44 */                       // was 24, 80×36
public const float ScreenButtonFont = 36f, ButtonFont = 34f;                          // was 34 / 30
```

Optional but recommended for labels that are seen from both the Wide (≈410 px/m) and Close (≈1000 px/m) cameras: a small `WorldLabel` behaviour that billboards (`rotation = LookRotation(position - cam.position)`, same convention as `Face`) and scales `localScale = base × clamp(depth / refDepth, 0.7, 2.5)` with `refDepth` = the depth the metre size above was tuned for; this keeps the 28 px prop labels at 28 px in every shot without touching timing or dialogue.