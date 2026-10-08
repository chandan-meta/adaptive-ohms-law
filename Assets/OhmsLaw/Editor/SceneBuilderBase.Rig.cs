using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace OhmsLaw.Editor
{
    // Stage kit: the modern circuit on the classroom bench (Scenes 8-13), laid out like the reference frames
    // (Docs/Polish/References/Scene09_*.png): DC source, ammeter, X —nichrome wire— Y, switch, the voltmeter behind
    // the wire and, while the meters wait (Scenes 8-9), the parts tray at the right end. The instruments are the imported prefabs; posts, badges, key,
    // cables, contact shadows and the X → Y pulse are built here.
    public abstract partial class SceneBuilderBase
    {
        const string Imported = "Assets/AdaptiveLearning/Prefabs/ImportedLabEquipment/";
        const string RigTextures = Art + "/Textures/";
        const string ImportedTextures = "Assets/AdaptiveLearning/Art/ImportedOhmsLab/OhmsLaw_circuit_Final.fbm/";

        // Classroom frame (ART_DIRECTION §2, §5): bench top centre at the origin, 2.0 x 0.9 m, cameras on the -Z side,
        // so +X is frame-right and every dial faces -Z. "Bench Close" stands at RigBenchCam; text is faced to it at build.
        public static readonly Vector3 RigBench = new Vector3(0, 0.80f, 0);
        public static readonly Vector3 TrayPointC = RigBench + new Vector3(1.0f, 0.55f, 1.0f);   // above the meter tray, never between the lens and a meter face
        public static readonly Vector3 KeyPointC = RigBench + new Vector3(0.95f, 0.5f, 1.0f);    // above the key, inside the frame the panel leaves free
        public static readonly Vector3 RigBenchCam = new Vector3(0, 1.15f, -1.9f), RigBenchLook = new Vector3(0, 0.82f, 0);
        public const float RigBenchFov = 36f;

        // Bench-local layout (metres on the top), left → right as in the reference: source, ammeter (back row), X, wire,
        // Y, key (front right); voltmeter behind the wire; parts tray behind the key, where the meters wait in Scenes 8-9.
        const float SourceScale = 0.18f, MeterScale = 0.21f;                                          // 0.34 x 0.22 x 0.24 m, 0.24 x 0.22 x 0.34 m
        static readonly Vector3 SourceAt = new Vector3(-0.75f, 0, 0.06f), AmmeterSlotAt = new Vector3(-0.28f, 0, 0.21f);
        static readonly Vector3 VoltmeterSlotAt = new Vector3(0.12f, 0, 0.27f), KeyAt = new Vector3(0.75f, 0, -0.18f);
        static readonly Vector3 TrayAt = new Vector3(0.70f, 0, 0.21f);
        const float TrayWidth = 0.56f, TrayDepth = 0.44f, TrayBase = 0.012f, TraySlotDx = 0.14f;
        static readonly Vector2 MeterFootprint = new Vector2(0.24f, 0.34f), SourceFootprint = new Vector2(0.34f, 0.24f);
        // Lead anchors in each imported model's own space at scale 1 (fronts face -Z; from the old lesson's anchors).
        // The source posts get a red / black sleeve whose tip is 0.05 in front of the anchor.
        static readonly Vector3 SourcePlusPost = new Vector3(-0.08f, 0.27f, -0.70f), SourceMinusPost = new Vector3(-0.58f, 0.27f, -0.70f);
        static readonly Vector3 MeterPostLeft = new Vector3(-0.46f, 0.18f, -0.63f), MeterPostRight = new Vector3(0.46f, 0.18f, -0.63f);
        const float SleeveLength = 0.1f;
        // Wire XY: a thin nichrome rod 5 cm over the bench between two brushed posts on black bases.
        const float WireZ = -0.18f, WireHalf = 0.35f, WireRadius = 0.004f;
        const float BaseSize = 0.07f, BaseHeight = 0.015f, PostRadius = 0.013f, PostHeight = 0.05f;
        const float PostTop = BaseHeight + PostHeight, WireHeight = PostTop - 0.01f;                 // the rod runs through each post just under its top
        const float BadgeSize = 0.062f, BadgeBack = 0.012f;                                           // dark cube on the post's +X side (reference)
        static readonly Vector3 WireCentre = new Vector3(0, WireHeight, WireZ);
        static readonly Vector3 XPost = new Vector3(-WireHalf, 0, WireZ), YPost = new Vector3(WireHalf, 0, WireZ);
        // Lever key: black base, two brushed terminals, a hinged arm with a rubber grip. CircuitRig turns "LeverHinge" about Z.
        const float KeyBaseW = 0.26f, KeyBaseH = 0.04f, KeyBaseD = 0.10f, KeyTerminalDx = 0.09f, KeyTerminalR = 0.014f, KeyTerminalH = 0.03f;
        const float KeyHingeX = -0.06f, KeyPivotY = KeyBaseH + KeyTerminalH + 0.004f, KeyArmLength = 0.15f, KeyHandleLength = 0.065f, KeyOpenDegrees = 35f;
        // Cables: 6 mm radius (ART_DIRECTION §5) resting on the top; the voltmeter leads are thinner.
        const float LeadWidth = 0.012f, ThinLead = 0.006f, LeadRest = 0.006f, LeadExit = 0.03f, PathWidth = 0.009f;
        const float ReadingHeight = 1.3f;                                                              // readout above a meter, in the meter's local units
        static readonly Color Magenta = new Color(0.878f, 0.251f, 0.984f);                             // #E040FB, the pulse
        const float ChevronBar = 0.03f, ChevronDegrees = 38f;

        Material nichrome, steel, matteBlack, badgeNavy, rubber, graphite, leadRed, leadBlack, sleeveRed, sleeveBlack, rigShadow;
        Material pulseCore, pulseHalo, pulseTrail, pulseChevron;

        // Modern circuit on the bench. assembled: meters already in their slots (Scene 10 onward). A closed key with
        // voltage 0 reads nothing; Scene 9 closes it at CircuitRig.KeyClosedDefaultVolts, so Scene 10 should start there.
        protected CircuitRig Rig(bool assembled, bool keyClosed, float voltage, bool active = true)
        {
            var root = Node("Circuit Rig", null, RigBench);
            var rig = root.AddComponent<CircuitRig>();
            rig.state = new CircuitState { keyClosed = keyClosed, voltage = voltage };
            rig.sfx = sfx;
            rig.click = sounds["Click"];
            rig.snap = sounds["Pop"];
            var bench = root.transform;
            Vector3 W(Vector3 local) => bench.TransformPoint(local);
            RigMaterials();
            var billboards = new List<Transform>();

            // Nichrome wire XY: the rod and its two posts with the X / Y badges ("Card X", "Card Y"). Never glows.
            var wire = Ref(Node("NICHROME WIRE — XY", bench, WireCentre));
            Prim(PrimitiveType.Cylinder, "Nichrome Rod", wire.transform, Vector3.zero, new Vector3(WireRadius * 2, WireHalf, WireRadius * 2), nichrome, new Vector3(0, 0, 90));
            Post("X", wire.transform);
            Post("Y", wire.transform);
            // "It highlights" without the rod glowing: a soft cyan band standing just behind XY (behind the badges too),
            // alpha-blended (additive vanishes on the light top), so the dull rod reads as a dark line across it. Lying flat
            // on the top it was a 3 px sliver at the bench cameras' grazing angle.
            var highlight = Prim(PrimitiveType.Quad, "Wire Highlight", bench, new Vector3(0, WireHeight, WireZ + 0.05f), new Vector3(2 * WireHalf, 0.08f, 1),
                Decal("WireHighlight", new Color(0.15f, 0.7f, 0.9f, 0.55f), RigTexture(RigTextures + "RigShadow.png")));
            highlight.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            highlight.SetActive(false);
            rig.wireHighlight = Ref(highlight);

            // Meters: in their slots (Scene 10 onward) or side by side in the parts tray (Scenes 8-9), dials to the camera.
            rig.ammeterSlot = Slot("Ammeter Slot", bench, AmmeterSlotAt);
            rig.voltmeterSlot = Slot("Voltmeter Slot", bench, VoltmeterSlotAt);
            var trayA = Slot("Tray Ammeter", bench, TrayAt + new Vector3(-TraySlotDx, TrayBase, 0));
            var trayV = Slot("Tray Voltmeter", bench, TrayAt + new Vector3(TraySlotDx, TrayBase, 0));
            var ammeter = Meter("Ammeter", "AMMETER", palette.current, "Ammeter_0to1p2.png", "Ammeter1", "niddle_grp", "0.00 A",
                assembled ? rig.ammeterSlot : trayA, bench, billboards, out rig.ammeterReading, out rig.ammeterNeedle);
            var voltmeter = Meter("Voltmeter", "VOLTMETER", palette.voltage, "VoltMeter_0to6.png", "Voltmeter", "niddle_grp1", "0.0 V",
                assembled ? rig.voltmeterSlot : trayV, bench, billboards, out rig.voltmeterReading, out rig.voltmeterNeedle);
            rig.ammeter = ammeter.transform;
            rig.voltmeter = voltmeter.transform;
            rig.ammeterPlaced = rig.voltmeterPlaced = assembled;
            Ref(ammeter); Ref(voltmeter);
            rig.seriesGlow = SlotGlow("Series Slot Glow", bench, AmmeterSlotAt, palette.current);
            rig.parallelGlow = SlotGlow("Parallel Slot Glow", bench, VoltmeterSlotAt, palette.voltage);
            if (!assembled) PartsTray(bench); // empty once the meters are in: not part of the §5 bench layout

            // Variable DC source at the left, panel to the camera: repainted 0-5 V panel, knob and pilot lamp driven by
            // CircuitRig, a red + and a black − sleeve on its posts.
            var source = Model(Imported + "DCSource.prefab", "cells / controllable DC source", bench, SourceAt, Vector3.zero, SourceScale);
            var panelBody = FindChild(source, "Power_Supply 1").GetComponent<Renderer>();
            var panelMats = panelBody.sharedMaterials;
            var panelSource = panelMats[0];
            var panelTexture = RigTexture(RigTextures + "Power_Supply_0to5.jpg");
            var lampMap = AssetDatabase.LoadAssetAtPath<Texture2D>(ImportedTextures + "Power_Supply_EMS.jpg");
            panelMats[0] = Mat("DCSource_Panel", panelSource.shader.name, m =>
            {
                m.CopyPropertiesFromMaterial(panelSource);
                if (panelTexture) m.SetTexture("_BaseMap", panelTexture);
                m.EnableKeyword("_EMISSION");
                m.SetTexture("_EmissionMap", lampMap);
                m.SetColor("_EmissionColor", Color.black); // CircuitRig lights it per renderer while the key is closed
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            });
            panelBody.sharedMaterials = panelMats;
            rig.sourceBody = panelBody;
            rig.lampMaterialIndex = 0;
            rig.sourceKnob = FindChild(source, "Round_Switch");
            foreach (var (name, post, sleeve) in new[] { ("Post +", SourcePlusPost, sleeveRed), ("Post −", SourceMinusPost, sleeveBlack) })
                Prim(PrimitiveType.Cylinder, name, source.transform, post, new Vector3(0.11f, SleeveLength / 2, 0.11f), sleeve, new Vector3(90, 0, 0));
            RigShadow(source.transform, Vector3.zero, SourceFootprint, SourceScale);
            Ref(source);

            // Key at the front right: the lever rests on the far terminal when closed, rises 35° when open.
            var key = Ref(Node("switch / key", bench, KeyAt));
            var k = key.transform;
            Prim(PrimitiveType.Cube, "Base", k, Vector3.up * (KeyBaseH / 2), new Vector3(KeyBaseW, KeyBaseH, KeyBaseD), matteBlack);
            foreach (float side in new[] { -1f, 1f })
                Prim(PrimitiveType.Cylinder, side < 0 ? "Terminal A" : "Terminal B", k, new Vector3(side * KeyTerminalDx, KeyBaseH + KeyTerminalH / 2, 0), new Vector3(KeyTerminalR * 2, KeyTerminalH / 2, KeyTerminalR * 2), steel);
            Prim(PrimitiveType.Cube, "Hinge Block", k, new Vector3(KeyHingeX, (KeyBaseH + KeyPivotY) / 2, 0), new Vector3(0.02f, KeyPivotY - KeyBaseH, 0.024f), steel);
            var hinge = Node("LeverHinge", k, new Vector3(KeyHingeX, KeyPivotY, 0)).transform;
            Prim(PrimitiveType.Cube, "Lever Arm", hinge, new Vector3(KeyArmLength / 2, 0.004f, 0), new Vector3(KeyArmLength, 0.008f, 0.016f), steel);
            Prim(PrimitiveType.Cylinder, "Handle", hinge, new Vector3(KeyArmLength + KeyHandleLength / 2, 0.004f, 0), new Vector3(0.024f, KeyHandleLength / 2, 0.024f), rubber, new Vector3(0, 0, 90));
            hinge.localRotation = Quaternion.Euler(0, 0, keyClosed ? 0 : KeyOpenDegrees);
            rig.key = k;
            rig.keyLever = hinge;
            FitCollider(key); // the tap target, fitted before the cables (renderers too) join the key
            RigShadow(k, Vector3.zero, new Vector2(KeyBaseW, KeyBaseD));

            // Terminals, world space. Red on the + side (source + → ammeter → X), black return (Y → key → source −);
            // the voltmeter leads are thin and take the posts' tops. The series lead enters X from the outer side and
            // leaves Y from the back, so neither crosses a badge.
            Vector3 sourcePlus = source.transform.TransformPoint(SourcePlusPost + Vector3.back * (SleeveLength / 2));
            Vector3 sourceMinus = source.transform.TransformPoint(SourceMinusPost + Vector3.back * (SleeveLength / 2));
            Vector3 ammeterL = rig.ammeterSlot.TransformPoint(MeterPostLeft * MeterScale), ammeterR = rig.ammeterSlot.TransformPoint(MeterPostRight * MeterScale);
            Vector3 voltmeterL = rig.voltmeterSlot.TransformPoint(MeterPostLeft * MeterScale), voltmeterR = rig.voltmeterSlot.TransformPoint(MeterPostRight * MeterScale);
            Vector3 xOuter = W(XPost + new Vector3(-PostRadius, WireHeight, 0)), xTop = W(XPost + Vector3.up * PostTop);
            Vector3 yBack = W(YPost + new Vector3(0, WireHeight, PostRadius)), yTop = W(YPost + Vector3.up * PostTop);
            Vector3 keyA = W(KeyAt + new Vector3(-KeyTerminalDx - KeyTerminalR, KeyBaseH + KeyTerminalH / 2, 0));
            Vector3 keyB = W(KeyAt + new Vector3(KeyTerminalDx, KeyBaseH + KeyTerminalH / 2, KeyTerminalR));
            Vector3 OnTop(float x, float z) => W(new Vector3(x, LeadRest, z));
            Vector3 Lifted(float x, float y, float z) => W(new Vector3(x, y, z));

            var toAmmeter = LeadPath(sourcePlus, Vector3.back, ammeterL, Vector3.up, OnTop(-0.73f, -0.15f), OnTop(-0.60f, -0.10f), OnTop(-0.45f, 0.0f));
            var toX = LeadPath(ammeterR, Vector3.up, xOuter, Vector3.left, Lifted(-0.25f, 0.02f, -0.02f), OnTop(-0.39f, -0.09f), Lifted(-0.42f, 0.02f, -0.16f));
            var toKey = LeadPath(yBack, Vector3.forward, keyA, Vector3.left, Lifted(0.38f, 0.015f, -0.09f), OnTop(0.50f, -0.10f), OnTop(0.57f, -0.15f));
            // The return cable keeps well behind the nichrome rod (z -0.18) so it never reads as the wire from the front cameras.
            var toSource = LeadPath(keyB, Vector3.forward, sourceMinus, Vector3.back,
                OnTop(0.84f, -0.06f), OnTop(0.55f, 0.04f), OnTop(0.0f, 0.06f), OnTop(-0.45f, 0.05f),
                OnTop(-0.70f, -0.10f), OnTop(-0.87f, -0.19f), Lifted(-0.875f, 0.025f, -0.14f));
            rig.ammeterLeads = Node("Ammeter Leads", bench);
            Lead("Lead Source → Ammeter", rig.ammeterLeads.transform, leadRed, LeadWidth, toAmmeter);
            Lead("Lead Ammeter → X", rig.ammeterLeads.transform, leadRed, LeadWidth, toX);
            rig.ammeterLeads.SetActive(assembled);
            Lead("Lead Y → Key", k, leadBlack, LeadWidth, toKey);
            Lead("Lead Key → Source", k, leadBlack, LeadWidth, toSource);
            rig.voltmeterLeads = Lead("Voltmeter Lead X", bench, leadRed, ThinLead, LeadPath(voltmeterL, Vector3.up, xTop, Vector3.up, OnTop(-0.20f, 0.0f), Lifted(-0.29f, 0.025f, -0.06f), Lifted(-0.345f, 0.03f, -0.10f)));
            rig.voltmeterLeadY = Lead("Voltmeter Lead Y", bench, leadBlack, ThinLead, LeadPath(voltmeterR, Vector3.up, yTop, Vector3.up, OnTop(0.30f, 0.0f), Lifted(0.315f, 0.025f, -0.06f), Lifted(0.325f, 0.03f, -0.11f)));
            rig.voltmeterLeads.enabled = rig.voltmeterLeadY.enabled = assembled;

            // Current path: an additive overlay along the cables only, set a little toward the camera so it sits over the
            // ribbons; narrower than the cables and under the bloom threshold, so it tints them amber. Two legs, source ->
            // ammeter -> X and Y -> key -> source: nothing is drawn along XY (the nichrome wire never glows). The return leg
            // is a child, so hiding "Current Path" hides both. Lit by CircuitRig once the ammeter is in (and showPath).
            var pathGlow = Glow("Glow_CurrentPath", Hdr(Color.white, 0.8f), false);
            LineRenderer PathLeg(string name, Transform parent, params Vector3[][] legs)
            {
                var points = new List<Vector3>();
                foreach (var leg in legs) Append(points, leg);
                var line = Polyline(name, parent, pathGlow, PathWidth, false, true, points.Select(p => p + (RigBenchCam - p).normalized * 0.012f).ToArray());
                line.startColor = line.endColor = Color.clear;
                return line;
            }
            rig.currentPath = PathLeg("Current Path", bench, toAmmeter, toX);
            rig.currentPathReturn = PathLeg("Current Path Return", rig.currentPath.transform, toKey, toSource);

            Pulse(rig, bench, billboards);
            foreach (var card in billboards) Face(card.gameObject, RigBenchCam); // CircuitRig keeps them on the live camera
            rig.billboards = billboards.ToArray();

            // Where Wattson hovers to point at things: 0.55 m behind the bench like a teacher behind the desk, never
            // between the cameras and the subject. His prefab pivot is 0.27 m above his boots and 0.44 m under his head:
            // at bench + 0.22 the boots drop just behind the back edge and the head top (y 1.46) sits at 88 % of the
            // Bench Close half-frame at z 1.0, so his bob never clips; the wide shots have room above.
            Marker("Wire Point", RigBench + new Vector3(-0.52f, 0.22f, 1.0f));
            Marker("Tray Point", TrayPointC);
            Marker("Voltmeter Point", RigBench + new Vector3(0.50f, 0.22f, 1.0f));
            Marker("Source Point", RigBench + new Vector3(-1.02f, 0.22f, 1.0f));
            Marker("Key Point", KeyPointC);
            root.SetActive(active);
            Ref(root);
            return rig;
        }

        void RigMaterials()
        {
            var shade = RigTexture(RigTextures + "CableShade.png");
            nichrome = Lit("Nichrome", new Color(0.55f, 0.50f, 0.40f), 0.85f, 0.45f);             // dull gold-grey alloy, no emission
            steel = Lit("PostSteel", new Color(0.70f, 0.71f, 0.73f), 1f, 0.58f);                  // brushed posts, terminals, lever
            matteBlack = Lit("MatteBlack", new Color(0.045f, 0.048f, 0.055f), 0.1f, 0.32f);        // post bases, key base
            badgeNavy = Lit("BadgeNavy", new Color(0.106f, 0.133f, 0.2f), 0f, 0.4f);               // #1B2233
            rubber = Lit("KeyHandle", new Color(0.06f, 0.06f, 0.065f), 0f, 0.5f);
            graphite = Lit("TrayGraphite", new Color(0.1f, 0.11f, 0.12f), 0.3f, 0.45f);
            leadRed = Cable("LeadRed", new Color(0.8f, 0.1f, 0.08f), shade);
            leadBlack = Cable("LeadBlack", new Color(0.09f, 0.09f, 0.1f), shade);
            sleeveRed = Lit("SleeveRed", new Color(0.75f, 0.09f, 0.07f), 0f, 0.55f);
            sleeveBlack = Lit("SleeveBlack", new Color(0.05f, 0.05f, 0.055f), 0f, 0.5f);
            rigShadow = Translucent("RigShadow", new Color(0, 0, 0, 0.7f));
            rigShadow.SetTexture("_BaseMap", RigTexture(RigTextures + "RigShadow.png")); // solid under the footprint, soft penumbra
            rigShadow.renderQueue = (int)RenderQueue.Transparent - 20;                   // under the slot glows and cable overlays
            EditorUtility.SetDirty(rigShadow);
            pulseCore = Lit("PulseCore", new Color(1f, 0.75f, 1f), 0f, 0.7f, Hdr(Magenta, 4.5f));
            pulseHalo = Glow("Glow_PulseHalo", Hdr(new Color(Magenta.r, Magenta.g, Magenta.b, 0.7f), 1.8f));
            pulseTrail = Glow("Glow_PulseTrail", Hdr(Magenta, 2.6f), false);
            pulseChevron = Glow("Glow_PulseChevron", Hdr(Magenta, 3f), false);
        }

        // Dark rubber insulation; the shade texture runs across the ribbon so the flat line reads as a round tube.
        static Material Cable(string name, Color color, Texture2D shade)
        {
            var material = Lit(name, color, 0f, 0.5f);
            material.SetTexture("_BaseMap", shade);
            EditorUtility.SetDirty(material);
            return material;
        }

        // An imported meter at a slot: repainted dial, collider for drag and tap, digit readout above it, contact shadow.
        // The readout is built with its widest runtime text (`widest`) so its backing plate fits every value CircuitRig writes.
        GameObject Meter(string prefab, string name, Color tint, string dial, string bodyMesh, string needlePivot, string widest, Transform at, Transform bench,
            List<Transform> billboards, out TextMeshPro reading, out Transform needle)
        {
            var go = Model(Imported + prefab + ".prefab", name, bench, at.localPosition, Vector3.zero, MeterScale); // collider fitted here, before the readout and shadow join
            var body = FindChild(go, bodyMesh).GetComponent<Renderer>();
            var printed = body.sharedMaterial;
            var texture = RigTexture(RigTextures + dial);
            body.sharedMaterial = Mat(prefab + "_Dial", printed.shader.name, m => { m.CopyPropertiesFromMaterial(printed); if (texture) m.SetTexture("_BaseMap", texture); });
            needle = FindChild(go, needlePivot);
            reading = WorldText.Card("Reading", widest, tint, go.transform, new Vector3(0, ReadingHeight, -0.1f), WorldText.Cap.Reading, cardBacking);
            reading.transform.localScale = Vector3.one / MeterScale;
            billboards.Add(reading.transform);
            RigShadow(go.transform, Vector3.zero, MeterFootprint, MeterScale);
            return go;
        }

        // X / Y terminal (reference): matte black base, brushed post with the rod through it just under the top, and the
        // navy badge with its orange letter on the camera face, fixed to the post's +X side. Badge root = "Card <letter>".
        void Post(string letter, Transform wire)
        {
            float side = letter == "X" ? -1f : 1f;
            var post = Node("Post " + letter, wire, new Vector3(side * WireHalf, -WireHeight, 0)).transform; // at bench level
            // The base carries the badge too (nothing floats, ART_DIRECTION §5): it extends past the post on the badge side.
            float baseW = BaseSize + BadgeSize;
            Prim(PrimitiveType.Cube, "Base", post, new Vector3(BadgeSize / 2, BaseHeight / 2, 0), new Vector3(baseW, BaseHeight, BaseSize), matteBlack);
            Prim(PrimitiveType.Cylinder, "Post", post, Vector3.up * (BaseHeight + PostHeight / 2), new Vector3(PostRadius * 2, PostHeight / 2, PostRadius * 2), steel);
            RigShadow(post, new Vector3(BadgeSize / 2, 0, 0), new Vector2(baseW, BaseSize));
            var badge = Node("Card " + letter, post, new Vector3(PostRadius + BadgeSize / 2, BaseHeight + BadgeSize / 2, BadgeBack));
            Prim(PrimitiveType.Cube, "Cube", badge.transform, Vector3.zero, Vector3.one * BadgeSize, badgeNavy);
            WorldText.Card("Letter", letter, palette.current, badge.transform, new Vector3(0, 0, -(BadgeSize / 2 + 0.0008f)), WorldText.Cap.Terminal);
            Ref(badge); // no PopIn: the badges open every match-cut scene already visible
        }

        // Soft dark quad on the top under an object (contact AO). size in metres; parentScale is the parent's uniform
        // scale (the quad is expressed in the parent's units), 1.35x the footprint so the penumbra shows past the base.
        void RigShadow(Transform parent, Vector3 foot, Vector2 size, float parentScale = 1f)
        {
            var quad = Prim(PrimitiveType.Quad, "Contact Shadow", parent, foot + Vector3.up * (0.002f / parentScale), new Vector3(size.x, size.y, 1f) * (1.35f / parentScale), rigShadow, new Vector3(90, 0, 0));
            quad.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // A pose on the bench for a meter; dials face -Z, toward the cameras, everywhere. The slot carries a flat collider
        // the size of the meter's footprint, so a tap on "the highlighted position" places the selected meter.
        Transform Slot(string name, Transform bench, Vector3 at)
        {
            var slot = Node(name, bench, at);
            var box = slot.AddComponent<BoxCollider>();
            box.center = new Vector3(0, 0.03f, 0);
            box.size = new Vector3(MeterFootprint.x + 0.08f, 0.06f, MeterFootprint.y + 0.08f);
            return slot.transform;
        }

        // The drop target on the bench under a slot: a tinted fill and an 8 mm outline of the meter's footprint, alpha-
        // blended (an additive glow adds nothing visible on the light top). CircuitRig shows and pulses it while that part
        // may be dragged. The root lies flat (its Z points down), so the outline's TransformZ ribbon lies on the top.
        GameObject SlotGlow(string name, Transform bench, Vector3 at, Color colour)
        {
            var glow = Node(name, bench, at + Vector3.up * 0.004f);
            glow.transform.localRotation = Quaternion.Euler(90, 0, 0);
            string key = name.Replace(" ", "");
            Vector2 half = MeterFootprint / 2 + Vector2.one * 0.02f;
            Prim(PrimitiveType.Quad, "Fill", glow.transform, Vector3.zero, new Vector3(half.x * 2, half.y * 2, 1), Translucent(key + "_Fill", new Color(colour.r, colour.g, colour.b, 0.25f)))
                .GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var outline = Polyline("Outline", glow.transform, Translucent(key + "_Outline", new Color(colour.r, colour.g, colour.b, 0.9f)), 0.008f, true, false,
                new Vector3(-half.x, -half.y, -0.001f), new Vector3(half.x, -half.y, -0.001f), new Vector3(half.x, half.y, -0.001f), new Vector3(-half.x, half.y, -0.001f));
            outline.alignment = LineAlignment.TransformZ;
            glow.SetActive(false);
            return glow;
        }

        // Shallow graphite tray behind the key, at the right end; the meters wait in it side by side until Scene 9 places them.
        // Built only for an unassembled rig; hide "Parts Tray" once both meters are in so Scene 9's last frame matches Scene 10's.
        void PartsTray(Transform bench)
        {
            var tray = Node("Parts Tray", bench, TrayAt);
            Prim(PrimitiveType.Cube, "Base", tray.transform, new Vector3(0, TrayBase / 2, 0), new Vector3(TrayWidth, TrayBase, TrayDepth), graphite);
            foreach (float sx in new[] { -1f, 1f })
                Prim(PrimitiveType.Cube, "Rim", tray.transform, new Vector3(sx * (TrayWidth - 0.012f) / 2, 0.02f, 0), new Vector3(0.012f, 0.04f, TrayDepth), graphite);
            foreach (float sz in new[] { -1f, 1f })
                Prim(PrimitiveType.Cube, "Rim", tray.transform, new Vector3(0, 0.02f, sz * (TrayDepth - 0.012f) / 2), new Vector3(TrayWidth, 0.04f, 0.012f), graphite);
            RigShadow(tray.transform, Vector3.zero, new Vector2(TrayWidth, TrayDepth));
            Ref(tray);
        }

        // "[VFX] Pulse travels from X to Y": a magenta glowing sphere with a short trail and three chevrons ahead,
        // running along the rod from X to Y (ART_DIRECTION §6). Hidden; CircuitRig plays it whenever it is shown
        // (a cue, the ammeter snap, the key closing) and hides it again after the repeats.
        void Pulse(CircuitRig rig, Transform bench, List<Transform> billboards)
        {
            var pulse = Node("[VFX] Pulse travels from X to Y", bench, XPost + Vector3.up * WireHeight);
            var head = Prim(PrimitiveType.Sphere, "Pulse", pulse.transform, Vector3.zero, Vector3.one * 0.04f, pulseCore);
            head.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            var halo = Prim(PrimitiveType.Quad, "Halo", head.transform, Vector3.zero, Vector3.one * 2.6f, pulseHalo);
            halo.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            billboards.Add(halo.transform);
            var trail = head.AddComponent<TrailRenderer>();
            trail.sharedMaterial = pulseTrail;
            trail.time = 0.3f;
            trail.minVertexDistance = 0.004f;
            trail.startWidth = 0.028f;
            trail.endWidth = 0f;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0.9f, 0), new GradientAlphaKey(0, 1) });
            trail.colorGradient = fade;
            trail.numCapVertices = 4;
            trail.textureMode = LineTextureMode.Stretch;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.autodestruct = false;
            var chevrons = new Transform[3];
            for (int i = 0; i < chevrons.Length; i++) chevrons[i] = Chevron(pulse.transform, i);
            rig.pulse = Ref(pulse);
            rig.pulseHead = head.transform;
            rig.pulseTrail = trail;
            rig.pulseChevrons = chevrons;
            rig.pulseTo = new Vector3(2 * WireHalf, 0, 0);
            pulse.SetActive(false);
        }

        // A ">" of two glowing bars in the wire's vertical plane, tip at the origin, pointing +X (toward Y); farther ones smaller.
        Transform Chevron(Transform parent, int index)
        {
            var chevron = Node("Chevron " + (index + 1), parent).transform;
            float size = 1f - 0.2f * index, rad = ChevronDegrees * Mathf.Deg2Rad;
            foreach (float sign in new[] { -1f, 1f })
                Prim(PrimitiveType.Cube, "Bar", chevron, new Vector3(-ChevronBar / 2 * Mathf.Cos(rad), sign * ChevronBar / 2 * Mathf.Sin(rad), 0) * size,
                    new Vector3(ChevronBar, 0.005f, 0.003f) * size, pulseChevron, new Vector3(0, 0, -sign * ChevronDegrees))
                    .GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            return chevron;
        }

        static Texture2D RigTexture(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (!texture) Debug.LogWarning("Rig: texture missing, the material keeps its default: " + path);
            return texture;
        }

        // A cable between two terminals, world space: leaves each terminal along its `out` direction, then sags through
        // the waypoints (bench-level knots rest on the top). Catmull-Rom through the knots.
        static Vector3[] LeadPath(Vector3 from, Vector3 fromOut, Vector3 to, Vector3 toOut, params Vector3[] via)
        {
            var knots = new List<Vector3> { from, from + fromOut * LeadExit };
            knots.AddRange(via);
            knots.Add(to + toOut * LeadExit);
            knots.Add(to);
            return CatmullRom(knots, 8);
        }

        static Vector3[] CatmullRom(List<Vector3> knots, int subdivisions)
        {
            var points = new List<Vector3>();
            for (int i = 0; i < knots.Count - 1; i++)
            {
                Vector3 p0 = knots[Mathf.Max(i - 1, 0)], p1 = knots[i], p2 = knots[i + 1], p3 = knots[Mathf.Min(i + 2, knots.Count - 1)];
                for (int s = 0; s < subdivisions; s++)
                {
                    float t = s / (float)subdivisions, t2 = t * t, t3 = t2 * t;
                    points.Add(0.5f * (2 * p1 + (p2 - p0) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (3 * p1 - p0 - 3 * p2 + p3) * t3));
                }
            }
            points.Add(knots[knots.Count - 1]);
            return points.ToArray();
        }

        // Lit cable ribbon: lighting data on, so URP/Lit shades it (a LineRenderer has no normals otherwise).
        LineRenderer Lead(string name, Transform parent, Material insulation, float width, Vector3[] path)
        {
            var line = Polyline(name, parent, insulation, width, false, true, path);
            line.generateLightingData = true;
            line.numCapVertices = 4;
            line.textureMode = LineTextureMode.Stretch;
            return line;
        }

        static void Append(List<Vector3> path, IEnumerable<Vector3> points)
        {
            foreach (var p in points)
                if (path.Count == 0 || (path[path.Count - 1] - p).sqrMagnitude > 1e-6f) path.Add(p);
        }
    }
}
