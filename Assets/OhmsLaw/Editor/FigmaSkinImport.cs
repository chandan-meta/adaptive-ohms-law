using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace OhmsLaw.Editor
{
    public sealed class FigmaSkinImport : AssetPostprocessor
    {
        const string Folder = "Assets/OhmsLaw/Shared/Resources/FigmaUI";
        const string ManifestPath = "Docs/UI_v2/unity-figma-assets.json";
        const string ReportPath = "Logs/FigmaUI/import-verification.txt";

        static bool IsFigmaPng(string path) => path.StartsWith(Folder + "/", StringComparison.Ordinal)
            && path.EndsWith(".png", StringComparison.OrdinalIgnoreCase);

        void OnPreprocessTexture()
        {
            if (!IsFigmaPng(assetPath)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            importer.spriteBorder = Border(assetPath, width, height);
        }

        static Vector4 Border(string path, int width, int height)
        {
            string name = Path.GetFileNameWithoutExtension(path);
            if (name.StartsWith("Icon-", StringComparison.Ordinal) || name.StartsWith("Title", StringComparison.Ordinal))
                return Vector4.zero;
            float size = Mathf.Min(16, Mathf.Min(width, height) / 4);
            return new Vector4(size, size, size, size);
        }

        [Serializable] sealed class Manifest { public SourceAsset[] assets; }
        [Serializable] sealed class SourceAsset { public string name; public string path; public string nodeId; }

        [MenuItem("Ohm's Law/Figma/Configure and verify exported sprites")]
        public static void ConfigureAndVerify()
        {
            var report = new StringBuilder("Figma PNG sprite import verification\n");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            try
            {
                Require(Directory.Exists(Folder), "Figma export folder exists");
                var paths = Directory.GetFiles(Folder, "*.png", SearchOption.AllDirectories)
                    .Select(path => path.Replace('\\', '/')).OrderBy(path => path, StringComparer.Ordinal).ToArray();
                Require(paths.Length > 0, "Figma export folder contains PNGs");
                foreach (string path in paths)
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                    var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    Require(importer && sprite && sprite.texture && sprite.rect.width > 0 && sprite.rect.height > 0,
                        "Non-empty sprite: " + path);
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    importer.GetSourceTextureWidthAndHeight(out int width, out int height);
                    Vector4 border = Border(path, width, height);
                    Require(importer.textureType == TextureImporterType.Sprite && importer.spriteImportMode == SpriteImportMode.Single
                        && settings.spriteMeshType == SpriteMeshType.FullRect, "Single FullRect sprite: " + path);
                    Require(importer.alphaIsTransparency && importer.alphaSource == TextureImporterAlphaSource.FromInput
                        && importer.sRGBTexture, "Source alpha and sRGB preserved: " + path);
                    Require(!importer.mipmapEnabled && importer.filterMode == FilterMode.Bilinear
                        && importer.textureCompression == TextureImporterCompression.Uncompressed
                        && importer.npotScale == TextureImporterNPOTScale.None && importer.maxTextureSize == 4096,
                        "UI texture settings: " + path);
                    Require(importer.spriteBorder == border && sprite.border == border, "Correct slicing: " + path);
                    Require(sprite.rect.width <= 4096 && sprite.rect.height <= 4096
                        && (Mathf.Max(width, height) > 4096 || sprite.rect.width == width && sprite.rect.height == height),
                        "Export dimensions retained: " + path);
                    report.AppendLine($"PASS {path} ({width}x{height}, border {border.x})");
                }
                report.AppendLine("PNG count: " + paths.Length);
                VerifyManifest(paths, report);

                var bound = Resources.FindObjectsOfTypeAll<Image>().Where(image => image.sprite
                    && IsFigmaPng(AssetDatabase.GetAssetPath(image.sprite))).ToArray();
                foreach (var image in bound)
                {
                    string path = AssetDatabase.GetAssetPath(image.sprite);
                    Require(paths.Contains(path), "Bound Image uses an imported export: " + image.name);
                    if (image.type == Image.Type.Sliced)
                        Require(image.sprite.border != Vector4.zero, "Sliced Image has sprite borders: " + image.name);
                }
                report.AppendLine("Loaded Image bindings checked: " + bound.Length);
                report.AppendLine("RESULT PASS");
                Debug.Log(report.ToString());
            }
            catch (Exception error)
            {
                report.AppendLine("RESULT FAIL: " + error.Message);
                throw;
            }
            finally { File.WriteAllText(ReportPath, report.ToString()); }
        }

        static void VerifyManifest(string[] paths, StringBuilder report)
        {
            if (!File.Exists(ManifestPath))
            {
                report.AppendLine("Source manifest pending: " + ManifestPath);
                return;
            }
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
            Require(manifest != null && manifest.assets != null, "Source manifest has an assets list");
            var recorded = new HashSet<string>(StringComparer.Ordinal);
            foreach (var asset in manifest.assets)
            {
                Require(asset != null && !string.IsNullOrWhiteSpace(asset.path)
                    && !string.IsNullOrWhiteSpace(asset.nodeId), "Every export records a source path and Figma node");
                string path = asset.path.Replace('\\', '/');
                Require(IsFigmaPng(path) && paths.Contains(path) && recorded.Add(path), "Unique existing source PNG: " + path);
                report.AppendLine($"SOURCE {path} <- Figma {asset.nodeId}");
            }
            Require(recorded.Count == paths.Length, "Every imported PNG is listed in the source manifest");
        }

        static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
