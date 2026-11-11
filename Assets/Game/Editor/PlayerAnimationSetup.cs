using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.InputSystem;
using EchoesOfTheFog.Player;
using EchoesOfTheFog.Combat;
using EchoesOfTheFog.Core;

namespace EchoesOfTheFog.Editor
{
    public static class PlayerAnimationSetup
    {
        private const string AnimationFolder = "Assets/Game/Animations/Player";
        private const string GeneratedFolder = "Assets/Game/Animations/Player/Generated";
        private const string PlayerPath = "Assets/Game/Prefabs/Player/PlayerCapsule.prefab";
        private const string WeaponPath = "Assets/Game/Prefabs/Weapons/PlayerRifle.prefab";
        private const string DataFolder = "Assets/Game/Data/Weapons";

        public static void UpdateJumpAndGun31()
        {
            Apply();
            ProjectCleanup.Run();
        }

        public static void Apply()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Close Unity and run setup in batch mode.");
            Directory.CreateDirectory(GeneratedFolder); Directory.CreateDirectory(DataFolder);
            Directory.CreateDirectory("Assets/Game/Prefabs/Weapons"); Directory.CreateDirectory("Logs/PlayerAnimation");
            AssetDatabase.Refresh();
            ProjectBackups.PreserveAsset(PlayerPath, "BeforeAnimationSetup");
            ConfigureInput();
            ConfigureClips();
            var controller = CreateController();
            ConfigureAimPose(controller);
            ConfigureJump(controller);
            AssetDatabase.SaveAssets();
            CreateWeaponPrefab();
            ConfigurePlayer(controller);
            CreateCombatScene();
            AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/PlayerAnimation/setup.txt", "Humanoid clips configured; Player controller, physics-driven jump and Gun_31 attached.\n");
        }

        private static void ConfigureInput()
        {
            const string path = "Assets/InputSystem_Actions.inputactions";
            var actions = InputActionAsset.FromJson(File.ReadAllText(path));
            try
            {
                var map = actions.FindActionMap("Player", true);
                if (map.FindAction("Reload") == null)
                {
                    var reload = map.AddAction("Reload", InputActionType.Button);
                    reload.AddBinding("<Keyboard>/r").WithGroup("Keyboard&Mouse");
                    reload.AddBinding("<Gamepad>/buttonWest").WithGroup("Gamepad");
                }
                File.WriteAllText(path, actions.ToJson());
            }
            finally { UnityEngine.Object.DestroyImmediate(actions); }
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        }

        private static void ConfigureClips()
        {
            var avatar = AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Characters/Player/Elena_Model.fbx").OfType<Avatar>().First(avatar => avatar.isHuman && avatar.isValid);
            var report = new StringBuilder();
            foreach (string rawPath in Directory.GetFiles(AnimationFolder, "*.fbx"))
            {
                string path = rawPath.Replace('\\', '/');
                string name = Path.GetFileNameWithoutExtension(path).Split('@').Last();
                bool loop = name.Contains("Idle") || name.Contains("Walk") || name.Contains("Run") && !name.Contains("Reload") || name.StartsWith("Strafe");
                var importer = (ModelImporter)AssetImporter.GetAtPath(path);
                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar = avatar;
                importer.importCameras = false; importer.importLights = false;
                var clips = importer.defaultClipAnimations;
                if (clips.Length != 1) throw new InvalidOperationException("Expected one take in " + path);
                clips[0].name = name; clips[0].loopTime = loop;
                clips[0].lockRootRotation = true; clips[0].keepOriginalOrientation = true;
                bool jumping = name.Contains("Jump");
                clips[0].lockRootHeightY = !jumping; clips[0].keepOriginalPositionY = !jumping;
                // Extract translation as root motion, then discard it at Animator.applyRootMotion=false.
                // This also handles Mixamo clips exported without the In Place option.
                clips[0].lockRootPositionXZ = false; clips[0].keepOriginalPositionXZ = true;
                // Feet-based root height keeps non-root-motion jumps anchored to
                // the capsule instead of lowering the model to its mass center.
                clips[0].heightFromFeet = loop || jumping;
                importer.clipAnimations = clips; importer.SaveAndReimport();
                var clip = Clip(name);
                if (!clip.humanMotion) throw new InvalidOperationException("Clip did not retarget as Humanoid: " + name);
                report.AppendLine($"{name}: Human, loop={loop}, seconds={clip.length:F3}, averageSpeed={clip.averageSpeed}");
            }
            File.WriteAllText("Logs/PlayerAnimation/retargeted-clips.txt", report.ToString());
        }

        private static AnimationClip Clip(string name)
            => AssetDatabase.LoadAllAssetsAtPath(AnimationFolder + "/Ch11_nonPBR@" + name + ".fbx").OfType<AnimationClip>().First(clip => !clip.name.StartsWith("__preview__"));

        private static AnimatorController CreateController()
        {
            string path = GeneratedFolder + "/ElenaRifle.controller";
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (existing != null && existing.layers.Length > 0 && existing.parameters.Length > 0) return existing;
            var controller = existing != null ? existing : AnimatorController.CreateAnimatorControllerAtPath(path);
            if (controller.layers.Length == 0) controller.AddLayer("Base Layer");
            controller.AddParameter("MoveX", AnimatorControllerParameterType.Float); controller.AddParameter("MoveY", AnimatorControllerParameterType.Float);
            controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool); controller.AddParameter("Dead", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Reloading", AnimatorControllerParameterType.Bool); controller.AddParameter("Fire", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
            var machine = controller.layers[0].stateMachine;
            var locomotion = machine.AddState("Grounded", new Vector3(220, 80));
            var blend = new BlendTree { name = "Rifle Locomotion", blendType = BlendTreeType.FreeformDirectional2D, blendParameter = "MoveX", blendParameterY = "MoveY", useAutomaticThresholds = false };
            AssetDatabase.AddObjectToAsset(blend, controller);
            blend.AddChild(Clip("Rifle Idle"), Vector2.zero);
            blend.AddChild(Clip("Rifle Walk"), Vector2.up);
            blend.AddChild(Clip("Rifle Run"), new Vector2(0, 2.08f));
            blend.AddChild(Clip("Backwards Rifle Walk"), Vector2.down);
            blend.AddChild(Clip("Strafe Left"), Vector2.left);
            blend.AddChild(Clip("Strafe Righr"), Vector2.right);
            locomotion.motion = blend; locomotion.iKOnFeet = true; machine.defaultState = locomotion;
            var air = machine.AddState("AirHold", new Vector3(440, 80)); air.motion = Clip("Rifle Idle"); air.speed = 0;
            Transition(locomotion, air, "Grounded", false, 0.12f); Transition(air, locomotion, "Grounded", true, 0.12f);
            var dead = machine.AddState("Dead", new Vector3(440, 260)); dead.motion = Clip("Rifle Death");
            var death = machine.AddAnyStateTransition(dead); death.hasExitTime = false; death.duration = 0.12f; death.canTransitionToSelf = false;
            death.AddCondition(AnimatorConditionMode.If, 0, "Dead");

            var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(GeneratedFolder + "/RifleUpperBody.mask");
            if (mask == null)
            {
                mask = new AvatarMask { name = "Rifle Upper Body" };
                AssetDatabase.CreateAsset(mask, GeneratedFolder + "/RifleUpperBody.mask");
            }
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++) mask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            foreach (var part in new[] { AvatarMaskBodyPart.Body, AvatarMaskBodyPart.Head, AvatarMaskBodyPart.LeftArm, AvatarMaskBodyPart.RightArm, AvatarMaskBodyPart.LeftFingers, AvatarMaskBodyPart.RightFingers, AvatarMaskBodyPart.LeftHandIK, AvatarMaskBodyPart.RightHandIK }) mask.SetHumanoidBodyPartActive(part, true);
            EditorUtility.SetDirty(mask);
            var upper = new AnimatorControllerLayer { name = "UpperBody", defaultWeight = 1, avatarMask = mask, iKPass = true, blendingMode = AnimatorLayerBlendingMode.Override, stateMachine = new AnimatorStateMachine { name = "UpperBody" } };
            AssetDatabase.AddObjectToAsset(upper.stateMachine, controller); controller.AddLayer(upper);
            var idle = upper.stateMachine.AddState("RifleReady", new Vector3(200, 80)); idle.motion = Clip("Rifle Idle"); upper.stateMachine.defaultState = idle;
            var reload = upper.stateMachine.AddState("Reload", new Vector3(440, 80)); reload.motion = Clip("Reloading");
            var fire = upper.stateMachine.AddState("Fire", new Vector3(440, 200)); fire.motion = Clip("Firing Rifle"); fire.speed = 2;
            var hit = upper.stateMachine.AddState("Hit", new Vector3(440, 320)); hit.motion = Clip("Hit Reaction");
            var enterReload = upper.stateMachine.AddAnyStateTransition(reload); enterReload.hasExitTime = false; enterReload.duration = 0.08f; enterReload.canTransitionToSelf = false;
            enterReload.AddCondition(AnimatorConditionMode.If, 0, "Reloading");
            Transition(reload, idle, "Reloading", false, 0.1f);
            var enterFire = upper.stateMachine.AddAnyStateTransition(fire); enterFire.hasExitTime = false; enterFire.duration = 0.025f; enterFire.canTransitionToSelf = true;
            enterFire.AddCondition(AnimatorConditionMode.If, 0, "Fire"); enterFire.AddCondition(AnimatorConditionMode.IfNot, 0, "Reloading");
            var enterHit = upper.stateMachine.AddAnyStateTransition(hit); enterHit.hasExitTime = false; enterHit.duration = 0.05f; enterHit.canTransitionToSelf = false;
            enterHit.AddCondition(AnimatorConditionMode.If, 0, "Hit"); enterHit.AddCondition(AnimatorConditionMode.IfNot, 0, "Reloading");
            foreach (var state in new[] { fire, hit }) { var exit = state.AddTransition(idle); exit.hasExitTime = true; exit.exitTime = 0.9f; exit.duration = 0.08f; }
            EditorUtility.SetDirty(controller); return controller;
        }

        private static void Transition(AnimatorState from, AnimatorState to, string condition, bool value, float duration)
        {
            var transition = from.AddTransition(to); transition.hasExitTime = false; transition.duration = duration;
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, condition);
        }

        private static void ConfigureAimPose(AnimatorController controller)
        {
            if (controller.parameters.Any(parameter => parameter.name == "Aiming")) return;
            controller.AddParameter("Aiming", AnimatorControllerParameterType.Bool);
            var machine = controller.layers[1].stateMachine;
            var ready = machine.states.First(state => state.state.name == "RifleReady").state;
            var aim = machine.AddState("Aim", new Vector3(200, 240));
            aim.motion = Clip("Firing Rifle"); aim.speed = 0; aim.cycleOffset = 0.1f;
            Transition(ready, aim, "Aiming", true, 0.12f);
            Transition(aim, ready, "Aiming", false, 0.12f);
            foreach (var state in machine.states) EditorUtility.SetDirty(state.state);
            EditorUtility.SetDirty(machine); EditorUtility.SetDirty(controller);
        }

        private static void CreateWeaponPrefab()
        {
            bool existing = File.Exists(WeaponPath);
            var root = existing ? PrefabUtility.LoadPrefabContents(WeaponPath) : new GameObject("PlayerRifle");
            try
            {
                if (root.transform.Find("Gun31_Visual") != null) return;
                ProjectBackups.PreserveAsset(WeaponPath, "BeforeGun31");
                foreach (var old in root.GetComponentsInChildren<Renderer>().Select(renderer => renderer.transform).Where(item => item.parent == root.transform).ToArray())
                    UnityEngine.Object.DestroyImmediate(old.gameObject);
                var oldVisual = root.transform.Find("M4_Visual");
                if (oldVisual != null) UnityEngine.Object.DestroyImmediate(oldVisual.gameObject);
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Free Low Poly Weapons/Prefabs/Gun_31.prefab"), root.transform);
                visual.name = "Gun31_Visual"; visual.transform.localPosition = Vector3.zero; visual.transform.localRotation = Quaternion.identity; visual.transform.localScale = Vector3.one;
                var renderers = visual.GetComponentsInChildren<Renderer>(); var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                var trigger = visual.GetComponentsInChildren<Transform>().First(item => item.name.Contains("Trigger"));
                float scale = 0.65f / bounds.size.z;
                Vector3 grip = new Vector3(0, trigger.position.y - 0.12f, trigger.position.z - 0.04f);
                Vector3 barrel = new Vector3(0, trigger.position.y + 0.22f, bounds.max.z + 0.025f);
                Vector3 foreGrip = new Vector3(-0.025f, trigger.position.y - 0.04f, trigger.position.z + 0.7f);
                visual.transform.localScale = Vector3.one * scale; visual.transform.localPosition = -grip * scale;
                foreach (var collider in visual.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
                foreach (var body in visual.GetComponentsInChildren<Rigidbody>()) UnityEngine.Object.DestroyImmediate(body);
                foreach (var transform in root.GetComponentsInChildren<Transform>()) transform.gameObject.layer = LayerMask.NameToLayer("Player");
                var material = AssetDatabase.LoadAssetAtPath<Material>(DataFolder + "/RifleMetal.mat");
                if (material == null)
                {
                    material = new Material(Shader.Find("Standard")) { name = "RifleMetal" };
                    AssetDatabase.CreateAsset(material, DataFolder + "/RifleMetal.mat");
                }
                material.mainTexture = renderers[0].sharedMaterial.mainTexture; material.color = Color.white;
                material.SetFloat("_Metallic", 0.35f); material.SetFloat("_Glossiness", 0.3f);
                EditorUtility.SetDirty(material);
                foreach (var renderer in renderers) renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => material).ToArray();
                var muzzle = root.transform.Find("Muzzle");
                if (muzzle == null) { muzzle = new GameObject("Muzzle").transform; muzzle.SetParent(root.transform, false); }
                muzzle.localPosition = (barrel - grip) * scale;
                var left = root.transform.Find("LeftHandGrip");
                if (left == null) { left = new GameObject("LeftHandGrip").transform; left.SetParent(root.transform, false); }
                left.localPosition = (foreGrip - grip) * scale;
                var view = root.GetComponent<RifleView>();
                if (view == null) view = root.AddComponent<RifleView>();
                Set(view, "muzzle", muzzle); Set(view, "leftHandGrip", left);
                PrefabUtility.SaveAsPrefabAsset(root, WeaponPath);
            }
            finally { if (existing) PrefabUtility.UnloadPrefabContents(root); else UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void ConfigureJump(AnimatorController controller)
        {
            if (!controller.parameters.Any(parameter => parameter.name == "Armed")) controller.AddParameter("Armed", AnimatorControllerParameterType.Float);
            if (!controller.parameters.Any(parameter => parameter.name == "JumpPhase")) controller.AddParameter("JumpPhase", AnimatorControllerParameterType.Float);
            var air = controller.layers[0].stateMachine.states.First(state => state.state.name == "AirHold").state;
            var blend = air.motion as BlendTree;
            if (blend == null)
            {
                blend = new BlendTree { name = "Jump - armed and unarmed", blendType = BlendTreeType.Simple1D, blendParameter = "Armed", useAutomaticThresholds = false };
                AssetDatabase.AddObjectToAsset(blend, controller);
            }
            blend.children = new[] { new ChildMotion { motion = Clip("Jump"), threshold = 0, timeScale = 1 }, new ChildMotion { motion = Clip("Rifle Jump"), threshold = 1, timeScale = 1 } };
            air.motion = blend; air.speed = 1; air.timeParameter = "JumpPhase"; air.timeParameterActive = true;
            EditorUtility.SetDirty(blend); EditorUtility.SetDirty(air); EditorUtility.SetDirty(controller);
        }

        private static void ConfigurePlayer(AnimatorController controller)
        {
            var root = PrefabUtility.LoadPrefabContents(PlayerPath);
            try
            {
                var animator = root.GetComponentInChildren<Animator>();
                if (animator == null || !animator.isHuman) throw new InvalidOperationException("Player needs Elena's Humanoid Animator.");
                animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind(); animator.Update(0); animator.SetFloat("Armed", 1); animator.SetBool("Grounded", true); animator.Play("Grounded", 0, 0); animator.Update(0.1f);
                var hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
                var socket = hand.Find("RifleSocket");
                if (socket == null)
                {
                    socket = new GameObject("RifleSocket").transform; socket.SetParent(hand, false);
                    var weapon = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(WeaponPath), socket);
                    weapon.transform.localPosition = Vector3.zero; weapon.transform.localRotation = Quaternion.identity;
                }
                var finger = animator.GetBoneTransform(HumanBodyBones.RightMiddleProximal);
                socket.position = finger != null ? Vector3.Lerp(hand.position, finger.position, 0.65f) : hand.position;
                socket.rotation = root.transform.rotation;
                foreach (var transform in socket.GetComponentsInChildren<Transform>()) transform.gameObject.layer = root.layer;
                var rifleView = socket.GetComponentInChildren<RifleView>();
                rifleView.LeftHandGrip.rotation = animator.GetBoneTransform(HumanBodyBones.LeftHand).rotation;
                var rifle = root.GetComponent<HitscanRifle>() ?? root.AddComponent<HitscanRifle>();
                var config = AssetDatabase.LoadAssetAtPath<RifleConfig>(DataFolder + "/PrototypeRifle.asset");
                if (config == null)
                {
                    config = ScriptableObject.CreateInstance<RifleConfig>(); config.reloadDuration = Clip("Reloading").length + 0.1f;
                    AssetDatabase.CreateAsset(config, DataFolder + "/PrototypeRifle.asset");
                }
                Set(rifle, "config", config); Set(rifle, "muzzle", rifleView.Muzzle);
                var so = new SerializedObject(rifle); so.FindProperty("hitMask").intValue = ~(1 << root.layer); so.ApplyModifiedPropertiesWithoutUndo();
                var fx = root.transform.Find("RifleShotTracer");
                if (fx == null)
                {
                    fx = new GameObject("RifleShotTracer").transform; fx.SetParent(root.transform, false);
                    var line = fx.gameObject.AddComponent<LineRenderer>(); line.positionCount = 2; line.useWorldSpace = true;
                    line.startWidth = 0.012f; line.endWidth = 0.006f; line.startColor = new Color(0.7f, 1, 0.5f); line.endColor = new Color(0.6f, 0.9f, 0.4f, 0.4f);
                    var material = new Material(Shader.Find("Sprites/Default")) { name = "ShotTracer" }; AssetDatabase.CreateAsset(material, DataFolder + "/ShotTracer.mat");
                    line.sharedMaterial = material; line.enabled = false;
                }
                Set(rifle, "tracer", fx.GetComponent<LineRenderer>());
                var flash = rifleView.Muzzle.GetComponent<Light>();
                if (flash == null) flash = rifleView.Muzzle.gameObject.AddComponent<Light>();
                flash.type = LightType.Point; flash.color = new Color(0.8f, 1, 0.55f); flash.range = 3; flash.intensity = 2; flash.enabled = false;
                Set(rifle, "muzzleLight", flash);
                var driver = root.GetComponent<PlayerAnimationDriver>() ?? root.AddComponent<PlayerAnimationDriver>(); Set(driver, "animator", animator);
                var ik = animator.GetComponent<PlayerRifleIK>() ?? animator.gameObject.AddComponent<PlayerRifleIK>(); Set(ik, "leftHandGrip", rifleView.LeftHandGrip);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void CreateCombatScene()
        {
            const string path = "Assets/Game/Scenes/Tests/CombatTest.unity";
            if (File.Exists(path)) return;
            if (!AssetDatabase.CopyAsset("Assets/Game/Scenes/Tests/PlayerTest.unity", path)) throw new IOException("Could not create CombatTest scene.");
            var scene = EditorSceneManager.OpenScene(path);
            foreach (var position in new[] { new Vector3(0, 1, 8), new Vector3(-2, 1, 13), new Vector3(2, 1, 17) })
            {
                var target = GameObject.CreatePrimitive(PrimitiveType.Capsule); target.name = "Damage test target - 100 HP";
                target.transform.position = position; target.AddComponent<Health>(); target.AddComponent<DamageTargetFeedback>();
            }
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var so = new SerializedObject(target); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void Inspect()
        {
            if (!Application.isBatchMode) throw new InvalidOperationException("Run setup in batch mode with Unity closed.");
            Directory.CreateDirectory("Logs/PlayerAnimation");
            var report = new StringBuilder();
            foreach (string path in Directory.GetFiles("Assets/Game/Animations/Player", "*.fbx"))
            {
                report.AppendLine(path);
                foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(clip => !clip.name.StartsWith("__preview__")))
                    report.AppendLine($"  {clip.name}: duration={clip.length:F3}; human={clip.humanMotion}; root={clip.hasRootCurves}; frames={clip.frameRate}");
            }
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var sun = new GameObject("Studio light").AddComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.2f;
            sun.transform.rotation = Quaternion.Euler(40, -35, 0);
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
            var camera = new GameObject("Studio camera").AddComponent<Camera>(); camera.fieldOfView = 32;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.23f, 0.25f, 0.27f);
            foreach (string name in new[] { "Gun_1", "Gun_10", "Gun_31" })
            {
                string path = "Assets/Free Low Poly Weapons/Prefabs/" + name + ".prefab";
                var item = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                item.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity); item.transform.localScale = Vector3.one;
                var renderers = item.GetComponentsInChildren<Renderer>();
                var bounds = renderers[0].bounds;
                foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                report.AppendLine($"Weapon {name}: size={bounds.size}, center={bounds.center}");
                foreach (var part in item.GetComponentsInChildren<Transform>())
                    if (part.name.Contains("Trigger") || part.name.Contains("Storage")) report.AppendLine($"  {part.name}: position={part.position}");
                item.transform.position -= bounds.center;
                camera.transform.position = new Vector3(1.4f, 0.7f, 1.6f) * Mathf.Max(bounds.size.magnitude, 0.5f);
                camera.transform.LookAt(Vector3.zero);
                SaveCamera(camera, "Logs/PlayerAnimation/" + name + ".png");
                UnityEngine.Object.DestroyImmediate(item);
            }
            File.WriteAllText("Logs/PlayerAnimation/inspection.txt", report.ToString());
        }

        private static void SaveCamera(Camera camera, string path)
        {
            var target = new RenderTexture(960, 540, 24);
            var pixels = new Texture2D(960, 540, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 960, 540), 0, 0); pixels.Apply(); File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = null; RenderTexture.active = previous; target.Release();
                UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels);
            }
        }
    }
}
