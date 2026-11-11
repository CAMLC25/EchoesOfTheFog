using System;
using System.IO;
using System.Linq;
using EchoesOfTheFog.Core;
using EchoesOfTheFog.Enemy;
using EchoesOfTheFog.Mission;
using EchoesOfTheFog.Player;
using EchoesOfTheFog.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace EchoesOfTheFog.Editor
{
    public static class ForestGameFlowSetup
    {
        public static void Apply()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Close Unity and run in batch mode.");
            ProjectBackups.PreserveAsset(GameScenes.Forest, "BeforePauseGameOver");
            ProjectBackups.PreserveAsset(GameScenes.MainMenu, "BeforePauseGameOver");
            var scene = EditorSceneManager.OpenScene(GameScenes.Forest);
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerInputReader>();
            if (UnityEngine.Object.FindFirstObjectByType<ForestGameFlow>() == null) CreateUI(player);
            AddRunners(player.GetComponent<Health>());
            EditorSceneManager.SaveScene(scene);
            scene = EditorSceneManager.OpenScene(GameScenes.MainMenu);
            foreach (var label in Resources.FindObjectsOfTypeAll<TMP_Text>().Where(t => t.gameObject.scene == scene))
                if (label.text == "Thả chuột" || label.text == "Thả con trỏ" || label.text == "Tạm dừng / tiếp tục") label.text = "Tạm dừng";
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); EditorSceneManager.OpenScene(GameScenes.Forest);
        }
        internal static void CreateUI(PlayerInputReader player)
        {
            var gameplay = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas).ToArray();
            var canvas = new GameObject("Pause and Game Over canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; canvas.GetComponent<Canvas>().sortingOrder = 100; canvas.GetComponent<Canvas>().pixelPerfect = true;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var pause = Panel(canvas.transform, "Pause", "TẠM DỪNG", "Hít thở một chút. Khu rừng vẫn đang chờ.");
            var resume = Button(pause.transform.Find("Card"), "ResumeButton", "TIẾP TỤC", 220);
            var restart = Button(pause.transform.Find("Card"), "PauseRestartButton", "CHƠI LẠI", 304);
            var menu = Button(pause.transform.Find("Card"), "PauseMenuButton", "VỀ MENU", 388);
            var death = Panel(canvas.transform, "Game Over", "BẠN ĐÃ GỤC NGÃ", "Hành trình chưa kết thúc. Hãy thử lại từ đầu màn.");
            var retry = Button(death.transform.Find("Card"), "RetryButton", "CHƠI LẠI", 252);
            var deathMenu = Button(death.transform.Find("Card"), "DeathMenuButton", "VỀ MENU", 346);
            Label(pause.transform.Find("Card"), "Escape hint", "Esc — Tiếp tục", 20, 492, 680, 32, 22);
            var flow = canvas.AddComponent<ForestGameFlow>(); var serialized = new SerializedObject(flow);
            Set(serialized, "input", player); Set(serialized, "health", player.GetComponent<Health>()); Set(serialized, "mission", UnityEngine.Object.FindFirstObjectByType<ForestMission>());
            Set(serialized, "pausePanel", pause); Set(serialized, "gameOverPanel", death);
            Set(serialized, "resumeButton", resume); Set(serialized, "pauseRestartButton", restart); Set(serialized, "pauseMenuButton", menu);
            Set(serialized, "retryButton", retry); Set(serialized, "deathMenuButton", deathMenu);
            var canvases = serialized.FindProperty("gameplayCanvases"); canvases.arraySize = gameplay.Length;
            for (int i = 0; i < gameplay.Length; i++) canvases.GetArrayElementAtIndex(i).objectReferenceValue = gameplay[i];
            serialized.ApplyModifiedPropertiesWithoutUndo(); pause.SetActive(false); death.SetActive(false);
        }
        private static GameObject Panel(Transform parent, string name, string title, string description)
        {
            var panel = Rect(name, parent, 0, 0, 0, 0); panel.anchorMin = Vector2.zero; panel.anchorMax = Vector2.one; panel.sizeDelta = Vector2.zero;
            panel.gameObject.AddComponent<Image>().color = new Color(.015f, .025f, .03f, .86f);
            var card = Rect("Card", panel, 0, 0, 720, 560); card.anchorMin = card.anchorMax = card.pivot = new Vector2(.5f, .5f); card.anchoredPosition = Vector2.zero;
            card.gameObject.AddComponent<Image>().color = new Color(.04f, .07f, .075f, .98f);
            Label(card, "Title", title, 30, 48, 660, 68, 42);
            Label(card, "Description", description, 40, 132, 640, 72, 26);
            return panel.gameObject;
        }
        private static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h); return rect;
        }
        private static TMP_Text Label(Transform parent, string name, string text, float x, float y, float w, float h, int size)
        {
            var label = Rect(name, parent, x, y, w, h).gameObject.AddComponent<TextMeshProUGUI>(); label.font = MainMenuTypography.GetFontAsset(); label.fontSize = size;
            label.text = text; label.alignment = TextAlignmentOptions.Center; label.color = new Color(.91f, .93f, .87f); label.extraPadding = true; label.raycastTarget = false; return label;
        }
        private static Button Button(Transform parent, string name, string text, float y)
        {
            var rect = Rect(name, parent, 100, y, 520, 64); var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.19f, .27f, .24f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(.78f, .9f, .8f); colors.selectedColor = colors.highlightedColor; colors.pressedColor = new Color(.55f, .7f, .6f); button.colors = colors;
            Label(rect, "Caption", text, 8, 0, 504, 64, 28); return button;
        }
        private static void Set(SerializedObject target, string field, UnityEngine.Object value) => target.FindProperty(field).objectReferenceValue = value;
        private static float RouteX(float z) => Mathf.Sin((z + 18) * .075f) * 3.4f + Mathf.Sin(z * .17f) * .8f;
        private static void AddRunners(Health player)
        {
            if (GameObject.Find("Additional Runner encounters") != null) return;
            var group = new GameObject("Additional Runner encounters").transform;
            // Side patrols preserve the safe opening and avoid placing enemies directly on supply tables.
            var stations = new[] { new Vector2(20, -7), new Vector2(28, 8), new Vector2(44, -7) };
            foreach (var station in stations)
            {
                float z = station.x, side = station.y;
                Vector3 desired = new Vector3(RouteX(z) + side, .5f, z);
                if (!NavMesh.SamplePosition(desired, out var spawn, 3, NavMesh.AllAreas)) throw new InvalidOperationException("No NavMesh for added Runner " + z);
                var enemy = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemies/Runner.prefab"), group);
                enemy.name = "Runner side patrol " + z; enemy.transform.SetPositionAndRotation(spawn.position, Quaternion.Euler(0, 90, 0));
                var brain = enemy.GetComponent<RunnerBrain>(); var serialized = new SerializedObject(brain); Set(serialized, "target", player);
                var points = serialized.FindProperty("patrolPoints"); points.arraySize = 2;
                for (int i = 0; i < 2; i++)
                {
                    float pz = z + (i == 0 ? -2 : 2); Vector3 wanted = new Vector3(RouteX(pz) + side, .5f, pz);
                    if (!NavMesh.SamplePosition(wanted, out var hit, 3, NavMesh.AllAreas)) throw new InvalidOperationException("No patrol floor");
                    var path = new NavMeshPath();
                    if (!NavMesh.CalculatePath(spawn.position, hit.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete) throw new InvalidOperationException("Unreachable added patrol");
                    var point = new GameObject(enemy.name + " waypoint " + i).transform; point.SetParent(group); point.position = hit.position; points.GetArrayElementAtIndex(i).objectReferenceValue = point;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
