using System;
using System.IO;
using System.Linq;
using EchoesOfTheFog.Combat;
using EchoesOfTheFog.Core;
using EchoesOfTheFog.Interaction;
using EchoesOfTheFog.Mission;
using EchoesOfTheFog.Player;
using EchoesOfTheFog.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace EchoesOfTheFog.Editor
{
    public static class OpeningFlowSetup
    {
        private const string PlayerPath = "Assets/Game/Prefabs/Player/PlayerCapsule.prefab";
        private const string PickupPath = "Assets/Game/Prefabs/Items/WeaponPickup.prefab";
        private static TMP_FontAsset font;
        private static readonly Color Ink = new Color(0.04f, 0.07f, 0.075f, 0.96f), Paper = new Color(0.91f, 0.93f, 0.87f), Muted = new Color(0.73f, 0.81f, 0.76f);

        public static void Apply()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Close Unity before opening-flow batch setup.");
            Directory.CreateDirectory("Assets/Game/Prefabs/Items"); Directory.CreateDirectory("Logs/OpeningFlow"); AssetDatabase.Refresh();
            int layer = InteractionLayer();
            ConfigurePlayer(layer); ConfigureAnimator(); CreatePickup(layer); ConfigureMenuIntro(); ConfigureForest(layer);
            AssetDatabase.SaveAssets(); EditorSceneManager.OpenScene(GameScenes.MainMenu);
            File.WriteAllText("Logs/OpeningFlow/setup.txt", "Story intro + unarmed Forest start + F weapon pickup + find-weapon/reach-village objectives configured. Test scenes retain their equipped Player.\n");
        }
        private static int InteractionLayer()
        {
            int existing = LayerMask.NameToLayer("Interactable"); if (existing >= 0) return existing;
            var serialized = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = serialized.FindProperty("layers");
            for (int i = 9; i < 32; i++) if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
            { layers.GetArrayElementAtIndex(i).stringValue = "Interactable"; serialized.ApplyModifiedPropertiesWithoutUndo(); return i; }
            throw new InvalidOperationException("No free Interactable layer.");
        }
        private static void ConfigurePlayer(int layer)
        {
            ProjectBackups.PreserveAsset(PlayerPath, "BeforeOpeningFlow");
            var root = PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                var equipment = root.GetComponent<PlayerEquipment>(); if (equipment == null) equipment = root.AddComponent<PlayerEquipment>();
                Set(equipment, "weaponVisual", root.GetComponentInChildren<RifleView>(true).gameObject);
                var interactor = root.GetComponent<PlayerInteractor>(); if (interactor == null) interactor = root.AddComponent<PlayerInteractor>();
                var serialized = new SerializedObject(interactor);
                serialized.FindProperty("interactableMask").intValue = 1 << layer;
                serialized.FindProperty("occlusionMask").intValue = ~((1 << layer) | (1 << root.layer) | (1 << LayerMask.NameToLayer("Enemy")));
                serialized.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(root, PlayerPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
        private static AnimationClip Clip(string name) => AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Animations/Player/Ch11_nonPBR@" + name + ".fbx")
            .OfType<AnimationClip>().First(clip => !clip.name.StartsWith("__preview__"));
        private static void ConfigureAnimator()
        {
            const string path = "Assets/Game/Animations/Player/Generated/ElenaRifle.controller";
            ProjectBackups.PreserveAsset(path, "BeforeOpeningFlow");
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (!controller.parameters.Any(parameter => parameter.name == "MoveSpeed")) controller.AddParameter("MoveSpeed", AnimatorControllerParameterType.Float);
            var ground = controller.layers[0].stateMachine.states.First(state => state.state.name == "Grounded").state;
            if (ground.motion.name != "Weapon locomotion")
            {
                var unarmed = new BlendTree { name = "Unarmed locomotion", blendType = BlendTreeType.Simple1D, blendParameter = "MoveSpeed", useAutomaticThresholds = false };
                AssetDatabase.AddObjectToAsset(unarmed, controller);
                unarmed.AddChild(Clip("Idle"), 0); unarmed.AddChild(Clip("Walking"), 1); unarmed.AddChild(Clip("Running"), 2.08f);
                var equipment = new BlendTree { name = "Weapon locomotion", blendType = BlendTreeType.Simple1D, blendParameter = "Armed", useAutomaticThresholds = false };
                AssetDatabase.AddObjectToAsset(equipment, controller); equipment.AddChild(unarmed, 0); equipment.AddChild(ground.motion, 1);
                ground.motion = equipment; EditorUtility.SetDirty(unarmed); EditorUtility.SetDirty(equipment); EditorUtility.SetDirty(ground);
            }
            EditorUtility.SetDirty(controller); AssetDatabase.SaveAssets();
        }
        private static void CreatePickup(int layer)
        {
            if (File.Exists(PickupPath)) return;
            var root = new GameObject("Súng ức chế - nhặt bằng F"); root.layer = layer;
            try
            {
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Weapons/PlayerRifle.prefab"), root.transform);
                visual.name = "Pickup Gun_31"; visual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 0, 90));
                var bounds = Bounds(visual); visual.transform.localPosition -= new Vector3(bounds.center.x, bounds.min.y - 0.025f, bounds.center.z);
                foreach (var transform in root.GetComponentsInChildren<Transform>()) transform.gameObject.layer = layer;
                var collider = root.AddComponent<BoxCollider>(); collider.isTrigger = true; collider.center = new Vector3(0, 0.18f, 0); collider.size = new Vector3(1.2f, 0.65f, 1.2f);
                root.AddComponent<WeaponPickup>();
                var glow = new GameObject("Supply beacon").AddComponent<Light>(); glow.transform.SetParent(root.transform, false); glow.transform.localPosition = new Vector3(0, 0.6f, 0);
                glow.type = LightType.Point; glow.range = 3.5f; glow.intensity = 1.8f; glow.color = new Color(1, 0.76f, 0.35f); glow.shadows = LightShadows.None;
                PrefabUtility.SaveAsPrefabAsset(root, PickupPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        public static void ConfigureMenuIntro()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use batch mode for story UI authoring.");
            ProjectBackups.PreserveAsset(GameScenes.MainMenu, "BeforeOpeningFlow"); font = MainMenuTypography.GetFontAsset();
            var scene = EditorSceneManager.OpenScene(GameScenes.MainMenu);
            var canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>().transform;
            var controller = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
            var story = canvas.Find("Story");
            if (story == null)
            {
                story = Stretch("Story", canvas); story.gameObject.AddComponent<Image>().color = new Color(0.015f, 0.025f, 0.03f, 0.82f);
                var panel = Center("Story panel", story, 1120, 840); panel.gameObject.AddComponent<Image>().color = Ink;
                Label("Story eyebrow", panel, "GIỚI THIỆU", 60, 42, 1000, 30, 22, Muted);
                Label("Story title", panel, "BLACKWOOD", 60, 88, 1000, 62, 44, Paper, true);
                Label("Story role", panel, "Elena  /  Chuyên viên quân y", 60, 162, 1000, 42, 27, Muted);
                string narrative = "Blackwood bị phong tỏa sau đợt bùng phát bào tử. Những người dân nhiễm bệnh đã trở thành những kẻ mộng du, và quân đội chuẩn bị phá hủy khu vực.\n\n"
                    + "Elena, một chuyên viên quân y, tiến vào thung lũng để tìm mẫu dịch chiết bào tử, cứu em gái và những người sống sót. Chiếc xe tải cũ là hy vọng đưa mọi người thoát khỏi Blackwood.\n\n"
                    + "Trước mắt là bìa rừng. Hãy tìm vũ khí và vượt qua những kẻ mộng du để tới cổng làng.";
                Label("Story body", panel, narrative, 60, 224, 1000, 334, 26, Paper);
                var objective = Rect("First objective", panel, 60, 580, 1000, 76); objective.gameObject.AddComponent<Image>().color = new Color(0.13f, 0.2f, 0.17f, 0.95f);
                Label("First objective heading", objective, "NHIỆM VỤ MỞ ĐẦU", 18, 8, 964, 27, 20, Muted);
                Label("First objective description", objective, "Tìm khẩu súng ức chế sinh học", 18, 37, 964, 32, 26, Paper, true);
                Button("ContinueStoryButton", panel, "VÀO BLACKWOOD", 60, 690, 360, 64, true);
                Button("SkipStoryButton", panel, "BỎ QUA", 446, 690, 220, 64, false);
                Label("Story cancel hint", panel, "Esc để trở về menu", 60, 786, 1000, 30, 20, Muted);
                story.gameObject.SetActive(false);
            }
            Set(controller, "storyPanel", story.gameObject);
            Set(controller, "continueButton", story.GetComponentsInChildren<Button>(true).First(button => button.name == "ContinueStoryButton"));
            Set(controller, "skipButton", story.GetComponentsInChildren<Button>(true).First(button => button.name == "SkipStoryButton"));
            AddInteractionGuide(canvas);
            EditorSceneManager.SaveScene(scene, GameScenes.MainMenu);
        }
        private static void AddInteractionGuide(Transform canvas)
        {
            var panel = canvas.GetComponentsInChildren<RectTransform>(true).First(item => item.name == "Guide panel");
            if (panel.Find("Interact key") != null) return;
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 840);
            var template = panel.GetComponentsInChildren<TMP_Text>(true).First(label => label.name == "Key" && label.text == "W A S D").transform.parent.gameObject;
            var key = UnityEngine.Object.Instantiate(template, panel).GetComponent<RectTransform>(); key.name = "Interact key"; key.anchoredPosition = new Vector2(60, -494); key.GetComponentInChildren<TMP_Text>().text = "F";
            var action = UnityEngine.Object.Instantiate(panel.GetComponentsInChildren<TMP_Text>(true).First(label => label.name == "Action" && label.text == "Di chuyển"), panel);
            action.text = "Nhặt vật phẩm"; action.rectTransform.anchoredPosition = new Vector2(270, -494);
            Label("Equipment note", panel, "E — Dùng cứu thương (+40 máu)", 530, 494, 410, 44, 22, Muted);
            var note = panel.GetComponentsInChildren<TMP_Text>(true).First(label => label.name == "Survival tip"); note.rectTransform.anchoredPosition = new Vector2(60, -568);
            var back = panel.GetComponentsInChildren<Button>(true).First(button => button.name == "BackButton"); ((RectTransform)back.transform).anchoredPosition = new Vector2(380, -686);
            var hint = panel.GetComponentsInChildren<TMP_Text>(true).First(label => label.name == "Back shortcut"); hint.rectTransform.anchoredPosition = new Vector2(300, -762);
        }

        private static void ConfigureForest(int layer)
        {
            ProjectBackups.PreserveAsset(GameScenes.Forest, "BeforeOpeningFlow");
            var scene = EditorSceneManager.OpenScene(GameScenes.Forest);
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerEquipment>();
            var settings = new SerializedObject(player); settings.FindProperty("startEquipped").boolValue = false; settings.ApplyModifiedPropertiesWithoutUndo();
            player.WeaponVisual.SetActive(false); PrefabUtility.RecordPrefabInstancePropertyModifications(player.WeaponVisual); PrefabUtility.RecordPrefabInstancePropertyModifications(player);
            var mission = UnityEngine.Object.FindFirstObjectByType<ForestMission>();
            if (mission == null)
            {
                var group = new GameObject("Forest opening objective").transform;
                var table = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flooded_Grounds/Prefabs/Props/Prop_SmallTable_A.prefab"), group);
                table.name = "Supply table"; table.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 20, 0)); table.transform.localScale = Vector3.one;
                var bounds = Bounds(table); table.transform.localScale = Vector3.one * (0.7f / bounds.size.y); bounds = Bounds(table);
                Vector3 ground = Floor(player.transform.position + new Vector3(-2, 0, 3.2f), layer);
                table.transform.position += ground - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z); bounds = Bounds(table);
                if (table.GetComponentsInChildren<Collider>().Length == 0)
                {
                    var collider = table.AddComponent<BoxCollider>(); collider.center = table.transform.InverseTransformPoint(bounds.center);
                    collider.size = bounds.size / table.transform.localScale.x;
                }
                var obstacle = new GameObject("Supply table navigation obstacle").AddComponent<NavMeshObstacle>(); obstacle.transform.SetParent(group, false);
                obstacle.transform.position = bounds.center; obstacle.shape = NavMeshObstacleShape.Box; obstacle.size = bounds.size; obstacle.carving = true;
                var pickup = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PickupPath), group);
                pickup.transform.SetPositionAndRotation(new Vector3(bounds.center.x, bounds.max.y + 0.02f, bounds.center.z), Quaternion.Euler(0, -15, 0));
                mission = group.gameObject.AddComponent<ForestMission>(); Set(mission, "player", player); Set(mission, "weaponTarget", pickup.transform);
                var gate = new GameObject("Village entry goal"); gate.transform.SetParent(group, false);
                Vector3 entrance = GameObject.Find("VillageEntrance").transform.position + Vector3.forward * 3;
                gate.transform.position = Floor(entrance, layer) + Vector3.up * 1.5f;
                var trigger = gate.AddComponent<BoxCollider>(); trigger.isTrigger = true; trigger.size = new Vector3(7, 3, 4);
                Set(gate.AddComponent<ForestExitTrigger>(), "mission", mission); Set(mission, "villageTarget", gate.transform);
                CreateMissionUI(mission, player.GetComponent<PlayerInteractor>());
            }
            EditorSceneManager.SaveScene(scene, GameScenes.Forest);
        }
        private static void CreateMissionUI(ForestMission mission, PlayerInteractor interactor)
        {
            font = MainMenuTypography.GetFontAsset();
            var canvas = new GameObject("Forest objective canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; canvas.GetComponent<Canvas>().pixelPerfect = true;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;
            var root = canvas.GetComponent<RectTransform>();
            var card = Rect("Current objective", root, 0, 0, 600, 146); card.anchorMin = card.anchorMax = card.pivot = Vector2.one; card.anchoredPosition = new Vector2(-32, -24);
            var image = card.gameObject.AddComponent<Image>(); image.color = Ink; image.raycastTarget = false;
            Label("Objective heading", card, "BÌA RỪNG BLACKWOOD", 20, 14, 560, 28, 20, Muted);
            var objective = Label("Objective", card, "Tìm khẩu súng ức chế sinh học", 20, 48, 560, 56, 27, Paper, true);
            var detail = Label("Objective distance", card, "", 20, 111, 560, 28, 21, Muted);
            var prompt = Rect("Interaction prompt", root, 0, 0, 690, 58); prompt.anchorMin = prompt.anchorMax = new Vector2(0.5f, 0); prompt.pivot = new Vector2(0.5f, 0); prompt.anchoredPosition = new Vector2(0, 96);
            prompt.gameObject.AddComponent<Image>().color = Ink;
            var promptLabel = Label("Interaction label", prompt, "", 10, 0, 670, 58, 28, Paper, true); promptLabel.alignment = TextAlignmentOptions.Center;
            var completion = Stretch("Forest completed", root); completion.gameObject.AddComponent<Image>().color = new Color(0.015f, 0.025f, 0.03f, 0.82f);
            var panel = Center("Forest completed panel", completion, 960, 440); panel.gameObject.AddComponent<Image>().color = Ink;
            Label("Completed title", panel, "ĐÃ TỚI CỔNG LÀNG", 50, 60, 860, 68, 42, Paper, true);
            Label("Completed description", panel, "Bạn đã hoàn thành chặng bìa rừng.\nNhững người sống sót vẫn đang chờ phía trước.", 50, 154, 860, 108, 27, Muted);
            var back = Button("ForestMenuButton", panel, "TRỞ VỀ MENU", 50, 316, 340, 66, true); completion.gameObject.SetActive(false);
            var hud = canvas.AddComponent<ForestMissionHUD>(); Set(hud, "mission", mission); Set(hud, "interactor", interactor);
            Set(hud, "objectiveLabel", objective); Set(hud, "detailLabel", detail); Set(hud, "interactionLabel", promptLabel);
            Set(hud, "interactionPanel", prompt.gameObject); Set(hud, "completionPanel", completion.gameObject); Set(hud, "menuButton", back);
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
                new GameObject("Forest EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule)).GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }
        private static Vector3 Floor(Vector3 position, int interactionLayer)
        {
            int mask = ~((1 << interactionLayer) | (1 << LayerMask.NameToLayer("Player")) | (1 << LayerMask.NameToLayer("Enemy")));
            Physics.SyncTransforms();
            if (!Physics.Raycast(new Vector3(position.x, 10, position.z), Vector3.down, out var hit, 25, mask, QueryTriggerInteraction.Ignore))
                throw new InvalidOperationException("No ground at opening-flow position: " + position);
            return hit.point;
        }
        private static Bounds Bounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(); var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds); return bounds;
        }
        private static RectTransform Stretch(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero; return rect;
        }
        private static RectTransform Rect(string name, Transform parent, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); return rect;
        }
        private static RectTransform Center(string name, Transform parent, float width, float height)
        { var rect = Rect(name, parent, 0, 0, width, height); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = Vector2.zero; return rect; }
        private static TMP_Text Label(string name, Transform parent, string text, float x, float y, float width, float height, int size, Color color, bool bold = false)
        {
            var label = Rect(name, parent, x, y, width, height).gameObject.AddComponent<TextMeshProUGUI>(); label.font = font; label.fontSize = size;
            label.text = text; label.color = color; label.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal; label.raycastTarget = false; label.richText = false;
            label.textWrappingMode = TextWrappingModes.Normal; label.overflowMode = TextOverflowModes.Overflow; label.extraPadding = true; return label;
        }
        private static Button Button(string name, Transform parent, string text, float x, float y, float width, float height, bool primary)
        {
            var rect = Rect(name, parent, x, y, width, height); var image = rect.gameObject.AddComponent<Image>();
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; var colors = button.colors;
            colors.normalColor = primary ? new Color(0.84f, 0.88f, 0.81f) : new Color(0.15f, 0.22f, 0.2f);
            colors.highlightedColor = primary ? Paper : new Color(0.26f, 0.36f, 0.31f); colors.selectedColor = colors.highlightedColor; colors.pressedColor = Muted; colors.fadeDuration = 0.1f; button.colors = colors;
            var label = Label("Button caption", rect, text, 8, 0, width - 16, height, 28, primary ? new Color(0.05f, 0.09f, 0.07f) : Paper, true); label.alignment = TextAlignmentOptions.Center; return button;
        }
        private static void Set(UnityEngine.Object target, string property, UnityEngine.Object value)
        { var serialized = new SerializedObject(target); serialized.FindProperty(property).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
