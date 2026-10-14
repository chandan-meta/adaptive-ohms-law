using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OhmsLaw.Editor
{
    // Exports a scene's set for editing in Blender / Maya: Exports/<name>/<name>.obj + .mtl + textures/, world-space
    // geometry, one object per renderer, cables as 6-sided tubes, the wire's copper lattice baked to spheres. X is mirrored
    // (Unity is left-handed) so Tools/obj_to_blend.py's .fbx (forward -Z, up Y) re-imports into Unity exactly in place.
    // Left out: the characters, particles (electrons), world text, lights, cameras, VFX.
    // ponytail: OBJ keeps geometry, UVs, base colours and textures only (no pivots, hierarchy or shaders); add the FBX
    // Exporter package if a full round-trip of hierarchy and pivots is ever needed.
    public static class SetExport
    {
        const string Out = "Exports";
        static readonly string[] Skip = { "Wattson", "Georg Ohm", "Audience", "Macro", "[VFX]", "Counter", "Card Terminal Label", "Meter Plates", "Tap Hint", "Drift Arrow", "Bracket" };
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

        [MenuItem("Ohm's Law/Export/Our Lab (Scene 10 set)")]
        public static void Lab() => Export("Assets/OhmsLaw/Scenes/Scene10_Experiment.unity", "OurLab", null);

        [MenuItem("Ohm's Law/Export/Inside the Wire (Scene 5 set)")]
        public static void Wire() => Export("Assets/OhmsLaw/Scenes/Scene05_InsideTheWire.unity", "InsideTheWire", "Wire Section");

        // rootName: export that root and everything under it, hidden parts included (the wire waits inactive until the cut);
        // null: every active renderer of the scene.
        static void Export(string scenePath, string name, string rootName)
        {
            var setup = EditorSceneManager.GetSceneManagerSetup();
            string dir = Path.Combine(Out, name);
            Directory.CreateDirectory(Path.Combine(dir, "textures"));
            var w = new Writer(dir);
            try
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                var roots = scene.GetRootGameObjects().Where(r => rootName == null || r.name == rootName).ToArray();
                bool hidden = rootName != null;
                foreach (var r in roots.SelectMany(g => g.GetComponentsInChildren<Renderer>(hidden)))
                {
                    if (Skipped(r.transform) || r.GetComponent<TMP_Text>() || (!hidden && !(r.enabled && r.gameObject.activeInHierarchy))) continue;
                    if (r is MeshRenderer && r.TryGetComponent(out MeshFilter f) && f.sharedMesh) w.Mesh(Path_(r.transform), f.sharedMesh, r.transform.localToWorldMatrix, r.sharedMaterials);
                    else if (r is LineRenderer line && Tube(line) is Mesh tube) w.Mesh(Path_(r.transform), tube, Matrix4x4.identity, r.sharedMaterials);
                }
                foreach (var wire in roots.SelectMany(g => g.GetComponentsInChildren<WireInterior>(true))) Lattice(w, wire);
                w.Save(name);
                Debug.Log($"Exported {name}: {w.Objects} objects, {w.Vertices} vertices, {w.MaterialCount} materials -> {Path.GetFullPath(dir)}");
            }
            finally
            {
                var opened = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
                if (opened.isLoaded && !setup.Any(s => s.path == scenePath)) EditorSceneManager.CloseScene(opened, true); // discard: nothing here is saved
                EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        static bool Skipped(Transform t)
        {
            for (; t; t = t.parent) foreach (var s in Skip) if (t.name.StartsWith(s)) return true;
            return false;
        }

        static string Path_(Transform t) => t.parent ? Path_(t.parent) + "/" + t.name : t.name;

        // A LineRenderer cable as a closed 6-sided tube along its points (world space), its width from the curve.
        static Mesh Tube(LineRenderer line)
        {
            int n = line.positionCount;
            if (n < 2) return null;
            var p = new Vector3[n];
            line.GetPositions(p);
            if (!line.useWorldSpace) for (int i = 0; i < n; i++) p[i] = line.transform.TransformPoint(p[i]);
            const int sides = 6;
            var v = new List<Vector3>();
            var tris = new List<int>();
            for (int i = 0; i < n; i++)
            {
                var dir = (p[Mathf.Min(i + 1, n - 1)] - p[Mathf.Max(i - 1, 0)]).normalized;
                var a = Vector3.Cross(dir, Mathf.Abs(dir.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
                var b = Vector3.Cross(dir, a);
                float r = 0.5f * line.widthMultiplier * line.widthCurve.Evaluate(i / (float)(n - 1));
                for (int s = 0; s < sides; s++) { float t = s * Mathf.PI * 2f / sides; v.Add(p[i] + (a * Mathf.Cos(t) + b * Mathf.Sin(t)) * r); }
            }
            for (int i = 0; i < n - 1; i++)
                for (int s = 0; s < sides; s++)
                {
                    int a0 = i * sides + s, a1 = i * sides + (s + 1) % sides, b0 = a0 + sides, b1 = a1 + sides;
                    tris.AddRange(new[] { a0, a1, b0, a1, b1, b0 });
                }
            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(v);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            return mesh;
        }

        // The wire's copper lattice is placed at runtime (WireInterior.Cavern, seeded): place it now and bake every atom
        // to a small sphere in one object, coloured with the cavern tint.
        static void Lattice(Writer w, WireInterior wire)
        {
            if (!wire.cavern) return;
            wire.gameObject.SetActive(true); // the set is saved inactive (it shows at the cut): a particle system holds particles only while active
            typeof(WireInterior).GetMethod("Cavern", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(wire, null);
            var atoms = new ParticleSystem.Particle[wire.cavern.main.maxParticles];
            int count = wire.cavern.GetParticles(atoms);
            var ico = Icosphere();
            var v = new List<Vector3>();
            var tris = new List<int>();
            for (int i = 0; i < count; i++)
            {
                var c = wire.cavern.transform.TransformPoint(atoms[i].position);
                float r = atoms[i].startSize * 0.5f;
                foreach (var t in ico.triangles) tris.Add(v.Count + t);
                foreach (var p in ico.vertices) v.Add(c + p * r);
            }
            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(v);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateNormals();
            var copper = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "Copper_Atom" };
            copper.SetColor("_BaseColor", wire.cavernTint);
            w.Mesh("Wire Section/Copper Lattice (" + count + " atoms)", mesh, Matrix4x4.identity, new[] { copper });
        }

        static Mesh Icosphere()
        {
            float t = (1f + Mathf.Sqrt(5f)) / 2f;
            var v = new[] { new Vector3(-1, t, 0), new Vector3(1, t, 0), new Vector3(-1, -t, 0), new Vector3(1, -t, 0), new Vector3(0, -1, t), new Vector3(0, 1, t),
                            new Vector3(0, -1, -t), new Vector3(0, 1, -t), new Vector3(t, 0, -1), new Vector3(t, 0, 1), new Vector3(-t, 0, -1), new Vector3(-t, 0, 1) }.Select(p => p.normalized).ToArray();
            var f = new[] { 0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8,
                            3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1 };
            return new Mesh { vertices = v, triangles = f };
        }

        // OBJ + MTL text, with separate running offsets for v / vt / vn.
        class Writer
        {
            readonly string dir;
            readonly StringBuilder obj = new StringBuilder(), mtl = new StringBuilder();
            readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
            readonly HashSet<string> names = new HashSet<string>();
            int v = 1, vt = 1, vn = 1;
            public int Objects, Vertices;
            public int MaterialCount => materials.Count;

            public Writer(string dir) => this.dir = dir;

            static string F(float x) => x.ToString("0.######", Inv);
            static string Safe(string s) => new string(s.Replace("−", "minus").Replace("+", "plus").Replace("→", "to").Select(c => char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.' || c == '/' ? c : '_').ToArray());

            public void Mesh(string name, Mesh mesh, Matrix4x4 m, Material[] mats)
            {
                string id = Safe(name);
                for (int k = 2; !names.Add(id); k++) id = Safe(name) + "_" + k;
                var pos = mesh.vertices;
                var nor = mesh.normals;
                var uv = mesh.uv;
                bool hasN = nor.Length == pos.Length, hasUV = uv.Length == pos.Length;
                var nm = m.inverse.transpose;
                obj.Append("o ").AppendLine(id);
                foreach (var p in pos) { var q = m.MultiplyPoint3x4(p); obj.Append("v ").Append(F(-q.x)).Append(' ').Append(F(q.y)).Append(' ').AppendLine(F(q.z)); }
                if (hasUV) foreach (var t in uv) obj.Append("vt ").Append(F(t.x)).Append(' ').AppendLine(F(t.y));
                if (hasN) foreach (var n in nor) { var q = nm.MultiplyVector(n).normalized; obj.Append("vn ").Append(F(-q.x)).Append(' ').Append(F(q.y)).Append(' ').AppendLine(F(q.z)); }
                bool mirrored = m.determinant < 0; // the X mirror flips the winding; a mirrored transform flips it back
                string Idx(int i) => hasUV && hasN ? $"{v + i}/{vt + i}/{vn + i}" : hasN ? $"{v + i}//{vn + i}" : hasUV ? $"{v + i}/{vt + i}" : $"{v + i}";
                for (int s = 0; s < mesh.subMeshCount; s++)
                {
                    if (mesh.GetTopology(s) != MeshTopology.Triangles) continue;
                    var mat = mats.Length > 0 ? mats[Mathf.Min(s, mats.Length - 1)] : null;
                    string mn = mat ? Safe(mat.name).Replace('/', '_') : "Default";
                    if (mat && !materials.ContainsKey(mn)) materials[mn] = mat;
                    obj.Append("usemtl ").AppendLine(mn);
                    var tri = mesh.GetTriangles(s);
                    for (int i = 0; i < tri.Length; i += 3)
                    {
                        int a = tri[i], b = tri[i + 1], c = tri[i + 2];
                        if (!mirrored) (b, c) = (c, b);
                        obj.Append("f ").Append(Idx(a)).Append(' ').Append(Idx(b)).Append(' ').AppendLine(Idx(c));
                    }
                }
                v += pos.Length;
                if (hasUV) vt += uv.Length;
                if (hasN) vn += nor.Length;
                Objects++;
                Vertices += pos.Length;
            }

            public void Save(string name)
            {
                mtl.AppendLine("newmtl Default").AppendLine("Kd 0.8 0.8 0.8");
                foreach (var pair in materials)
                {
                    var m = pair.Value;
                    var c = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
                    mtl.AppendLine().Append("newmtl ").AppendLine(pair.Key);
                    mtl.Append("Kd ").Append(F(c.r)).Append(' ').Append(F(c.g)).Append(' ').AppendLine(F(c.b));
                    if (c.a < 0.999f) mtl.Append("d ").AppendLine(F(c.a));
                    if (m.IsKeywordEnabled("_EMISSION") && m.HasProperty("_EmissionColor"))
                    {
                        var e = m.GetColor("_EmissionColor");
                        mtl.Append("Ke ").Append(F(e.r)).Append(' ').Append(F(e.g)).Append(' ').AppendLine(F(e.b));
                    }
                    string slot = m.HasProperty("_BaseMap") ? "_BaseMap" : m.HasProperty("_MainTex") ? "_MainTex" : null;
                    var tex = slot != null ? m.GetTexture(slot) : null;
                    string src = tex ? AssetDatabase.GetAssetPath(tex) : null;
                    if (string.IsNullOrEmpty(src) || !File.Exists(src)) continue;
                    string file = Path.GetFileName(src);
                    File.Copy(src, Path.Combine(dir, "textures", file), true);
                    var tiling = m.GetTextureScale(slot);
                    mtl.Append("map_Kd ");
                    if (tiling != Vector2.one) mtl.Append("-s ").Append(F(tiling.x)).Append(' ').Append(F(tiling.y)).Append(" 1 ");
                    mtl.Append("textures/").AppendLine(file);
                }
                File.WriteAllText(Path.Combine(dir, name + ".mtl"), mtl.ToString());
                File.WriteAllText(Path.Combine(dir, name + ".obj"), "# Ohm's Law module set, exported from Unity (metres, X mirrored for a right-handed OBJ)\nmtllib " + name + ".mtl\n" + obj);
            }
        }
    }
}
