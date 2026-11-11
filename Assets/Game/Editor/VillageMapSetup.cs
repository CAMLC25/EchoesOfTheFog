using System;
using System.IO;
using System.Linq;
using EchoesOfTheFog.Core;
using EchoesOfTheFog.Enemy;
using EchoesOfTheFog.Interaction;
using EchoesOfTheFog.Mission;
using EchoesOfTheFog.Player;
using EchoesOfTheFog.UI;
using TMPro;
using Unity.AI.Navigation;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace EchoesOfTheFog.Editor
{
    public static class VillageMapSetup
    {
        private const string Output = "Assets/Game/Art/Village";
        private static Transform environment;
        private static Material ground, wood, metal, sign;
        private static VillageRoute route;
        private static System.Random random;
        public static void Build()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Use batch mode with Unity closed.");
            if (File.Exists(GameScenes.Village)) throw new InvalidOperationException("Village already exists; preserve authored scene and use explicit migration.");
            Directory.CreateDirectory(Output); Directory.CreateDirectory("Logs/Village"); AssetDatabase.Refresh();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            random = new System.Random(150212); environment = new GameObject("Blackwood Village Environment").transform;
            ground = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Art/ForestEntryStudy/Materials/ForestFloor.mat");
            wood = Material("Weathered bridge wood", new Color(.23f, .19f, .13f));
            metal = Material("Generator painted steel", new Color(.16f, .23f, .19f)); sign = Material("Medical white", new Color(.82f, .81f, .68f));
            Lighting(); Ground("South bank", -28, 36); Ground("North bank", 44, 114); River();
            Houses(); Dressing(); Boundaries();
            route = new GameObject("Village route - generator to clinic").AddComponent<VillageRoute>();
            Bridge(); Generator(); Clinic();
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Player/PlayerCapsule.prefab"));
            player.transform.SetPositionAndRotation(new Vector3(0, .05f, -15), Quaternion.identity); route.player = player.GetComponent<PlayerInputReader>();
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CinemachineBrain)); camera.tag = "MainCamera";
            camera.GetComponent<Camera>().nearClipPlane = .1f; camera.GetComponent<Camera>().farClipPlane = 220; camera.GetComponent<Camera>().fieldOfView = 58;
            camera.transform.position = new Vector3(.4f, 2, -18.5f); camera.transform.LookAt(player.transform.position + Vector3.up * 1.3f);
            UI(); SuppliesSetup.CreateUI(player.GetComponent<PlayerSupplies>()); ForestGameFlowSetup.CreateUI(route.player);
            Bake(); Enemies(player.GetComponent<Health>());
            // Bake the lowered deck, then let the carving obstacle close its route at runtime.
            route.bridgePivot.localRotation = Quaternion.Euler(-78, 0, 0); route.bridgeFloor.enabled = false; route.bridgeBlocker.enabled = true;
            EditorSceneManager.SaveScene(scene, GameScenes.Village); AssetDatabase.SaveAssets(); ConnectForest();
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(GameScenes.MainMenu, true), new EditorBuildSettingsScene(GameScenes.Forest, true), new EditorBuildSettingsScene(GameScenes.Village, true) };
            EditorSceneManager.OpenScene(GameScenes.Village);
        }
        private static Material Material(string name, Color color)
        {
            string path = Output + "/" + name + ".mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result == null) { result = new Material(Shader.Find("Standard")) { name = name }; AssetDatabase.CreateAsset(result, path); }
            result.color = color; result.SetFloat("_Glossiness", .15f); EditorUtility.SetDirty(result); return result;
        }
        private static Bounds Bounds(GameObject go)
        { var renderers = go.GetComponentsInChildren<Renderer>(); var result = renderers[0].bounds; foreach (var renderer in renderers) result.Encapsulate(renderer.bounds); return result; }
        private static GameObject Place(string path, Vector3 position, float yaw, float scale, Transform parent = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flooded_Grounds/Prefabs/" + path + ".prefab");
            if (prefab == null) throw new InvalidOperationException("Missing prefab " + path);
            var item = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent == null ? environment : parent);
            item.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, yaw, 0)); item.transform.localScale = Vector3.one * scale;
            var bounds = Bounds(item); item.transform.position += position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z); return item;
        }
        private static GameObject Box(string name, Vector3 position, Vector3 size, Material material, Transform parent = null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(parent == null ? environment : parent, false);
            go.transform.position = position; go.transform.localScale = size; go.GetComponent<Renderer>().sharedMaterial = material; return go;
        }
        private static void Ground(string name, int startZ, int endZ)
        {
            const int width = 96; int length = endZ - startZ;
            var vertices = new Vector3[(width + 1) * (length + 1)]; var uv = new Vector2[vertices.Length]; var colors = new Color[vertices.Length]; var triangles = new int[width * length * 6];
            for (int z = 0; z <= length; z++) for (int x = 0; x <= width; x++)
            {
                int i = z * (width + 1) + x; float wx = x - 48, wz = z + startZ;
                float elevation = Mathf.Abs(wx) < 30 ? 0 : Mathf.SmoothStep(0, 2.8f, Mathf.InverseLerp(30, 46, Mathf.Abs(wx)));
                vertices[i] = new Vector3(wx, elevation, wz); uv[i] = new Vector2(wx, wz);
                float road = 1 - Mathf.SmoothStep(3, 5.5f, Mathf.Abs(wx));
                float square = (1 - Mathf.SmoothStep(10, 15, Mathf.Abs(wx))) * (1 - Mathf.SmoothStep(8, 12, Mathf.Abs(wz - 16)));
                float branch = (1 - Mathf.SmoothStep(2, 4, Mathf.Abs(wz - 25))) * (wx < 1 && wx > -24 ? 1 : 0);
                colors[i] = new Color(Mathf.Max(road, square, branch), 0, 0, 1);
                if (z == length || x == width) continue; int t = (z * width + x) * 6;
                triangles[t] = i; triangles[t + 1] = i + width + 1; triangles[t + 2] = i + 1; triangles[t + 3] = i + 1; triangles[t + 4] = i + width + 1; triangles[t + 5] = i + width + 2;
            }
            var mesh = new Mesh { name = name, vertices = vertices, uv = uv, colors = colors, triangles = triangles }; mesh.RecalculateNormals(); mesh.RecalculateTangents();
            string meshPath = Output + "/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (existing == null) AssetDatabase.CreateAsset(mesh, meshPath);
            else { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); mesh = existing; EditorUtility.SetDirty(mesh); }
            var floor = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider)); floor.transform.SetParent(environment);
            floor.GetComponent<MeshFilter>().sharedMesh = mesh; floor.GetComponent<MeshRenderer>().sharedMaterial = ground; floor.GetComponent<MeshCollider>().sharedMesh = mesh;
        }
        private static void River()
        {
            var water = Box("Canal water - hazardous", new Vector3(0, -.75f, 40), new Vector3(96, .08f, 8), AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Art/ForestEntryStudy/Materials/CreekWater.mat"));
            UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());
            var hazard = new GameObject("Deep canal fall trigger", typeof(BoxCollider), typeof(VillageWaterHazard)); hazard.transform.SetParent(environment); hazard.transform.position = new Vector3(0, -3, 40);
            hazard.GetComponent<BoxCollider>().size = new Vector3(100, 5, 8); hazard.GetComponent<BoxCollider>().isTrigger = true;
            for (int side = -1; side <= 1; side += 2) for (int x = -38; x <= 38; x += 4)
                if (Mathf.Abs(x) > 4) Place("Nature/Rocks/CobbleRock_A", new Vector3(x, -.55f, side < 0 ? 35.4f : 44.6f), x * 17, .5f);
        }
        private static void Houses()
        {
            Place("Buildings/Structures1/Struct_Fence1_Gate_A", new Vector3(0, 0, -9), 0, .7f);
            Sign("BLACKWOOD", new Vector3(0, 3.4f, -9.7f), 0, 1.8f);
            foreach (var spot in new[] { new Vector3(-14, 0, 1), new Vector3(14, 0, 3), new Vector3(-15, 0, 18), new Vector3(15, 0, 21), new Vector3(-14, 0, 58), new Vector3(15, 0, 62), new Vector3(-16, 0, 78), new Vector3(15, 0, 82) })
            {
                bool damaged = spot.z == 21 || spot.z == 78;
                Place("Buildings/Cabins/" + (damaged ? "Cabin1_DM" : "Cabin1"), spot, spot.x < 0 ? 90 : -90, .62f);
            }
            Place("Buildings/GuardHouse/GuardHouse_A", new Vector3(-22, 0, 29), 100, .36f);
            Place("Buildings/Structures1/Struct_Kiosk_A", new Vector3(8, 0, 14), -90, .65f);
            Place("Props/Prop_Car_A", new Vector3(6.5f, 0, 28), -28, .65f);
            Place("Props/Prop_ParkBench_A", new Vector3(-7, 0, 14), 90, .6f);
            for (int side = -1; side <= 1; side += 2) for (int i = 0; i < 6; i++)
                Place("Buildings/Structures1/Struct_Fence1_Mid_A", new Vector3(side * 27, 0, -4 + i * 6.2f), 90, .94f);
            Sign("MÁY PHÁT  ←     TRẠM Y TẾ  ↑", new Vector3(-4.5f, 2.3f, 24), 0, 1.6f);
        }
        private static void Bridge()
        {
            var pivot = new GameObject("Drawbridge hinge - south bank").transform; pivot.SetParent(environment); pivot.position = new Vector3(0, .12f, 34);
            var boards = Place("Buildings/Structures1/Struct_WoodPath_A", new Vector3(0, 0, 0), 90, 1, pivot);
            var bounds = Bounds(boards); boards.transform.localScale = Vector3.Scale(boards.transform.localScale, new Vector3(4.2f / bounds.size.z, 1, 12 / bounds.size.x));
            // Normalize after rotation in world space with a parent wrapper to keep the deck at 4.2 x 12 m.
            bounds = Bounds(boards);
            var wrapper = new GameObject("Bridge deck art").transform; wrapper.SetParent(pivot, false); boards.transform.SetParent(wrapper, true);
            wrapper.localScale = new Vector3(4.2f / bounds.size.x, 1, 12 / bounds.size.z);
            bounds = Bounds(boards); wrapper.position += new Vector3(-bounds.center.x, .15f - bounds.max.y, 40 - bounds.center.z);
            var floor = Box("Bridge walkable collision", new Vector3(0, .05f, 40), new Vector3(4.2f, .2f, 12), wood, pivot); floor.GetComponent<Renderer>().enabled = false;
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Bridge rail", new Vector3(side * 2.05f, 1.05f, 40), new Vector3(.12f, .16f, 12), wood, pivot);
                for (int z = 34; z <= 46; z += 3) Box("Bridge rail post", new Vector3(side * 2.05f, .65f, z), new Vector3(.15f, 1.25f, .15f), wood, pivot);
            }
            var obstacle = new GameObject("Closed bridge navigation blocker").AddComponent<NavMeshObstacle>(); obstacle.transform.SetParent(environment); obstacle.transform.position = new Vector3(0, 1, 40); obstacle.shape = NavMeshObstacleShape.Box; obstacle.size = new Vector3(5.5f, 4, 11); obstacle.carving = true; obstacle.enabled = false;
            route.bridgePivot = pivot; route.bridgeFloor = floor.GetComponent<Collider>(); route.bridgeBlocker = obstacle;
        }
        private static void Generator()
        {
            var group = new GameObject("Generator station - temporary custom prop").transform; group.SetParent(environment);
            Box("Generator weathered casing", new Vector3(-18, .55f, 25), new Vector3(1.6f, 1.1f, 1), metal, group);
            for (int i = 0; i < 5; i++) Box("Cooling grille", new Vector3(-18.6f + i * .3f, .7f, 24.48f), new Vector3(.09f, .45f, .035f), wood, group);
            var button = Box("Power switch - F", new Vector3(-17.9f, 1.15f, 24.35f), new Vector3(.35f, .25f, .15f), sign, group);
            button.layer = LayerMask.NameToLayer("Interactable"); button.GetComponent<BoxCollider>().isTrigger = true;
            button.AddComponent<VillagePowerSwitch>().route = route; route.generatorTarget = button.transform;
            Sign("NGUỒN ĐIỆN CẦU", new Vector3(-18, 2.1f, 25), 0, 1.4f);
            Lamp(new Vector3(-17, 2.5f, 24), new Color(1, .74f, .35f), true);
            var marker = new GameObject("Future Tanker encounter area"); marker.transform.SetParent(environment); marker.transform.position = new Vector3(-10, 0, 29);
        }
        private static void Clinic()
        {
            Place("Buildings/Cabins/Cabin1", new Vector3(0, 0, 100), 0, .72f);
            Sign("TRẠM Y TẾ BLACKWOOD", new Vector3(0, 3.2f, 94.1f), 0, 2.5f);
            var red = Material("Medical cross red", new Color(.55f, .08f, .065f));
            Box("Medical cross horizontal", new Vector3(0, 4.5f, 94), new Vector3(1.2f, .32f, .1f), red);
            Box("Medical cross vertical", new Vector3(0, 4.5f, 93.99f), new Vector3(.32f, 1.2f, .1f), red);
            var target = new GameObject("Clinic arrival and future survivor marker").transform; target.SetParent(environment); target.position = new Vector3(0, 0, 90); route.clinicTarget = target;
            route.restoredLights = new[] { Lamp(new Vector3(0, 2.2f, 92), new Color(.7f, 1, .7f), false), Lamp(new Vector3(-3, 2.4f, 47), new Color(1, .8f, .4f), false) };
            Station(new Vector3(5, 0, 88)); Station(new Vector3(-7, 0, 8)); Station(new Vector3(8, 0, 52));
            var future = new GameObject("Future extraction route - Level03 not built"); future.transform.SetParent(environment); future.transform.position = new Vector3(24, 0, 104);
        }
        private static void Station(Vector3 position)
        {
            var table = Place("Props/Prop_LargeTable_A", position, 0, .59f); var bounds = Bounds(table);
            foreach (string kind in new[] { "Ammo", "Medkit" })
            {
                var pickup = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Items/" + kind + "Pickup.prefab"), environment);
                pickup.transform.position = new Vector3(position.x + (kind == "Ammo" ? -.35f : .35f), bounds.max.y + .02f, position.z);
            }
        }
        private static void Dressing()
        {
            string[] trees = { "TreeCreator_Tall_A", "TreeCreator_Tall_B", "TreeCreator_Small_B" };
            for (int side = -1; side <= 1; side += 2) for (int row = 0; row < 2; row++) for (int i = 0; i < 19; i++)
            {
                float x = side * (33 + row * 9 + (float)random.NextDouble() * 4), z = -23 + i * 7.5f + (float)random.NextDouble() * 3;
                if (z > 35 && z < 45) continue;
                string path = "Assets/Game/Prefabs/Environment/Tree/" + trees[i % 3] + ".prefab";
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Flooded_Grounds/Prefabs/Nature/Trees/" + trees[i % 3] + ".prefab");
                if (prefab == null) throw new InvalidOperationException("Missing tree " + path);
                var tree = (GameObject)PrefabUtility.InstantiatePrefab(prefab, environment); tree.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, i * 71, 0));
                foreach (var renderer in tree.GetComponentsInChildren<Renderer>())
                {
                    var materials = renderer.sharedMaterials;
                    for (int m = 0; m < materials.Length; m++)
                    {
                        var repaired = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Art/ForestEntryStudy/Materials/" + trees[i % 3] + "_" + m + ".mat");
                        if (repaired != null) materials[m] = repaired;
                    }
                    renderer.sharedMaterials = materials;
                }
                tree.transform.localScale = Vector3.one; var bounds = Bounds(tree); tree.transform.localScale *= (14 + i % 4 * 2) / bounds.size.y; bounds = Bounds(tree);
                tree.transform.position += new Vector3(x - bounds.center.x, (row == 0 ? .15f : 1.5f) - bounds.min.y, z - bounds.center.z);
                var trunk = new GameObject("Trunk collision").AddComponent<CapsuleCollider>(); trunk.transform.SetParent(environment); trunk.transform.position = new Vector3(x, 2, z); trunk.height = 4; trunk.radius = .4f;
            }
            for (int i = 0; i < 60; i++)
            {
                float x = (i % 2 == 0 ? -1 : 1) * (23 + (float)random.NextDouble() * 9), z = -20 + (float)random.NextDouble() * 126;
                if (z > 34 && z < 46) continue;
                Place("Nature/Rocks/CobbleRock_" + (char)('A' + i % 6), new Vector3(x, -.1f, z), i * 47, .15f + i % 4 * .05f);
            }
        }
        private static void Boundaries()
        {
            foreach (var item in new[] { Box("West level boundary", new Vector3(-46, 3, 40), new Vector3(1, 8, 150), wood), Box("East level boundary", new Vector3(46, 3, 40), new Vector3(1, 8, 150), wood), Box("South level boundary", new Vector3(0, 3, -27), new Vector3(94, 8, 1), wood), Box("North level boundary", new Vector3(0, 3, 113), new Vector3(94, 8, 1), wood) }) item.GetComponent<Renderer>().enabled = false;
        }
        private static Light Lamp(Vector3 position, Color color, bool enabled)
        {
            var lamp = new GameObject("Objective lantern").AddComponent<Light>(); lamp.transform.SetParent(environment); lamp.transform.position = position;
            lamp.type = LightType.Point; lamp.range = 8; lamp.intensity = 2; lamp.color = color; lamp.shadows = LightShadows.None; lamp.enabled = enabled; return lamp;
        }
        private static void Sign(string text, Vector3 position, float yaw, float width)
        {
            var label = new GameObject(text).AddComponent<TextMeshPro>(); label.transform.SetParent(environment); label.transform.SetPositionAndRotation(position, Quaternion.Euler(0, yaw, 0));
            label.font = MainMenuTypography.GetFontAsset(); label.text = text; label.fontSize = 3; label.alignment = TextAlignmentOptions.Center; label.rectTransform.sizeDelta = new Vector2(width * 2, 1); label.color = new Color(.88f, .88f, .73f);
        }
        private static void Lighting()
        {
            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Art/ForestEntryStudy/Materials/OvercastCubemap.mat");
            RenderSettings.ambientMode = AmbientMode.Trilight; RenderSettings.ambientSkyColor = new Color(.35f, .42f, .46f); RenderSettings.ambientEquatorColor = new Color(.25f, .3f, .28f); RenderSettings.ambientGroundColor = new Color(.12f, .14f, .12f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 30; RenderSettings.fogEndDistance = 115; RenderSettings.fogColor = new Color(.34f, .41f, .42f);
            var sun = new GameObject("Overcast afternoon light").AddComponent<Light>(); sun.transform.rotation = Quaternion.Euler(38, -35, 0); sun.type = LightType.Directional; sun.intensity = .9f; sun.color = new Color(.79f, .85f, .9f); sun.shadows = LightShadows.Soft;
        }
        private static void Bake()
        {
            var surface = new GameObject("Village Navigation").AddComponent<NavMeshSurface>(); surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = ~LayerMask.GetMask("Player", "Enemy", "Interactable"); surface.BuildNavMesh();
            if (surface.navMeshData == null) throw new InvalidOperationException("Village NavMesh bake failed");
            AssetDatabase.CreateAsset(surface.navMeshData, Output + "/Village_NavMesh.asset");
        }
        private static void Enemies(Health player)
        {
            foreach (var position in new[] { new Vector3(8, 0, 18), new Vector3(-12, 0, 30), new Vector3(7, 0, 57), new Vector3(-8, 0, 76) })
            {
                if (!NavMesh.SamplePosition(position, out var hit, 3, NavMesh.AllAreas)) throw new InvalidOperationException("No Runner floor " + position);
                var runner = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Enemies/Runner.prefab")); runner.transform.SetPositionAndRotation(hit.position, Quaternion.Euler(0, 90, 0));
                var brain = runner.GetComponent<RunnerBrain>(); var data = new SerializedObject(brain); data.FindProperty("target").objectReferenceValue = player;
                var points = data.FindProperty("patrolPoints"); points.arraySize = 2;
                for (int i = 0; i < 2; i++)
                {
                    if (!NavMesh.SamplePosition(position + Vector3.forward * (i == 0 ? -2 : 2), out var point, 3, NavMesh.AllAreas)) throw new InvalidOperationException("No patrol floor");
                    var waypoint = new GameObject("Village Runner waypoint").transform; waypoint.position = point.position; points.GetArrayElementAtIndex(i).objectReferenceValue = waypoint;
                }
                data.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        private static TMP_Text Label(Transform parent, string text, Vector2 size, Vector2 anchor, Vector2 offset, int fontSize)
        {
            var rect = new GameObject(text, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = anchor; rect.sizeDelta = size; rect.anchoredPosition = offset;
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font = MainMenuTypography.GetFontAsset(); label.fontSize = fontSize; label.text = text; label.raycastTarget = false; label.extraPadding = true; return label;
        }
        private static void UI()
        {
            var canvas = new GameObject("Village objective canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)); canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            Label(canvas.transform, "LÀNG BLACKWOOD", new Vector2(600, 32), Vector2.one, new Vector2(-32, -24), 22);
            var background = new GameObject("Objective background", typeof(RectTransform), typeof(Image)); var rect = background.GetComponent<RectTransform>(); rect.SetParent(canvas.transform, false); rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one; rect.sizeDelta = new Vector2(620, 150); rect.anchoredPosition = new Vector2(-24, -20); background.GetComponent<Image>().color = new Color(.03f, .06f, .065f, .9f); rect.SetAsFirstSibling();
            route.objectiveLabel = Label(canvas.transform, "Tìm máy phát điện", new Vector2(580, 44), Vector2.one, new Vector2(-44, -68), 27);
            route.detailLabel = Label(canvas.transform, "", new Vector2(580, 32), Vector2.one, new Vector2(-44, -120), 22);
            route.interactionLabel = Label(canvas.transform, "", new Vector2(800, 58), new Vector2(.5f, 0), new Vector2(0, 96), 28); route.interactionLabel.alignment = TextAlignmentOptions.Center; route.interactionPanel = route.interactionLabel.gameObject;
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule)).GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
        }
        private static void ConnectForest()
        {
            ProjectBackups.PreserveAsset(GameScenes.Forest, "BeforeVillage"); var scene = EditorSceneManager.OpenScene(GameScenes.Forest);
            var completion = Resources.FindObjectsOfTypeAll<ForestMissionHUD>().First(h => h.gameObject.scene == scene).transform.Find("Forest completed/Forest completed panel");
            if (completion.Find("ContinueVillageButton") == null)
            {
                var menu = completion.Find("ForestMenuButton"); var button = UnityEngine.Object.Instantiate(menu.gameObject, completion).GetComponent<Button>(); button.name = "ContinueVillageButton";
                button.GetComponent<RectTransform>().anchoredPosition = new Vector2(440, -316); button.GetComponentInChildren<TMP_Text>().text = "VÀO LÀNG";
                UnityEditor.Events.UnityEventTools.AddPersistentListener(button.onClick, UnityEngine.Object.FindFirstObjectByType<ForestGameFlow>().ContinueToVillage);
            }
            EditorSceneManager.SaveScene(scene);
        }
        public static void Capture()
        {
            EditorSceneManager.OpenScene(GameScenes.Village); Directory.CreateDirectory("Logs/Village");
            var views = new[] { new Vector3(0, 2, -11), new Vector3(9, 4, 28), new Vector3(-15, 2.5f, 20), new Vector3(5, 2.4f, 83), new Vector3(65, 85, -15) };
            var targets = new[] { new Vector3(0, 2, 20), new Vector3(0, 2, 40), new Vector3(-18, 1, 25), new Vector3(0, 2.5f, 96), new Vector3(0, 0, 43) };
            for (int i = 0; i < views.Length; i++)
            {
                var item = new GameObject("Survey camera").AddComponent<Camera>(); item.transform.position = views[i]; item.transform.LookAt(targets[i]); item.fieldOfView = i == 4 ? 65 : 65; item.farClipPlane = 240;
                bool fog = RenderSettings.fog; if (i == 4) RenderSettings.fog = false;
                var texture = new RenderTexture(1280, 720, 24); texture.Create(); item.targetTexture = texture; item.Render(); RenderTexture.active = texture;
                var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false); pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply(); File.WriteAllBytes("Logs/Village/View" + i + ".png", pixels.EncodeToPNG());
                RenderSettings.fog = fog; RenderTexture.active = null; texture.Release(); UnityEngine.Object.DestroyImmediate(texture); UnityEngine.Object.DestroyImmediate(pixels); UnityEngine.Object.DestroyImmediate(item.gameObject);
            }
        }
        public static void Polish()
        {
            ProjectBackups.PreserveAsset(GameScenes.Village, "BeforeVillagePolish");
            var scene = EditorSceneManager.OpenScene(GameScenes.Village); environment = GameObject.Find("Blackwood Village Environment").transform;
            if (GameObject.Find("Canal retaining bank") != null) throw new InvalidOperationException("Polish already applied; use targeted scene edits rather than duplicating decoration.");
            ground = AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Art/ForestEntryStudy/Materials/ForestFloor.mat");
            wood = AssetDatabase.LoadAssetAtPath<Material>(Output + "/Weathered bridge wood.mat");
            // Cabin's entrance faces +Z in the source; present its porch to the village street.
            var clinic = environment.GetComponentsInChildren<Transform>().Where(t => t.name == "Cabin1" && t.position.z > 95).First();
            Vector3 center = Bounds(clinic.gameObject).center; clinic.Rotate(0, 180, 0, Space.World); var after = Bounds(clinic.gameObject); clinic.position += new Vector3(center.x - after.center.x, 0, center.z - after.center.z);
            var water = GameObject.Find("Canal water - hazardous"); var waterMaterial = Material("Village canal water", new Color(.09f, .16f, .15f)); waterMaterial.SetFloat("_Glossiness", .8f); water.GetComponent<Renderer>().sharedMaterial = waterMaterial;
            for (int side = -1; side <= 1; side += 2) Box("Canal retaining bank", new Vector3(0, -.65f, side < 0 ? 35.9f : 44.1f), new Vector3(96, 1.3f, .25f), ground);
            for (int x = -40; x <= 40; x += 4)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Environment/Tree/TreeCreator_Small_B.prefab");
                var tree = (GameObject)PrefabUtility.InstantiatePrefab(prefab, environment); tree.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, x * 31, 0)); tree.transform.localScale = Vector3.one;
                var bounds = Bounds(tree); tree.transform.localScale *= (17 + Mathf.Abs(x) % 6) / bounds.size.y; bounds = Bounds(tree); tree.transform.position += new Vector3(x - bounds.center.x, -bounds.min.y, 111 - bounds.center.z);
            }
            foreach (var z in new[] { 4, 18, 57, 78 }) for (int side = -1; side <= 1; side += 2)
            {
                for (int i = 0; i < 4; i++)
                {
                    float x = side * (7.5f + i * .8f);
                    Place("Nature/Bushes/DecoBush_" + (char)('A' + i % 4), new Vector3(x, 0, z + 5.5f), i * 41, .35f);
                }
                Place("Buildings/Structures1/Struct_Fence1_Mid_B", new Vector3(side * 10, 0, z + 6.8f), 0, .6f);
                Place("Buildings/Structures1/Struct_Pole_A", new Vector3(side * 6, 0, z - 4), 0, .5f);
                Lamp(new Vector3(side * 5.9f, 2.4f, z - 4), new Color(1, .7f, .38f), true).range = 5;
            }
            var navigation = UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>(); route = UnityEngine.Object.FindFirstObjectByType<VillageRoute>();
            route.bridgePivot.localRotation = Quaternion.identity; route.bridgeFloor.enabled = true; route.bridgeBlocker.enabled = false;
            var original = navigation.navMeshData; navigation.RemoveData(); navigation.BuildNavMesh(); var generated = navigation.navMeshData;
            EditorUtility.CopySerialized(generated, original); navigation.RemoveData(); navigation.navMeshData = original; navigation.AddData(); UnityEngine.Object.DestroyImmediate(generated); EditorUtility.SetDirty(original);
            route.bridgePivot.localRotation = Quaternion.Euler(-78, 0, 0); route.bridgeFloor.enabled = false; route.bridgeBlocker.enabled = true;
            EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets(); Capture();
        }
    }
}
