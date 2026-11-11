using System;
using System.IO;
using System.Linq;
using EchoesOfTheFog.Core;
using EchoesOfTheFog.Player;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace EchoesOfTheFog.Editor
{
    public static class Phase2Setup
    {
        public const string PlayerPath = "Assets/Game/Prefabs/Player/PlayerCapsule.prefab";
        private const string MapPath = "Assets/Game/Scenes/AssetValidation.unity";
        private const string InputPath = "Assets/InputSystem_Actions.inputactions";
        private const string DataPath = "Assets/Game/Data/Player";

        public static void Run()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Run setup in batch mode with the Editor closed.");
            Directory.CreateDirectory("Assets/Game/Prefabs/Player");
            Directory.CreateDirectory("Logs/Phase2");
            Directory.CreateDirectory(DataPath);
            AssetDatabase.Refresh();
            ProjectBackups.PreserveAsset(MapPath, "BeforePhase2Setup");
            ConfigureInput();
            int layer = EnsurePlayerLayer();
            BuildPrefab(layer);
            ConfigureMap();
            CreatePlayerTestScene();
            EditorSceneManager.OpenScene(MapPath);
            AssetDatabase.SaveAssets();
            Validate();
        }

        private static void ConfigureInput()
        {
            var asset = InputActionAsset.FromJson(File.ReadAllText(InputPath));
            try
            {
                var map = asset.FindActionMap("Player", true);
                if (map.FindAction("Aim") == null)
                {
                    var aim = map.AddAction("Aim", InputActionType.Button);
                    aim.AddBinding("<Mouse>/rightButton").WithGroup("Keyboard&Mouse");
                    aim.AddBinding("<Gamepad>/leftTrigger").WithGroup("Gamepad");
                }
                if (map.FindAction("ReleaseCursor") == null)
                    map.AddAction("ReleaseCursor", InputActionType.Button, "<Keyboard>/escape");
                var interact = map.FindAction("Interact", true);
                for (int i = 0; i < interact.bindings.Count; i++)
                    if (interact.bindings[i].path == "<Keyboard>/e") interact.ChangeBinding(i).WithPath("<Keyboard>/f");
                File.WriteAllText(InputPath, asset.ToJson());
            }
            finally { UnityEngine.Object.DestroyImmediate(asset); }
            AssetDatabase.ImportAsset(InputPath, ImportAssetOptions.ForceSynchronousImport);
        }

        private static int EnsurePlayerLayer()
        {
            int existing = LayerMask.NameToLayer("Player");
            if (existing >= 0) return existing;
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tags.FindProperty("layers");
            for (int i = 8; i < 32; i++)
            {
                if (!string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) continue;
                layers.GetArrayElementAtIndex(i).stringValue = "Player";
                tags.ApplyModifiedPropertiesWithoutUndo();
                return i;
            }
            throw new InvalidOperationException("No free layer for Player. No existing layer was overwritten.");
        }

        private static void BuildPrefab(int layer)
        {
            // Only initial construction is automated; do not overwrite a prefab already edited by the team.
            if (File.Exists(PlayerPath)) return;
            var root = new GameObject("PlayerCapsule"); root.SetActive(false);
            root.layer = layer; root.tag = "Player";
            var body = root.AddComponent<Rigidbody>(); body.mass = 70;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            var capsule = root.AddComponent<CapsuleCollider>(); capsule.height = 1.8f; capsule.radius = 0.3f;
            capsule.center = Vector3.up * 0.9f;
            var friction = new PhysicsMaterial("PlayerNoFriction")
            {
                dynamicFriction = 0, staticFriction = 0, bounciness = 0,
                frictionCombine = PhysicsMaterialCombine.Minimum, bounceCombine = PhysicsMaterialCombine.Minimum
            };
            AssetDatabase.CreateAsset(friction, DataPath + "/PlayerNoFriction.physicMaterial"); capsule.sharedMaterial = friction;
            var input = root.AddComponent<PlayerInputReader>();
            root.AddComponent<Health>(); root.AddComponent<PlayerStamina>();
            var rig = root.AddComponent<PlayerCameraRig>();
            var motor = root.AddComponent<PlayerMotor>(); root.AddComponent<PlayerPrototypeHUD>();

            var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "CapsuleVisual"; visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0, 0.9f, 0); visual.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
            visual.layer = layer; UnityEngine.Object.DestroyImmediate(visual.GetComponent<Collider>());
            var material = new Material(Shader.Find("Standard")) { name = "PlayerCapsule", color = new Color(0.48f, 0.57f, 0.62f) };
            material.SetFloat("_Glossiness", 0.2f);
            AssetDatabase.CreateAsset(material, DataPath + "/PlayerCapsule.mat"); visual.GetComponent<Renderer>().sharedMaterial = material;

            var target = new GameObject("CameraTarget"); target.layer = layer; target.transform.SetParent(root.transform, false);
            target.transform.localPosition = Vector3.up * 1.5f; target.transform.localRotation = Quaternion.Euler(12, 0, 0);
            var cameraObject = new GameObject("CM_Player_OverShoulder"); cameraObject.layer = layer; cameraObject.transform.SetParent(root.transform, false);
            var camera = cameraObject.AddComponent<CinemachineCamera>(); camera.Follow = target.transform; camera.Priority = 10;
            camera.Lens.FieldOfView = 58; camera.Lens.NearClipPlane = 0.1f; camera.Lens.FarClipPlane = 180;
            var follow = cameraObject.AddComponent<CinemachineThirdPersonFollow>();
            follow.ShoulderOffset = new Vector3(0.45f, 0.05f, 0); follow.VerticalArmLength = 0.2f; follow.CameraDistance = 3.6f;
            follow.CameraSide = 1; follow.Damping = new Vector3(0.08f, 0.12f, 0.08f);
            follow.AvoidObstacles = new CinemachineThirdPersonFollow.ObstacleSettings
            {
                Enabled = true, CollisionFilter = ~(1 << layer), IgnoreTag = "Player", CameraRadius = 0.22f,
                DampingIntoCollision = 0, DampingFromCollision = 0.3f
            };
            SetObject(input, "actions", AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputPath));
            SetObject(rig, "cameraTarget", target.transform); SetObject(rig, "virtualCamera", camera); SetObject(rig, "follow", follow);
            var so = new SerializedObject(motor); so.FindProperty("groundMask").intValue = ~(1 << layer); so.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(true);
            PrefabUtility.SaveAsPrefabAsset(root, PlayerPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void ConfigureMap()
        {
            var scene = EditorSceneManager.OpenScene(MapPath);
            if (scene.GetRootGameObjects().Any(item => item.GetComponent<PlayerMotor>() != null)) return;
            foreach (var preview in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                preview.enabled = false; preview.tag = "Untagged";
                var listener = preview.GetComponent<AudioListener>(); if (listener != null) listener.enabled = false;
            }
            Transform marker = GameObject.Find("PlayerStart")?.transform;
            if (marker == null) throw new InvalidOperationException("Map must have a PlayerStart marker.");
            Vector3 spawn = marker.position;
            Physics.SyncTransforms();
            if (Physics.Raycast(spawn + Vector3.up * 4, Vector3.down, out var ground, 8, ~(1 << LayerMask.NameToLayer("Player")), QueryTriggerInteraction.Ignore))
                spawn.y = ground.point.y + 0.04f;
            else throw new InvalidOperationException("No collider floor at PlayerStart.");
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath), scene);
            player.transform.position = spawn;
            CreateOutputCamera(spawn);
            if (!EditorSceneManager.SaveScene(scene, MapPath)) throw new IOException("Could not save Player scene.");
        }

        private static void CreatePlayerTestScene()
        {
            const string path = "Assets/Game/Scenes/PlayerTest.unity";
            if (File.Exists(path)) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Box("Floor", new Vector3(0, -0.5f, 6), new Vector3(24, 1, 30));
            Box("Wall", new Vector3(4, 1.5f, 4), new Vector3(0.4f, 3, 8));
            Box("Low step 0.30m", new Vector3(-4, 0.15f, 6), new Vector3(3, 0.3f, 3));
            Box("High obstacle 1m", new Vector3(-4, 0.5f, 12), new Vector3(3, 1, 1));
            var sun = new GameObject("Directional Light"); sun.transform.rotation = Quaternion.Euler(45, -35, 0); sun.AddComponent<Light>().type = LightType.Directional;
            var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPath), scene);
            player.transform.position = Vector3.up * 0.04f;
            CreateOutputCamera(player.transform.position);
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void Box(string name, Vector3 position, Vector3 scale)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Cube); item.name = name; item.transform.position = position; item.transform.localScale = scale;
        }

        public static GameObject CreateOutputCamera(Vector3 spawn)
        {
            var output = new GameObject("Main Camera - Player"); output.tag = "MainCamera";
            output.transform.position = spawn + new Vector3(0.45f, 2.5f, -3.6f);
            var camera = output.AddComponent<Camera>(); camera.nearClipPlane = 0.1f; camera.farClipPlane = 180; camera.fieldOfView = 58;
            output.AddComponent<AudioListener>();
            var brain = output.AddComponent<CinemachineBrain>();
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            brain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0);
            return output;
        }

        private static void SetObject(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Validate()
        {
            var players = UnityEngine.Object.FindObjectsByType<PlayerMotor>(FindObjectsSortMode.None);
            if (players.Length != 1) throw new InvalidOperationException("Expected exactly one PlayerMotor.");
            if (UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(camera => camera.enabled) != 1)
                throw new InvalidOperationException("Expected one active output Camera.");
            if (UnityEngine.Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(listener => listener.enabled) != 1)
                throw new InvalidOperationException("Expected one active AudioListener.");
            var player = players[0]; var body = player.GetComponent<Rigidbody>();
            if (body.isKinematic || !body.useGravity || body.interpolation != RigidbodyInterpolation.Interpolate)
                throw new InvalidOperationException("Player Rigidbody setup is invalid.");
            if (player.gameObject.layer != LayerMask.NameToLayer("Player")) throw new InvalidOperationException("Player layer missing.");
            File.WriteAllText("Logs/Phase2/setup-validation.txt", "Player prefab and two scenes configured. One Player/output camera/listener. Rigidbody and collision layer valid.\n");
        }
    }
}
