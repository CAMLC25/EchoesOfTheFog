using System;
using System.IO;
using System.Linq;
using EchoesOfTheFog.Combat;
using EchoesOfTheFog.Core;
using EchoesOfTheFog.Interaction;
using EchoesOfTheFog.Player;
using EchoesOfTheFog.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace EchoesOfTheFog.Editor
{
    public static class SuppliesSetup
    {
        private const string PlayerPath = "Assets/Game/Prefabs/Player/PlayerCapsule.prefab";
        private const string ItemsPath = "Assets/Game/Prefabs/Items/";
        public static void Apply()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use batch mode with the Editor closed.");
            Directory.CreateDirectory("Logs/Supplies"); AssetDatabase.Refresh();
            ProjectBackups.PreserveAsset(PlayerPath, "BeforeSupplies");
            ProjectBackups.PreserveAsset(GameScenes.Forest, "BeforeSupplies");
            ProjectBackups.PreserveAsset(GameScenes.MainMenu, "BeforeSupplies");
            const string inputPath = "Assets/InputSystem_Actions.inputactions";
            // Edit a detached copy; the Editor's project-wide action asset may be enabled.
            var actions = InputActionAsset.FromJson(File.ReadAllText(inputPath));
            var map = actions.FindActionMap("Player", true);
            if (map.FindAction("UseMedkit") == null)
            {
                ProjectBackups.PreserveAsset(inputPath, "BeforeSupplies");
                var action = map.AddAction("UseMedkit", InputActionType.Button);
                action.AddBinding("<Keyboard>/e", groups: "Keyboard&Mouse");
                action.AddBinding("<Gamepad>/dpad/up", groups: "Gamepad");
                File.WriteAllText(inputPath, actions.ToJson()); AssetDatabase.ImportAsset(inputPath);
            }
            UnityEngine.Object.DestroyImmediate(actions);
            var player = PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                if (player.GetComponent<PlayerSupplies>() == null) player.AddComponent<PlayerSupplies>();
                PrefabUtility.SaveAsPrefabAsset(player, PlayerPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(player); }
            var config = AssetDatabase.LoadAssetAtPath<RifleConfig>("Assets/Game/Data/Weapons/PrototypeRifle.asset");
            config.maximumReserve = 180; EditorUtility.SetDirty(config);
            MakePickup(SupplyKind.Ammo, "Assets/Game/Art/Items/Ammo/Ammo_shotgun.fbx", .28f, 30);
            MakePickup(SupplyKind.Medkit, "Assets/Game/Art/Items/Medkit/FirstAidKit.fbx", .36f, 1);
            var scene = EditorSceneManager.OpenScene(GameScenes.Forest);
            // Explicit batch authoring owns only these generated supply stations.
            var previous = GameObject.Find("Forest supplies");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous);
            var group = new GameObject("Forest supplies").transform;
            Station(group, -6, 2.3f); Station(group, 26, -2.3f);
            var supplies = UnityEngine.Object.FindFirstObjectByType<PlayerSupplies>();
            if (UnityEngine.Object.FindFirstObjectByType<PlayerSuppliesHUD>() == null) CreateUI(supplies);
            EditorSceneManager.SaveScene(scene);
            scene = EditorSceneManager.OpenScene(GameScenes.MainMenu);
            var labels = Resources.FindObjectsOfTypeAll<TMP_Text>().Where(t => t.gameObject.scene == scene);
            foreach (var label in labels)
            {
                if (label.text == "Nhặt vũ khí") label.text = "Nhặt vật phẩm";
                if (label.name == "Equipment note") label.text = "E — Dùng cứu thương (+40 máu)";
            }
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(GameScenes.Forest);
        }
        private static Bounds Bounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("No renderer: " + go.name);
            var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds); return bounds;
        }
        private static void MakePickup(SupplyKind kind, string modelPath, float size, int quantity)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            if (importer.importAnimation || importer.animationType != ModelImporterAnimationType.None)
            { importer.importAnimation = false; importer.animationType = ModelImporterAnimationType.None; importer.SaveAndReimport(); }
            var root = new GameObject(kind + " pickup");
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(modelPath), root.transform);
                if (kind == SupplyKind.Ammo)
                {
                    const string palettePath = "Assets/Game/Art/Items/Ammo/Materials/AmmoPickupPalette.mat";
                    var palette = AssetDatabase.LoadAssetAtPath<Material>(palettePath);
                    if (palette == null)
                    {
                        palette = new Material(Shader.Find("Standard")) { name = "Ammo pickup palette" };
                        AssetDatabase.CreateAsset(palette, palettePath);
                    }
                    palette.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/Art/Items/Ammo/Textures/Diffuse_palette_2.jpg");
                    palette.SetFloat("_Glossiness", .2f); EditorUtility.SetDirty(palette);
                    foreach (var renderer in model.GetComponentsInChildren<Renderer>()) renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => palette).ToArray();
                }
                var raw = Bounds(model);
                model.transform.localScale *= size / Mathf.Max(raw.size.x, raw.size.y, raw.size.z);
                var bounds = Bounds(model); model.transform.position -= new Vector3(bounds.center.x, bounds.min.y - .02f, bounds.center.z);
                foreach (var node in root.GetComponentsInChildren<Transform>()) node.gameObject.layer = LayerMask.NameToLayer("Interactable");
                foreach (var collider in model.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
                foreach (var renderer in model.GetComponentsInChildren<Renderer>())
                    foreach (var material in renderer.sharedMaterials)
                        if (material == null || material.shader == null || !material.shader.isSupported) throw new InvalidOperationException("Invalid material on " + modelPath);
                var trigger = root.AddComponent<BoxCollider>(); trigger.isTrigger = true; trigger.center = Vector3.up * .2f; trigger.size = new Vector3(.65f, .6f, .65f);
                var pickup = root.AddComponent<SupplyPickup>(); var serialized = new SerializedObject(pickup);
                serialized.FindProperty("kind").enumValueIndex = (int)kind; serialized.FindProperty("quantity").intValue = quantity; serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, ItemsPath + kind + "Pickup.prefab");
                File.AppendAllText("Logs/Supplies/models.txt", $"{modelPath}: raw {raw.size}, normalized {Bounds(model).size}, materials {string.Join(", ", model.GetComponentsInChildren<Renderer>().SelectMany(r => r.sharedMaterials).Select(m => m.name + "/" + m.shader.name))}\n");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static void Station(Transform parent, float z, float side)
        {
            float x = Mathf.Sin((z + 18) * .075f) * 3.4f + Mathf.Sin(z * .17f) * .8f + side;
            int mask = ~LayerMask.GetMask("Player", "Enemy", "Interactable"); Physics.SyncTransforms();
            if (!Physics.Raycast(new Vector3(x, 10, z), Vector3.down, out var hit, 25, mask, QueryTriggerInteraction.Ignore)) throw new InvalidOperationException("No supply station ground");
            var group = new GameObject("Supply station " + z).transform; group.SetParent(parent);
            var table = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flooded_Grounds/Prefabs/Props/Prop_LargeTable_A.prefab"), group);
            table.transform.localScale = Vector3.one; var bounds = Bounds(table); table.transform.localScale *= .7f / bounds.size.y;
            bounds = Bounds(table); table.transform.position += hit.point - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z); bounds = Bounds(table);
            if (bounds.size.x < .95f || bounds.size.z < .6f) throw new InvalidOperationException("Supply tabletop is too small: " + bounds.size);
            if (table.GetComponentsInChildren<Collider>().Length == 0)
            { var box = table.AddComponent<BoxCollider>(); box.center = table.transform.InverseTransformPoint(bounds.center); box.size = bounds.size / table.transform.localScale.x; }
            var obstacle = new GameObject("Table navigation obstacle").AddComponent<NavMeshObstacle>(); obstacle.transform.SetParent(group); obstacle.transform.position = bounds.center;
            obstacle.shape = NavMeshObstacleShape.Box; obstacle.size = bounds.size; obstacle.carving = true;
            for (int i = 0; i < 2; i++)
            {
                var pickup = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ItemsPath + (i == 0 ? "Ammo" : "Medkit") + "Pickup.prefab"), group);
                pickup.transform.position = new Vector3(bounds.center.x + (i == 0 ? -.24f : .24f), bounds.max.y + .02f, bounds.center.z);
            }
            var beacon = new GameObject("Supply lantern").AddComponent<Light>(); beacon.transform.SetParent(group); beacon.transform.position = bounds.center + Vector3.up * 1.2f;
            beacon.type = LightType.Point; beacon.range = 3; beacon.intensity = 1.3f; beacon.color = new Color(1, .8f, .45f); beacon.shadows = LightShadows.None;
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 offset)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.sizeDelta = size; rect.anchoredPosition = offset; return rect;
        }
        private static TMP_Text Label(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 offset, int fontSize)
        {
            var label = Rect(name, parent, size, anchor, offset).gameObject.AddComponent<TextMeshProUGUI>();
            label.font = MainMenuTypography.GetFontAsset(); label.fontSize = fontSize; label.color = new Color(.91f, .93f, .87f);
            label.raycastTarget = false; label.extraPadding = true; return label;
        }
        internal static void CreateUI(PlayerSupplies supplies)
        {
            var canvas = new GameObject("Supplies canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.GetComponent<Canvas>().pixelPerfect = true;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            var panel = Rect("Supplies inventory", canvas.transform, new Vector2(480, 138), new Vector2(1, 0), new Vector2(-32, 28));
            panel.gameObject.AddComponent<Image>().color = new Color(.04f, .07f, .075f, .9f);
            var label = Label("Inventory label", panel, new Vector2(448, 114), new Vector2(0, 1), new Vector2(16, -12), 24);
            var feedback = Label("Supply feedback", canvas.transform, new Vector2(800, 44), new Vector2(.5f, 0), new Vector2(0, 170), 27); feedback.alignment = TextAlignmentOptions.Center;
            var hud = canvas.AddComponent<PlayerSuppliesHUD>(); hud.supplies = supplies; hud.rifle = supplies.GetComponent<HitscanRifle>(); hud.inventoryLabel = label; hud.feedbackLabel = feedback; hud.inventoryPanel = panel.gameObject;
        }
    }
}
