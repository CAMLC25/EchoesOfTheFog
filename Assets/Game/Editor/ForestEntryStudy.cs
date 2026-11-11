using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine.AI;

namespace EchoesOfTheFog.Editor
{
    // Explicit editor-only authoring for the approved AssetValidation visual slice.
    // Never executes during import, Play Mode or a player build.
    public static class ForestEntryStudy
    {
        private const string Source = "Assets/Flooded_Grounds/";
        private const string Output = "Assets/Game/Art/ForestEntryStudy";
        private const string ScenePath = "Assets/Game/Scenes/AssetValidation.unity";
        private static readonly Dictionary<string, GameObject> Prefabs = new Dictionary<string, GameObject>();
        private static Transform environment;
        private static System.Random dressing;

        public static void Build()
        {
            RequireBatch();
            EnsureFolders();
            ProjectBackups.PreserveAsset(ScenePath, "BeforeForestStudy");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "AssetValidation";
            Prefabs.Clear();
            dressing = new System.Random(15011);
            environment = new GameObject("Forest_To_Blackwood_VisualStudy").transform;
            BuildGround();
            BuildCrossing();
            BuildVillage();
            BuildForest();
            BuildLighting();
            BuildCameras();
            BakeNavigation();
            if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Could not save visual study.");
            AssetDatabase.SaveAssets();
            ValidateScene();
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(Output + "/Materials");
            Directory.CreateDirectory(Output + "/Meshes");
            Directory.CreateDirectory("Logs/ForestEntryStudy");
            AssetDatabase.Refresh();
        }

        private static float RouteX(float z) => Mathf.Sin((z + 18f) * 0.075f) * 3.4f + Mathf.Sin(z * 0.17f) * 0.8f;
        private static float CreekZ(float x) => 14f + Mathf.Sin(x * 0.1f) * 1.1f;
        private static float BaseHeight(float z) => 0.06f * Mathf.Sin(z * 0.12f);
        private static float Height(float x, float z)
        {
            float side = Mathf.Abs(x - RouteX(z));
            float berm = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(4, 16, side));
            float noise = Mathf.PerlinNoise((x + 90) * 0.052f, (z + 40) * 0.052f);
            float ground = BaseHeight(z) + berm * (0.2f + noise * 1.7f);
            ground += (Mathf.PerlinNoise((x + 130) * 0.35f, (z + 90) * 0.35f) - 0.5f) * 0.08f;
            float creek = Mathf.Exp(-Mathf.Pow((z - CreekZ(x)) / 1.9f, 2));
            return ground - creek * 1.05f;
        }

        private static Transform Group(string name)
        {
            var group = new GameObject(name).transform;
            group.SetParent(environment, false);
            return group;
        }

        private static void BuildGround()
        {
            var material = NewMaterial("ForestFloor", Shader.Find("EchoesOfTheFog/Forest Ground"));
            material.SetTexture("_Moss", Texture("GR_Moss1_AS.tif"));
            material.SetTexture("_Dirt", Texture("GR_Dirt1_AS.tif"));
            material.SetTexture("_MossNormal", Texture("GR_Moss1_N.tif"));
            material.SetTexture("_DirtNormal", Texture("GR_Dirt1_N.tif"));
            material.SetColor("_Color", new Color(0.85f, 0.86f, 0.78f));
            const int width = 120, length = 144;
            var vertices = new Vector3[(width + 1) * (length + 1)];
            var colors = new Color[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[width * length * 6];
            for (int z = 0; z <= length; z++)
            for (int x = 0; x <= width; x++)
            {
                int i = z * (width + 1) + x;
                float wx = x - 60, wz = z - 44;
                vertices[i] = new Vector3(wx, Height(wx, wz), wz);
                uv[i] = new Vector2(wx / 3, wz / 3);
                float dist = Mathf.Abs(wx - RouteX(wz));
                float halfWidth = 2.05f + 0.3f * Mathf.Sin(wz * 0.3f);
                float edge = Mathf.PerlinNoise(wx * 0.7f + 100, wz * 0.5f + 100) * 0.45f;
                float dirt = 1f - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(halfWidth - 0.4f, halfWidth + 1f + edge, dist));
                float wetBank = 1 - Mathf.Clamp01(Mathf.Abs(wz - CreekZ(wx)) / 4f);
                colors[i] = new Color(Mathf.Max(dirt, wetBank * 0.85f), 0, 0, 1);
            }
            int t = 0;
            for (int z = 0; z < length; z++)
            for (int x = 0; x < width; x++)
            {
                int i = z * (width + 1) + x;
                triangles[t++] = i; triangles[t++] = i + width + 1; triangles[t++] = i + 1;
                triangles[t++] = i + 1; triangles[t++] = i + width + 1; triangles[t++] = i + width + 2;
            }
            var mesh = SaveMesh("ForestFloor", vertices, triangles, uv, colors);
            var floor = MeshObject("Forest floor - sculpted banks and earth trail", Group("01_Ground_And_Water"), mesh, material);
            floor.AddComponent<MeshCollider>().sharedMesh = mesh;

            // A winding water strip sits inside the carved channel, not on top of the trail.
            var water = CloneMaterial("CreekWater", Source + "Content/Materials/BGR_Water.mat");
            water.shader = Shader.Find("Standard");
            water.SetColor("_Color", new Color(0.15f, 0.21f, 0.18f));
            water.SetTexture("_MainTex", null);
            water.SetFloat("_Metallic", 0.18f);
            water.SetFloat("_Glossiness", 0.86f);
            water.EnableKeyword("_NORMALMAP");
            var v = new Vector3[222]; var u = new Vector2[v.Length]; var tr = new int[110 * 6];
            for (int i = 0; i <= 110; i++)
            {
                float x = i - 55;
                v[i * 2] = new Vector3(x, 0, CreekZ(x) - 1.15f);
                v[i * 2 + 1] = new Vector3(x, 0, CreekZ(x) + 1.15f);
                u[i * 2] = new Vector2(i * 0.15f, 0); u[i * 2 + 1] = new Vector2(i * 0.15f, 1);
                if (i == 110) continue;
                int a = i * 2, b = i * 6;
                tr[b] = a; tr[b + 1] = a + 1; tr[b + 2] = a + 2;
                tr[b + 3] = a + 2; tr[b + 4] = a + 1; tr[b + 5] = a + 3;
            }
            var waterObject = MeshObject("Shallow runoff creek", floor.transform.parent, SaveMesh("Creek", v, tr, u), water);
            waterObject.transform.position = new Vector3(0, -0.32f, 0);
        }

        private static void BuildCrossing()
        {
            Transform group = Group("02_Timber_Crossing");
            var deck = Place("Buildings/Structures1/Struct_WoodPath_A", new Vector3(RouteX(14), 0.22f, 14), 90, 1, group);
            var bounds = BoundsOf(deck);
            var collision = new GameObject("Timber deck collider");
            collision.transform.SetParent(group, false);
            collision.transform.position = bounds.center;
            collision.AddComponent<BoxCollider>().size = new Vector3(bounds.size.x, 0.18f, bounds.size.z);
            // Edge posts use actual fence columns; the walking corridor remains clear.
            foreach (float z in new[] { 10.6f, 17.6f })
            foreach (float side in new[] { -1f, 1f })
                Place("Buildings/Structures1/Struct_Fence1_Column_A", new Vector3(RouteX(14) + side * 2.35f, 0.03f, z), 0, 0.6f, group);
            Lamp(new Vector3(RouteX(9) - 3.2f, Height(RouteX(9) - 3.2f, 9), 9), group, "Crossing lantern", 0.75f);
        }

        private static void BuildVillage()
        {
            Transform group = Group("03_Blackwood_Entrance");
            float gateX = RouteX(49);
            Place("Buildings/Structures1/Struct_Fence1_Gate_A", new Vector3(gateX, BaseHeight(49), 49), 0, 0.62f, group);
            // Fence modules are centred by their renderer bounds; their source pivots are on an end.
            for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 4; i++)
                Place("Buildings/Structures1/Struct_Fence1_Mid_A", new Vector3(gateX + side * (5.15f + i * 3.96f), 0.02f, 49 + i * 0.25f), side * 3, 0.62f, group);

            Place("Buildings/Cabins/Cabin1", new Vector3(gateX - 9.2f, BaseHeight(60), 60), 15, 0.62f, group);
            Place("Buildings/Cabins/Cabin1_Stairs", new Vector3(gateX - 7.6f, BaseHeight(55), 54.8f), 15, 0.62f, group);
            Place("Buildings/Cabins/Cabin1_DM", new Vector3(gateX + 10.8f, BaseHeight(67), 67), -22, 0.62f, group);
            Place("Buildings/Cabins/Cabin1", new Vector3(gateX - 15f, BaseHeight(79), 79), 32, 0.6f, group);
            Lamp(new Vector3(gateX - 3.5f, BaseHeight(48), 47.5f), group, "Gate beacon", 1.45f);
            Lamp(new Vector3(gateX + 3.5f, BaseHeight(48), 47.5f), group, "Gate companion lantern", 0.8f);

            var doorGlow = new GameObject("Cabin occupied doorway light");
            doorGlow.transform.SetParent(group, false);
            doorGlow.transform.position = new Vector3(gateX - 7.2f, 1.8f, 55.8f);
            var light = doorGlow.AddComponent<Light>(); light.type = LightType.Point;
            light.color = new Color(1, 0.62f, 0.28f); light.intensity = 1.6f; light.range = 7;
        }

        private static void BuildForest()
        {
            Transform trees = Group("04_Trees_Framing_The_Trail");
            Transform undergrowth = Group("05_Undergrowth_And_Rocks");
            string[] treeNames = { "TreeCreator_Tall_A", "TreeCreator_Tall_B", "TreeCreator_Tall_C", "TreeCreator_Crinkly_A", "TreeCreator_Small_A", "TreeCreator_Small_B" };
            // Authored rhythm: offset rows with openings at the crossing and village reveal.
            for (int row = 0; row < 4; row++)
            for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < 17; i++)
            {
                float z = -32 + i * 7.1f + Range(-4.0f, 4.0f) + row * 1.3f;
                float x = RouteX(z) + side * (7 + row * 8.5f + Range(-2.8f, 2.8f));
                if (row == 0 && z > 36 && z < 58) continue;
                if (Mathf.Abs(z - CreekZ(x)) < 2.6f) continue;
                if (z > 52 && Mathf.Abs(x) < 22) continue;
                string type = treeNames[(i + row * 3 + (side > 0 ? 2 : 0)) % treeNames.Length];
                var prefab = Prefab("Nature/Trees/" + type);
                float sourceHeight = BoundsOfNormalized(prefab).size.y;
                float targetHeight = row == 0 ? Range(15, 23) : Range(19, 30);
                if (type.Contains("Small")) targetHeight *= 0.72f;
                var tree = Place("Nature/Trees/" + type, new Vector3(x, Height(x, z) - 0.04f, z), Range(0, 360), targetHeight / sourceHeight, trees);
                TintVegetation(tree);
                var b = BoundsOf(tree);
                var trunk = tree.AddComponent<CapsuleCollider>();
                trunk.height = b.size.y * 0.25f / tree.transform.localScale.y;
                trunk.radius = 0.28f / tree.transform.localScale.x;
                trunk.center = new Vector3(0, trunk.height * 0.5f, 0);
            }

            // A back treeline closes the horizon beyond the cabins, outside the reviewed route.
            for (int i = 0; i < 13; i++)
            {
                float x = -27 + i * 4.6f, z = 87 + Range(-4, 5);
                var tree = Place("Nature/Trees/TreeCreator_Crinkly_A", new Vector3(x, Height(x, z), z), Range(0, 360), Range(0.8f, 1.1f), trees);
                TintVegetation(tree);
            }

            // Rock accents mark turns and banks; the centre of the path stays open.
            var accents = new[] { new Vector3(-6, 0, -13), new Vector3(7.8f, 0, -3), new Vector3(-5.2f, 0, 7), new Vector3(6.7f, 0, 19), new Vector3(-5.4f, 0, 30), new Vector3(8.1f, 0, 39) };
            for (int i = 0; i < accents.Length; i++)
            {
                Vector3 p = accents[i]; p.x += RouteX(p.z); p.y = Height(p.x, p.z) - 0.2f;
                Place("Nature/Rocks/CobbleRock_" + (char)('A' + i), p, i * 63, 0.8f + i % 3 * 0.2f, undergrowth);
            }

            for (int i = 0; i < 85; i++)
            {
                float z = Range(-27, 76), side = i % 2 == 0 ? -1 : 1;
                float x = RouteX(z) + side * Range(4.3f, 17);
                if (Mathf.Abs(z - CreekZ(x)) < 2 || (z > 45 && Mathf.Abs(x) < 13)) continue;
                var bush = Place("Nature/Trees/TreeCreator_Bush_A", new Vector3(x, Height(x, z) - 0.08f, z), Range(0, 360), Range(0.38f, 0.65f), undergrowth);
                TintVegetation(bush);
            }
            for (int i = 0; i < 350; i++)
            {
                float z = Range(-25, 74), side = i % 2 == 0 ? -1 : 1;
                float x = RouteX(z) + side * Range(2.9f, 8.5f);
                if (Mathf.Abs(z - CreekZ(x)) < 1.7f || (z > 46 && Mathf.Abs(x) < 5)) continue;
                Place("Nature/Grass/Grass_Small_" + (char)('A' + i % 4), new Vector3(x, Height(x, z) - 0.02f, z), Range(0, 360), Range(0.5f, 0.95f), undergrowth);
                if (i % 5 == 0)
                    Place("Nature/Rocks/CobbleRock_" + (char)('A' + i % 6), new Vector3(x + side, Height(x + side, z) - 0.12f, z), Range(0, 360), Range(0.16f, 0.4f), undergrowth);
            }
        }

        private static void BuildLighting()
        {
            Transform group = Group("06_Lighting");
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.39f, 0.46f, 0.51f);
            RenderSettings.ambientEquatorColor = new Color(0.31f, 0.35f, 0.32f);
            RenderSettings.ambientGroundColor = new Color(0.15f, 0.17f, 0.13f);
            RenderSettings.fog = true; RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.36f, 0.43f, 0.44f);
            RenderSettings.fogStartDistance = 32; RenderSettings.fogEndDistance = 104;
            var sky = CloneMaterial("OvercastCubemap", Source + "Content/Materials/BGR_Sky1.mat");
            sky.SetColor("_Tint", new Color(0.48f, 0.52f, 0.55f));
            sky.SetFloat("_Exposure", 0.65f); sky.SetFloat("_RotSpeed", 0);
            RenderSettings.skybox = sky;
            var sun = new GameObject("Overcast late afternoon sun"); sun.transform.SetParent(group, false);
            sun.transform.rotation = Quaternion.Euler(34, -38, 0);
            var light = sun.AddComponent<Light>(); light.type = LightType.Directional;
            light.color = new Color(0.94f, 0.94f, 0.87f); light.intensity = 0.8f;
            light.shadows = LightShadows.Soft; light.shadowStrength = 0.72f; light.shadowBias = 0.025f;
            RenderSettings.sun = light;
            var wind = new GameObject("Soft forest wind"); wind.transform.SetParent(group, false);
            wind.transform.rotation = Quaternion.Euler(0, 30, 0);
            var zone = wind.AddComponent<WindZone>(); zone.windMain = 0.18f; zone.windTurbulence = 0.12f;
        }

        private static void BuildCameras()
        {
            Transform group = Group("07_Preview_Cameras_And_Markers");
            CameraAt("01_ForestEntry", new Vector3(RouteX(-16) + 0.6f, 2.1f, -20), new Vector3(RouteX(5), 1.7f, 5), group, true);
            CameraAt("02_TimberCrossing", new Vector3(RouteX(6) + 0.5f, 2.05f, 5), new Vector3(RouteX(28), 1.8f, 28), group);
            CameraAt("03_VillageReveal", new Vector3(RouteX(32) + 0.6f, 2.1f, 30), new Vector3(RouteX(49), 2.2f, 53), group);
            CameraAt("04_LayoutOverview", new Vector3(48, 54, -28), new Vector3(0, 0, 23), group);
            foreach (var pair in new[] { new KeyValuePair<string, float>("PlayerStart", -16), new KeyValuePair<string, float>("Crossing", 14), new KeyValuePair<string, float>("VillageEntrance", 49) })
            {
                var marker = new GameObject(pair.Key); marker.transform.SetParent(group, false);
                marker.transform.position = new Vector3(RouteX(pair.Value), BaseHeight(pair.Value) + 0.6f, pair.Value);
            }
        }

        private static Camera CameraAt(string name, Vector3 position, Vector3 lookAt, Transform parent, bool main = false)
        {
            var item = new GameObject(name); item.transform.SetParent(parent, false);
            item.transform.position = position; item.transform.LookAt(lookAt);
            var camera = item.AddComponent<Camera>(); camera.fieldOfView = 58; camera.nearClipPlane = 0.1f; camera.farClipPlane = 180;
            camera.allowHDR = true; camera.allowMSAA = true; camera.enabled = main;
            if (main) { item.tag = "MainCamera"; item.AddComponent<AudioListener>(); }
            return camera;
        }

        private static void BakeNavigation()
        {
            var item = new GameObject("Navigation - walkable visual slice"); item.transform.SetParent(environment, false);
            var surface = item.AddComponent<NavMeshSurface>();
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.collectObjects = CollectObjects.All;
            surface.BuildNavMesh();
            if (surface.navMeshData == null) throw new InvalidOperationException("Navigation bake failed.");
            string path = Output + "/ForestEntry_NavMesh.asset";
            var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
            if (existing == null) AssetDatabase.CreateAsset(surface.navMeshData, path);
            else { EditorUtility.CopySerialized(surface.navMeshData, existing); surface.RemoveData(); surface.navMeshData = existing; surface.AddData(); EditorUtility.SetDirty(existing); }
        }

        public static void ValidateScene()
        {
            RequireBatch();
            if (SceneManager.GetActiveScene().path != ScenePath) EditorSceneManager.OpenScene(ScenePath);
            var report = new StringBuilder(); int errors = 0;
            report.AppendLine("AssetValidation forest-entry visual study");
            foreach (var renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh == null) { errors++; report.AppendLine("Missing mesh: " + renderer.name); }
                foreach (var material in renderer.sharedMaterials)
                    if (material == null || material.shader == null || material.shader.name == "Hidden/InternalErrorShader")
                    { errors++; report.AppendLine("Missing/error material: " + renderer.name); }
            }
            var navPath = new NavMeshPath();
            bool starts = NavMesh.SamplePosition(new Vector3(RouteX(-16), 0.2f, -16), out var start, 2, NavMesh.AllAreas);
            bool ends = NavMesh.SamplePosition(new Vector3(RouteX(53), 0.2f, 53), out var end, 2, NavMesh.AllAreas);
            bool reachable = starts && ends && NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, navPath) && navPath.status == NavMeshPathStatus.PathComplete;
            report.AppendLine("Start-to-village complete NavMesh path: " + reachable);
            if (!reachable) errors++;
            int groundHits = 0;
            Physics.SyncTransforms();
            for (int z = -16; z <= 53; z++)
                if (Physics.Raycast(new Vector3(RouteX(z), 5, z), Vector3.down, out var hit, 8) && hit.normal.y > 0.6f) groundHits++;
            report.AppendLine("Route floor raycasts: " + groundHits + "/70");
            if (groundHits != 70) errors++;
            report.AppendLine("Errors: " + errors);
            report.AppendLine("Visual cameras are static previews, not a third-person controller.");
            File.WriteAllText("Logs/ForestEntryStudy/validation.txt", report.ToString());
            Debug.Log(report.ToString());
            if (errors > 0) throw new InvalidOperationException("Visual study validation failed; see validation.txt.");
        }

        public static void Capture()
        {
            RequireBatch();
            EditorSceneManager.OpenScene(ScenePath);
            foreach (var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                var target = new RenderTexture(1600, 900, 24);
                var pixels = new Texture2D(1600, 900, TextureFormat.RGB24, false);
                var previous = RenderTexture.active; var previousTarget = camera.targetTexture;
                bool previousFog = RenderSettings.fog;
                try
                {
                    if (camera.name == "04_LayoutOverview") RenderSettings.fog = false;
                    camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                    pixels.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0); pixels.Apply();
                    File.WriteAllBytes("Logs/ForestEntryStudy/" + camera.name + ".png", pixels.EncodeToPNG());
                }
                finally
                {
                    camera.targetTexture = previousTarget; RenderTexture.active = previous; RenderSettings.fog = previousFog;
                    target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels);
                }
            }
            ValidateScene();
        }

        private static GameObject Prefab(string relative)
        {
            if (!Prefabs.TryGetValue(relative, out var prefab))
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Source + "Prefabs/" + relative + ".prefab");
                if (prefab == null) throw new InvalidOperationException("Missing Flooded Grounds prefab: " + relative);
                Prefabs.Add(relative, prefab);
            }
            return prefab;
        }

        private static Bounds BoundsOfNormalized(GameObject prefab)
        {
            var item = UnityEngine.Object.Instantiate(prefab);
            item.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); item.transform.localScale = Vector3.one;
            var bounds = BoundsOf(item); UnityEngine.Object.DestroyImmediate(item); return bounds;
        }

        private static GameObject Place(string relative, Vector3 ground, float yaw, float scale, Transform parent)
        {
            var item = (GameObject)PrefabUtility.InstantiatePrefab(Prefab(relative), parent);
            item.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, yaw, 0));
            item.transform.localScale = Vector3.one * scale;
            var bounds = BoundsOf(item);
            item.transform.position += new Vector3(ground.x - bounds.center.x, ground.y - bounds.min.y, ground.z - bounds.center.z);
            return item;
        }

        private static Material NewMaterial(string name, Shader shader)
        {
            if (shader == null) throw new InvalidOperationException("Missing shader for " + name);
            string path = Output + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(shader) { name = name }; AssetDatabase.CreateAsset(material, path); }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material CloneMaterial(string name, string source)
        {
            var original = AssetDatabase.LoadAssetAtPath<Material>(source);
            if (original == null) throw new InvalidOperationException("Missing material " + source);
            return CloneMaterial(name, original);
        }

        private static Material CloneMaterial(string name, Material original)
        {
            string path = Output + "/Materials/" + name + ".mat";
            var result = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (result != null) { EditorUtility.SetDirty(result); return result; }
            result = new Material(original) { name = name }; AssetDatabase.CreateAsset(result, path); return result;
        }

        private static void TintVegetation(GameObject item)
        {
            foreach (var renderer in item.GetComponentsInChildren<Renderer>())
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (source == null) throw new InvalidOperationException("Unrepaired tree/bush material: " + item.name);
                    string key = item.name.Replace("(Clone)", "") + "_" + i;
                    var clone = CloneMaterial(key, source);
                    if (clone.HasProperty("_Color")) clone.SetColor("_Color", source.name.Contains("Leaf") ? new Color(0.76f, 0.79f, 0.66f) : new Color(0.83f, 0.84f, 0.77f));
                    EditorUtility.SetDirty(clone); materials[i] = clone;
                }
                renderer.sharedMaterials = materials;
            }
        }

        private static Texture2D Texture(string name)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Source + "Content/Textures/" + name);
            if (texture == null) throw new InvalidOperationException("Missing texture: " + name);
            return texture;
        }

        private static Mesh SaveMesh(string name, Vector3[] vertices, int[] triangles, Vector2[] uv, Color[] colors = null)
        {
            var mesh = new Mesh { name = name, vertices = vertices, triangles = triangles, uv = uv };
            if (colors != null) mesh.colors = colors;
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            string path = Output + "/Meshes/" + name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) AssetDatabase.CreateAsset(mesh, path);
            else { EditorUtility.CopySerialized(mesh, existing); UnityEngine.Object.DestroyImmediate(mesh); EditorUtility.SetDirty(existing); mesh = existing; }
            return mesh;
        }

        private static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material material)
        {
            var item = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)); item.transform.SetParent(parent, false);
            item.GetComponent<MeshFilter>().sharedMesh = mesh; item.GetComponent<MeshRenderer>().sharedMaterial = material;
            return item;
        }

        private static void Lamp(Vector3 position, Transform parent, string name, float intensity)
        {
            var postMaterial = NewMaterial("LampPostWeatheredIron", Shader.Find("Standard")); postMaterial.color = new Color(0.13f, 0.15f, 0.13f);
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder); post.name = name + " post"; post.transform.SetParent(parent, false);
            post.transform.position = position + Vector3.up * 1.45f; post.transform.localScale = new Vector3(0.1f, 1.45f, 0.1f);
            post.GetComponent<Renderer>().sharedMaterial = postMaterial;
            var lamp = Place("Props/Prop_Lamp_B", position + new Vector3(0.22f, 2.05f, 0), 0, 0.45f, parent);
            lamp.name = name;
            var glowMaterial = NewMaterial("LanternGlass", Shader.Find("Standard")); glowMaterial.color = new Color(0.8f, 0.46f, 0.12f);
            glowMaterial.EnableKeyword("_EMISSION"); glowMaterial.SetColor("_EmissionColor", new Color(1.8f, 0.85f, 0.21f));
            var glow = GameObject.CreatePrimitive(PrimitiveType.Sphere); glow.name = name + " flame"; glow.transform.SetParent(lamp.transform, true);
            glow.transform.position = position + new Vector3(0.22f, 2.45f, -0.04f); glow.transform.localScale = Vector3.one * 0.26f;
            UnityEngine.Object.DestroyImmediate(glow.GetComponent<Collider>());
            glow.GetComponent<Renderer>().sharedMaterial = glowMaterial;
            var light = glow.AddComponent<Light>(); light.type = LightType.Point; light.color = new Color(1, 0.62f, 0.27f);
            light.intensity = intensity * 2.2f; light.range = 7; light.shadows = LightShadows.Soft;
        }

        private static float Range(float minimum, float maximum) => minimum + (float)dressing.NextDouble() * (maximum - minimum);

        public static void Inspect()
        {
            RequireBatch();
            var report = new StringBuilder();
            foreach (string folder in new[] { "Nature/Trees", "Nature/Bushes", "Nature/Grass", "Nature/Rocks", "Buildings/Cabins", "Buildings/Structures1", "Props" })
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Source + "Prefabs/" + folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (folder == "Props" && !path.Contains("Lamp") && !path.Contains("Boat")) continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var instance = UnityEngine.Object.Instantiate(prefab);
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                instance.transform.localScale = Vector3.one;
                Bounds bounds = BoundsOf(instance);
                report.AppendLine(path + " | size=" + bounds.size.ToString("F2") + " center=" + bounds.center.ToString("F2"));
                foreach (var filter in instance.GetComponentsInChildren<MeshFilter>())
                    if (filter.sharedMesh == null) report.AppendLine("  INVALID MESH " + filter.name);
                UnityEngine.Object.DestroyImmediate(instance);
            }
            Directory.CreateDirectory("Logs/ForestEntryStudy");
            File.WriteAllText("Logs/ForestEntryStudy/asset-sizes.txt", report.ToString());
        }

        private static void RequireBatch()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Close the Editor and invoke this operation in batch mode.");
        }

        private static Bounds BoundsOf(GameObject item)
        {
            var renderers = item.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("No renderer: " + item.name);
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }
    }
}
