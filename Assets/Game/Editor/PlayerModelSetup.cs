using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.Cinemachine;
using UnityEngine;

namespace EchoesOfTheFog.Editor
{
    // Explicit batch authoring only. Does not run on import or during gameplay.
    public static class PlayerModelSetup
    {
        private const string ModelPath = "Assets/Game/Characters/Player/Elena_Model.fbx";
        private const string PlayerPath = "Assets/Game/Prefabs/Player/PlayerCapsule.prefab";

        public static void Apply()
        {
            RequireBatch();
            Directory.CreateDirectory("Logs/PlayerModel");
            AssetDatabase.Refresh();
            ProjectBackups.PreserveAsset(PlayerPath, "BeforeElenaSetup");
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            if (importer == null) throw new InvalidOperationException("No model importer for Elena.");
            if (importer.animationType != ModelImporterAnimationType.Human || importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
            }
            foreach (string name in new[] { "Ch11_1001_Normal.png", "Ch11_1002_Normal.png" })
            {
                var texture = (TextureImporter)AssetImporter.GetAtPath("Assets/Game/Characters/Player/Textures/" + name);
                if (texture == null) throw new InvalidOperationException("Missing normal texture: " + name);
                if (texture.textureType != TextureImporterType.NormalMap)
                {
                    texture.textureType = TextureImporterType.NormalMap;
                    texture.SaveAndReimport();
                }
            }

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            var sourceAnimator = source.GetComponentInChildren<Animator>();
            if (sourceAnimator == null || sourceAnimator.avatar == null || !sourceAnimator.avatar.isValid || !sourceAnimator.avatar.isHuman)
                throw new InvalidOperationException("Humanoid mapping failed; capsule prefab has not been changed.");

            var root = PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                var placeholder = root.transform.Find("CapsuleVisual");
                if (placeholder == null) throw new InvalidOperationException("No capsule visual to preserve.");
                var visualRoot = root.transform.Find("ElenaVisual");
                if (visualRoot == null)
                {
                    visualRoot = new GameObject("ElenaVisual").transform;
                    visualRoot.SetParent(root.transform, false);
                    var model = (GameObject)PrefabUtility.InstantiatePrefab(source, visualRoot);
                    model.name = "Elena_Model";
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localRotation = Quaternion.identity;
                    model.transform.localScale = Vector3.one;
                    foreach (var transform in model.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = root.layer;
                    var animator = model.GetComponent<Animator>();
                    animator.applyRootMotion = false;
                    animator.runtimeAnimatorController = null;
                    var bounds = BoundsOf(model);
                    if (bounds.size.y <= 0) throw new InvalidOperationException("Invalid model height.");
                    float scale = 1.75f / bounds.size.y;
                    visualRoot.localScale = Vector3.one * scale;
                    visualRoot.localPosition = new Vector3(-bounds.center.x * scale, -bounds.min.y * scale, -bounds.center.z * scale);
                    File.WriteAllText("Logs/PlayerModel/placement.txt", $"Raw mesh bounds: {bounds}\nUniform visual scale: {scale}\nOffset: {visualRoot.localPosition}\nTarget height: 1.75 m; physics capsule remains 1.8 m.\nNo locomotion animation clips are installed.\n");
                }
                placeholder.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            Inspect();
        }

        public static void Capture()
        {
            RequireBatch();
            Directory.CreateDirectory("Logs/PlayerModel");
            EditorSceneManager.OpenScene("Assets/Game/Scenes/Tests/AssetValidation.unity");
            var camera = Camera.main;
            if (camera == null) throw new InvalidOperationException("No main Player camera.");
            var brain = camera.GetComponent<CinemachineBrain>();
            if (brain != null) brain.ManualUpdate();
            SaveCamera(camera, "Elena_InMap");
            var player = UnityEngine.Object.FindFirstObjectByType<EchoesOfTheFog.Player.PlayerMotor>();
            if (player == null) throw new InvalidOperationException("No Player in map.");
            var studio = new GameObject("Temporary model inspection camera");
            try
            {
                var front = studio.AddComponent<Camera>(); front.cullingMask = 1 << player.gameObject.layer;
                front.clearFlags = CameraClearFlags.SolidColor; front.backgroundColor = new Color(0.17f, 0.19f, 0.2f);
                front.fieldOfView = 36;
                studio.transform.position = player.transform.position + new Vector3(0, 1.1f, 3.3f);
                studio.transform.LookAt(player.transform.position + Vector3.up * 0.9f);
                SaveCamera(front, "Elena_Front");
            }
            finally { UnityEngine.Object.DestroyImmediate(studio); }
        }

        private static void SaveCamera(Camera camera, string name)
        {
            var target = new RenderTexture(1280, 720, 24);
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previous = RenderTexture.active; var previousTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                File.WriteAllBytes("Logs/PlayerModel/" + name + ".png", pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget; RenderTexture.active = previous;
                target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels);
            }
        }

        public static void Inspect()
        {
            RequireBatch();
            Directory.CreateDirectory("Logs/PlayerModel");
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null) throw new InvalidOperationException("Elena_Model.fbx is missing.");
            var instance = UnityEngine.Object.Instantiate(model);
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            instance.transform.localScale = Vector3.one;
            try
            {
                var report = new StringBuilder();
                report.AppendLine("Model: " + ModelPath);
                report.AppendLine("Bounds: " + BoundsOf(instance));
                var animator = instance.GetComponentInChildren<Animator>();
                report.AppendLine("Animator: " + (animator != null));
                report.AppendLine("Avatar valid/human: " + (animator != null && animator.avatar != null ? animator.avatar.isValid + "/" + animator.avatar.isHuman : "none"));
                foreach (var renderer in instance.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    report.AppendLine($"Skin: {renderer.name}, bones={renderer.bones.Length}, vertices={renderer.sharedMesh?.vertexCount}");
                    foreach (var material in renderer.sharedMaterials) report.AppendLine("Material: " + (material != null ? material.name + " / " + material.shader.name : "MISSING"));
                }
                foreach (var transform in instance.GetComponentsInChildren<Transform>())
                    if (transform.name.Contains("Hips") || transform.name.Contains("Head") || transform.name.Contains("Foot")) report.AppendLine("Bone: " + transform.name);
                foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<AnimationClip>())
                    if (!clip.name.StartsWith("__preview__")) report.AppendLine($"Embedded clip: {clip.name}, length={clip.length}, curves={AnimationUtility.GetCurveBindings(clip).Length}");
                File.WriteAllText("Logs/PlayerModel/inspection.txt", report.ToString());
                Debug.Log(report.ToString());
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        private static Bounds BoundsOf(GameObject item)
        {
            var renderers = item.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("No skinned character renderer.");
            // Use deformed vertices, not the often padded renderer bounds, to put feet on the floor.
            var bounds = new Bounds();
            bool initialized = false;
            foreach (var renderer in renderers)
            {
                var mesh = new Mesh();
                try
                {
                    renderer.BakeMesh(mesh);
                    foreach (var vertex in mesh.vertices)
                    {
                        Vector3 point = item.transform.InverseTransformPoint(renderer.transform.TransformPoint(vertex));
                        if (!initialized) { bounds = new Bounds(point, Vector3.zero); initialized = true; }
                        else bounds.Encapsulate(point);
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(mesh); }
            }
            if (!initialized) throw new InvalidOperationException("No model vertices to place.");
            return bounds;
        }

        private static void RequireBatch()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Run authoring in batch mode with Unity closed.");
        }
    }
}
