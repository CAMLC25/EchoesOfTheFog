using System;
using System.IO;
using System.Text;
using EchoesOfTheFog.Core;
using EchoesOfTheFog.Data;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EchoesOfTheFog.Editor
{
    // Explicit batch entry point only: never runs automatically on import/domain reload.
    public static class Phase1Validation
    {
        private const string ScenePath = "Assets/Game/Scenes/Tests/AssetValidation.unity";
        private const string PrefabDirectory = "Assets/Game/Prefabs/Environment";
        private const string MaterialDirectory = "Assets/Game/Art/Materials";

        public static void CapturePreview()
        {
            if (!Application.isBatchMode)
                throw new InvalidOperationException("Preview capture is a batch-only operation.");
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var camera = Camera.main;
            if (camera == null) throw new InvalidOperationException("Missing validation camera.");
            var target = new RenderTexture(1280, 720, 24);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            var previousTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                pixels.Apply();
                Directory.CreateDirectory("Logs/Phase1");
                File.WriteAllBytes("Logs/Phase1/asset-validation.png", pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previous;
                target.Release();
                UnityEngine.Object.DestroyImmediate(target);
                UnityEngine.Object.DestroyImmediate(pixels);
            }
        }

        public static void Run()
        {
            if (!Application.isBatchMode)
                throw new InvalidOperationException("Run this setup through Unity batch mode with the Editor closed.");

            var config = GameConfigLoader.LoadDefault();
            var report = new StringBuilder();
            report.AppendLine("Phase 1 validation: " + DateTime.UtcNow.ToString("O"));
            report.AppendLine("Unity: " + Application.unityVersion);
            report.AppendLine("CSV archetypes loaded: " + config.Count);
            foreach (var package in UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages())
                if (package.name == "com.unity.cinemachine" || package.name == "com.unity.ai.navigation" || package.name == "com.unity.splines")
                    report.AppendLine("Package: " + package.name + " " + package.version);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            bool alreadyBuilt = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "Phase1_Validation") alreadyBuilt = true;
                else if (root.name != "Main Camera" && root.name != "Directional Light")
                    throw new InvalidOperationException("Validation scene has unexpected content; refusing to rebuild it.");
            }

            if (!alreadyBuilt) BuildScene(scene);
            report.AppendLine("Scene: " + ScenePath);
            report.AppendLine("Scene renderers: " + UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length);

            int brokenTrees = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Flooded_Grounds/Prefabs/Nature/Trees" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var tree = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var filter = tree.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    brokenTrees++;
                    report.AppendLine("UNRESOLVED source tree mesh: " + path);
                }
            }
            report.AppendLine("Broken original tree prefabs: " + brokenTrees);
            report.AppendLine("ValidationTreePlaceholder is a blockout substitute, not a recovered Flooded Grounds tree.");
            report.AppendLine("Visual inspection and Play Mode collision checks remain manual.");
            Directory.CreateDirectory("Logs/Phase1");
            File.WriteAllText("Logs/Phase1/asset-validation.txt", report.ToString());
            Debug.Log(report.ToString());
        }

        private static void BuildScene(Scene scene)
        {
            Directory.CreateDirectory(PrefabDirectory);
            Directory.CreateDirectory(MaterialDirectory);
            AssetDatabase.Refresh();
            var groundMaterial = Material("ValidationGround", new Color(0.22f, 0.26f, 0.23f));
            var barkMaterial = Material("ValidationBark", new Color(0.25f, 0.17f, 0.11f));
            var leavesMaterial = Material("ValidationLeaves", new Color(0.12f, 0.28f, 0.16f));

            var root = new GameObject("Phase1_Validation");
            Primitive("Ground", PrimitiveType.Cube, root.transform, new Vector3(0, -0.25f, 0), new Vector3(40, 0.5f, 40), groundMaterial);
            var target = Primitive("Health_Test_Target", PrimitiveType.Capsule, root.transform, new Vector3(-3, 1, 0), Vector3.one, barkMaterial);
            target.AddComponent<Health>();
            Primitive("Collision_Wall", PrimitiveType.Cube, root.transform, new Vector3(-7, 1, 0), new Vector3(0.5f, 2, 6), groundMaterial);

            string treePath = PrefabDirectory + "/ValidationTreePlaceholder.prefab";
            var treePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(treePath);
            if (treePrefab == null)
            {
                var tree = new GameObject("ValidationTreePlaceholder");
                Primitive("Trunk", PrimitiveType.Cylinder, tree.transform, new Vector3(0, 1.5f, 0), new Vector3(0.45f, 1.5f, 0.45f), barkMaterial);
                Primitive("Canopy", PrimitiveType.Sphere, tree.transform, new Vector3(0, 3.4f, 0), new Vector3(2.7f, 2.5f, 2.7f), leavesMaterial);
                treePrefab = PrefabUtility.SaveAsPrefabAsset(tree, treePath);
                UnityEngine.Object.DestroyImmediate(tree);
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, scene);
            instance.transform.SetParent(root.transform);
            instance.transform.position = new Vector3(6, 0, 0);

            Sample("Assets/Flooded_Grounds/Prefabs/Nature/Rocks/Rock_A.prefab", new Vector3(5, 0, 7), root.transform);
            Sample("Assets/Flooded_Grounds/Prefabs/Buildings/Bridge/BLD_Bridge_A.prefab", new Vector3(0, 0, 12), root.transform);
            var water = AssetDatabase.LoadAssetAtPath<Material>("Assets/Flooded_Grounds/Content/Materials/BGR_Water.mat");
            if (water == null || water.shader == null) throw new InvalidOperationException("Water sample material/shader is missing.");
            Primitive("Water_Material_Sample", PrimitiveType.Cube, root.transform, new Vector3(-4, 0.5f, -7), new Vector3(5, 0.1f, 5), water);

            var camera = Camera.main;
            if (camera == null) throw new InvalidOperationException("Validation scene needs a Main Camera.");
            camera.transform.position = new Vector3(20, 18, -24);
            camera.transform.LookAt(new Vector3(0, 1, 5));
            camera.farClipPlane = 150f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.4f, 0.45f, 0.47f);
            RenderSettings.fogStartDistance = 30f;
            RenderSettings.fogEndDistance = 100f;
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
                throw new IOException("Could not save validation scene.");
            AssetDatabase.SaveAssets();
        }

        private static Material Material(string name, Color color)
        {
            string path = MaterialDirectory + "/" + name + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            var shader = Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Built-in Standard shader is missing.");
            var material = new Material(shader) { name = name, color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject Primitive(string name, PrimitiveType type, Transform parent, Vector3 position, Vector3 scale, Material material)
        {
            var item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            item.GetComponent<Renderer>().sharedMaterial = material;
            return item;
        }

        private static void Sample(string path, Vector3 position, Transform parent)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new InvalidOperationException("Missing sample prefab: " + path);
            var item = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            item.transform.position = position;
        }
    }
}
