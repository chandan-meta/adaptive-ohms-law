# Reusable visual systems in `Assets/AdaptiveLearning` (old AdaptiveOhmsLaw lesson) — survey for the Scenes 5–13 polish pass

Project root: `D:\AI unity project\AI OHM'S LAW`. All paths below are under that root. Nothing was modified.

**Global caveats that apply to every system**

- **World scale differs ~4×.** The old lab is built at "big" scale: bench top 9 × 4.8 units at y = 0, meters 1.15 m wide, DC source 1.9 m, camera at (0, 4.5, −6.7). The new module (`Assets/OhmsLaw/Editor/SceneBuilderBase.Stages.cs`) uses real scale: bench 2.4 × 1.2 m at `BenchTop = 0.9`, meter bodies 0.28 m, tunnel radius 2.5 / length 16 along X. Every old prefab needs a uniform scale of roughly 0.25–0.4 when instantiated (per-prefab numbers below).
- **URP:** everything the old scenes actually render is URP (Lit/Unlit) or the project's own URP HLSL outline shader. The legacy `Standard` materials under `Art/ImportedOhmsLab/Materials/` and `Dependencies/2SIDED.shader` are not URP and render magenta; use the converted copies in `Art/ImportedOhmsLab/URP/` only. No post-processing volume (no Bloom) exists anywhere in the old module.
- **Runtime coupling:** the small visual components (`LabMeterVisual`, `LabWireRoute`, `RotaryDialGraphic`, `WarmLabTheme` colours) are self-contained. Everything else in `Scripts/` hangs off `SimpleCircuitModel` / `CircuitExperimentController` / `QuestionDefinition` / `LabSceneView` (namespace `AdaptiveOhmsLaw`) and must not be referenced by `OhmsLaw` runtime code.
- Reference screenshots of the old look: `Logs/LabQA/Logs/Orange-outline-ammeter.png` (warm lab + outline), `Logs/WarmUI/Scene_06_AdaptivePrediction-1920x1080.png` (chamber), `Logs/WarmUI/Scene_10_BecomeTheExperimenter-1920x1080.png` (bench, rotary, meters), `Logs/LabQA/Logs/Five-reading-graph-1440x1080.png` (graph).

---

## 1. Inside-the-wire chamber — `ConductorChamber.prefab`

**Path:** `D:\AI unity project\AI OHM'S LAW\Assets\AdaptiveLearning\Prefabs\Learning\ConductorChamber.prefab` (11.7k lines, 128 renderers, **no scripts**)
Built by `Assets/AdaptiveLearning/Editor/Scene06Builder.cs` (lines 70–87) and saved as a prefab by `Editor/ImmersiveLessonRevision.cs` (`Apply()`, `PrefabUtility.SaveAsPrefabAsset(F<Transform>("Conductor")...)`).

**Geometry** (all built-in Cube meshes, colliders removed, root at identity):
- 16 `Wall` panels: a faceted 16-gon tube, **radius 7**, panels at angle `i·22.5°`, local scale `(2.85, 0.22, 48)`, rotation `Euler(0,0,angle−90)`, centre `z = 12` → tube runs **z = −12 … 36, axis = +Z**.
- 112 `Rib` bars: 7 rings at **z = −4, 2, 8, 14, 20, 26, 32** (pitch 6), radius 6.85, scale `(2.7, 0.055, 0.06)`.
- Inventory bounds (`Logs/Blockout/assets.md`): 14.23 × 14.23 × 48.00 m, centre offset (0, 0, 12). Thumbnail: `Logs/Blockout/assets/ConductorChamber.png`.

**Materials (in prefab):**
- `Materials/Conductor.mat` — URP/Lit, base (0.045, 0.12, 0.17), metallic 0, smoothness 0.5 (walls).
- `Materials/ConductorRibs.mat` — URP/**Unlit**, (0.08, 0.31, 0.36) (ribs read as flat teal lines).
- Alternative darker set used by old Scene 11 (`Editor/Scene08CompositionRevision.cs:36-38`): `Materials/StudyChamber.mat` (URP/Lit (0.016,0.036,0.052), smooth 0.15) + `Materials/StudyChamberTrim.mat` (URP/Lit, emission ×0.35 of (0.025,0.13,0.16)).

**Intended camera / atmosphere (Scene06Builder lines 96–105):** camera at **(0, 0.6, −8)** looking +Z (inside the tube, 4 m from the mouth), FOV 48, near 0.1, far 70, `clearFlags = SolidColor`, background = fog colour; `RenderSettings.fog = true`, **ExponentialSquared, density 0.027** (0.022 in the Scene 7 reuse), fog colour (0.012, 0.035, 0.055), ambient (0.34, 0.43, 0.5); one directional "SoftKey" intensity 1.5 at Euler(35, −25, 0), shadows off. Comment in source: "Faceted open tunnel, viewed from inside. Opaque surfaces keep tablet overdraw low."

**URP:** fully compatible (URP Lit/Unlit, instancing enabled). 128 static renderers; mark static for batching.

**Fit to the new module:** the new `WireStage` tunnel is `TunnelRadius 2.5`, `TunnelLength 16`, axis **X**, origin `WireOrigin (100,0,0)`, camera `WireCamPos = origin + (−3.2, −0.3, −1.7)`. To drop the chamber in: rotate **Y 90°**, uniform scale **≈ 0.357** (7 → 2.5 radius; length becomes 17.1 m — or z-scale 0.333 for exactly 16), offset so the centre (local z = 12) lands on `WireOrigin`. Ribs become ~2 cm thin at that scale; consider thickening `Rib` scale y/z ×2. Replace the Unlit rib material with an emissive URP/Lit (StudyChamberTrim style) if Scene 4's glow look is wanted.

**Verdict:** **Reuse with changes** (rotate/scale to the new tunnel constants, keep the materials; it is a clear upgrade over the blockout's smooth inside-out cylinder seen in `Logs/Blockout/shots/Scene05_InsideTheWire/02_S05_L02.png`).

---

## 2. Imported lab equipment prefabs (G10 archive models)

**Folder:** `D:\AI unity project\AI OHM'S LAW\Assets\AdaptiveLearning\Prefabs\ImportedLabEquipment\` — built by `Editor/ImportedLabModels.cs` (`Make()`), documented in `Assets/AdaptiveLearning/IMPORTED_LAB_MODELS.md`.
Source FBX: `Art/ImportedOhmsLab/OhmsLaw_circuit_Final.fbx` (globalScale 15; supply, ammeter, voltmeter, rocker switch, also an unused LED `Diod`, rigged bendable cables `wire_rig1:joint6-11`, `Table1`), `Ohmslaw_v02.fbx` (rod + clamps), `Table.fbx` (globalScale 10). Textures with AO/emission maps exist in `Art/ImportedOhmsLab/OhmsLaw_circuit_Final.fbm/` (`Ammeter_AO.png`, `Power_Supply_AO.jpg`, `Power_Supply_EMS.jpg`, `Switch_AO.jpg`, `Wire_Connector_AO.jpg`) but are **not wired** into the URP materials (only `_BaseMap`; Wood1/Steel also have `_BumpMap`).

Prefab structure convention (all five): `Root (scale 1, pivot = bottom-centre of bounds, model faces −Z)` → `"Imported model - editable meshes"` (uniform child scale that normalises width) → FBX group → meshes. Materials are the converted URP/Lit copies in `Art/ImportedOhmsLab/URP/` (smoothness 0.28, metallic 0.05, Chrome 0.65).

| Prefab | Size (m) | Child scale | Moving/interactive child | Materials | Notes |
|---|---|---|---|---|---|
| `Ammeter.prefab` | 1.15 × 1.04 × 1.62 | 1.2737 | **`niddle_grp`** (needle pivot; mesh `Needle` is its child) | `URP/Ammeter_Diff` ; `Glass` renderer **disabled** (`Glass1` is legacy Standard) | Printed dial 0–3 A. Has `LabMeterVisual` |
| `Voltmeter.prefab` | 1.15 × 1.04 × 1.62 | 1.2737 | **`niddle_grp1`** (`Needle 1`) | `URP/VoltMeter_Diff`, `URP/Ammeter_Diff`; `Glass 1` disabled | Printed dial 0–3 V; scenes set `fullScale = 6` and label "×2". Has `LabMeterVisual` |
| `DCSource.prefab` | 1.90 × 1.24 × 1.33 | 0.9766 | **`Round_Switch`** (volt-selector knob, rotated about local Z), `Button1` (on/off rocker) | `URP/Power_Supply_Diff`, `URP/VoltMeter_Diff` | "BATTERY ELIMINATOR", printed 0–12 scale |
| `Switch.prefab` | 1.50 × 0.64 × 0.99 | 1.6169 | **`Button`** (rocker; Scene 8/9 wrapped it in `"Switch interaction pivot"` at `Euler(0,0,−30)` open; Scene 10+ drove `Button` directly with open `Euler(0,0,35)`, closed 0) | `URP/Switch_Diff` | A rocker, not a plug key |
| `NichromeWireXY.prefab` | 2.00 × 0.18 × 0.30 | 1.3045 | none | `URP/lambert1` (rod), `URP/Chrome`, `URP/Wire_Connector_Diff` | `Rod_` recentred between `Wire_Connector_02/03`; it is just a chrome rod + two white clamps |
| `Workbench.prefab` | 9.00 × 5.70 × 6.16 | 0.7706 | — | `URP/Wood1` (BaseColor+Normal), `URP/Border`, `URP/Steel` | Tall table; old scenes sank it to y = −5.696 so the top is at y ≈ 0 |

**How the old scenes placed them on the bench** (bench top y = 0, bench 9 × 4.8 centred z = 1.2; `Editor/ExperimentLabVisualUpgrade.cs:72`, `Scene12And13Builder.cs:218`, `EditableLabBuilder.cs`):

| Device | Position (Scenes 10–13) | Scene 8/9 difference |
|---|---|---|
| DCSource | (−3.0, 0.015, 2.1) | same |
| Ammeter | (−0.6, 0.015, 2.5) | same |
| NichromeWireXY | (1.8, 0.015, 2.5) | z = 3.2 |
| Voltmeter | (1.6, 0.015, −0.15) | same |
| Switch | (−2.8, 0.015, −0.15) | same |

Lead anchor offsets from each device root (`ExperimentLabVisualUpgrade.cs:112-116` / `ImportedLabModels.cs:112-118`): source +/− = (−0.08, 0.27, −0.70)/(−0.58, 0.27, −0.70); meters +/− = (±0.46, 0.18, −0.63); wire X/Y = (∓0.86, 0.12, 0); switch = (±0.71, 0.27, 0). Leads are 4-point `LineRenderer`s (width 0.035, drop to y = 0.08 and z −0.25) with `Materials/ReferenceRedLead.mat` (0.7, 0.025, 0.018) / `ReferenceBlackLead.mat`; Scene 8/9 used `Scripts/Lab/LabWireRoute.cs` (Catmull-Rom through 4 editable waypoints, clamps y ≥ 0.06; fields `a, b, points[], line`; `Show()`).
Workbench placement recipe (`Editor/RepairLabSceneGeometry.cs`): scale `(9/bounds.x, 1, 4.8/bounds.z)` then `position = (−centre.x, −max.y, 1.2 − centre.z)`.
Camera used with this layout: (0, 4.5, −6.7) → LookAt (0, 0.35, 1.45), FOV 46 (Scene 12/13) or physical 36 × 20.25 sensor, 24.4 mm, horizontal gate fit (Scene 8/9, `Editor/ReferenceLabStyle.cs`).

**Needle driver — `Scripts/Lab/LabMeterVisual.cs`** (`AdaptiveOhmsLaw.LabMeterVisual`, sealed, no dependencies):
- Serialized: `Transform needle`, `Quaternion zeroRotation` (rest local rotation of `niddle_grp`, stored as (0.3496, −0.1943, 0.4453, 0.8011)), `float fullScale = 3`, `float sweepDegrees = −120`, `bool measuresCurrent`.
- API: `Configure(Transform pointer, bool current)`, `SetValue(float)` → `needle.localRotation = zeroRotation * AngleAxis(sweep * clamp01(v/fullScale), Vector3.forward)`, `ShowCompletedCircuit()` (0.2 A / 1.0 V). `Awake` zeroes the needle.
- Scenes 10–13 fed it from `Scripts/Circuit/ImportedExperimentMeters.cs` (`Configure(SimpleCircuitModel, LabMeterVisual, LabMeterVisual)`, `BindReadouts(Text, Text)`) — that bridge is lesson-coupled; the new `CircuitRig` already rotates `ammeterNeedle`/`voltmeterNeedle` with `Euler(0,0,60 − 120·v/range)`, so just point those fields at `niddle_grp` and multiply by the stored zero rotation, or call `LabMeterVisual.SetValue` from `CircuitRig`.

**Knob/lever drivers in old code:** `RotaryVoltageControl.physicalKnob = Round_Switch`, `physicalScaleMaximum = 12` → `localRotation = Euler(0,0,135 − V/12·270)`. `CircuitSwitchInteraction.Configure(Button, model)` with `openEuler (0,0,35)`.

**Scale for the new bench:** uniform ≈ **0.25** for meters (1.15 → 0.29 m, matches the 0.28 m blockout meter), ≈ 0.19–0.25 for DCSource (1.9 → 0.36–0.48 m), ≈ 0.15–0.25 for Switch, **0.4** for NichromeWireXY (2.0 → 0.8 m, matches X/Y terminals at ±0.4), Workbench ≈ (0.267, 0.158, 0.195) or use only the `Wood` top.

**Verdict:** **Reuse as-is** for Ammeter, Voltmeter, DCSource, NichromeWireXY, Switch (instantiate, scale ~0.25, keep `LabMeterVisual`); **reuse with changes** for Workbench (non-uniform scale or top-only; or keep the blockout bench). Add the AO maps to the URP materials and give `Glass`/`Glass 1` a URP transparent material if you re-enable them.

**Older procedural set** `Prefabs/LabEquipment/*.prefab` (Ammeter/Voltmeter with `NeedleHinge`→`Needle`, `VoltageSource` with the procedural `VoltageKnobModel` pivot at (−0.43, −0.14, −0.69) [KnobBezel, KnurledKnob, 20 `Grip`s, amber `DialIndex`], knife `Switch` with `LeverHinge`, `Resistor`, `Tabletop`): primitive boxes with URP materials from `Materials/`. **Do not reuse** (lower fidelity than the imported set; the knob pivot is the only interesting bit).

---

## 3. Equipment outline / highlight shader

**Shader:** `D:\AI unity project\AI OHM'S LAW\Assets\AdaptiveLearning\Shaders\EquipmentOutline.shader` — `"AdaptiveLearning/EquipmentOutline"`, URP HLSL (`Core.hlsl`, `TransformObjectToHClip`, `UnityPerMaterial` CBUFFER → SRP-batcher OK), one pass `LightMode = SRPDefaultUnlit`, Transparent queue, `Cull Off`, `ZWrite Off`, `ZTest LEqual`, alpha blend, `Stencil Ref 64 / ReadMask 64 / WriteMask 64`. Vertex shader pushes verts along the **view-space normal by `_Width` screen pixels** (`2·_Width/_ScreenParams.xy · w`). Properties: `_Color`, `_Width`, `_StencilComp`, `_StencilPass`, `_ColorMask`.

**Material set:** `Assets/AdaptiveLearning/Art/Interface/EquipmentOutlines/` (created by `Editor/EquipmentOutlineUpgrade.cs::Material()`):

| Material | renderQueue | `_Width` px | `_StencilComp` | `_StencilPass` | `_ColorMask` | colour / alpha |
|---|---|---|---|---|---|---|
| `Mask.mat` | 3000 | 0 | 8 (Always) | 2 (Replace) | 0 | writes stencil only |
| `Soft outer glow.mat` | 3001 | 11 | 6 (NotEqual) | 0 | 15 | (1, 0.32, 0.015, 0.07) |
| `Middle glow.mat` | 3002 | 8 | 6 | 0 | 15 | α 0.12 |
| `Inner glow.mat` | 3003 | 5 | 6 | 0 | 15 | α 0.25 |
| `Orange edge.mat` | 3004 | 2.5 | 6 | 0 | 15 | α 1 |

Mechanism: the mask stamps the instrument's silhouette into stencil bit 64; the four widened shells draw only outside it, giving a clean contour plus a cheap glow with no bloom (see `Documentation/EquipmentOutline.txt`, screenshot `Logs/LabQA/Logs/Orange-outline-ammeter.png`).

**Baking tool:** `Editor/EquipmentOutlineUpgrade.cs` (menu *Adaptive Learning/Presentation/Apply Orange Equipment Outlines*): for each `LessonCinematicDirector.focusTargets` entry it copies every active `MeshFilter` of the apparatus (excluding names containing `needle`/`niddle`/`glass`), transforms verts into the equipment-root space, **welds normals per position** (so the hull does not split at hard edges), saves meshes to `EquipmentOutlines/<Scene>-<Subject>-<n>.asset` (282 assets; the Scene_12/13 ones are the minimal imported-prefab set: Ammeter 1, Source 3, Voltmeter 1, WireXY 3 meshes), and creates an inactive group `"Orange outline - <Subject>"` with 5 shells per mesh (shadows/probes off). Highlight is toggled by `LessonCinematicDirector.SetHighlight()` via `CinematicFocusTarget.highlight` (`GameObject.SetActive`). The Scene_12/13 baked meshes are in prefab-root space at scale 1 and can be parented under any instance of the same imported prefab.

**Already proven in the new module:** `Assets/OhmsLaw/Editor/Scene04Builder.cs:985-990` uses the same shader with just two materials on one renderer (`sharedMaterials = { Outline_WireMask (queue 2990, width 0, Always/Replace, ColorMask 0), Outline_Wire (queue 3000, width 5, NotEqual) }`, HDR colour) — `Assets/OhmsLaw/Scene04/Materials/Outline_Wire.mat`, `Outline_WireMask.mat`.

**URP:** compatible (Unity 6 URP Forward; the SRPDefaultUnlit pass renders in both Forward and Forward+). Not instanced; fine for a handful of shells.

**Verdict:** **Reuse as-is** (shader + 5-material recipe; re-bake meshes with the editor tool or replicate the Scene 4 two-material trick on a duplicate renderer; drive the group's `SetActive` from `SceneDirector`).

---

## 4. Warm lab theme — lights, materials, BenchWood

**Where it is defined:** `Editor/Scene12And13Builder.cs:168-213` (room + lights), `Editor/ReferenceLabStyle.cs` (reference styling for Scenes 8/9, procedural BenchWood), `Editor/EditableLabBuilder.cs:34-45`; saved as **`D:\AI unity project\AI OHM'S LAW\Assets\AdaptiveLearning\Prefabs\Learning\SharedLaboratory.prefab`** (22 × 10.15 × 22 m, 10 renderers, 2 lights, no scripts; derived from Scene 12's `Environment`). Docs: `REFERENCE_STYLE.md`, `Documentation/WarmUI.txt`.

**SharedLaboratory contents (local):** `Workbench` (imported prefab instance at (0, −5.696, 1.2), scale (1, 1, 0.78)), 4 × `Bench leg` cubes (±3.9, −1.15, −0.7/3.1) scale (0.22, 1.8, 0.22) `LabFrame`, `Floor` (0, −2.1, 1) 22 × 0.2 × 22 `LabFloor`, `Rear wall` (0, 1.2, 6) 18 × 6.5 × 0.2 `LabWall`, `Left wall` (−8, 1.2, 0) 0.2 × 6.5 × 12, `Bench front light strip` (0, −0.04, −1.19) 9 × 0.018 × 0.02 `ReferenceAmberLight`, and two lights:
- `SoftLabLight` — Directional, intensity **1.4**, colour **(1, 0.92, 0.82)**, rotation Euler(45, −30, 0), no shadows (Scene 8/9 variant: 1.7, (1, 0.87, 0.72), Soft shadows).
- `WarmTabletopLight` — Point at **(0, 3, 1)**, range 8, intensity **2.0**, colour **(1, 0.65, 0.32)**, no shadows (Scene 8/9: range 7, intensity 2.5).
- Scene 8/9 also add `Cool lab fill` directional 0.45 × (0.5, 0.75, 1) at Euler(40, 135, 0), a back amber strip (0, 0.035, 3.5) 9 × 0.025 × 0.02, three shelf strips, a `Circuit blueprint` board (`ReferenceBlueprint` (0.025, 0.11, 0.25)) with TextMesh sign, and `Reference book` props. Fog off in lab scenes.

**Materials (`Assets/AdaptiveLearning/Materials/`, all URP/Lit unless noted):**

| Material | Base colour | Other |
|---|---|---|
| `ReferenceWarmWood.mat` | white × **`Art/Interface/BenchWood.png`** | smoothness 0.3; texture is a procedural 512 × 128 RGB Perlin grain from (0.48, 0.28, 0.13) to (0.64, 0.40, 0.22) (`ReferenceLabStyle.cs:16`) |
| `LabWarmWood.mat` | (0.28, 0.15, 0.075) | fallback flat wood |
| `URP/Wood1.mat` (imported table top) | white × `Dependencies/Table_Wood1_BaseColor.png` + `Table_Wood1_Normal.png` | what the Scene 10 screenshot shows (dark walnut) |
| `LabWall.mat` | (0.11, 0.18, 0.22) | teal-grey walls |
| `LabFloor.mat` | (0.08, 0.10, 0.12) | |
| `LabFrame.mat` | (0.075, 0.10, 0.12) | legs/steel |
| `ReferenceAmberLight.mat` | (1, 0.48, 0.08) | **emission (2, 0.96, 0.16)** — bench edge strips |
| `ReferenceNavy.mat` | (0.02, 0.05, 0.08) | book props |
| `ReferenceRedLead.mat` / `ReferenceBlackLead.mat` | (0.7, 0.025, 0.018) / (0.018, 0.018, 0.025) | lead LineRenderers |
| `LabLeadTeal.mat` | (0.08, 0.62, 0.68) | Scene 8/9 teal leads |
| `InstrumentGraphite/Alloy/Cyan/Amber.mat` | graphite (0.075,0.12,0.15), alloy (0.42,0.53,0.57), cyan (0.1,0.7,0.8) emissive, amber (1,0.55,0.1) emissive | metallic 0.4, smooth 0.55 |

**URP:** all compatible. Light intensities assume URP default exposure, no HDR tone mapping.

**Verdict:** **Reuse with changes** — keep the lighting recipe (warm key 1.4 @ (45,−30), warm point pool above the bench, amber emissive bench edge, teal-grey walls) and the wood materials; re-scale the SharedLaboratory geometry by ~0.25 or rebuild walls/floor at new-module scale (the prefab's cubes are trivially recreated by `Prim`).

---

## 5. Graph — `Graph.fbx` and the old Scene 11 plotting

**FBX:** `D:\AI unity project\AI OHM'S LAW\Assets\AdaptiveLearning\Art\ImportedOhmsLab\Graph.fbx` (and `Graph(1).fbx`): Maya export, globalScale 100, 9.73 × 10.30 × 10.17 m, parts `LINE_GRAPH:Line`, `LINE_GRAPH:Line1`, `pCone11/12` (axis arrow cones), `polySurface1/2` (planes). Materials `LINE_GRAPH_BLACK`, `BAR_GRAPH_BLACK`, `LINE_GRAPH_lambert5` are **legacy Standard → magenta in URP** (see `Logs/Blockout/assets/Graph.png`). **Not referenced by any script, scene or prefab** in the old module.

**How old Scene 11 actually drew the graph** — screen-space UGUI, not 3D (`Editor/Scene08Builder.cs:50-80`, `Scene08CompositionRevision.cs`, `Scene11ScientificAxesUpgrade.cs`):
- `Scripts/Graph/GraphGridGraphic.cs` (`MaskableGraphic`): `maxVoltage = 6`, `maxCurrent = 1.2`, `showTrend`, `List<Vector2> points`, `Refresh()`. `OnPopulateMesh` draws 6 grid lines per axis (`i·CurrentStep/maxCurrent`, `i/maxVoltage`, axis lines 3 px, grid 1 px, `WarmLabTheme.Sage`) and a 3 px `Plum` polyline through sorted points via a static `Line(vh, a, b, width, color, rect)` quad helper.
- `Scripts/Graph/GraphPointGraphic.cs` (`Image` subclass): 32-segment ring/disc, radius 6/7 px, `SetMarked(bool)`; the Button's RectTransform (64–68 px) is the touch target.
- `Scripts/Graph/GraphPlotView.cs`: 25 `Plot_x_y` target buttons (5 × 5 grid at `(i%5+1)/6, (i/5+1)/6` anchors), 5 `GraphReading` row buttons, status text; validates placement against `ExperimentObservation` data; lesson-coupled (`SupportLevel`, `LessonExperimentData`).
- `Scripts/Graph/ResistanceGraphGraphic.cs` (Scene 12): same `Line` helper plus `showSlopeTriangle`, `slopePointA/B` in data units → ΔI base in `Amber` (4 px) and ΔV riser in `Plum`; `ResistanceGraphView.cs` handles two-point selection.
- Axis labels: `YAxisTitle` "Potential difference / V", `XAxisTitle` "Current / A", ticks `CurrentTick0-5` (0–5 V), `VoltageTick0-5` (0.0–1.0 A), panel ink (0.027, 0.08, 0.12) with 1 px `Outline` (0.12, 0.3, 0.34). Screenshot: `Logs/LabQA/Logs/Five-reading-graph-1440x1080.png`.

**URP:** UGUI, pipeline-agnostic; `MaskableGraphic` works on a world-space canvas too.

**Verdict:** `Graph.fbx` — **do not reuse** (legacy materials, generic bar/line prop, unused even by the old module). Old graph scripts — **do not reuse** as components (the new `Assets/OhmsLaw/Shared/GraphUI.cs` already does a world-space V–I graph with the ΔV/ΔI triangle); the `Line()` vertex-helper and the slope-triangle colouring (amber base, plum riser) are worth copying if `GraphUI` needs crisper lines than `LineRenderer`.

---

## 6. Voltage knob / rotary control

**Files:** `D:\AI unity project\AI OHM'S LAW\Assets\AdaptiveLearning\Scripts\UI\RotaryDialGraphic.cs`, `...\Scripts\UI\RotaryVoltageControl.cs`; authored in `Editor/ImmersiveLessonRevision.cs:118-127` and `Editor/ExperimentLabVisualUpgrade.cs:21-40`. Screenshot: `Logs/LabQA/Logs/Scene07-rotary-1920x1080.png`.

- **`RotaryDialGraphic`** (`MaskableGraphic`, no dependencies): `SetValue(float normalized)`, `SetGuidance(bool)`. Draws concentric discs (ink (0.025,0.07,0.10), rim (0.22,0.31,0.35), (0.065,0.13,0.17), (0.10,0.19,0.23)), 31 tick marks over a **270° sweep from 135° to −135°** (every 10th tick 3 px, lit ticks `WarmLabTheme.Amber`, unlit (0.34,0.47,0.5)), a 5 px amber pointer, and an optional amber guidance ring. Authored at 240 × 240 px with child labels `KnobValue` ("1.0 V", 27 pt), `KnobTitle` "SOURCE VOLTAGE", `KnobHelp` "DRAG TO TURN / 0–5 V • snaps at each whole volt", and `Setting0..5` numerals placed at `(135 − i·54)°` radius 0.61.
- **`RotaryVoltageControl`** (`Selectable, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler`): serialized `experiment` (**`CircuitExperimentController` — lesson coupling**), `face`, `valueLabel`, `physicalKnob`, `physicalScaleMaximum` (12 for the imported supply), `minimum 0`, `maximum 5`, `sweepDegrees 270`, `step 0.1`, `detentTolerance 0.12`. Relative-angle drag (no jump on grab), single active pointer released on deselect/focus-loss/pause, scroll-wheel and keyboard left/right, **snaps to whole volts within ±0.12 V else 0.1 V steps**; `Refresh()` rotates `physicalKnob.localRotation = Euler(0,0,135 − V/physicalScaleMaximum·270)`. `Configure(controller, graphic, label, model)`.
- 3D knobs available: imported `DCSource/Round_Switch` (printed 0–12 scale) or the procedural `VoltageKnobModel` pivot in `Prefabs/LabEquipment/VoltageSource.prefab`.

**URP:** UGUI; no issue.

**Verdict:** `RotaryDialGraphic` **reuse as-is**; `RotaryVoltageControl` **reuse with changes** — replace the `CircuitExperimentController` field/`SelectVoltage` calls with a `UnityEvent<float>`/callback into `CircuitRig.state.voltage` (keep the detent, pointer and `physicalKnob` logic). It is a better fit for "set the battery to 1 volt" than the blockout slider.

---

## 7. Particle / charge-carrier visuals (old Scenes 5/6)

- **`Scripts/Core/ChargeDriftVisualization.cs`**: 64 `Charge_n` **spheres** (scale 0.09, `Materials/Charge.mat` URP/Unlit cyan (0.25, 0.91, 0.91)) seeded in a box x ±6, y ±2.5, z 2–22 inside the chamber; `SetVoltage(v)` drifts origins along **+X** at `driftPerVolt 0.16`/s with wrap at ±6, plus sinusoidal jitter (amplitude 0.065, 1.7 Hz); `HighlightCharges(bool)` scales spheres 0.09 → 0.135. `Configure(Transform[])`. No particle systems.
- **`Scripts/Core/VoltageVisualization.cs`**: battery prop (white `SourceBody` 1.55 × 0.85 × 0.45 at (−2.2, 0.3, 5), amber `PositiveTerminal`/`SourceBand`, `VoltageHighlight` bar) + `SetVoltage`, `ShowSupport(SupportLevel)`, `Demonstrate()` coroutine (1 V → 2 V). Lesson-coupled (`SupportLevel`).
- **`Scripts/Core/MicroscopicWireView.cs`** (old Scene 12): a **2D UGUI** lattice (8 × 3 copper-orange `Image` ions, 20 cyan electrons, one yellow tracked electron) with thermal speed 160 px/s, drift 35 px/s, collision scatter and ion jitter; lesson-coupled buttons/captions.

**URP:** Unlit spheres fine; UGUI fine.

**Verdict:** **Do not reuse** — the new `Assets/OhmsLaw/Shared/WireInterior.cs` already drives 420 particle carriers + 600 lattice ions with drift/jitter/impede/cross-section counting, which supersedes all three. Only `Charge.mat`'s cyan and the "enlarge tracked electron" idea (`HighlightCharges`) are worth borrowing.

---

## 8. Question panel UI style

**Prefab:** `D:\AI unity project\AI OHM'S LAW\Assets\AdaptiveLearning\Prefabs\UI\QuestionPanel.prefab` (pre-warm-theme snapshot from `Scene06Builder`): a low strip anchored (0.04, 0.065)–(0.96, 0.205), ink `Image` (0.025, 0.055, 0.085, 0.96); `AccessibleQuestion` text (hidden); `Answers` `HorizontalLayoutGroup` (spacing 18, force-expand) with 3 buttons `AnswerA/B/C` — teal (0.08, 0.24, 0.29), `LayoutElement.minHeight 72`, LegacyRuntime 27 pt white labels "A   Increase", highlighted (0.65, 1, 1), pressed (0.35, 0.8, 0.85); `PredictionDecision` group with `TryAgain` / `KeepPrediction`.
**Script:** `Scripts/UI/QuestionUI.cs` — `Show(QuestionDefinition)`, `ShowAnswers()`, `ShowDecision()`, `SetInteractable(bool)`, events `Selected(int)`, `RetryRequested`, `KeepRequested`; `Configure(Text, Button[], GameObject, GameObject, Button, Button)`. Depends on `AdaptiveOhmsLaw.QuestionDefinition`.

**Approved "Warm UI" styling (applied only to saved scenes, not to the prefab):** `Scripts/UI/WarmLabTheme.cs` (ScriptableObject at `Resources/WarmLabTheme.asset`) + `Editor/WarmLabThemeUpgrade.cs`; Figma reference in `Documentation/WarmUI.txt`.
- Colour roles (static): **Ivory** #F7F3EA (panels), **Ink** #26312E (text on paper), **Amber** #F2B544 (primary buttons: continue/next/finish/record/check), **Sage** #426B55 (default buttons / graph grid), **Plum** #735B83 (Ask Wattson / speaker name), **Paper** #E8E7DA (inputs, rows, disabled).
- Fonts: body `Art/Interface/Fonts/SourceSans3-Regular.ttf`, control `SourceSans3-Semibold.ttf`, heading `NunitoSans-Bold.ttf` (OFL; `Fonts/SOURCES.txt`). The new module already has `Assets/OhmsLaw/Shared/UI/Nunito SDF.asset`; Source Sans would need a TMP SDF asset.
- Sprite: `Art/Interface/RoundedPanel.png` — 64 × 64 RGBA, sliced border 16 px, used `Image.Type.Sliced` on every panel and button; button `colors`: normal = role tint, highlighted = lerp to white 0.13, pressed = lerp to Ink 0.22, fade 0.12 s; labels Semibold, Ink on Amber/Ivory, Ivory on Sage/Plum; all `Outline` effects disabled.
- Layout seen in `Logs/WarmUI/*.png`: ivory dialogue footer (0.02, 0.03)–(0.98, 0.205) with Plum "WATTSON" speaker, 30 pt body; ivory lesson badge top-left; Amber primary button bottom-right.
- `WarmLabTheme.Apply(Transform)` / `StyleButton(Button)` are generic over UGUI `Image/Text/Button` and have **no lesson dependency**.

**URP:** UGUI; pipeline-agnostic.

**Verdict:** Prefab + `QuestionUI` — **do not reuse** (legacy `Text`, tied to `QuestionDefinition`; the new `Assets/OhmsLaw/Shared/MCQPanel.cs` exists). Style tokens — **reuse as-is**: port the five colours, the sliced `RoundedPanel.png`, the button-state formula and the Nunito/Source Sans pairing into `ModulePalette`/`UI.Theme`.

---

## 9. Other items noticed (brief)

- **Cinematic framing & highlight hookup:** `Scripts/Cinematic/LessonCinematicDirector.cs` — `CinematicFocusTarget { subject, target, highlight, cameraDistance 2–8 (default 4.8; Source 4.8, meters 4.2, wire 3.8), wattsonSide ±1 }`; shot = focus + 0.65 up, camera pulled back along the current view direction by max(distance, 2.2/tan(half-FOV)) + 0.45 up, blend 1.1 s. Older cyan `Focus highlight` LineRenderer ring used `Materials/CinematicFocus.mat` (URP/Unlit (0.05, 0.9, 1)). Heavily lesson-coupled → **do not reuse**, but the distance table is a good starting point for `SceneDirector` close-ups.
- **`Scripts/Lab/LabCameraOrbit.cs`** (focus (0, 0.35, 1.4), yaw ±45°, elevation 20–60°, speed 32) + `UI/LabOrbitJoystick.cs` — self-contained orbit; **reuse with changes** (scale the focus) if a look-around joystick is wanted.
- **`Scripts/UI/CompanionViewport.cs`** — renders the bench as a picture-in-picture (camera FOV 38 at (0, 1, −8), scales the apparatus into the right column) for the graph scene; **do not reuse** (new Scene 11 keeps the graph in-world).
- **`Art/ImportedOhmsLab/Voltmeter_01.fbx`** (1.81 × 1.63 × 2.54, chrome-trim voltmeter with `Needle`) and `Barrier.fbx` — unused alternates, legacy materials; **do not reuse** unless converted.
- **Legacy `Standard` material folder** `Art/ImportedOhmsLab/Materials/` and `Dependencies/2SIDED.shader` ("Standard 2-Sided", built-in RP) — **do not reuse**.
- **`Prefabs/Wattson/Wattson.prefab`** and `Art/env/Environment.fbx` are already consumed by the new module (`SceneBuilderBase.cs:29`, Scene 4 lab prefab); no change.