using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // Stage kit: the modern circuit on the bench (Scenes 8-13), built from the imported instruments in Scene 4's study frame.
    public abstract partial class SceneBuilderBase
    {
        const string Imported = "Assets/AdaptiveLearning/Prefabs/ImportedLabEquipment/";
        const string KeyPrefab = "Assets/AdaptiveLearning/Prefabs/LabEquipment/Switch.prefab"; // knife switch: a lever the learner sees close
        const string RigTextures = Art + "/Textures/";
        const string ImportedTextures = "Assets/AdaptiveLearning/Art/ImportedOhmsLab/OhmsLaw_circuit_Final.fbm/";

        // Bench-local layout: origin at BenchCentre on the bench top, +X toward the room, +Z toward the cameras. Bench 2.0 x 0.9 m.
        // The cameras sit on the room side looking toward -Z, so +X is frame-left: read left to right the frame shows
        // source, ammeter slot, X —wire— Y, key, parts tray (the textbook diagram order), with the voltmeter slot behind the wire.
        const float MeterScale = 0.21f, SourceScale = 0.17f, WireScale = 0.45f, KeyScale = 0.19f;   // 0.24 x 0.22 x 0.34, 0.32 x 0.21 x 0.23, 0.9 x 0.08 x 0.14, 0.23 x 0.2 x 0.13 m
        static readonly Vector3 SourceAt = new Vector3(0.75f, 0, 0.14f), AmmeterSlotAt = new Vector3(0.50f, 0, -0.20f);
        static readonly Vector3 WireAt = new Vector3(0.02f, 0, 0.09f), VoltmeterSlotAt = new Vector3(0.02f, 0, -0.20f);
        static readonly Vector3 KeyAt = new Vector3(-0.42f, 0, 0.28f), TrayAt = new Vector3(-0.72f, 0, 0);
        const float TrayWidth = 0.30f, TrayDepth = 0.78f, TrayBase = 0.012f;
        // Where leads attach, in each model's own space at scale 1 (fronts face local -Z; from the old lesson's anchors).
        static readonly Vector3[] SourcePosts = { new Vector3(-0.08f, 0.27f, -0.70f), new Vector3(-0.58f, 0.27f, -0.70f) };
        static readonly Vector3[] MeterPosts = { new Vector3(0.46f, 0.18f, -0.63f), new Vector3(-0.46f, 0.18f, -0.63f) };
        static readonly Vector3 WireTerminalX = new Vector3(-0.86f, 0.12f, 0), WireTerminalY = new Vector3(0.86f, 0.12f, 0); // the wire is turned 180°, so X lands at +X
        static readonly Vector3[] KeyContacts = { new Vector3(-0.4f, 0.12f, 0), new Vector3(0.4f, 0.12f, 0) };   // hinge side, blade side
        const float RodHeight = 0.137f;  // rod axis above the base at scale 1
        // Cap heights (metres) from Docs/Polish/TYPOGRAPHY.md.
        const float TerminalCap = 0.04f, MeterReadingCap = 0.04f, PropLabelCap = 0.03f, CaptionWrap = 0.45f, PathLabelCap = 0.045f, PathLabelWrap = 1.2f;
        const float LeadWidth = 0.007f, LeadRest = 0.004f;

        Material leadRed, leadBlack;

        // Modern circuit on the bench. assembled: meters already in their slots (Scene 10 onward). A closed key with
        // voltage 0 reads nothing; Scene 9 closes it at CircuitRig.KeyClosedDefaultVolts, so Scene 10 should start there.
        protected CircuitRig Rig(bool assembled, bool keyClosed, float voltage, bool active = true)
        {
            var root = Node("Circuit Rig", null, BenchCentre);
            var rig = root.AddComponent<CircuitRig>();
            rig.state = new CircuitState { keyClosed = keyClosed, voltage = voltage };
            rig.sfx = sfx;
            rig.click = sounds["Click"];
            rig.snap = sounds["Pop"];
            var bench = root.transform;
            Vector3 eye = Vector3.Lerp(StudyBenchCam, StudyCloseCam, 0.5f) - BenchCentre; // the instruments turn toward the cameras
            var billboards = new List<Transform>();
            leadRed = Lit("LeadRed", new Color(0.55f, 0.06f, 0.05f), 0f, 0.6f);
            leadBlack = Lit("LeadBlack", new Color(0.05f, 0.05f, 0.06f), 0f, 0.6f);

            // Nichrome wire XY between its two clamps, along the bench. Dull alloy finish; never glows.
            var wire = Model(Imported + "NichromeWireXY.prefab", "NICHROME WIRE — XY", bench, WireAt, new Vector3(0, 180, 0), WireScale, collider: false);
            FindChild(wire, "Rod_").GetComponent<Renderer>().sharedMaterial = Lit("Nichrome", NichromeTint, 0.75f, 0.42f);
            Caption(wire, "NICHROME WIRE — XY", 0.5f, WireScale, billboards);
            Ref(wire);
            Vector3 x = wire.transform.TransformPoint(WireTerminalX), y = wire.transform.TransformPoint(WireTerminalY); // clamp terminals, world
            var glowFace = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Scene04/Materials/Word_CURRENT.mat"); // Scene 4's HDR amber word face
            foreach (var (name, at) in new[] { ("X", x), ("Y", y) })
            {
                var badge = WorldText.Card("Card " + name, name, glowFace ? Color.white : palette.current, bench, bench.InverseTransformPoint(at) + Vector3.up * 0.11f, TerminalCap);
                if (glowFace) badge.fontSharedMaterial = glowFace;
                billboards.Add(Ref(badge.gameObject).transform);
            }
            var rod = WireAt + Vector3.up * (RodHeight * WireScale);
            var sheath = Hdr(palette.wattsonAccent, 1.2f);
            sheath.a = 0.35f;
            var highlight = Prim(PrimitiveType.Cylinder, "Wire Highlight", bench, rod, new Vector3(0.05f, 0.31f, 0.05f), Glow("Glow_WireHighlight", sheath, false), new Vector3(0, 0, 90));
            highlight.SetActive(false);
            rig.wireHighlight = Ref(highlight);

            // Meters: in their slots (Scene 10 onward) or in the parts tray (Scenes 8-9), dials toward the cameras.
            rig.ammeterSlot = Slot("Ammeter Slot", bench, AmmeterSlotAt, eye);
            rig.voltmeterSlot = Slot("Voltmeter Slot", bench, VoltmeterSlotAt, eye);
            var trayA = Slot("Tray Ammeter", bench, TrayAt + new Vector3(0, TrayBase, -0.19f), null);
            var trayV = Slot("Tray Voltmeter", bench, TrayAt + new Vector3(0, TrayBase, 0.19f), null);
            var ammeter = Meter("Ammeter", "AMMETER", "Measures current — I — ampere (A)", palette.current, "Ammeter_0to1p2.png", "Ammeter1", "niddle_grp",
                assembled ? rig.ammeterSlot : trayA, bench, billboards, out rig.ammeterReading, out rig.ammeterNeedle);
            var voltmeter = Meter("Voltmeter", "VOLTMETER", "Measures potential difference — V — volt (V)", palette.voltage, "VoltMeter_0to6.png", "Voltmeter", "niddle_grp1",
                assembled ? rig.voltmeterSlot : trayV, bench, billboards, out rig.voltmeterReading, out rig.voltmeterNeedle);
            rig.ammeter = ammeter.transform;
            rig.voltmeter = voltmeter.transform;
            rig.ammeterPlaced = rig.voltmeterPlaced = assembled;
            Ref(ammeter); Ref(voltmeter);
            rig.seriesGlow = SlotGlow("Series Slot Glow", bench, AmmeterSlotAt, palette.current);
            rig.parallelGlow = SlotGlow("Parallel Slot Glow", bench, VoltmeterSlotAt, palette.voltage);
            PartsTray(bench);

            // Variable DC source at the room end (frame left): repainted 0-5 V panel, knob and pilot lamp driven by CircuitRig.
            var source = Model(Imported + "DCSource.prefab", "cells / controllable DC source", bench, SourceAt, Vector3.zero, SourceScale);
            source.transform.localRotation = Facing(SourceAt, eye, 90f);
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
            Caption(source, "cells / controllable DC source", 1.7f, SourceScale, billboards);
            Ref(source);

            // Key: a knife switch in Scene 4's brass and dark wood; the hinge is the lever pivot CircuitRig animates (open 35°, closed 0°).
            var key = Model(KeyPrefab, "switch / key", bench, KeyAt + Vector3.up * (0.06f * KeyScale), Vector3.zero, KeyScale);
            key.transform.localRotation = Facing(KeyAt, eye, 20f);
            var brass = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Scene04/Materials/Brass.mat");
            var wood = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Scene04/Materials/DarkWood.mat");
            if (!brass) brass = Lit("KeyBrass", new Color(0.78f, 0.6f, 0.3f), 1f, 0.6f);
            if (!wood) wood = Lit("KeyWood", new Color(0.22f, 0.13f, 0.08f), 0f, 0.45f);
            var handle = Lit("KeyHandle", new Color(0.08f, 0.07f, 0.07f), 0f, 0.55f);
            foreach (var r in key.GetComponentsInChildren<Renderer>()) r.sharedMaterial = r.name == "SwitchBase" ? wood : r.name == "Handle" ? handle : brass;
            rig.key = key.transform;
            rig.keyLever = FindChild(key, "LeverHinge");
            rig.keyLever.localRotation = Quaternion.Euler(0, 0, keyClosed ? 0 : 35);
            Placeholder(key, "Knife-switch key built from primitives (LabEquipment/Switch.prefab re-skinned); swap for a sculpted plug key when one exists");
            Caption(key, "switch / key", 1.6f, KeyScale, billboards);
            Ref(key);

            // Leads, in world space. Red on the source's + side, black on the return; the meter leads appear with their meter.
            // Each lead takes the terminal nearest to where it comes from, so the layout can move without re-plumbing.
            Vector3 W(Vector3 local) => bench.TransformPoint(local);
            var ammeterPosts = MeterPosts.Select(p => rig.ammeterSlot.TransformPoint(p * MeterScale)).ToArray();
            var voltmeterPosts = MeterPosts.Select(p => rig.voltmeterSlot.TransformPoint(p * MeterScale)).ToArray();
            var sourcePosts = SourcePosts.Select(p => source.transform.TransformPoint(p)).ToArray();
            var keyContacts = KeyContacts.Select(p => key.transform.TransformPoint(p)).ToArray();
            Vector3 ammeterIn = Nearest(ammeterPosts, sourcePosts[0]), ammeterOut = Other(ammeterPosts, ammeterIn);
            Vector3 sourcePlus = Nearest(sourcePosts, ammeterIn), sourceMinus = Other(sourcePosts, sourcePlus);
            Vector3 keyIn = Nearest(keyContacts, y), keyOut = Other(keyContacts, keyIn);
            float restY = BenchCentre.y + LeadRest;
            // Source → ammeter goes round the source's inner side, between it and the X clamp; the return runs along the front edge.
            var toAmmeter = LeadPath(sourcePlus, ammeterIn, W(new Vector3(0.55f, 0, 0.26f)), W(new Vector3(0.515f, 0, 0.09f)));
            var toX = LeadPath(ammeterOut, x, Over(x, ammeterOut));
            var toKey = LeadPath(y, keyIn, Over(y, keyIn));
            var toSource = LeadPath(keyOut, sourceMinus, W(new Vector3(-0.52f, 0, 0.40f)), W(new Vector3(0.72f, 0, 0.40f)), W(new Vector3(0.80f, 0, 0.35f)));
            rig.ammeterLeads = Node("Ammeter Leads", bench);
            Lead("Lead Source → Ammeter", rig.ammeterLeads.transform, leadRed, toAmmeter);
            Lead("Lead Ammeter → X", rig.ammeterLeads.transform, leadRed, toX);
            rig.ammeterLeads.SetActive(assembled);
            Lead("Lead Y → Key", key.transform, leadBlack, toKey);
            Lead("Lead Key → Source", key.transform, leadBlack, toSource);
            Vector3 voltmeterX = Nearest(voltmeterPosts, x), voltmeterY = Other(voltmeterPosts, voltmeterX);
            rig.voltmeterLeads = Lead("Voltmeter Lead X", bench, leadRed, LeadPath(x, voltmeterX, Over(x, voltmeterX)));
            rig.voltmeterLeadY = Lead("Voltmeter Lead Y", bench, leadBlack, LeadPath(y, voltmeterY, Over(y, voltmeterY)));
            rig.voltmeterLeads.enabled = rig.voltmeterLeadY.enabled = assembled;

            // Current path overlay along the leads, across the ammeter's posts and along the bench in front of the wire
            // (the path lights up; the nichrome wire itself never glows). Lit amber by CircuitRig once the ammeter is in.
            Vector3 UnderWire(float t) { var p = Vector3.Lerp(x, y, t); return new Vector3(p.x, restY + 0.01f, p.z + 0.05f); }
            var path = new List<Vector3>();
            Append(path, toAmmeter);
            Append(path, toX);
            Append(path, new[] { x, UnderWire(0.12f), UnderWire(0.88f), y });
            Append(path, toKey);
            Append(path, toSource);
            var glow = Glow("Glow_CurrentPath", Hdr(Color.white, 1.6f), false);
            rig.currentPath = Polyline("Current Path", bench, glow, 0.014f, false, true, path.Select(p => p + Vector3.up * 0.006f).ToArray());
            rig.currentPath.startColor = rig.currentPath.endColor = Color.clear;
            rig.pathLabel = WorldText.Card("Path Label", "Source → Ammeter → Nichrome Wire XY → Switch → Source", palette.current, bench, new Vector3(-0.1f, 0.36f, -0.44f), PathLabelCap, cardBacking, PathLabelWrap);
            rig.pathLabel.gameObject.SetActive(false);
            billboards.Add(rig.pathLabel.transform);
            foreach (var card in billboards) Face(card.gameObject, BenchCentre + eye); // CircuitRig keeps them on the live camera
            rig.billboards = billboards.ToArray();

            // Where Wattson hovers to point at things: behind the bench, 0.35-0.4 m up, never between the cameras and the subject.
            Marker("Wire Point", BenchCentre + new Vector3(0.35f, 0.4f, -0.75f));
            Marker("Tray Point", BenchCentre + new Vector3(-0.78f, 0.35f, -0.78f));
            Marker("Voltmeter Point", BenchCentre + new Vector3(-0.42f, 0.35f, -0.75f));
            Marker("Source Point", BenchCentre + new Vector3(0.80f, 0.4f, -0.75f));
            Marker("Key Point", BenchCentre + new Vector3(-0.25f, 0.4f, -0.78f));
            root.SetActive(active);
            Ref(root);
            return rig;
        }

        // An imported meter at a slot: repainted dial, collider for drag, digit card and a hidden caption above it.
        GameObject Meter(string prefab, string name, string caption, Color tint, string dial, string bodyMesh, string needlePivot, Transform at, Transform bench,
            List<Transform> billboards, out TextMeshPro reading, out Transform needle)
        {
            var go = Model(Imported + prefab + ".prefab", name, bench, at.localPosition, Vector3.zero, MeterScale); // collider fitted before the turn, so it stays tight
            go.transform.localRotation = at.localRotation;
            var body = FindChild(go, bodyMesh).GetComponent<Renderer>();
            var printed = body.sharedMaterial;
            var texture = RigTexture(RigTextures + dial);
            body.sharedMaterial = Mat(prefab + "_Dial", printed.shader.name, m => { m.CopyPropertiesFromMaterial(printed); if (texture) m.SetTexture("_BaseMap", texture); });
            needle = FindChild(go, needlePivot);
            reading = WorldText.Card("Reading", "0.0", tint, go.transform, new Vector3(0, 1.3f, -0.1f), MeterReadingCap, cardBacking);
            reading.transform.localScale = Vector3.one / MeterScale;
            billboards.Add(reading.transform);
            Caption(go, name + "\n<size=83%>" + caption + "</size>", 1.75f, MeterScale, billboards);
            return go;
        }

        // Instrument caption ("Caption <part>"), hidden until a cue shows it; sized for the close camera.
        void Caption(GameObject part, string text, float localHeight, float scale, List<Transform> billboards)
        {
            var card = WorldText.Card("Caption " + part.name, text, Color.white, part.transform, new Vector3(0, localHeight, 0), PropLabelCap, cardBacking, CaptionWrap);
            card.transform.localScale = Vector3.one / scale;
            card.gameObject.SetActive(false);
            billboards.Add(Ref(card.gameObject).transform);
        }

        // A pose on the bench for a meter: its dial turned toward the cameras (or straight out of the tray).
        Transform Slot(string name, Transform bench, Vector3 at, Vector3? eye)
        {
            var slot = Node(name, bench, at).transform;
            slot.localRotation = eye.HasValue ? Facing(at, eye.Value) : Quaternion.Euler(0, 180, 0);
            return slot;
        }

        // The imported models face local -Z: yaw so that face points at the eye, at most `cap` degrees off the bench axis.
        static Quaternion Facing(Vector3 at, Vector3 eye, float cap = 28f)
        {
            Vector3 away = at - eye;
            away.y = 0;
            float off = Mathf.DeltaAngle(180f, Quaternion.LookRotation(away).eulerAngles.y);
            return Quaternion.Euler(0, 180f + Mathf.Clamp(off, -cap, cap), 0);
        }

        // Soft additive disc on the bench under a slot; CircuitRig shows and pulses it while that part may be dragged.
        GameObject SlotGlow(string name, Transform bench, Vector3 at, Color colour)
        {
            var hdr = Hdr(colour, 1.6f);
            hdr.a = 0.55f;
            var glow = Prim(PrimitiveType.Quad, name, bench, at + Vector3.up * 0.004f, new Vector3(0.5f, 0.6f, 1), Glow("Glow_" + name.Replace(" ", ""), hdr), new Vector3(90, 0, 0));
            glow.SetActive(false);
            return glow;
        }

        // Shallow graphite tray at the wall end of the bench (frame right); the meters wait in it until Scene 9 places them.
        void PartsTray(Transform bench)
        {
            var tray = Node("Parts Tray", bench, TrayAt);
            var graphite = Lit("TrayGraphite", new Color(0.1f, 0.11f, 0.12f), 0.3f, 0.45f);
            Prim(PrimitiveType.Cube, "Base", tray.transform, new Vector3(0, TrayBase / 2, 0), new Vector3(TrayWidth, TrayBase, TrayDepth), graphite);
            foreach (float sx in new[] { -1f, 1f })
                Prim(PrimitiveType.Cube, "Rim", tray.transform, new Vector3(sx * (TrayWidth - 0.012f) / 2, 0.02f, 0), new Vector3(0.012f, 0.04f, TrayDepth), graphite);
            foreach (float sz in new[] { -1f, 1f })
                Prim(PrimitiveType.Cube, "Rim", tray.transform, new Vector3(0, 0.02f, sz * (TrayDepth - 0.012f) / 2), new Vector3(TrayWidth, 0.04f, 0.012f), graphite);
            Ref(tray);
        }

        static Texture2D RigTexture(string path)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (!texture) Debug.LogWarning("Rig: repainted texture missing, the printed scale stays: " + path);
            return texture;
        }

        // A lead between two terminals: leaves each post upward, then sags onto the bench through the waypoints
        // (or arcs straight across when there are none). World space.
        Vector3[] LeadPath(Vector3 from, Vector3 to, params Vector3[] via)
        {
            var knots = new List<Vector3> { from, from + Vector3.up * 0.035f };
            float restY = BenchCentre.y + LeadRest;
            if (via.Length == 0) knots.Add(Vector3.Lerp(from, to, 0.5f) + Vector3.up * 0.03f);
            foreach (var v in via) knots.Add(new Vector3(v.x, Mathf.Max(v.y, restY), v.z));
            knots.Add(to + Vector3.up * 0.035f);
            knots.Add(to);
            return CatmullRom(knots, 6);
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

        LineRenderer Lead(string name, Transform parent, Material insulation, Vector3[] path) => Polyline(name, parent, insulation, LeadWidth, false, true, path);

        // A knot that lifts a lead over the clamp it plugs into.
        static Vector3 Over(Vector3 terminal, Vector3 other) => Vector3.Lerp(terminal, other, 0.35f) + Vector3.up * 0.05f;

        static Vector3 Nearest(Vector3[] points, Vector3 to) => points.OrderBy(p => (p - to).sqrMagnitude).First();
        static Vector3 Other(Vector3[] pair, Vector3 one) => pair[0] == one ? pair[1] : pair[0];

        static void Append(List<Vector3> path, IEnumerable<Vector3> points)
        {
            foreach (var p in points)
                if (path.Count == 0 || (path[path.Count - 1] - p).sqrMagnitude > 1e-6f) path.Add(p);
        }
    }
}
