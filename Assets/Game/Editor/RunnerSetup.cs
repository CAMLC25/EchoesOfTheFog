using System;
using System.IO;
using System.Linq;
using System.Text;
using EchoesOfTheFog.Core;
using EchoesOfTheFog.Data;
using EchoesOfTheFog.Enemy;
using EchoesOfTheFog.Player;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace EchoesOfTheFog.Editor
{
    public static class RunnerSetup
    {
        private const string ModelPath = "Assets/Game/Characters/Runner/Runner_Model.fbx";
        private const string AnimationFolder = "Assets/Game/Animations/Runner";
        private const string Generated = AnimationFolder + "/Generated";
        private const string PrefabPath = "Assets/Game/Prefabs/Enemies/Runner.prefab";
        private const string TuningPath = "Assets/Game/Data/Enemies/RunnerTuning.asset";

        public static void Apply()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Close Unity before batch setup.");
            foreach (string folder in new[] { Generated, "Assets/Game/Prefabs/Enemies", "Assets/Game/Data/Enemies", "Assets/Game/Data/Navigation", "Logs/Runner" }) Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            int layer = EnemyLayer();
            ConfigureImports();
            var tuning = AssetDatabase.LoadAssetAtPath<RunnerTuning>(TuningPath);
            if (tuning == null) { tuning = ScriptableObject.CreateInstance<RunnerTuning>(); AssetDatabase.CreateAsset(tuning, TuningPath); }
            var controller = Controller(tuning);
            AssetDatabase.SaveAssets();
            CreatePrefab(layer, tuning, controller);
            AddPlayerNoise();
            CreateTestScene(layer);
            CreateForestScene();
            AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/Runner/setup.txt", "Runner Humanoid / 7 clips / FSM / CSV stats / hearing / two test scenes configured.\n");
        }

        private static int EnemyLayer()
        {
            int existing = LayerMask.NameToLayer("Enemy"); if (existing >= 0) return existing;
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tags.FindProperty("layers");
            for (int i = 9; i < 32; i++)
                if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue))
                { layers.GetArrayElementAtIndex(i).stringValue = "Enemy"; tags.ApplyModifiedPropertiesWithoutUndo(); return i; }
            throw new InvalidOperationException("No free Enemy physics layer.");
        }

        private static void ConfigureImports()
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
            importer.animationType = ModelImporterAnimationType.Human; importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importCameras = false; importer.importLights = false; importer.SaveAndReimport();
            var avatar = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
            if (avatar == null || !avatar.isValid || !avatar.isHuman) throw new InvalidOperationException("Runner Humanoid Avatar is invalid.");
            foreach (string path in Directory.GetFiles("Assets/Game/Characters/Runner/Textures", "*Normal.png"))
            {
                var texture = (TextureImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
                if (texture.textureType != TextureImporterType.NormalMap) { texture.textureType = TextureImporterType.NormalMap; texture.SaveAndReimport(); }
            }
            var report = new StringBuilder();
            foreach (string rawPath in Directory.GetFiles(AnimationFolder, "*.fbx"))
            {
                string path = rawPath.Replace('\\', '/'); string name = Path.GetFileNameWithoutExtension(path).Split('@').Last();
                var clipImporter = (ModelImporter)AssetImporter.GetAtPath(path);
                clipImporter.animationType = ModelImporterAnimationType.Human; clipImporter.avatarSetup = ModelImporterAvatarSetup.CopyFromOther; clipImporter.sourceAvatar = avatar;
                clipImporter.importCameras = false; clipImporter.importLights = false;
                var clips = clipImporter.defaultClipAnimations;
                if (clips.Length != 1) throw new InvalidOperationException("Expected one Runner take in " + path);
                var clip = clips[0]; clip.name = name; clip.loopTime = name.EndsWith("Idle") || name.EndsWith("Walk") || name.EndsWith("Run");
                clip.lockRootRotation = true; clip.keepOriginalOrientation = true;
                clip.lockRootHeightY = true; clip.keepOriginalPositionY = true;
                clip.lockRootPositionXZ = false; clip.keepOriginalPositionXZ = true;
                clipImporter.clipAnimations = clips; clipImporter.SaveAndReimport();
                var imported = Clip(name);
                if (!imported.humanMotion) throw new InvalidOperationException("Runner clip is not Humanoid: " + name);
                report.AppendLine($"{name}: Human; loop={clip.loopTime}; duration={imported.length:F3}; speed={imported.averageSpeed}");
            }
            File.WriteAllText("Logs/Runner/imports.txt", report.ToString());
        }

        private static AnimationClip Clip(string name) => AssetDatabase.LoadAllAssetsAtPath(AnimationFolder + "/Ch10_nonPBR@" + name + ".fbx")
            .OfType<AnimationClip>().First(clip => !clip.name.StartsWith("__preview__"));

        private static AnimatorController Controller(RunnerTuning tuning)
        {
            string path = Generated + "/Runner.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller != null && controller.parameters.Length > 0) return controller;
            if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            if (controller.layers.Length == 0) controller.AddLayer("Base Layer");
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float); controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger); controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("AttackIndex", AnimatorControllerParameterType.Int); controller.AddParameter("AttackPlayback", AnimatorControllerParameterType.Float);
            var machine = controller.layers[0].stateMachine;
            var locomotion = machine.AddState("Locomotion"); machine.defaultState = locomotion;
            var blend = new BlendTree { name = "Runner locomotion", blendType = BlendTreeType.Simple1D, blendParameter = "Speed", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(blend, controller);
            blend.AddChild(Clip("Zombie Idle"), 0); blend.AddChild(Clip("Zombie Walk"), tuning.patrolSpeed); blend.AddChild(Clip("Zombie Run"), GameConfigLoader.LoadDefault()["Runner"].MoveSpeed);
            locomotion.motion = blend;
            var death = machine.AddState("Dead"); death.motion = Clip("Zombie Death");
            var die = machine.AddAnyStateTransition(death); die.hasExitTime = false; die.duration = 0.08f; die.canTransitionToSelf = false; die.AddCondition(AnimatorConditionMode.If, 0, "Dead");
            var hit = machine.AddState("Hit"); hit.motion = Clip("Zombie Reaction Hit"); hit.speed = hit.motion.averageDuration / tuning.hitStun;
            var hurt = machine.AddAnyStateTransition(hit); hurt.hasExitTime = false; hurt.duration = 0.04f; hurt.canTransitionToSelf = false;
            hurt.AddCondition(AnimatorConditionMode.If, 0, "Hit"); hurt.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead"); Exit(hit, locomotion);
            for (int i = 0; i < 2; i++)
            {
                var state = machine.AddState("Attack" + (i + 1)); state.motion = Clip("Zombie Attack " + (i + 1));
                state.speedParameterActive = true; state.speedParameter = "AttackPlayback";
                var enter = machine.AddAnyStateTransition(state); enter.hasExitTime = false; enter.duration = 0.05f; enter.canTransitionToSelf = false;
                enter.AddCondition(AnimatorConditionMode.If, 0, "Attack"); enter.AddCondition(AnimatorConditionMode.Equals, i, "AttackIndex"); enter.AddCondition(AnimatorConditionMode.IfNot, 0, "Dead");
                Exit(state, locomotion);
            }
            foreach (var child in machine.states) EditorUtility.SetDirty(child.state);
            EditorUtility.SetDirty(blend); EditorUtility.SetDirty(machine); EditorUtility.SetDirty(controller); return controller;
        }
        private static void Exit(AnimatorState state, AnimatorState locomotion)
        { var exit = state.AddTransition(locomotion); exit.hasExitTime = true; exit.exitTime = 0.95f; exit.duration = 0.05f; }

        private static void CreatePrefab(int layer, RunnerTuning tuning, AnimatorController controller)
        {
            if (File.Exists(PrefabPath)) return;
            var root = new GameObject("Runner"); root.layer = layer;
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), root.transform);
                model.name = "RunnerVisual"; model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity; model.transform.localScale = Vector3.one;
                var renderers = model.GetComponentsInChildren<SkinnedMeshRenderer>();
                var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                float scale = 1.8f / bounds.size.y;
                model.transform.localScale = Vector3.one * scale;
                model.transform.localPosition = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z) * scale;
                foreach (var item in model.GetComponentsInChildren<Transform>()) item.gameObject.layer = layer;
                foreach (var renderer in renderers)
                    if (renderer.sharedMaterials.Any(material => material == null || material.shader == null || !material.shader.isSupported)) throw new InvalidOperationException("Runner material is missing or unsupported.");
                var animator = model.GetComponent<Animator>(); animator.runtimeAnimatorController = controller; animator.applyRootMotion = false; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var collider = root.AddComponent<CapsuleCollider>(); collider.height = 1.8f; collider.radius = 0.3f; collider.center = Vector3.up * 0.9f;
                var body = root.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false; body.constraints = RigidbodyConstraints.FreezeRotation;
                var health = root.AddComponent<Health>(); SetFloat(health, "maxHealth", GameConfigLoader.LoadDefault()["Runner"].Health);
                var agent = root.AddComponent<NavMeshAgent>(); agent.enabled = false; agent.height = 1.8f; agent.radius = 0.3f; agent.baseOffset = 0; agent.acceleration = 20; agent.angularSpeed = tuning.turnSpeed;
                var perception = root.AddComponent<EnemyPerception>(); Set(perception, "tuning", tuning);
                var serialized = new SerializedObject(perception); serialized.FindProperty("occlusionMask").intValue = ~((1 << layer) | (1 << LayerMask.NameToLayer("Player"))); serialized.ApplyModifiedPropertiesWithoutUndo();
                var brain = root.AddComponent<RunnerBrain>(); Set(brain, "tuning", tuning);
                var driver = root.AddComponent<RunnerAnimationDriver>(); Set(driver, "animator", animator);
                SetFloat(driver, "attackClipLength1", Clip("Zombie Attack 1").length); SetFloat(driver, "attackClipLength2", Clip("Zombie Attack 2").length);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                File.WriteAllText("Logs/Runner/model.txt", $"Raw bounds={bounds}; scale={scale}; target height=1.8m; Avatar valid={animator.avatar.isValid}; Human={animator.isHuman}\n");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void AddPlayerNoise()
        {
            const string path = "Assets/Game/Prefabs/Player/PlayerCapsule.prefab";
            ProjectBackups.PreserveAsset(path, "BeforeRunner");
            var root = PrefabUtility.LoadPrefabContents(path);
            try { if (root.GetComponent<PlayerNoiseEmitter>() == null) root.AddComponent<PlayerNoiseEmitter>(); PrefabUtility.SaveAsPrefabAsset(root, path); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void CreateTestScene(int enemyLayer)
        {
            const string path = "Assets/Game/Scenes/Tests/RunnerTest.unity"; if (File.Exists(path)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var floor = Box("Arena floor", new Vector3(0, -0.25f, 0), new Vector3(40, 0.5f, 48));
            var material = new Material(Shader.Find("Standard")) { name = "RunnerArena", color = new Color(0.25f, 0.29f, 0.26f) };
            string materialPath = "Assets/Game/Data/Enemies/RunnerArena.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (existing == null) { AssetDatabase.CreateAsset(material, materialPath); existing = material; } else UnityEngine.Object.DestroyImmediate(material);
            floor.GetComponent<Renderer>().sharedMaterial = existing;
            Box("Cover wall", new Vector3(3, 1.5f, 0), new Vector3(1, 3, 12));
            Box("Cover crate", new Vector3(-9, 1, 0), new Vector3(4, 2, 2));
            var sun = new GameObject("Directional Light").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.2f; sun.transform.rotation = Quaternion.Euler(45, -35, 0);
            RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.5f);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Player/PlayerCapsule.prefab"));
            player.transform.position = new Vector3(-4, 0.05f, -12); Phase2Setup.CreateOutputCamera(player.transform.position);
            var navigation = new GameObject("Navigation - Runner arena").AddComponent<NavMeshSurface>();
            navigation.useGeometry = NavMeshCollectGeometry.PhysicsColliders; navigation.layerMask = ~((1 << enemyLayer) | (1 << LayerMask.NameToLayer("Player")));
            Physics.SyncTransforms(); navigation.BuildNavMesh();
            string navPath = "Assets/Game/Data/Navigation/RunnerTest_NavMesh.asset";
            var saved = AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
            if (saved == null) AssetDatabase.CreateAsset(navigation.navMeshData, navPath);
            else { EditorUtility.CopySerialized(navigation.navMeshData, saved); navigation.RemoveData(); navigation.navMeshData = saved; navigation.AddData(); }
            var group = new GameObject("Runner encounter").transform;
            Spawn(new Vector3(-4, 0, 9), new[] { new Vector3(-7, 0, 9), new Vector3(-1, 0, 9) }, group, player.GetComponent<Health>());
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void CreateForestScene()
        {
            const string path = "Assets/Game/Scenes/Main/Level01_Forest.unity"; if (File.Exists(path)) return;
            if (!AssetDatabase.CopyAsset("Assets/Game/Scenes/Tests/AssetValidation.unity", path)) throw new IOException("Could not create Runner forest test scene.");
            var scene = EditorSceneManager.OpenScene(path);
            var surface = UnityEngine.Object.FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include, FindObjectsSortMode.None).First();
            if (surface.navMeshData == null) throw new InvalidOperationException("Forest NavMesh data is missing.");
            surface.gameObject.SetActive(true); surface.AddData();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerMotor>();
            var group = new GameObject("Runner encounters").transform;
            foreach (float z in new[] { 6f, 35f })
            {
                float x = RouteX(z);
                Spawn(new Vector3(x + 2, 0.5f, z), new[] { new Vector3(RouteX(z - 3), 0.5f, z - 3), new Vector3(RouteX(z + 3) + 2, 0.5f, z + 3) }, group, player.GetComponent<Health>());
            }
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void Spawn(Vector3 position, Vector3[] waypoints, Transform parent, Health player)
        {
            if (!NavMesh.SamplePosition(position, out var spawn, 3, NavMesh.AllAreas)) throw new InvalidOperationException("No NavMesh at Runner spawn " + position);
            var enemy = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), parent);
            enemy.transform.SetPositionAndRotation(spawn.position, Quaternion.Euler(0, 180, 0));
            var brain = enemy.GetComponent<RunnerBrain>(); Set(brain, "target", player);
            var serialized = new SerializedObject(brain); var points = serialized.FindProperty("patrolPoints"); points.arraySize = waypoints.Length;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (!NavMesh.SamplePosition(waypoints[i], out var point, 3, NavMesh.AllAreas)) throw new InvalidOperationException("No NavMesh at patrol point.");
                var waypoint = new GameObject(enemy.name + " waypoint " + (i + 1)).transform; waypoint.SetParent(parent, false); waypoint.position = point.position;
                points.GetArrayElementAtIndex(i).objectReferenceValue = waypoint;
                var route = new NavMeshPath();
                if (!NavMesh.CalculatePath(spawn.position, point.position, NavMesh.AllAreas, route) || route.status != NavMeshPathStatus.PathComplete)
                    throw new InvalidOperationException("Runner patrol point is unreachable.");
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static float RouteX(float z) => Mathf.Sin((z + 18) * 0.075f) * 3.4f + Mathf.Sin(z * 0.17f) * 0.8f;
        private static GameObject Box(string name, Vector3 position, Vector3 scale)
        { var item = GameObject.CreatePrimitive(PrimitiveType.Cube); item.name = name; item.transform.position = position; item.transform.localScale = scale; return item; }
        private static void Set(UnityEngine.Object target, string property, UnityEngine.Object value)
        { var serialized = new SerializedObject(target); serialized.FindProperty(property).objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetFloat(UnityEngine.Object target, string property, float value)
        { var serialized = new SerializedObject(target); serialized.FindProperty(property).floatValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); }
    }
}
