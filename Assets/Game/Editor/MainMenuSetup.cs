using System;
using System.IO;
using System.Text;
using EchoesOfTheFog.Core;
using EchoesOfTheFog.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace EchoesOfTheFog.Editor
{
    public static class MainMenuSetup
    {
        private static TMP_FontAsset font;
        private static readonly Color White = new Color(0.91f, 0.93f, 0.87f);
        private static readonly Color Muted = new Color(0.65f, 0.73f, 0.70f);

        public static void Apply()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Close Unity before scene migration and menu setup.");
            Directory.CreateDirectory("Assets/Game/Scenes/Main"); Directory.CreateDirectory("Assets/Game/Scenes/Tests"); Directory.CreateDirectory("Logs/MainMenu");
            AssetDatabase.Refresh();
            var report = new StringBuilder();
            foreach (string name in new[] { "AssetValidation", "PlayerTest", "CombatTest", "RunnerTest" })
                MoveScene("Assets/Game/Scenes/" + name + ".unity", "Assets/Game/Scenes/Tests/" + name + ".unity", report);
            MoveScene("Assets/Game/Scenes/RunnerForestTest.unity", GameScenes.Forest, report);
            bool newMenu = !File.Exists(GameScenes.MainMenu);
            CreateMainMenu();
            if (newMenu) MainMenuTypography.Apply();
            OpeningFlowSetup.ConfigureMenuIntro();
            // Keep prototype/test arenas out of the playable build.
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(GameScenes.MainMenu, true), new EditorBuildSettingsScene(GameScenes.Forest, true) };
            if (File.Exists(GameScenes.Village))
                EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(GameScenes.MainMenu, true), new EditorBuildSettingsScene(GameScenes.Forest, true), new EditorBuildSettingsScene(GameScenes.Village, true) };
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(GameScenes.MainMenu);
            report.AppendLine("Build order: MainMenu -> Level01_Forest. GUIDs preserved. Background image referenced directly.");
            File.WriteAllText("Logs/MainMenu/setup.txt", report.ToString());
        }

        private static void MoveScene(string source, string destination, StringBuilder report)
        {
            VerifyAssetPath(source); VerifyAssetPath(destination);
            if (!File.Exists(source))
            {
                if (!File.Exists(destination)) throw new FileNotFoundException("Missing scene at both migration paths: " + source);
                report.AppendLine("Already classified: " + destination); return;
            }
            if (File.Exists(destination)) throw new IOException("Scene destination already exists: " + destination);
            ProjectBackups.PreserveAsset(source, "BeforeSceneOrganization");
            string guid = AssetDatabase.AssetPathToGUID(source);
            string error = AssetDatabase.MoveAsset(source, destination);
            if (!string.IsNullOrEmpty(error)) throw new IOException(error);
            if (AssetDatabase.AssetPathToGUID(destination) != guid) throw new InvalidOperationException("Scene GUID changed during migration.");
            report.AppendLine(source + " -> " + destination + " / GUID=" + guid);
        }
        private static void VerifyAssetPath(string path)
        {
            string root = Path.GetFullPath(Application.dataPath).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!Path.GetFullPath(path).StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Scene path is outside project Assets: " + path);
        }

        private static void CreateMainMenu()
        {
            if (File.Exists(GameScenes.MainMenu)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            font = MainMenuTypography.GetFontAsset();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/UI/Backgrounds/Background.png");
            if (texture == null) throw new FileNotFoundException("Main menu background is missing.");
            var camera = new GameObject("Main Menu Camera", typeof(Camera), typeof(AudioListener)); camera.tag = "MainCamera";
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor; camera.GetComponent<Camera>().backgroundColor = new Color(0.02f, 0.04f, 0.05f);
            var canvasObject = new GameObject("Main Menu Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = 0.5f;
            var root = canvasObject.GetComponent<RectTransform>();
            var background = Stretch("Background", root).gameObject.AddComponent<RawImage>(); background.texture = texture; background.raycastTarget = false;
            var fit = background.gameObject.AddComponent<AspectRatioFitter>(); fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; fit.aspectRatio = (float)texture.width / texture.height;
            var shade = Stretch("Readability gradient", root).gameObject.AddComponent<Image>(); shade.color = Color.white; shade.raycastTarget = false;
            shade.gameObject.AddComponent<MenuGradient>();
            var main = Stretch("Main", root);
            var content = Rect("Menu content", main, 0, 0, 920, 1080);
            var line = Rect("Eyebrow accent", content, 112, 122, 38, 2).gameObject.AddComponent<Image>(); line.color = Muted; line.raycastTarget = false;
            Label("Eyebrow", content, "BLACKWOOD  /  KHU VỰC CÁCH LY", 112, 146, 760, 30, 16, Muted);
            var title = Label("Title", content, "ECHOES\nOF THE FOG", 106, 216, 800, 210, 84, White, FontStyle.Bold); title.lineSpacing = -2;
            Label("Subtitle", content, "T H E   E X T R A C T I O N", 112, 438, 740, 32, 20, Muted);
            var story = Label("Invitation", content, "Đi vào màn sương.\nTìm đường trở về.", 112, 510, 680, 76, 23, new Color(0.77f, 0.81f, 0.78f)); story.lineSpacing = 4;
            var start = Button("StartButton", content, "BẮT ĐẦU CHƠI", 112, 652, 378, 66, true);
            var guide = Button("GuideButton", content, "HƯỚNG DẪN", 112, 738, 378, 62, false);
            var status = Label("Status", content, "", 112, 820, 680, 55, 18, White);
            var footer = Label("Footer", main, "ECHOES OF THE FOG  /  NHÓM 15", 112, 0, 750, 28, 13, new Color(0.65f, 0.72f, 0.69f, 0.75f));
            footer.rectTransform.anchorMin = footer.rectTransform.anchorMax = new Vector2(0, 0); footer.rectTransform.pivot = Vector2.zero; footer.rectTransform.anchoredPosition = new Vector2(112, 44);

            var guideRoot = Stretch("Guide", root);
            var veil = guideRoot.gameObject.AddComponent<Image>(); veil.color = new Color(0.015f, 0.025f, 0.03f, 0.77f);
            var panel = Rect("Guide panel", guideRoot, 0, 0, 1000, 740);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f); panel.anchoredPosition = Vector2.zero;
            var panelImage = panel.gameObject.AddComponent<Image>(); panelImage.color = new Color(0.045f, 0.075f, 0.08f, 0.98f); panelImage.raycastTarget = false; Border(panel, new Color(0.39f, 0.49f, 0.45f, 0.6f));
            Label("Guide title", panel, "HƯỚNG DẪN", 60, 44, 880, 55, 37, White, FontStyle.Bold);
            Label("Guide intro", panel, "Sẵn sàng trước khi bước vào màn sương.", 60, 112, 880, 35, 20, Muted);
            var divider = Rect("Divider", panel, 60, 166, 880, 1).gameObject.AddComponent<Image>(); divider.color = new Color(0.3f, 0.4f, 0.36f, 0.7f); divider.raycastTarget = false;
            Label("Movement heading", panel, "DI CHUYỂN", 60, 198, 410, 30, 16, Muted, FontStyle.Bold);
            Label("Combat heading", panel, "CHIẾN ĐẤU", 530, 198, 410, 30, 16, Muted, FontStyle.Bold);
            string[] leftKeys = { "W A S D", "CHUỘT", "SHIFT", "SPACE" }, leftActions = { "Di chuyển", "Xoay góc nhìn", "Chạy nhanh", "Nhảy" };
            string[] rightKeys = { "CHUỘT PHẢI", "CHUỘT TRÁI", "R", "ESC" }, rightActions = { "Ngắm bắn", "Bắn", "Nạp đạn", "Tạm dừng" };
            for (int i = 0; i < 4; i++)
            {
                Control(panel, leftKeys[i], leftActions[i], 60, 249 + i * 61);
                Control(panel, rightKeys[i], rightActions[i], 530, 249 + i * 61);
            }
            var note = Label("Survival tip", panel, "Runner có thể nghe tiếng súng và bước chạy.\nDi chuyển cẩn thận, giữ khoảng cách và chú ý lượng đạn.", 60, 523, 880, 78, 19, Muted); note.lineSpacing = 4;
            var back = Button("BackButton", panel, "TRỞ VỀ", 380, 636, 240, 58, true);
            var escape = Label("Back shortcut", panel, "Esc để quay lại", 300, 704, 400, 25, 14, Muted); escape.alignment = TextAlignmentOptions.Center;
            guideRoot.gameObject.SetActive(false);
            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            eventSystem.GetComponent<EventSystem>().firstSelectedGameObject = start.gameObject;
            var controller = new GameObject("Main Menu").AddComponent<MainMenuController>();
            Set(controller, "mainPanel", main.gameObject); Set(controller, "guidePanel", guideRoot.gameObject);
            Set(controller, "startButton", start); Set(controller, "guideButton", guide); Set(controller, "backButton", back);
            Set(controller, "startLabel", start.GetComponentInChildren<TMP_Text>()); Set(controller, "statusLabel", status);
            EditorSceneManager.SaveScene(scene, GameScenes.MainMenu);
        }

        private static RectTransform Stretch(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero; return rect;
        }
        private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); return rect;
        }
        private static TMP_Text Label(string name, Transform parent, string text, float x, float y, float width, float height, int size, Color color, FontStyle style = FontStyle.Normal)
        {
            var label = Rect(name, parent, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = font; label.fontSize = size; label.fontStyle = style == FontStyle.Bold ? FontStyles.Bold : FontStyles.Normal; label.text = text; label.color = color;
            label.raycastTarget = false; label.richText = false; label.alignment = TextAlignmentOptions.TopLeft;
            label.textWrappingMode = TextWrappingModes.Normal; label.overflowMode = TextOverflowModes.Overflow; return label;
        }
        private static Button Button(string name, Transform parent, string text, float x, float y, float width, float height, bool primary)
        {
            var rect = Rect(name, parent, x, y, width, height); var image = rect.gameObject.AddComponent<Image>(); image.color = Color.white;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = primary ? new Color(0.84f, 0.88f, 0.81f) : new Color(0.05f, 0.09f, 0.09f, 0.7f);
            colors.highlightedColor = primary ? new Color(0.98f, 0.99f, 0.92f) : new Color(0.2f, 0.29f, 0.27f, 0.95f);
            colors.selectedColor = colors.highlightedColor; colors.pressedColor = primary ? new Color(0.64f, 0.72f, 0.65f) : new Color(0.1f, 0.18f, 0.16f);
            colors.disabledColor = new Color(0.45f, 0.51f, 0.47f, 0.8f); colors.fadeDuration = 0.12f; button.colors = colors;
            var label = Label("Label", rect, text, 10, 0, width - 20, height, 20, primary ? new Color(0.065f, 0.1f, 0.085f) : White, FontStyle.Bold); label.alignment = TextAlignmentOptions.Center;
            if (!primary) Border(rect, new Color(0.63f, 0.72f, 0.65f, 0.6f));
            return button;
        }
        private static void Control(Transform panel, string key, string action, float x, float y)
        {
            var cap = Rect(key, panel, x, y, 190, 44); var image = cap.gameObject.AddComponent<Image>(); image.color = new Color(0.12f, 0.17f, 0.16f); image.raycastTarget = false;
            Border(cap, new Color(0.3f, 0.39f, 0.35f, 0.7f));
            var label = Label("Key", cap, key, 2, 0, 186, 44, 24, White, FontStyle.Bold); label.alignment = TextAlignmentOptions.Center;
            var description = Label("Action", panel, action, x + 210, y, 200, 44, 26, White); description.alignment = TextAlignmentOptions.Left;
        }
        private static void Border(RectTransform parent, Color color)
        {
            for (int i = 0; i < 4; i++)
            {
                var edge = Stretch("Edge " + i, parent); var image = edge.gameObject.AddComponent<Image>(); image.color = color; image.raycastTarget = false;
                if (i < 2) { edge.anchorMin = new Vector2(0, i); edge.anchorMax = new Vector2(1, i); edge.sizeDelta = new Vector2(0, 1); }
                else { edge.anchorMin = new Vector2(i - 2, 0); edge.anchorMax = new Vector2(i - 2, 1); edge.sizeDelta = new Vector2(1, 0); }
            }
        }
        private static void Set(UnityEngine.Object target, string property, UnityEngine.Object value)
        { var serialized = new SerializedObject(target); serialized.FindProperty(property).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
