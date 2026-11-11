using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EchoesOfTheFog.Core;
using EchoesOfTheFog.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace EchoesOfTheFog.Editor
{
    // Explicit migration preserves the existing scene, background, objects and button wiring.
    public static class MainMenuTypography
    {
        private const string FontPath = "Assets/Game/UI/Fonts/MenuVietnameseSDF.asset";
        private static readonly Dictionary<string, float> Sizes = new Dictionary<string, float>
        {
            { "Title", 84 }, { "Eyebrow", 22 }, { "Subtitle", 24 }, { "Invitation", 28 },
            { "Footer", 20 }, { "Status", 24 }, { "Label", 30 }, { "Key", 24 }, { "Action", 26 },
            { "Guide title", 42 }, { "Guide intro", 26 }, { "Movement heading", 24 },
            { "Combat heading", 24 }, { "Survival tip", 24 }, { "Back shortcut", 20 }
        };

        // Unity imports .unitypackage resources asynchronously; run this entry point without -quit.
        public static void ImportEssentials()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use batch mode for TMP resource import.");
            var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
            if (package == null) throw new InvalidOperationException("The installed TextMeshPro package cannot be located.");
            AssetDatabase.importPackageCompleted += _ => { AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport); EditorApplication.Exit(0); };
            AssetDatabase.importPackageFailed += (_, error) => { Debug.LogError(error); EditorApplication.Exit(1); };
            AssetDatabase.importPackageCancelled += _ => EditorApplication.Exit(1);
            AssetDatabase.ImportPackage(Path.Combine(package.resolvedPath, "Package Resources/TMP Essential Resources.unitypackage"), false);
        }

        public static TMP_FontAsset GetFontAsset()
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (existing != null) return existing;
            string source = AssetDatabase.FindAssets("LiberationSans t:Font", new[] { "Assets" }).Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(path => path.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase));
            if (source == null || Shader.Find("TextMeshPro/Mobile/Distance Field") == null)
            {
                var package = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(TMP_Text).Assembly);
                if (package == null) throw new InvalidOperationException("The installed TextMeshPro package cannot be located.");
                string resources = Path.Combine(package.resolvedPath, "Package Resources/TMP Essential Resources.unitypackage");
                throw new InvalidOperationException("Import bundled TMP resources once using MainMenuTypography.ImportEssentials (without -quit): " + resources);
            }
            if (source == null) throw new FileNotFoundException("Liberation Sans source font was not imported.");
            Directory.CreateDirectory("Assets/Game/UI/Fonts"); AssetDatabase.Refresh();
            var font = TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(source), 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, false);
            if (font == null) throw new InvalidOperationException("Could not generate the menu SDF font.");
            font.name = "Menu Vietnamese SDF"; font.material.name = "Menu Vietnamese SDF Material";
            font.atlasTextures[0].name = "Menu Vietnamese SDF Atlas";
            AssetDatabase.CreateAsset(font, FontPath);
            AssetDatabase.AddObjectToAsset(font.material, font); AssetDatabase.AddObjectToAsset(font.atlasTextures[0], font);
            uint[] characters = Enumerable.Range(32, 95).Select(value => (uint)value)
                .Concat("ÀÁÂÃÈÉÊÌÍÒÓÔÕÙÚÝàáâãèéêìíòóôõùúýĂăĐđĨĩŨũƠơƯư…–—‘’“”•·".Select(character => (uint)character))
                .Concat(Enumerable.Range(0x1EA0, 0x1EFA - 0x1EA0).Select(value => (uint)value)).Distinct().ToArray();
            if (!font.TryAddCharacters(characters, out uint[] missing))
                throw new InvalidOperationException("Menu font is missing required glyphs: " + string.Join(", ", missing.Select(value => "U+" + value.ToString("X4"))));
            // Bake once so runtime font quality does not depend on OS fonts or dynamic glyph generation.
            font.atlasPopulationMode = AtlasPopulationMode.Static;
            font.atlasTextures[0].filterMode = FilterMode.Bilinear; font.atlasTextures[0].wrapMode = TextureWrapMode.Clamp;
            EditorUtility.SetDirty(font.atlasTextures[0]); EditorUtility.SetDirty(font.material); EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();
            return font;
        }

        public static void Apply()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Close Unity before batch typography migration.");
            Directory.CreateDirectory("Logs/MainMenu");
            ProjectBackups.PreserveAsset(GameScenes.MainMenu, "BeforeMenuTypography");
            var font = GetFontAsset();
            var scene = EditorSceneManager.OpenScene(GameScenes.MainMenu, OpenSceneMode.Single);
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) throw new InvalidOperationException("Menu Canvas is missing.");
            int converted = 0;
            foreach (var old in canvas.GetComponentsInChildren<Text>(true))
            {
                var owner = old.gameObject; string text = old.text; Color color = old.color;
                int size = old.fontSize; bool bold = old.fontStyle == FontStyle.Bold || old.fontStyle == FontStyle.BoldAndItalic;
                TextAlignmentOptions alignment = Alignment(old.alignment);
                UnityEngine.Object.DestroyImmediate(old);
                var label = owner.AddComponent<TextMeshProUGUI>();
                label.text = text; label.color = color; label.fontSize = size; label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
                label.alignment = alignment; converted++;
            }
            var labels = canvas.GetComponentsInChildren<TMP_Text>(true);
            foreach (var label in labels)
            {
                label.font = font; label.fontSharedMaterial = font.material; label.raycastTarget = false;
                label.richText = false; label.enableAutoSizing = false; label.extraPadding = true;
                label.textWrappingMode = TextWrappingModes.Normal; label.overflowMode = TextOverflowModes.Overflow;
                label.margin = Vector4.zero; label.characterSpacing = 0; label.wordSpacing = 0; label.paragraphSpacing = 0;
                label.lineSpacing = label.name == "Title" ? -2 : label.name == "Invitation" || label.name == "Survival tip" ? 4 : 0;
                if (Sizes.TryGetValue(label.name, out float size)) label.fontSize = size;
                if (label.name == "Footer") label.color = new Color(0.76f, 0.82f, 0.77f, 0.95f);
                if (label.name == "Eyebrow" || label.name == "Movement heading" || label.name == "Combat heading") label.color = new Color(0.75f, 0.82f, 0.77f);
                if (label.name == "Key")
                {
                    var cap = (RectTransform)label.transform.parent;
                    cap.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 190);
                    label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 186);
                }
                foreach (char character in label.text)
                    if (!char.IsWhiteSpace(character) && !font.HasCharacter(character)) throw new InvalidOperationException("Missing menu glyph: " + character);
                EditorUtility.SetDirty(label);
            }
            foreach (var label in labels.Where(label => label.name == "Action"))
            {
                var key = labels.Where(candidate => candidate.name == "Key" && Mathf.Abs(((RectTransform)candidate.transform.parent).anchoredPosition.y - label.rectTransform.anchoredPosition.y) < 1
                    && ((RectTransform)candidate.transform.parent).anchoredPosition.x < label.rectTransform.anchoredPosition.x)
                    .OrderByDescending(candidate => ((RectTransform)candidate.transform.parent).anchoredPosition.x).First();
                var position = label.rectTransform.anchoredPosition; position.x = ((RectTransform)key.transform.parent).anchoredPosition.x + 210;
                label.rectTransform.anchoredPosition = position; label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 200);
            }
            canvas.pixelPerfect = true; EditorUtility.SetDirty(canvas);
            var controller = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("startLabel").objectReferenceValue = canvas.GetComponentsInChildren<Button>(true).First(button => button.name == "StartButton").GetComponentInChildren<TMP_Text>();
            serialized.FindProperty("statusLabel").objectReferenceValue = labels.First(label => label.name == "Status"); serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, GameScenes.MainMenu); AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/MainMenu/typography.txt", $"Converted {converted} Legacy labels; {labels.Length} TMP labels; SDFAA 2048x2048, sampling 90, padding 9; static Vietnamese glyph set; pixel-perfect Canvas.\n");
        }

        private static TextAlignmentOptions Alignment(TextAnchor alignment)
        {
            switch (alignment)
            {
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                case TextAnchor.LowerRight: return TextAlignmentOptions.BottomRight;
                default: return TextAlignmentOptions.TopLeft;
            }
        }
    }
}
