# Kit reports (P0 infrastructure agents)

## study

### summary

Rebuilt the lab stage kit in `D:\AI unity project\AI OHM'S LAW\Assets\OhmsLaw\Editor\SceneBuilderBase.Lab.cs` and extended `D:\AI unity project\AI OHM'S LAW\Assets\OhmsLaw\Scene04\Scripts\OhmWorker.cs`. Compile check (bash Tools/compile_check.sh study): zero errors, both files in the build.

- StudySet(bool modernBench): Study() + StudyLighting() in Scene 4's frame. The study instance is unpacked (Unity refuses to re-parent children of a prefab instance; the error string is in Unity.dll) so the desk parts can be grouped: ref "Old Table" = Victorian_Desk (desk body, drawers, extension, all desk dressing incl. Open_Book, Paper, Inkstand, Feather_Quill, Oil_Lamp_Large, Globe), Chair, Candle_Holder_5, hat_and_glass, magnify_glass, plus the "Desk Candle" and "Oil Lamp" flicker lights moved under it; ref "Lab" = Study + Old Table + Bench + Lighting (one `hide Lab` empties the room for Scene 12's dive). Paper_001 (a floor paper by the fireplace, a child of the desk in the FBX) is kept with the room. Book_Pile_4 is hidden in these scenes: it is 25 cm tall in front of the desk's left end, hides that corner from every study camera, and sits in the bench footprint.
- "Bench" = Workbench.prefab fitted to 2.0 x 0.82 x 0.9 m (render-bounds fit, top at DeskY, centred at BenchCentre) with a child cool-white spot ("Bench Lamp", 5000 K-ish, intensity 5, range 3.2, 75 deg, soft shadows) at y 1.3. Chosen over a material swap because the kit frame and the rig layout assume a 2.0 x 0.9 top (the Victorian top is 1.61 x 0.66 so instruments would overhang), a shape change reads at 1.8-3 m where a texture swap on carved dark wood would not, and the desk's L-extension/drawers would otherwise keep a Victorian silhouette. modernBench=true: Old Table inactive, Bench active; false: the reverse. Scene 8's existing cues `Hide("Old Table"); Show("Bench")` therefore also remove chair, hat, candle, lamp and their lights.
- Cameras registered with Vcam + Scene 4 lenses under the old names: "Bench Wide" (StudyWideCam, FOV 46, Lens_Wide, handheld 0.35, focus "Bench Point"), "Bench Close" (StudyBenchCam, 38, Lens_Medium), "Table Close" (StudyCloseCam, 36, Lens_Medium, focus "Apparatus Point"), "Boards" (StudyBoardsCam, 44, Lens_Medium, focus "Boards Anchor"). Markers: "Wattson Home" (StudyWattsonHome), "Bench Point" (BenchCentre + 0.1 up), "Boards Anchor" (BoardsAnchor), "Apparatus Point" (Galvanometer4 + 0.08 up). Occlusion check against the hierarchy bounds: chair top (y -0.235) is below every sight line; the oil lamp is right of the apparatus and in frame but blocks nothing; the bookcase is 45 deg outside the Bench Wide half-FOV; the Boards ray passes above the lamp and right of the chair. The desk's own front-row dressing (Open_Book top y -0.22, inkstand, lamp) hides anything flat on the desk's front/right, which drove the layout below.
- OhmApparatus(): Scene 4's recipe at Scene 4 positions with Lit() materials (Copper/Brass/Zinc/DarkWood/Paper/Ink values from the style guide): test wire between brass posts at Wire4, galvanometer at Galvanometer4 (plus needle pivot, thin glass tube r 0.02 around a torsion thread, brass head/knob), notebook with ink lines at Notebook4. The voltaic pile is REPLACED at Pile4 by the copper-bismuth thermoelectric source (wooden base, copper hot vessel with a soft steam emitter, zinc cold vessel with ice cubes, two copper junction blocks in the vessel mouths joined by a bismuth bar; labels "hot", "cold"): 1826 is the thermocouple year, the Scene 7 FAQ explains why he dropped the battery, Scene 8 swaps one source for one DC source, and the desk has no room for two sources. Extras: a wire rack at (-3.115, DeskY, -1.9) with four copper test wires of 0.20/0.16/0.12/0.08 m, four mercury cups (zinc, mercury surface) on a block at (-3.115, DeskY, -1.72), both at the desk's left end which is the only desk-level area all four cameras see. Pages: "Page 1..3" quads (0.17 x 0.12) stack on the notebook with 0.012 m handwritten tables (Ohm's own 1826 Zoll/degree readings, monospaced), PopIn on show; "Pages Fan" = five fanned pages 0.25 m above the notebook facing the Bench Close camera, PopIn. Labels only for the storyboard's names (copper-bismuth thermoelectric source, hot, cold, torsion galvanometer, copper test wires, mercury cups): Cap.PropLabel 0.03 m, faced to StudyBenchCam and billboarded via WorldText.Billboard(refDepth 1.8). No placeholders remain in the kit.
- Georg Ohm: Ohm_Scene04.prefab at OhmFeet4 facing +Z (scale 1.55) via Model(); OhmWorker targets at Scene 4's offsets ("Ohm Notebook Target" Notebook4 + 0.02 up, "Ohm Galvanometer Target" Galvanometer4 + (0.02, 0.08, -0.02), "Ohm Wire Target" Wire4 + (0.21, 0.03, -0.03)); falls back to the base Ohm() if the prefab is missing.
- Scene 8 swap groups: "torsion galvanometer" (galvanometer + its two leads), "Copper Test Wires" (mounted wire, posts, source lead, rack + spares), "Thermoelectric Source", "Mercury Cups", then "Old Table"/"Bench"/"Ohm Apparatus" (notebook, pages, fan). Existing cues work unchanged; hides are instant (the rig's new props can PopIn).
- OhmWorker: `frozen` stops the work clock (the pose is re-written every frame from a held clock, so it holds even when the cue fires before his first LateUpdate, which is why Scene 8 showed a T-pose); optional `needle` pivot swings to a new reading each time he adjusts the wire and settles while he writes.
- Transitional [Obsolete] aliases keep the untouched scene builders and Rig.cs compiling: BenchTop (= DeskY), BenchWideCam/Look, BenchCloseCam/Look, BoardsCam/Look, WattsonHome, OhmFeet, LabGreybox(bool) -> StudySet, OhmTable() -> OhmApparatus. FloorGrid stays in the base and is no longer called; "Back Wall" is gone.
- Materials written to Assets/OhmsLaw/Shared/Art/Materials: Copper, Brass, Zinc, DarkWood, Paper, Ink, Bismuth, Mercury, Ice, Glass_Torsion, Glow_Steam.

### api

SceneBuilderBase (Lab.cs):
- `protected void StudySet(bool modernBench)` — Scene 4's study + lighting + the four study cameras + markers; registers refs "Lab", "Old Table", "Bench", "Study", "Study/<part>", "Wattson Home", "Bench Point", "Boards Anchor", "Apparatus Point", cams "Bench Wide"/"Bench Close"/"Table Close"/"Boards". Call first in Stage().
- `protected void OhmApparatus()` — Ohm's apparatus + Georg Ohm; refs "Ohm Apparatus", "Thermoelectric Source", "torsion galvanometer", "Copper Test Wires", "Mercury Cups", "notebook", "Page 1".."Page 3", "Pages Fan", "Georg Ohm", markers "Ohm Notebook Target", "Ohm Galvanometer Target", "Ohm Wire Target". Scenes 7 and 8 only.
- `protected static void Fit(GameObject go, Vector3 size, Vector3 foot)` — scales a model so its render bounds measure `size` and stands the bounds' bottom centre at `foot` (used for the bench; handy for the imported instruments).
- `protected TextMeshPro PropLabel(string text, Transform parent, Vector3 position)` — Cap.PropLabel label faced to StudyBenchCam and billboarded (WorldText.Billboard, refDepth 1.8); explicit "\n" for line breaks.
- `[Obsolete]` transitional: `const float BenchTop = DeskY`; `static Vector3 BenchWideCam/BenchWideLook/BenchCloseCam/BenchCloseLook/BoardsCam/BoardsLook/WattsonHome/OhmFeet` (=> Study* / OhmFeet4); `void LabGreybox(bool)` => StudySet; `void OhmTable()` => OhmApparatus. Remove once nothing references them.

OhmWorker (OhmsLaw namespace):
- `public bool frozen` — stops the work clock; he holds his current pose (set it instead of disabling the component).
- `public Transform needle` — optional galvanometer needle pivot, rotated about local Y; `public float needleSwing = 38f` full deflection in degrees.
- Existing `notebook`, `galvanometer`, `wirePost`, `lean`, `writeSeconds`, `checkSeconds` unchanged; the prefab Ohm_Scene04.prefab needs no edit (new fields default).

### callerChanges

All of Scene07..13Builder.cs:
- `LabGreybox(modernBench: x)` -> `StudySet(modernBench: x)`; Scenes 7, 8: `OhmTable()` -> `OhmApparatus()` (the obsolete forwarders work meanwhile).
- `Wattson(WattsonHome, BenchWideCam)` -> `Wattson(StudyWattsonHome, StudyWideCam)`.
- Remove `KidsAt(...)` (kids are the POV; no capsules in the lab scenes).
- Re-place every Card/Vfx/Cam position in Scene 4's frame: desk/bench top is DeskY = -0.26, bench spans x -3.3..-1.3, z -2.25..-1.35; cameras stand at +Z/+X, so cards should Face(card, StudyWideCam) (or StudyBoardsCam for board-shot cards). Anything that used `BenchTop + h` at (x≈0, z≈0) must move to BenchCentre + (dx, h, dz).
Scene07Builder: delete `Cam("Table Close", ...)` (StudySet registers it); the "[VFX] galvanometer needle swing" placeholder can go (the needle now swings via OhmWorker) or sit over Galvanometer4 + (0, 0.3, 0); the pages/fan cues (`Show("Page 1")`.. `Show("Pages Fan")`, `Hide("Pages Fan")`) work unchanged; Card Voltage/Current near StudyWattsonHome + (±0.45, 0.35, 0.1), Card Question above the desk, e.g. (-2.3, 0.65, -1.6), ProseCard tier, faced to StudyWideCam.
Scene08Builder: all swap cues work unchanged (`Hide("torsion galvanometer")`, `Hide("Copper Test Wires")`, `Hide("Thermoelectric Source")`, `Hide("Mercury Cups")`, `Hide("Old Table"); Show("Bench"); Hide("Ohm Apparatus")`); `Gesture(Point, point: "Bench Point")` still valid (now BenchCentre + 0.1 up); Vfx positions -> over the bench, e.g. (-2.4, 0.35, -1.8). `Cue("ohm.freeze")` unchanged (needs the SceneDirector change below to hold the pose).
Scene09..11, 13: only the frame moves (above); "Wattson Home" marker exists for `moveTo: "Wattson Home"`.
Scene12Builder: delete `Ref(Node("Lab")).SetActive(true);` and the `foreach (... "Floor Grid", "Back Wall", "Bench", "Old Table" ...) SetParent(refs["Lab"]...)` loop — StudySet already provides "Lab" containing study, desk group, bench and lighting; a second "Lab" would overwrite refs["Lab"] and the director could no longer hide the room. Keep `refs["Circuit Rig"].transform.SetParent(refs["Lab"].transform, true);`. Re-aim `Cam("Dive", ...)` at the rig's nichrome wire in the study frame (e.g. from the wire + (0, 0.25, 0.6) looking at the wire, FOV 70). `Show("Wire Section"); Hide("Lab")` and the `lab` cue work unchanged.
SceneBuilderBase.Rig.cs (rig agent): `float y = BenchTop` now equals DeskY (-0.26) via the alias; lay the rig out around BenchCentre on the 2.0 x 0.9 top (x -3.3..-1.3, z -2.25..-1.35; the bench's -X end meets the wall) and move "Wire Point"/"Tray Point"/"Voltmeter Point"/"Source Point" into that frame; `Fit(go, size, foot)` is available for the imported Ammeter/Voltmeter/DCSource/Switch/NichromeWireXY prefabs; `PropLabel(text, parent, pos)` gives the AMMETER/VOLTMETER-style name labels at the TYPOGRAPHY size.
SceneBuilderBase.Boards.cs (boards agent): "Boards Anchor" marker at BoardsAnchor is the Boards camera's focus target; the Boards camera is 2.6 m from the anchor at FOV 44 (≈575 px/m), not the blockout's 3.6 m.

### requests

1. Assets/OhmsLaw/Shared/SceneDirector.cs line 150 — replace
   `case "ohm.freeze": foreach (var worker in FindObjectsByType<OhmWorker>(FindObjectsSortMode.None)) worker.enabled = false; break;`
   with
   `case "ohm.freeze": foreach (var worker in FindObjectsByType<OhmWorker>(FindObjectsSortMode.None)) worker.frozen = true; break;`
   (disabling the component before his first LateUpdate is what produced the T-pose).
2. Assets/OhmsLaw/Editor/SceneBuilderBase.Look.cs — `BenchCentre` x -2.3f -> -2.2f: with width 2.0 the bench's -X end at x -3.3 is 8.5 cm inside the window wall (inner face x -3.215); -2.2 leaves it flush with 1.5 cm clearance. Invisible either way (the end is inside the wall), so low priority. Optional: `StudyCloseLook` y -0.2f -> -0.05f only if Ohm's face is wanted in "Table Close" (at FOV 36 the frame top is 2.5 deg below horizontal, his head at y 0.45 is out; the shot is about hands, needle and pages, so I left it).
3. Wire agent / Scene 12: `WireLighting()` adds a second global volume ("Wire_Global") at the same priority as the study's "Study_Global"; while "Lab" is visible both apply in undefined order. Parent the wire's "Lighting" node under "Wire Section" (inactive until the dive) or give the two profiles different Volume priorities.
4. Optional continuity (Scene04Builder.cs): Book_Pile_4 is hidden in Scenes 7-13 (it blocks the desk's left end from every study camera and sits in the bench footprint); hiding it in Scene 4 too (alongside Book_Pile_1) keeps the desk identical.
5. Docs/Polish/ASSETS.md thermoelectric row: note the decision — the pile is replaced by the Cu-Bi source in Scenes 7-13.

### unverified

Needs a Unity build to confirm: (a) PrefabUtility.UnpackPrefabInstance(Completely) on the Lab_Scene04 variant followed by the re-parenting (static flags and flame materials should survive; scene files grow by the unpacked hierarchy); (b) the Workbench fit (expected scale ≈ (0.222, 0.144, 0.146), top at y -0.26; legs become slightly elliptical) and how its URP/Wood1 top reads under the bench spot (intensity 5 may need ±2); (c) label legibility at 0.03 m cap from Bench Close (≈26 px) and the WorldLabel billboarding against the candlelit room; (d) the hot/cold/source label stack over the source and the "copper test wires" label against the window; (e) the ink tables (mspace 0.6em, 16 chars in a 0.16 m rect) fitting the 0.17 m page; (f) the needle swing amplitude (38 deg) and the steam emitter's density; (g) that hiding "Lab" (which now includes Lighting and the Study_Global volume) gives the intended inside-the-wire look in Scene 12 and that the `lab` cue restores it; (h) Ohm frozen behind the bench in Scene 8 (bench back edge z -2.25, his feet z -2.36: ~5 cm clearance to his hips while leaning); (i) the three "Again" beats in Scene 7 cannot sync with the 9 s work loop without changing writeSeconds/checkSeconds, so the needle swings once per loop, not once per "again".

compiles: True

## rig

### summary

Replaced the greybox circuit with the imported instruments in Scene 4's study frame. Files: `D:\AI unity project\AI OHM'S LAW\Assets\OhmsLaw\Editor\SceneBuilderBase.Rig.cs` (rewritten), `...\Assets\OhmsLaw\Shared\CircuitRig.cs` (needle/knob/lamp/lead drivers, same public API), new `...\Assets\OhmsLaw\Shared\Art\Textures\{VoltMeter_0to6.png, Ammeter_0to1p2.png, Power_Supply_0to5.jpg, README.txt}` (Pillow repaints: voltmeter 0-3 V -> 0-6 V, ammeter 0-3 A -> 0-1.2 A so 1 V -> 0.2 A lands on printed marks, battery-eliminator panel 2-12 -> 0-5 V; arcs/ticks untouched, numerals redrawn radially). `bash Tools/compile_check.sh rig` = zero errors.
Rig root "Circuit Rig" sits at BenchCentre (-2.3,-0.26,-1.8); bench-local layout (+X = room = frame-LEFT because the study cameras look toward -Z): DC source (0.75,0,0.14) scale 0.17 facing the cameras; ammeter slot (0.50,0,-0.20); NichromeWireXY (0.02,0,0.09) scale 0.45 turned 180 deg so X is at +0.39 (frame left) and Y at -0.37; voltmeter slot (0.02,0,-0.20) behind the wire; knife-switch key (-0.42,0,0.28) scale 0.19; graphite parts tray (-0.72,0,0) 0.30x0.78 holding both meters (yaw 180) at z -/+0.19. Meters scale 0.21 (0.24x0.22x0.34 m); yaw toward the midpoint of StudyBenchCam/StudyCloseCam capped at 28 deg (key 20 deg). Offline SAT check: no overlaps, all margins >= 5.7 cm, no camera sight-line occlusion of X, Y, both dials or the knob from either study camera.
Meters: imported prefabs with body material copied to Ammeter_Dial / Voltmeter_Dial (repainted textures), BoxCollider fitted before the yaw, needle = niddle_grp / niddle_grp1 driven as rest * AngleAxis(-96 deg * value/fullScale, local Z) (fullScale 1.2 A / 6 V), digit card (cap 0.04, palette colour, dark backing) 5.5 cm above the body, hidden "Caption <NAME>" card (name cap 0.03 + caption at 83%, wrap 0.45). DC source: panel material copy with the 0-5 texture, _EmissionMap = Power_Supply_EMS.jpg, lamp lit via MaterialPropertyBlock while keyClosed; Round_Switch turns -36 deg/V. Key: LabEquipment/Switch.prefab re-skinned with Scene 4 Brass/DarkWood (LeverHinge = keyLever, open 35 / closed 0), the only remaining BlockoutPlaceholder. Leads: Catmull-Rom Polylines (width 7 mm, Lit red/black) Source+ -> ammeter (round the source's inner side), ammeter -> X, Y -> key, key -> source- along the front edge; voltmeter leads X/Y -> posts; terminals picked by nearest-post so the plumbing follows the layout. Ammeter leads appear when the ammeter is placed, voltmeter leads when the voltmeter is; "Current Path" is an additive white Glow overlay along the loop (dips to the bench in front of the wire, the rod never glows) coloured amber/clear by CircuitRig. Slot glows are soft additive discs (amber series, blue parallel) pulsed by CircuitRig; "Wire Highlight" is a translucent wattsonAccent sheath around the rod, pulsed while shown. X/Y badges use Scene 4's Word_CURRENT HDR face material (cap 0.04); path label cap 0.045 wrap 1.2 above the voltmeter at the back. All text is billboarded to Camera.main at runtime (and faced to the cameras at build time).
CircuitRig.CloseKey() now sets voltage to KeyClosedDefaultVolts (0.5) when it was <= 0, per CRITIQUE (Scene 9 closes at 0.5 V; Scene 10 should start there).

### api

CircuitRig (Assets/OhmsLaw/Shared/CircuitRig.cs) - unchanged: state, ammeterPlaced/voltmeterPlaced, ammeter, voltmeter, key, keyLever, ammeterSlot, voltmeterSlot, ammeterReading, voltmeterReading, pathLabel, ammeterNeedle, voltmeterNeedle, currentPath, voltmeterLeads, seriesGlow, parallelGlow, sfx, click, snap, draggable, keyArmed, Placed, KeyClosed, Volts, Amps, Place(string), IsPlaced(string), PlaceNow(string), CloseKey().
New public members:
- `public const float KeyClosedDefaultVolts = 0.5f;` - voltage applied by CloseKey() when state.voltage <= 0; use it for Scene 10's starting voltage.
- `public LineRenderer voltmeterLeadY;` - second voltmeter lead (Y -> voltmeter); voltmeterLeads is now the X -> voltmeter lead. Both enabled while voltmeterPlaced.
- `public GameObject ammeterLeads;` - group of the two source->ammeter->X leads, active while ammeterPlaced.
- `public GameObject wireHighlight;` - the "Wire Highlight" sheath; pulsed while a cue shows it.
- `public float ammeterFullScale = 1.2f, voltmeterFullScale = 6f, needleSweep = -96f;` - printed full scales and arc sweep (degrees about the pivot's local Z, negative = clockwise).
- `public Transform sourceKnob; public float knobDegreesPerVolt = -36f;` - Round_Switch turn per volt.
- `public Renderer sourceBody; public int lampMaterialIndex; [ColorUsage(false,true)] public Color lampColor;` - pilot lamp emission applied per renderer while keyClosed.
- `public Transform[] billboards;` - world text re-faced to Camera.main every LateUpdate.
Behaviour change: needles use rest * AngleAxis(sweep * clamp01(v/fullScale), Vector3.forward) (rest captured in Awake); shown readings start at their targets (no swing-up on a match-cut); CloseKey() applies KeyClosedDefaultVolts.

SceneBuilderBase (Editor/SceneBuilderBase.Rig.cs) - `protected CircuitRig Rig(bool assembled, bool keyClosed, float voltage, bool active = true)` signature unchanged. Everything else in the file is private (Meter, Caption, Slot, Facing, SlotGlow, PartsTray, RigTexture, LeadPath, CatmullRom, Lead, Over, Nearest, Other, Append). Old private helpers LineOf/SlotGlow(quad)/Meter(cube) are gone (no external callers).
Refs registered by Rig(): "Circuit Rig", "AMMETER", "VOLTMETER", "NICHROME WIRE — XY", "Card X", "Card Y", "cells / controllable DC source", "switch / key", "Wire Highlight", "Parts Tray", plus NEW hidden-by-default "Caption AMMETER", "Caption VOLTMETER", "Caption NICHROME WIRE — XY", "Caption cells / controllable DC source", "Caption switch / key". Markers (world): "Wire Point" (-1.95,0.14,-2.55), "Tray Point" (-3.08,0.09,-2.58), "Voltmeter Point" (-2.72,0.09,-2.55), "Source Point" (-1.50,0.14,-2.55), NEW "Key Point" (-2.55,0.14,-2.58) - all behind the bench, off the camera-subject lines.
Build-time assets written under Assets/OhmsLaw/Shared/Art/Materials: Nichrome, LeadRed, LeadBlack, TrayGraphite, KeyHandle (KeyBrass/KeyWood only if Scene 4's Brass/DarkWood are missing), Ammeter_Dial, Voltmeter_Dial, DCSource_Panel, Glow_WireHighlight, Glow_SeriesSlotGlow, Glow_ParallelSlotGlow, Glow_CurrentPath.

### callerChanges

1. Frame: the rig is now built at BenchCentre in Scene 4's study frame (bench top y = -0.26), not at the greybox origin. Scenes 8-13 must stage with Study()/StudyLighting() and the study cameras (StudyBenchCam/Look, StudyCloseCam/Look) instead of LabGreybox(modernBench)/BenchTop/Bench Wide/Bench Close, or the rig will sit 2.3 m away from their bench. World positions for cameras: source (-1.55,-0.26,-1.66), ammeter slot (-1.80,-0.26,-2.00), wire centre (-2.28,-0.26,-1.71) with X ~(-1.89,-0.21,-1.71) and Y ~(-2.67,-0.21,-1.71), voltmeter slot (-2.28,-0.26,-2.00), key (-2.72,-0.25,-1.52), tray (-3.02,-0.26,-1.80) with the ammeter at z -1.99 and the voltmeter at z -1.61.
2. Framing facts for shot choice: from StudyCloseCam the rig spans +/-32 deg horizontally (source and key at the frame edges) and the TRAY is 41 deg off-axis (out of frame); from StudyBenchCam everything is within +/-29 deg (tray 18 deg). So Scene 9's S09_PlaceAmmeter / S09_PlaceVoltmeter must run on StudyBenchCam (or a dedicated wider cam ~ (-1.6,0.6,-0.3) -> (-2.4,-0.2,-1.8) fov 44); StudyCloseCam is fine for reading the dials (Scene 10 L02/L03). StudyCloseCam's frame top is only ~0.45 m above the bench at 2 m depth, so Wattson at the markers (0.35-0.4 m up, 0.7 m tall) loses his head in that shot - use the bench cam when he is at a marker.
3. Scene08Builder: the Stage() hide-list (`"AMMETER", "VOLTMETER", "NICHROME WIRE — XY", "Card X", "Card Y", "cells / controllable DC source", "switch / key"`) still works. Labels are no longer auto-shown (no Prop labels): add `Show("Caption AMMETER")` with `Show("AMMETER")`, `Show("Caption VOLTMETER")`, `Show("Caption NICHROME WIRE — XY")`, `Show("Caption cells / controllable DC source")`, `Show("Caption switch / key")` in the transformation beat, and `Hide(...)` all five before the handoff (or leave them for Scene 9 to never show - they start hidden in every scene). The two Y/key return leads are children of "switch / key", so they appear with the key (after wire and source) - no extra cue needed.
4. Scene09Builder: `Gesture(Point, point: "switch / key", moveTo: "Source Point")` should become `moveTo: "Key Point"` (the source is now at the other end of the bench). Other markers keep their names. The reminder card / Vfx("X and Y pulse") positions must move to the study frame (X/Y world positions above). Note S09_CloseKey now leaves the circuit at 0.5 V (CircuitRig.CloseKey), matching CRITIQUE.
5. Scene10Builder: `Rig(assembled: true, keyClosed: true, voltage: CircuitRig.KeyClosedDefaultVolts)` and change `Cue("rig.voltage 0", ...)` to `Cue("rig.voltage 0.5", ...)` (or drop it) so the match-cut from Scene 9 is continuous and S10_Set1V has a visible 0.5 -> 1 V effect; Wattson `moveTo: "Source Point"` still points at the source. Scenes 11-13 keep `Rig(assembled: true, keyClosed: true, voltage: 5)`.
6. Scene12Builder: `refs["Circuit Rig"].transform.SetParent(refs["Lab"].transform, true)` still works (leads/path are world-space, meters are children); its "Dive" camera must target the wire's new world position (-2.28,-0.26,-1.71).
7. Instruments do not render their printed numerals legibly at this scale; the digit cards (children "Reading" of AMMETER/VOLTMETER) carry the values - keep dials unobstructed in shots where the learner reads them.

### requests

1. Assets/OhmsLaw/Shared/SceneDirector.Gates.cs, CloseKey(): delete the line `if (rig.state.voltage <= 0) rig.state.voltage = 1f;` (CircuitRig.CloseKey() now applies CircuitRig.KeyClosedDefaultVolts = 0.5 f itself; the line is dead but misleading). Optionally update the comment to "CircuitRig applies the small default voltage".
2. Assets/OhmsLaw/Shared/SceneDirector.cs (optional continuity): in Start(), when `matchCutFrom == SceneHandoffState.fromScene` and `rig`, apply `rig.state = SceneHandoffState.circuit` so the voltage carries across 9 -> 10 -> 11 without relying on builder constants.
3. Scene08Builder.cs / Scene09Builder.cs / Scene10Builder.cs: the cue/marker edits listed under caller changes (Show/Hide "Caption ..." refs, "Key Point", voltage 0.5).

### unverified

Needs a Unity build/play to confirm: (a) needle calibration - CircuitRig.needleSweep = -96 deg assumes the model's needle rest is the printed "0" and the printed arc subtends ~96 deg at the niddle_grp pivot (the texture measures 89-104 deg depending on which centre the pivot sits on); check 5 V -> needle on "5", 1.0 A -> "1.0", and tune needleSweep; (b) knob - Round_Switch pointer assumed to rest at the 0 (left) mark and to turn clockwise with -36 deg/V; flip the sign or add an offset if it points elsewhere; (c) pilot lamp - MaterialPropertyBlock _EmissionColor (2.5 white x red EMS map) should bloom with the Study_Global threshold 1; (d) lead routing - Catmull-Rom knots were placed from offline footprint maths (source-side via points, 5 cm lift over the clamps); a lead may graze a clamp or the source corner; LineRenderer + URP Lit lighting of the leads; (e) DC source post anchors come from the old lesson's anchors ((-0.08/-0.58, 0.27, -0.70) at scale 1, consistent with the Logs/Scene09-ImportedModels.png screenshot) not from mesh data; (f) FitCollider boxes (meters, key, source) and drag/tap hits; (g) the repainted textures import with default settings (sRGB, 2048 max) - no .meta files were written; (h) Word_CURRENT.mat on the X/Y TMP badges (falls back to plain amber if Scene 4 has not been built); (i) readout/caption card heights vs the StudyCloseCam frame top (caption tops at ~0.46 m above the bench are within ~1 deg of the frame edge); (j) overall look of the knife-switch key (primitive-built, re-skinned) next to the textured instruments.

compiles: True

## wire

### summary

Project: D:\AI unity project\AI OHM'S LAW. Files changed: Assets/OhmsLaw/Editor/SceneBuilderBase.Wire.cs (rewritten), Assets/OhmsLaw/Shared/WireInterior.cs (rewritten, same cue semantics), Assets/OhmsLaw/Editor/BlockoutAssets.cs (TunnelMesh + ChargeMaterial only). `bash Tools/compile_check.sh wire` finishes with zero errors.
- Tunnel: TunnelMesh() is a 64x32 grid tube wound to face the axis (rebuilt in place each build); the TunnelInside `_Cull Front` is gone, the wall is a per-tint Lit metal (metallic 0.7, smooth 0.5) whose base + faint emission WireInterior sets from materialTint each frame. 13 emissive rib rings every 1.2 m and a glowing cap ring + dark translucent disc at each end are children of the (length,radius,radius)-scaled tube so they follow the narrowing. WireLighting(tint) is called from WireStage; the nodes it creates are re-parented under "Wire Section" (volume priority 1, lights x0.7 for nichrome) and the fog/ambient/background it set are stored on WireInterior, which applies them in OnEnable and restores the previous atmosphere in OnDisable (Scene 12 dive in/out). With active:false the lab's RenderSettings are restored in the scene file.
- Camera: WireCamPos moved near the axis (origin + (-3.2,-0.4,-0.35), look (3.0,0.1,0.2)) so the 20% narrowing never puts it in the wall; "Wire Inside" (FOV 60, Lens_Wide, focus "Wire Focus Point" at 3.5 m) and "Wire Wattson" (FOV 40, Lens_Medium, focus "Platform Point") are Vcams.
- Carriers: 700 HDR "−" sphere sprites (new ChargeGlyph.png, _BaseColor x2.4), startSize 0.14 (+0.12 flash), distance-sorted, alpha fades to 0 within 0.45-0.8 m of Camera.main, frost tint while frozen, white flash on crossing and on each lattice bounce; narrowing clamped to >= 80% of the built radius (minRadiusFraction). Lattice ions: dull grey-blue IonGlyph sprites, size 0.3, alpha from latticeVisibility.
- Cross section: unit-radius additive ring + faint soft disc at x=+1.5, scaled by WireInterior; counter (0.5 / 0.14 caps, HDR glow faces, one-second ring) at frame (0,+0.56) 4.8 m out, Face()'d.
- Terminals: "Card Terminal −" (en dash glyph + "terminal", cool white) top-left at 2.4 m, "Card Terminal +" (warm white, cap 0.45) at origin+(5.5,0.85,0) before the far cap; each has a glowing cap ring child and HDR face, placeholder notes removed.
- Platform: dark deck + brass trim + accent glow rim at PlatformPos, warm point light above; markers "Platform Point" (2.6 m from the lens, lower-left third), "Wall Point", "Wire Focus Point".
- Six real "[VFX] ..." effects built hidden under the section with the exact cue names; WireFx() lets builders add/re-place them (field wave = one mesh ring sweeping -X->+X over the whole 16 m in 0.5 s plus a whole-volume shimmer). WireBracket() helper: glowing dashed line, end ticks, haloed "Point A"/"Point B". WireFramePoint/WireCardPosition give equal-depth card positions.

### api

SceneBuilderBase (SceneBuilderBase.Wire.cs), all protected:
- const float TunnelRadius = 2.5f, TunnelLength = 16f, CrossSectionX = 1.5f (NEW).
- static readonly Vector3 WireOrigin (100,0,0); WireCamPos = WireOrigin + (-3.2,-0.4,-0.35) (MOVED); WireCamLook = WireOrigin + (3.0,0.1,0.2) (MOVED); PlatformPoint = WireOrigin + (-0.65,-0.75,0.85) (NEW: where Wattson hovers, = marker "Platform Point"); PlatformPos = PlatformPoint + (0,-0.4,0) (MOVED: platform disc centre); WallPoint = WireOrigin + (2.0,0.7,2.2) (NEW, = marker "Wall Point").
- static readonly Color CopperTint, NichromeTint (unchanged); MinusTint (0.86,0.92,1), PlusTint (1,0.93,0.8) (NEW battery whites).
- const float WireCardDepth = 3.5f, WireConceptCap = 0.23f, WireQuestionDepth = 6f, WireQuestionCap = 0.38f, WireQuestionWidth = 6f (NEW, TYPOGRAPHY sizes; sub-lines via "<size=56%>" in the card text).
- static Vector3 WireFramePoint(float x, float y, float depth): a point in the Wire Inside frame, x/y in [-1,1] (0,0 = frame centre), depth in metres along the view.
- static Vector3 WireCardPosition(int side, float depth = WireCardDepth): side -1 = VOLTAGE (upper-left third), +1 = CURRENT (upper-right third), same depth = same on-screen size.
- static Vector3 WireQuestionPos (get): "WHAT MAKES CHARGE FLOW?" at 6 m; static Vector3 WireResistancePos (get): RESISTANCE at 4.5 m ("Symbol: R" goes 0.6 m under it).
- WireInterior WireStage(Color tint, bool lattice, bool active = true): unchanged signature. Registers refs "Wire Section", "Lattice Ions", "Cross Section", "Counter", "Card Terminal −", "Card Terminal +", "Platform Point", "Wall Point", "Wire Focus Point", "Cam Wire Inside", "Cam Wire Wattson", "[VFX] letters assemble from charges", "[VFX] field wave sweeps whole tunnel", "[VFX] time-freeze (Ohm-niscope holds the carriers)", "[VFX] Ohm-niscope flash", "[VFX] carriers impeded by lattice (slow, bounce)", "[VFX] RESISTANCE word forms from the lattice"; cams "Wire Inside", "Wire Wattson". Calls WireLighting(tint) itself.
- void Face(GameObject card, Vector3? from = null): unchanged.
- ParticleSystem Particles(string name, Transform parent, Material material, int max): unchanged (+ distance sorting).
- GameObject WireBracket(Vector3? a = null, Vector3? b = null, string name = "Bracket"): glowing dashed bracket with end ticks and haloed "Point A"/"Point B" children, registered as `name` (+ the points), starts hidden; defaults WireOrigin + (-0.7,-0.3,-1.3) -> (2.3,-0.3,-1.3).
- GameObject WireFx(string name, Vector3? position = null, Transform parent = null): builds "[VFX] <name>" (hidden, Ref'd; plays on enable), replacing any existing ref of that name (including a magenta Vfx tag). position is local to parent (world when parent is null; the tunnel default when omitted); "field wave..." always starts at the -X end. Known names: "race into wire surface" (dive streaks, forward = -Z toward the camera, rotate the returned object if needed), "letters assemble from charges", "field wave sweeps whole tunnel", "time-freeze (Ohm-niscope holds the carriers)", "Ohm-niscope flash", "carriers impeded by lattice (slow, bounce)", "RESISTANCE word forms from the lattice"; anything else = spark burst.
- static Vector3 WireFxDefault(string name): default position of a named effect relative to WireOrigin.
- static void WireGlowFace(TMP_Text label, Color color): Scene 4's HDR face + outer glow material (Art/Materials/Word_Wire_<hex>.mat) on any TextMeshPro, e.g. the question card.
BlockoutAssets: public static Mesh TunnelMesh() (unchanged signature); public static Material ChargeMaterial(Color tint, bool ion = false) (ion: dull lattice sphere sprite "IonGlyph").
WireInterior (runtime): kept count, thermalJitter, driftSpeed, fieldOn, frozen, impede, materialTint, latticeVisibility, tunnelRadius, length, carriers, lattice, tunnel, crossSection, counter, counterRing, counting, LastSecond. New: carrierSize = 0.14, nearFade = 0.8, minRadiusFraction = 0.8 (narrowing floor; wire.radius 1.6 lands at 2.0), fogColor/ambient/background/fogDensity (atmosphere applied in OnEnable, restored in OnDisable). count default 700. crossSection is now scaled (radius, radius, 1) for a unit-radius ring.

### callerChanges

Scene05Builder.cs Stage():
1. Wattson start: `Wattson(PlatformPoint + new Vector3(-4f, 0, 0), WireCamPos);` (was PlatformPos + (-4, 1.1, 0.4)).
2. Macro beat: replace `Vfx("race into wire surface", macro.transform, new Vector3(0, 1.2f, -1.5f), 1.6f)` with `WireFx("race into wire surface", new Vector3(0, 0.35f, -0.3f), macro.transform);` (streaks fly toward Macro B at -Z; cue name unchanged).
3. Delete the lines `Vfx("letters assemble from charges", ...)` and `Vfx("field wave sweeps whole tunnel", ...)`: WireStage builds both; a magenta tag registered after WireStage would win the ref and the cue would show the tag.
4. Replace the whole Bracket block (Node "Bracket", Point A, Point B, Bracket Bar, Placeholder, SetActive, Ref) with `WireBracket();` (refs "Bracket", "Point A", "Point B" keep working; `Gesture(Point, point: "Bracket")` unchanged).
5. Question card: `var question = Card("Question", "WHAT MAKES CHARGE FLOW?", Color.white, null, WireQuestionPos, WireQuestionCap, false, WireQuestionWidth); Face(question); WireGlowFace(question.GetComponent<TMP_Text>(), Color.white);` (needs `using TMPro;`).
6. Concept cards: `Face(Card("Voltage", "VOLTAGE\n<size=56%>Symbol: V\nUnit: volt (V)", palette.voltage, null, WireCardPosition(-1), WireConceptCap, false, 2.6f));` and `Face(Card("Current", "CURRENT\n<size=56%>Symbol: I\nUnit: ampere (A)", palette.current, null, WireCardPosition(+1), WireConceptCap, false, 2.6f));`. Beats() unchanged.
Scene06Builder.cs Stage():
1. `Wattson(PlatformPoint, WireCamPos);`
2. Same two concept-card lines as Scene 5 with `shown: true`.
3. Delete the two `Face(Vfx("time-freeze ...")` / `Face(Vfx("Ohm-niscope flash"...))` lines (WireStage provides them at Wattson's gadget and at Wall Point).
4. Battery Readout: `Face(Card("Battery Readout", "BATTERY ▲", palette.voltage, null, WireOrigin + new Vector3(5.5f, 1.9f, -0.7f), 0.35f));` (beside the + terminal, inside the wall; the cue `boost Card Terminal +` now also grows the terminal's cap ring).
5. For the match-cut from Scene 5, add at the top of Beats(): `Cue("show Card Terminal −"); Cue("show Card Terminal +"); Cue("show Cross Section"); Cue("show Counter"); Cue("wire.count 1");` (optional, fixes CRITIQUE Scene 6 #2).
Scene12Builder.cs Stage():
1. Cards: `Face(Card("Voltage", "VOLTAGE", palette.voltage, null, WireCardPosition(-1), WireConceptCap)); Face(Card("Current", "CURRENT", palette.current, null, WireCardPosition(+1), WireConceptCap)); Face(Card("Resistance", "RESISTANCE", palette.resistance, null, WireResistancePos, 0.3f)); Face(Card("Symbol R", "Symbol: R", palette.resistance, null, WireResistancePos + Vector3.down * 0.6f, 0.13f));`
2. Delete the two `Face(Vfx("carriers impeded ...")` / `Face(Vfx("RESISTANCE word forms ...")` lines (built by WireStage; "Lattice Ions" is now a ref so `Gesture(Point, point: "Lattice Ions")` resolves).
3. `Cue("wire.radius 1.6", ...)` may stay: WireInterior floors it at 2.0 (<= 20% narrowing, camera stays inside); change the note or the value to 2.0 for honesty.
4. Keep `WireStage(NichromeTint, lattice: true, active: false)` after the lab lighting call (either order works; with active:false the lab's RenderSettings are restored in the file).
All scenes: Wattson placed with `Wattson(PlatformPoint, ...)` hovers 0.4 m above the platform deck; `moveTo: "Platform Point"` unchanged.

### requests

1. Scene 12 lab lighting (whoever adds StudyLighting()/the directional "Window Light" and its Global Volume to Scene 12): parent that lighting node under the "Lab" root (`transform.SetParent(refs["Lab"].transform, true)`) so `Hide("Lab")` removes the directional light during the dive; otherwise it still reaches the tunnel interior through the open-angle ends (shadows hide most of it, the wire volume already has priority 1).
2. Optional (CRITIQUE Scene 12 #9, owner of BlockoutAssets.EnsureSymbols / Card polish): give the "Symbols SDF" fallback material the same HDR face/glow as Nunito, so a true "−" (U+2212) and "Ω" glow like the rest; the terminal card uses an en dash "–" from the Nunito atlas to avoid the dull fallback.
3. None in SceneDirector: `wire.*`, `show/hide`, `boost` cues work unchanged.

### unverified

Needs a Unity build/play to confirm: (a) the field-wave mesh particle orientation (ParticleSystemRenderer Mesh mode with alignment Local on an emitter rotated (0,90,0) should lay the torus in the YZ plane and move it +X); (b) HDR _BaseColor x2.4 on the alpha-blended Particles/Unlit sprite blooms as intended without over-exposing 700 carriers; (c) the Wattson_Scene04 root sits roughly at his feet, so a 0.4 m platform offset reads as hovering (adjust PlatformPos if his boots sink into the deck); (d) TextMeshPro "<size=45%>"/"<size=56%>" inside WorldText.Card and the backing bounds; (e) torus/disc children under the non-uniformly scaled tube render round (z-scale counter-stretch); (f) the root-snapshot capture of WireLighting's "Lighting" node and the Volume priority 1 override; (g) WireInterior's RenderSettings swap in Scene 12 (fog/ambient/background) and the near-lens fade distance; (h) the regenerated ChargeGlyph.png / IonGlyph.png import with default texture settings (sRGB, alpha) and TunnelMesh rebuilt in place on the existing Shared/UI/Tunnel.mesh asset; (i) exact on-screen sizes (counter 97/27 px, concept words 61 px, question 59 px, "+" card 48 px, "−" card 78 px at 1080p) computed from the TYPOGRAPHY formula, not measured.

compiles: True

## text

### summary

Project root "D:\AI unity project\AI OHM'S LAW". Typography + data boards pass (agent: text), compile check zero errors.

- WorldText.cs rewritten: cap-height formula from the font face (FontSizeForCap = cap*10*pointSize/(capLine*scale)), LineAdvance incl. lineSpacing, rect height from the real line count, wrap width, line spacing auto (-20 all-caps / 0 prose, overridable), backing padding 0.8/0.5 cap with the width clamp, rounded panel backing via the RoundedPanel sprite (9-sliced SpriteRenderer, corner ~0.4 cap, URP default2DMaterial) with quad fallback. Named tiers in WorldText.Cap/Wrap (PropLabel 0.03, Caption 0.025, Reading/Terminal/Name 0.04, PathLabel 0.045, DataCard 0.15, Omega 0.4, LetterCard 0.26 (+LetterSub 36%), HeroFormula 0.3, Title 0.32, ConceptWord 0.23/ConceptSub 0.13, Question 0.4, ProseCard 0.11 wrap 2.7, ProseSub 0.09, SymbolPair 0.095, VfxTag 0.035, Ink 0.012, Counter 0.5/0.14), screen targets WorldText.Px (20/24/28/36/56/64/115/120/150) and CapForScreenPx/CapAt for cameras at other depths. Scene 4 glow recipe: WorldText.Glow(label, faceMaterial) sets HDR face material + PopIn 0.55/1.45/bob 0.025.
- New Shared/WorldLabel.cs: billboard toward Camera.main (Face convention) with depth/refDepth scale clamped 0.7-2.5; WorldText.Billboard(target, refDepth).
- SceneBuilderBase.cs (Card/Prop/Vfx/Capsule/KidsAt/Materials only): Card() now builds a root "Card <name>" (Ref, Face, boost, billboard) with a "Text" child; Look.Glow (default) = per-colour TMP Distance Field asset Shared/Art/Materials/Word_<hex>.mat with Hdr(color,2.6)+GLOW_ON, PopIn, spark burst via Emitter()+Glow("Glow_Spark_<hex>"); Look.Panel = rounded backing from the palette panel colour; Look.Plain. Prop() labels only with label:true at PropLabel/wrap 0.45 facing an optional camera position. Vfx() tag at 0.035 cap with a frame hugging the text. Capsule names at Cap.Name. KidsAt() places models only; without models it logs and places nothing (kids = POV). cardBacking colour = palette panel at 0.92 alpha.
- SceneBuilderBase.Boards.cs: WorldCanvasScale 0.0015 (computed: StudyBoardsCam is 2.7-2.8 m from the boards -> ~485 px/m -> 44 px cells = ~23 px, 40 px ticks = ~20 px; 0.002 would not fit both boards in frame). Boards face the camera (LookRotation(pos - StudyBoardsCam)); positions exposed: TableBoard (-0.68,0.39,-2.45) lower-left 5-47% x 27-61% of frame, GraphBoard (-2.335,0.925,-2.25) upper-right 50-87% x 42-93%; no overlap, clear of the window wall and ceiling; BoardsWattson (-0.95,1.05,-1.9) used for "Table Point"/"Boards Point". Boards() gained an optional face parameter.
- ObservationTableUI.cs: fonts 44/36/40, RowHeight 84, HeaderRow 100, two-line headers ("Potential Difference\nV (V)", "Current\nI (A)", "V / I"), per-column widths 380/280/420 (sum 3x360), TagWidth 280, canvas 1080x520; palette colours per column (blue/amber/coral headers, lightened cells), highlight/glow backs as palette-derived constants, header auto-size safety net, tags hug the last visible column. Public API unchanged.
- GraphUI.cs: plot 740x520 at margins 170/170 in a 1060x750 canvas with a rounded dark board and faint grid; fonts 40/40/48/48, text rects 320x60 (tags 320x140); line in voltage blue ending at 1.1 A; points white 34 px / amber 50 px on highlight; triangle coral; labels placed up-left of points, tags down-right, ΔI below its leg and ΔV right of the vertical leg so nothing crosses the line or the axes. Public API unchanged.

### api

WorldText (Assets/OhmsLaw/Shared/WorldText.cs):
- enum WorldText.Look { Glow, Panel, Plain }
- static class WorldText.Cap { PropLabel 0.03, Caption 0.025, Reading 0.04, Terminal 0.04, PathLabel 0.045, DataCard 0.15, Omega 0.4, LetterCard 0.26, LetterSub = LetterCard*0.36, HeroFormula 0.3, Title 0.32, ConceptWord 0.23, ConceptSub 0.13, Question 0.4, Counter 0.5, CounterCaption 0.14, ProseCard 0.11, ProseSub 0.09, SymbolPair 0.095, Name 0.04, VfxTag 0.035, Ink 0.012 } -- cap heights in metres; never type a number.
- static class WorldText.Wrap { PropLabel 0.45, Caption 0.45, PathLabel 1.2, DataCard 2.4, Concept 2.6, ProseCard 2.7, Question 6, Ink 0.22 }
- static class WorldText.Px { Tick 20, Small 24, Medium 28, Large 36, DataCard 56, ConceptWord 64, Hero 115, Title 120, Omega 150 } -- screen cap targets.
- const CapToFontSize 14.0625, LineAdvancePerCap 1.918, PadX 0.8, PadY 0.5, CapsLineSpacing -20, ProseLineSpacing 0, BillboardMin 0.7, BillboardMax 2.5
- float FontSizeForCap(TMP_FontAsset f, float capMetres)
- float LineAdvance(TMP_FontAsset f, float capMetres, float lineSpacing) -- baseline-to-baseline in metres
- float CapForScreenPx(float px, float depth, float fovDeg); float CapAt(float px, Vector3 position, Vector3 cameraPos, Vector3 cameraLook, float fovDeg) -- e.g. CapAt(Px.DataCard, pos, StudyBoardsCam, StudyBoardsLook, 44) = 0.11 m.
- bool AllCaps(string text) -- ignores rich-text tags
- string Sub(string headline, string sub, float cap, float subCap) -> "V\n<size=36%>Potential Difference</size>"
- Color PanelColor -- palette panel RGB at 0.92 alpha
- TextMeshPro Card(string name, string text, Color color, Transform parent, Vector3 localPosition, float height = Cap.DataCard, Material backing = null, float width = 0, float? lineSpacing = null) -- height = cap metres; width > 0 wraps; backing != null adds the rounded panel child "Backing". Signature is positionally compatible with the old one.
- PopIn Glow(TextMeshPro label, Material faceMaterial) -- Scene 4 look (material + PopIn); builders add sparks.
- WorldLabel Billboard(Component target, float refDepth) -- refDepth 0 = face only.

WorldLabel (new Assets/OhmsLaw/Shared/WorldLabel.cs): MonoBehaviour { float refDepth; float minScale, maxScale } faces Camera.main in LateUpdate and scales localScale by clamp(depth/refDepth, 0.7, 2.5). Do not put it on an object the "boost" cue scales or that PopIn scales.

SceneBuilderBase (Assets/OhmsLaw/Editor/SceneBuilderBase.cs):
- GameObject Prop(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, string note, Material material = null, Vector3 euler = default, bool collider = false, bool label = false, Vector3? face = null) -- label true adds "Label <name>" at Cap.PropLabel/Wrap.PropLabel, facing `face` (camera position) else world -Z; parented to the prop.
- GameObject Vfx(string description, Transform parent, Vector3 position, float width = 1.2f) -- width is now the wrap width; frame hugs the 0.035 cap tag.
- GameObject Card(string name, string text, Color color, Transform parent, Vector3 position, float height = WorldText.Cap.DataCard, bool shown = false, float width = 0) -- unchanged shape, Look.Glow.
- GameObject Card(string name, string text, Color color, Transform parent, Vector3 position, float height, WorldText.Look look, bool shown = false, float width = 0, float? lineSpacing = null, bool sparks = true, float billboard = 0) -- returns the root "Card <name>" (Ref'd); the TextMeshPro is root.transform.Find("Text"); Sparks child only for Glow; billboard = refDepth metres.
- Material WordMaterial(Color color) -- Shared/Art/Materials/Word_<RRGGBB>.mat, Scene 4 recipe.
- ParticleSystem Sparks(Transform parent, Color color, float radius) -- Scene 4 burst (60, 1 s, sphere r).
- GameObject Capsule(...) unchanged signature, name label at Cap.Name.
- void KidsAt(Vector3 centre, Vector3 facing, Transform parent = null) -- models only; logs and places nothing otherwise.

SceneBuilderBase.Boards (Assets/OhmsLaw/Editor/SceneBuilderBase.Boards.cs):
- const float WorldCanvasScale = 0.0015f
- static Vector3 TableBoard => BoardsAnchor + (0.67, -0.36, -0.2)  // (-0.68, 0.39, -2.45), 1.62 x 0.78 m
- static Vector3 GraphBoard => BoardsAnchor + (-0.985, 0.175, 0)  // (-2.335, 0.925, -2.25), 1.59 x 1.125 m
- static readonly Vector3 BoardsWattson = (-0.95, 1.05, -1.9)  // marker position for "Table Point" and "Boards Point"
- static readonly Vector2 TableCanvas (1080, 520), GraphCanvas (1060, 750)
- void Boards(bool withTable, bool withGraph, bool unitsInTable = false, Vector3? face = null) -- face defaults to StudyBoardsCam; refs "Boards Set", "Observation Table", "Graph", markers "Table Point", "Boards Point", Vfx "[VFX] columns become axes" at the seam between the boards.

ObservationTableUI: public const ColumnWidth 360, RowHeight 84, HeaderRow 100, TagWidth 280, HeaderFont 36, CellFont 44, TagFont 40; headers default two-line; API (ShowRows, ShowRatio, SetRatio, Highlight, Pair, ClearHighlights, GlowColumn, Sweep, RowTapped, units) unchanged.
GraphUI: public const Width 740, Height 520, MaxI 1.2, MaxV 6, MarginLeft 170, MarginBottom 170, TickFont 40, PointFont 40, AxisTitleFont 48, TagFont 48; API (Axes, Plot, Line, Highlight, Triangle, Pulse) unchanged.

### callerChanges

All existing calls compile unchanged; Card heights were already caps (x14 -> x14.06), so only the semantics below need updating:

1. Card look. The 8-arg Card(...) is now Look.Glow (HDR word, no backing). Prose must pass the style: Scene07 "Card Question" -> Card("Question", text, Color.white, null, pos, WorldText.Cap.ProseCard, WorldText.Look.Panel, false, WorldText.Wrap.ProseCard); Scene09 "Card Reminder" the same (Cap.ProseCard, Look.Panel, Wrap.ProseCard) and its child WorldText.Card("Your Prediction", ..., WorldText.Cap.ProseSub, cardBacking, WorldText.Wrap.ProseCard). Scene06 "Card Battery Readout": Look.Glow is fine; it is boosted, so no billboard.
2. Cards are now root + "Text" child. Anything that needs the TextMeshPro uses refs["Card X"].transform.Find("Text"). Face(card) and card.transform.rotation (Scene05 question/Vfx) keep working on the root. "boost Card V/I/R" and "boost Card Terminal +" keep working (root scale, PopIn is on the child).
3. Tiers, by scene (replace the literal heights):
   - Scene05/06 (Wire Inside cam, FOV 60): Card("Voltage", WorldText.Sub("VOLTAGE", "Symbol: V\nUnit: volt (V)", WorldText.Cap.ConceptWord, WorldText.Cap.ConceptSub), palette.voltage, null, pos, WorldText.Cap.ConceptWord, false, WorldText.Wrap.Concept) and the same for CURRENT; both at ~3.5 m from WireCamPos (CRITIQUE S05 #4 positions o+(0.4,1.9,2.0) / o+(3.2,1.9,0.0)); Face() each. "Card Question" -> Cap.Question with Wrap.Question. Scene12 wire cards: VOLTAGE/CURRENT/RESISTANCE -> Cap.ConceptWord, "Symbol: R" -> Cap.ConceptSub.
   - Scene07: "Voltage → V"/"Current → I" -> Cap.SymbolPair, placed at |x| within the frame and Face()'d toward the camera that reads them.
   - Scene10 "Card Discovery" (V × 2 → I × 2), Scene11 "Card Proportional" (V ∝ I), Scene12 Constant/Constant Word/Resistance R/Formula/Slope/One Ohm: these are read from StudyBoardsCam (2.7 m, FOV 44), closer than the 3.5 m the Cap.DataCard tier assumed, so use WorldText.CapAt(WorldText.Px.DataCard, pos, StudyBoardsCam, StudyBoardsLook, 44) (= 0.11 m) with width WorldText.Wrap.DataCard, and "Ohm Symbol" CapAt(Px.Omega, ...) (= 0.30 m). Free regions in the Boards frame: above the table (frame 5-47% x 62-95%, world x -1.5..0.1, y 0.85..1.45, z -2.45; Wattson hovers at BoardsWattson in the upper-left of it) and over the bench below the graph (50-87% x 25-42%, world x -3.1..-1.6, y -0.05..0.3). Place "Card Discovery" above table rows 1-2: TableBoard + (0.3, 0.6, 0). Face every card: Face(card, StudyBoardsCam).
   - Scene13 (Cards cam ~3.9 m): Card("V", WorldText.Sub("V", "Potential Difference", WorldText.Cap.LetterCard, WorldText.Cap.LetterSub), palette.voltage, null, pos, WorldText.Cap.LetterCard, true, 1.3f) and I/R likewise at x = -1.6/0/1.6; "Card Law" -> Cap.HeroFormula; "Card Title" -> Cap.Title (or CapAt(Px.Hero/Px.Title, ...) if the camera moves). Face them toward the Cards camera.
4. Boards: Scene10/11/12 keep calling Boards(...); the table/graph now sit at TableBoard/GraphBoard facing StudyBoardsCam, so the Boards shot must use Cam/Vcam("Boards", StudyBoardsCam, StudyBoardsLook, 44) (and Scene12's "Wire Boards Anchor" offset still moves the set as a whole). Wattson's "Table Point"/"Boards Point" are at BoardsWattson (-0.95, 1.05, -1.9); point him at "Observation Table"/"Graph" from there. If Ohm stands behind the bench in 11-13, keep his head below y 0.36 at x -3.1..-1.5 or he pokes through the graph board's bottom margin.
5. KidsAt(...) in Scenes 7-13 now places nothing without models (it logs once); the calls can stay or be removed.
6. Prop(...) no longer labels by default. Rig/Lab kits: add label: true, face: <camera position> only where a storyboard name is wanted (e.g. Prop(..., metal, default, true, label: true, face: StudyCloseCam)).
7. Vfx(...) width is the wrap width; existing widths 0.9-2.4 are fine.

### requests

Minimal changes in files I do not own:

- Assets/OhmsLaw/Editor/SceneBuilderBase.Rig.cs (rig agent): line 33 Card(name, name, palette.current, root.transform, pos, 0.12f, true) -> height WorldText.Cap.Terminal; line 70 Path Label -> WorldText.Card(..., WorldText.Cap.PathLabel, cardBacking, WorldText.Wrap.PathLabel) and Face toward the Bench Close camera; Meter(): reading 0.06f -> WorldText.Cap.Reading, caption 0.05f -> WorldText.Cap.Caption with width WorldText.Wrap.Caption and rotation LookRotation(pos - <camera>), placed above the instrument; Prop(...) calls: add label: true, face: <camera> only for props that should keep a name.
- Assets/OhmsLaw/Editor/SceneBuilderBase.Wire.cs (wire agent): after creating the Counter, call Face(counter) (TYPOGRAPHY: the counter renders edge-on); counter sizes can stay as WorldText.Cap.Counter / Cap.CounterCaption; terminal cards 0.6f -> 0.4f (CRITIQUE S05 #4) and the + terminal colour should not be palette.resistance (S05 #5).
- Assets/OhmsLaw/Editor/SceneBuilderBase.Lab.cs (lab agent): already uses WorldText.Cap.Ink/PropLabel and WorldText.Billboard — no change needed; note WorldLabel must not sit under a non-uniformly scaled parent (it sets world rotation each frame).
- Assets/AdaptiveLearning/Art/Interface/RoundedPanel.png.meta (asset): if the world card backing renders wrong in Unity, set spriteMeshType to 0 (FullRect); Sliced SpriteRenderers warn on Tight sprites.
- Assets/OhmsLaw/Shared/SceneDirector.cs: none required. Optional: Boost() could scale refs["Card X"].transform.Find("Text") instead of the root if a billboarded card ever needs boosting.

### unverified

Needs a Unity build/play to confirm:
- SpriteRenderer 9-slice backing: RoundedPanel.png is imported with Tight mesh (spriteMeshType 1); Unity may warn and the slice may look off — fallback is a quad with the CardBacking material (used automatically when the palette sprite is missing). Also the URP default2DMaterial assignment renders as expected.
- The HDR face material on the TMP fallback font (Symbols SDF) for Ω, ∝, →, Δ: TMP copies face/glow properties to fallback materials, so these should glow like the Nunito glyphs, but that is unverified (CRITIQUE S12 #9).
- Screen-size math: the 20-24 px caps and the frame fractions (table 5-47% x 27-61%, graph 50-87% x 42-93%, Wattson 33% x 78%) were computed by hand from StudyBoardsCam/Look at FOV 44 and 16:9; a shot log will confirm.
- Header auto-size: "Potential Difference" at 36 px in a 380 px column is near the wrap limit; enableAutoSizing (min 28.8) should keep it on two lines. "1.0 ÷ 0.20 = 5" at 44 px in the 420 px V/I column assumed ~0.55 em/char.
- Glyph collision estimates in GraphUI (point labels up-left, tags down-right, ΔI/ΔV placement) assume Nunito Bold average advances; verify on the Scene 11/12 shots.
- Cards created under an inactive parent cannot lay out (TMP needs an awake component); all current kits build cards before deactivating, but new callers must too.
- The per-colour Word_<hex>.mat and Glow_Spark_<hex>.mat assets are created on first build under Assets/OhmsLaw/Shared/Art/Materials.
- The compile check briefly failed on SceneBuilderBase.Lab.cs (OhmWorker.needle, another agent's in-progress edit); it cleared on the final run.

compiles: True

## ui

### summary

Screen-UI polish pass, presentation only, in D:\AI unity project\AI OHM'S LAW (compile check "bash Tools/compile_check.sh ui": zero errors, no warnings in the owned files).
- Assets/OhmsLaw/Shared/UI.cs: type-scale constants from TYPOGRAPHY.md "Screen UI" (ButtonFont 34, ScreenButtonFont 36, SubtitleFont 44/SpeakerFont 28/SubtitleTop 236, MCQ 40/36/30 row 108, PromptFont 42, SliderTickFont 30), HudAlpha 0.95 and theme colour helpers (HudColor, AccentColor, AccentFill, RaisedColor); Panel() gained a corner-radius parameter (rescales the sliced RoundedPanel border); UI.Button default font 34, always creates its label (FlowOverlay/ModulePlayer pass "" and then set .text - that was a null label before), and gets a hover/pressed white wash via UI.Hover(); UI.Circle(hole) builds a crisp 128 px disc/ring sprite once; two runtime-only MonoBehaviours UIFade (fade in, Dismiss() fades out then destroys) and SliderReadout (label follows a Func<float>).
- SubtitleUI.cs: box 40..236, panel at 0.95 alpha with 22 px corners, speaker 28 (letter-spaced), body 44 (142 px body = two 60 px lines, a third pages); pages now turn when VO progress reaches each page's first character (equal pages: same 50 % as before; 2+1-line texts turn at two thirds instead of one half). S05_L08 (4 lines), S11_L04 (3 lines), S12_L15 (2 lines) checked against Nunito advances: nothing can clip.
- MCQPanel.cs: rounded 0.95 panel (28 px corners), question 40 white sized from GetPreferredValues (1-3 lines), options 92 px tall on 108 rows (>= 88) with the letter in a voltage-blue disc ("A. Increase" -> badge "A" + "Increase"; options without a prefix take the index letter), raised option tiles, hint 30 in palette.current, fade-in via UIFade; Close() fades out. API unchanged.
- SceneDirector.cs (Prompt/Slider/ScreenButton/TapHint + constants only): prompt is a text-fitted pill (480..1200 x 108, 54 px corners, AccentFill, 42 px, non-raycast) at y -140 below the controls, fading in/out; Slider is a 620x220 "BATTERY" panel at (60,260) above the subtitle box with title, live "0.0 V" readout of rig.state.voltage, blue fill, 0-5 V tick marks + 30 px labels in 96x44 rects, 96 px disc handle; ScreenButton = UI.Button at 36 px in AccentFill; TapHint = Scene 4's icon (0.22 disc + crisp ring + finger + palm, pulse 1 + 0.12 sin 5t, no "TAP" text); Spotlight untouched.
- FlowOverlay.cs: 26 px, dim key column + value tabbed to x 150 with monospaced digits for Time/FPS, 0.72 alpha rounded panel; behaviour unchanged.

### api

UI (static, Assets/OhmsLaw/Shared/UI.cs):
- public const float ButtonFont = 34, ScreenButtonFont = 36, SubtitleFont = 44, SpeakerFont = 28, SubtitleTop = 236, MCQQuestionFont = 40, MCQOptionFont = 36, MCQHintFont = 30, MCQRow = 108, PromptFont = 42, SliderTickFont = 30, HudAlpha = 0.95f  -- the screen type scale; use these instead of literals.
- public static Color PanelColor / HudColor / AccentColor / AccentFill / RaisedColor  -- theme panel, panel at 0.95 alpha, palette.voltage, blue button fill, raised tile colour (all fall back when Theme is null).
- public static Color Alpha(Color color, float alpha)
- public static Image Panel(RectTransform rect, Color color, bool round = true, float corner = 16f)  -- corner = on-screen radius in reference px (new optional parameter; existing calls unchanged).
- public static Button Button(..., float fontSize = ButtonFont)  -- default 30 -> 34; label child is now always present; hover wash added.
- public static Image Hover(Button button)  -- adds the hover/pressed wash and ColorBlock to a hand-built Button (targetGraphic becomes the wash; the panel Image on the button GameObject stays recolourable).
- public static Sprite Circle(float hole = 0f)  -- cached anti-aliased disc (0) or ring (inner radius fraction, e.g. 0.9).
New classes in UI.cs (runtime AddComponent only, never serialised):
- public class UIFade : MonoBehaviour { public float inSeconds = 0.25f, outSeconds = 0.18f; public void Dismiss(); }  -- fades a CanvasGroup in on creation, out + Destroy after Dismiss(); unscaled time.
- public class SliderReadout : MonoBehaviour { public TMP_Text label; public System.Func<float> value; public string format = "0.0 V"; }
SubtitleUI, MCQPanel, SceneDirector, FlowOverlay: no public signature changes (MCQPanel constructor, WaitForPick, Picked, Mark, ClearMarks, Hint, Shake, Close all kept; Close now fades instead of destroying instantly).

### callerChanges

None required. Cues "prompt <text>", "prompt" (clear), "rig.slider 1|0", "spotlight" and the gates (TapHint on rig.key / rig.ammeter, ScreenButton "Record" / "Times Two" at (720,250)) keep their names and positions. Option strings may keep the "A. " prefix: the letter is moved into the accent badge automatically; strings without a prefix ("5", "0.2") get the index letter. Builders that place other screen UI should keep clear of: subtitle box x 210..1710, y 40..236; battery control x 60..680, y 260..480 (Scene 10 while rig.slider is on); prompt pill centred at y -140..-248 from the top (up to 1200 wide); Record/x2 chip x 720..980, y 250..350.

### requests

None mandatory. Optional, CinematicControlsUI.cs (not owned): its volume popup (520x300 at (-36,-136), right-anchored) can overlap the right end of a prompt pill wider than ~800 px while both are open; the pill is text-fitted so the three existing prompts (<= ~930 px) do not reach it. If a longer prompt is ever added, move the popup to y -246 (below the pill) or let the controls' CanvasGroup also hide the popup after idle. Optional, ModulePlayer.cs: its Toggle buttons relied on UI.Button creating a label for "" - now fixed on the UI side, no change needed there.

### unverified

Everything visual needs a Unity run: the sliced RoundedPanel re-radiused through Image.pixelsPerUnitMultiplier (border 16 px assumed from the sprite meta), the pill/MCQ/battery layouts, the hover wash (ColorBlock alpha on a separate white Image; wash.canvasRenderer.SetColor(clear) is relied on to avoid a first-frame flash), TMP rich tags <pos>/<mspace>/<alpha> in FlowOverlay, and GetPreferredValues() measured before the first canvas layout (used for MCQ question height and prompt width). UIFade and SliderReadout live in UI.cs and are attached with AddComponent at runtime - fine for code-built UI, but they cannot be added in the Inspector or saved in a scene. Subtitle paging was checked arithmetically against the Nunito SDF glyph advances (S05_L08 4 lines -> 2 pages, S11_L04 3 lines -> 2 pages turning at ~2/3, S12_L15 2 lines -> 1 page), not rendered. No git baseline exists for Assets/OhmsLaw (untracked), so the edit scope in SceneDirector.cs was kept to the "on-screen controls" section by construction rather than confirmed by diff.

compiles: True
