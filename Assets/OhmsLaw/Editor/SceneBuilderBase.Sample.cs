using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OhmsLaw.Editor
{
    // Stage kit: Scene 12's copper-vs-nichrome sample (WireSample), far under the lab on its own layer, with the camera
    // that renders it into the lesson panel's 3D pop-up. Also the editor self-check for the electron motion, and the
    // meters' plates node the experiment scenes hide behind the centre glass.
    public abstract partial class SceneBuilderBase
    {
        public const int SampleLayer = 6;   // unnamed in TagManager and unused elsewhere: the pop-up camera sees only this
        static readonly Vector3 SampleStage = new Vector3(0f, -40f, 0f);
        static readonly Color SampleCopper = new Color(0.86f, 0.5f, 0.3f), SampleNichrome = new Color(0.58f, 0.6f, 0.52f);
        // The pop-up camera: 3.8 m out, 10° above the pair so the cut-aways read round, aimed 0.1 m above the middle (the
        // names sit over the wires); with the 32° lens the pair and both names fit the card's 940 x 440 view.
        static readonly Vector3 SampleCamera = new Vector3(0f, 0.66f, -3.74f), SampleAim = new Vector3(0f, 0.1f, 0f);

        // The meters' digital plates under one node ("show/hide Meter Plates"): behind the translucent hero, stage and
        // pop-up glass they read through ("1 Ω = 1.00 A 1 V"), and the top readings strip already carries the numbers.
        // CircuitRig still drives each plate; the node only hides them. The meters stay put in Scenes 10-13.
        protected void MeterPlates(bool shown)
        {
            var plates = Ref(Node("Meter Plates", director.rig.transform));
            foreach (var reading in new[] { director.rig.ammeterReading, director.rig.voltmeterReading })
                if (reading) reading.transform.SetParent(plates.transform, true);
            plates.SetActive(shown);
        }

        // The stage, hidden: "show Wire Sample" opens the pop-up (copper alone), "hide Wire Sample" closes it.
        protected WireSample WireSampleStage()
        {
            var root = Ref(Node("Wire Sample", null, SampleStage));
            var sample = root.AddComponent<WireSample>();
            sample.electronMaterial = BlockoutAssets.Mat("SampleElectron", new Color(0.1f, 0.75f, 1f), emissive: true);   // cyan: never lost among nichrome's grey ions
            sample.turntable = Node("Turntable", root.transform).transform;
            sample.copper = WireCutaway("Copper", sample.turntable, new Vector3(0f, 0.42f, 0f), SampleCopper, 0.34f, 0.09f, sample);
            sample.nichrome = WireCutaway("Nichrome", sample.turntable, new Vector3(0f, -0.42f, 0f), SampleNichrome, 0.22f, 0.12f, sample);
            var pop = sample.nichrome.gameObject.AddComponent<PopIn>();
            pop.duration = 0.4f;
            pop.fromScale = 0.6f;
            pop.overshoot = 1.1f;
            sample.nichrome.gameObject.SetActive(false);

            // Its camera: this layer only, a navy backdrop (the HUD glass's own), no post-processing: the lab's volumes would
            // give it their depth of field (focus ~2 m, the stage 3.8 m out) and blur it; WireSample hands it the texture.
            var camGo = Node("Sample Camera", root.transform, SampleCamera);
            camGo.transform.localRotation = Quaternion.LookRotation(SampleAim - SampleCamera);
            var cam = camGo.AddComponent<Camera>();
            cam.cullingMask = 1 << SampleLayer;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.031f, 0.102f, 0.169f);
            cam.fieldOfView = 32f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 30f;
            cam.allowHDR = true;
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = false;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            sample.stageCamera = cam;
            // The room's lights are 40 m up and the floor shadows them: the stage lights itself.
            var light = Node("Sample Light", root.transform, new Vector3(0.8f, 1.6f, -2.2f)).AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 9f;
            light.intensity = 2.5f;
            light.shadows = LightShadows.None;
            light.cullingMask = 1 << SampleLayer;
            // A cool fill from the lower left: the ions get a second highlight and the cut-aways read as round, not flat.
            var fill = Node("Sample Fill", root.transform, new Vector3(-1.4f, -0.6f, -2.4f)).AddComponent<Light>();
            fill.type = LightType.Point;
            fill.range = 9f;
            fill.intensity = 1.2f;
            fill.color = new Color(0.7f, 0.85f, 1f);
            fill.shadows = LightShadows.None;
            fill.cullingMask = 1 << SampleLayer;

            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = SampleLayer;
            if (camera) camera.cullingMask &= ~(1 << SampleLayer);
            root.SetActive(false);
            return sample;
        }

        // One see-through wire segment along X: tinted glass, a hexagonal column of lattice ions, its name over the left end.
        Transform WireCutaway(string name, Transform parent, Vector3 at, Color tint, float ionSpacing, float ionSize, WireSample sample)
        {
            var group = Node(name, parent, at).transform;
            float length = sample.length, radius = sample.radius;
            Prim(PrimitiveType.Cylinder, "Glass", group, Vector3.zero, new Vector3(radius * 2f, length / 2f, radius * 2f),
                SampleGlass(name, tint), new Vector3(0f, 0f, 90f));
            var ion = Lit("Sample_Ion_" + name, tint, 0.3f, 0.55f, tint * 0.25f);
            float ring = radius * 0.55f;
            int slice = 0;
            for (float x = -length / 2 + ionSpacing * 0.5f; x < length / 2; x += ionSpacing, slice++)
                for (int s = 0; s < 7; s++)
                {
                    float a = s * Mathf.PI / 3f + (slice % 2 == 0 ? 0f : Mathf.PI / 6f);   // alternate slices turn half a step
                    var p = s == 6 ? new Vector3(x, 0f, 0f) : new Vector3(x, Mathf.Cos(a) * ring, Mathf.Sin(a) * ring);
                    Prim(PrimitiveType.Sphere, "Ion", group, p, Vector3.one * ionSize, ion);
                }
            // The name over the left end; WireSample keeps it facing the stage camera as the pair turns.
            WorldText.Card("Label", name.ToUpperInvariant(), tint, group, new Vector3(-length / 2 + 0.35f, radius + 0.12f, 0f), 0.13f);
            return group;
        }

        // Tinted see-through glass that the stage lights shade and catch (a highlight runs along the tube): it reads as a
        // round wire, not a flat bar. Alpha-blended Lit, smooth, no depth write. Its alpha accumulates "over" the backdrop,
        // so the texture stays opaque (the pop-up's RawImage shows the texture's alpha: without post-processing a plain
        // One/Zero alpha would punch a see-through hole where the glass is).
        static Material SampleGlass(string name, Color tint) =>
            Mat("Sample_Glass_" + name, "Universal Render Pipeline/Lit", m =>
            {
                m.SetFloat("_Surface", 1);
                m.SetFloat("_Blend", 0);
                m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (m.HasProperty("_SrcBlendAlpha"))
                {
                    m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
                    m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                }
                m.SetFloat("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetOverrideTag("RenderType", "Transparent");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                m.SetColor("_BaseColor", new Color(tint.r, tint.g, tint.b, 0.24f));
                m.SetFloat("_Metallic", 0f);
                m.SetFloat("_Smoothness", 0.8f);
            });

        // The runnable check: the electrons stay in the wire, keep their spacing round the loop, and nichrome scatters hard.
        [MenuItem("Ohm's Law/Tests/Wire Sample")]
        public static void CheckWireSample()
        {
            int failed = 0;
            void Check(bool ok, string what)
            {
                if (!ok) failed++;
                Debug.Log((ok ? "PASS" : "FAIL") + " sample: " + what);
            }
            var root = new GameObject("Wire Sample Check (temporary)") { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                var sample = root.AddComponent<WireSample>();
                sample.copper = new GameObject("Copper").transform;
                sample.copper.SetParent(root.transform, false);
                sample.nichrome = new GameObject("Nichrome").transform;
                sample.nichrome.SetParent(root.transform, false);
                sample.Init();
                for (float t = 0; t < 6f; t += 1f / 60f) sample.Step(1f / 60f);

                float radial = 0f;
                foreach (var group in new[] { sample.copper, sample.nichrome })
                    foreach (Transform e in group) radial = Mathf.Max(radial, new Vector2(e.localPosition.y, e.localPosition.z).magnitude);
                Check(radial <= sample.radius + 1e-3f, $"electrons stay inside the wire (largest radius {radial:0.000} of {sample.radius:0.00} m)");
                foreach (bool copper in new[] { true, false })
                {
                    var xs = (Vector3[])sample.Homes(copper).Clone();
                    System.Array.Sort(xs, (p, q) => p.x.CompareTo(q.x));
                    float spacing = sample.length / sample.count, worst = 0f;
                    for (int i = 0; i < xs.Length; i++)
                    {
                        float gap = i + 1 < xs.Length ? xs[i + 1].x - xs[i].x : xs[0].x + sample.length - xs[i].x;
                        worst = Mathf.Max(worst, Mathf.Abs(gap - spacing));
                    }
                    Check(worst < 1e-3f, $"{(copper ? "copper" : "nichrome")}: places stay evenly spaced round the loop (worst gap error {worst:0.00000} m)");
                }
                Check(sample.NichromeHits > 3 * sample.CopperHits, $"nichrome scatters far more than copper ({sample.NichromeHits} vs {sample.CopperHits} collisions in 6 s)");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
            Debug.Log(failed == 0 ? "Wire Sample: all checks passed" : $"Wire Sample: {failed} check(s) FAILED");
        }
    }
}
