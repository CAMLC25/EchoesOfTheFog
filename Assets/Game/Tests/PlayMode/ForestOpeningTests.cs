#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using EchoesOfTheFog.Combat;
using EchoesOfTheFog.Core;
using EchoesOfTheFog.Interaction;
using EchoesOfTheFog.Mission;
using EchoesOfTheFog.Player;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace EchoesOfTheFog.Tests
{
    public sealed class ForestOpeningTests : InputTestFixture
    {
        private Keyboard keyboard;
        private Mouse mouse;
        private PlayerEquipment equipment;
        private PlayerInputReader input;
        private Rigidbody body;
        private WeaponPickup pickup;
        private ForestMission mission;

        private IEnumerator OpenForest()
        {
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenes.Forest, new LoadSceneParameters(LoadSceneMode.Single));
            yield return new WaitForSeconds(0.25f);
            equipment = Object.FindFirstObjectByType<PlayerEquipment>(); input = equipment.GetComponent<PlayerInputReader>(); body = equipment.GetComponent<Rigidbody>();
            pickup = Object.FindFirstObjectByType<WeaponPickup>(); mission = Object.FindFirstObjectByType<ForestMission>();
            input.SetCursorCaptured(true);
            Assert.That(equipment.HasWeapon, Is.False); Assert.That(equipment.WeaponVisual.activeInHierarchy, Is.False);
            Assert.That(mission.Objective, Is.EqualTo(ForestObjective.FindWeapon));
        }
        private void MoveToPickup()
        {
            Vector3 towardSpawn = Vector3.ProjectOnPlane(body.position - pickup.transform.position, Vector3.up).normalized;
            Vector3 position = pickup.transform.position + towardSpawn * 1.4f;
            PlaceOnFloor(position);
            equipment.GetComponent<PlayerCameraRig>().SetViewAngles(Quaternion.LookRotation(-towardSpawn).eulerAngles.y, 12);
        }
        private void PlaceOnFloor(Vector3 position)
        {
            int mask = ~((1 << LayerMask.NameToLayer("Player")) | (1 << LayerMask.NameToLayer("Enemy")) | (1 << LayerMask.NameToLayer("Interactable")));
            Assert.That(Physics.Raycast(position + Vector3.up * 6, Vector3.down, out var hit, 15, mask, QueryTriggerInteraction.Ignore), Is.True);
            body.position = hit.point + Vector3.up * 0.04f; body.linearVelocity = Vector3.zero; Physics.SyncTransforms();
        }
        private IEnumerator PressF()
        { Press(keyboard.fKey); yield return null; Release(keyboard.fKey); yield return null; }

        [UnityTest]
        public IEnumerator UnarmedStartBlocksAimFireReloadAndUsesUnarmedIdle()
        {
            yield return OpenForest();
            var rifle = equipment.GetComponent<HitscanRifle>();
            Assert.That(rifle.Loaded, Is.Zero); Assert.That(rifle.Reserve, Is.Zero); Assert.That(rifle.TryFire(), Is.False); Assert.That(rifle.TryStartReload(), Is.False);
            Press(mouse.rightButton); yield return new WaitForSeconds(0.2f);
            Assert.That(equipment.GetComponent<PlayerCameraRig>().IsAiming, Is.False); Release(mouse.rightButton);
            Press(mouse.leftButton); Press(keyboard.rKey); yield return new WaitForSeconds(0.25f);
            Assert.That(rifle.IsInFirePose, Is.False); Assert.That(rifle.IsReloading, Is.False); Assert.That(rifle.Loaded, Is.Zero);
            var animator = equipment.GetComponentInChildren<Animator>();
            Assert.That(animator.GetCurrentAnimatorClipInfo(0).Any(info => info.clip.name == "Idle" && info.weight > 0.9f), Is.True);
            Assert.That(animator.GetLayerWeight(1), Is.EqualTo(0).Within(0.01f));
        }

        [UnityTest]
        public IEnumerator FPickupChecksRangeCoverAndDoesNotFireUntilMouseIsReleased()
        {
            yield return OpenForest();
            yield return PressF(); Assert.That(equipment.HasWeapon, Is.False, "A distant pickup must not be collected.");
            MoveToPickup(); yield return new WaitForSeconds(0.2f);
            var interactor = equipment.GetComponent<PlayerInteractor>();
            Vector3 eye = equipment.transform.position + Vector3.up, target = pickup.GetComponent<Collider>().bounds.center;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = "Interaction occlusion test";
            wall.transform.SetPositionAndRotation((eye + target) * 0.5f, Quaternion.LookRotation(target - eye)); wall.transform.localScale = new Vector3(2, 2, 0.08f); Physics.SyncTransforms();
            yield return PressF(); Assert.That(equipment.HasWeapon, Is.False, "Interaction must not pass through cover.");
            Object.Destroy(wall); yield return null; Physics.SyncTransforms(); yield return new WaitForSeconds(0.15f);
            Assert.That(interactor.CurrentPrompt, Does.Contain("F"));
            yield return Capture("Weapon_prompt");
            int equippedEvents = 0; equipment.WeaponEquipped += () => equippedEvents++;
            Press(mouse.leftButton); yield return null;
            yield return PressF();
            Assert.That(equipment.HasWeapon, Is.True); Assert.That(equipment.WeaponVisual.activeInHierarchy, Is.True); Assert.That(pickup.gameObject.activeSelf, Is.False);
            Assert.That(mission.Objective, Is.EqualTo(ForestObjective.ReachVillage));
            var rifle = equipment.GetComponent<HitscanRifle>(); yield return new WaitForSeconds(0.25f);
            Assert.That(rifle.Loaded, Is.EqualTo(30), "Holding fire before picking up must not accidentally discharge the new weapon.");
            Release(mouse.leftButton); yield return null; yield return PressF(); Assert.That(equippedEvents, Is.EqualTo(1));
            Assert.That(equipment.GetComponentInChildren<Animator>().GetFloat("Armed"), Is.EqualTo(1));
            yield return Capture("Weapon_equipped");
            Press(mouse.leftButton); yield return null; Release(mouse.leftButton); yield return null;
            Assert.That(rifle.Loaded, Is.EqualTo(29), "Firing should unlock after weapon pickup and a fresh mouse press.");
        }

        [UnityTest]
        public IEnumerator VillageGoalRequiresWeaponThenCompletesOnceAndReturnsToMenu()
        {
            yield return OpenForest();
            var gate = Object.FindFirstObjectByType<ForestExitTrigger>();
            PlaceOnFloor(gate.transform.position); yield return new WaitForSeconds(0.1f);
            Assert.That(mission.Objective, Is.EqualTo(ForestObjective.FindWeapon), "The exit must not complete the opening objective without a weapon.");
            MoveToPickup(); yield return new WaitForSeconds(0.2f); yield return PressF(); Assert.That(equipment.HasWeapon, Is.True);
            int completions = 0; mission.ObjectiveChanged += objective => { if (objective == ForestObjective.Completed) completions++; };
            PlaceOnFloor(gate.transform.position);
            float timeout = Time.realtimeSinceStartup + 2;
            while (mission.Objective != ForestObjective.Completed && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(mission.Objective, Is.EqualTo(ForestObjective.Completed)); Assert.That(completions, Is.EqualTo(1));
            Assert.That(input.CanControl, Is.False); Assert.That(Time.timeScale, Is.Zero);
            Assert.That(mission.ReachVillage(equipment), Is.False);
            yield return Capture("Forest_completed");
            var menuButton = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(button => button.name == "ForestMenuButton");
            var rect = menuButton.GetComponent<RectTransform>(); Set(mouse.position, RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)));
            yield return null; Press(mouse.leftButton); yield return null; Release(mouse.leftButton); yield return null;
            timeout = Time.realtimeSinceStartup + 8;
            while (SceneManager.GetActiveScene().path != GameScenes.MainMenu && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(GameScenes.MainMenu)); Assert.That(Time.timeScale, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ReloadingForestRestoresPickupAndUnarmedObjective()
        {
            yield return OpenForest(); MoveToPickup(); yield return new WaitForSeconds(0.2f); yield return PressF();
            Assert.That(equipment.HasWeapon, Is.True);
            yield return SceneManager.LoadSceneAsync(GameScenes.Forest); yield return new WaitForSeconds(0.25f);
            Assert.That(Object.FindFirstObjectByType<PlayerEquipment>().HasWeapon, Is.False);
            Assert.That(Object.FindFirstObjectByType<WeaponPickup>(), Is.Not.Null);
            Assert.That(Object.FindFirstObjectByType<ForestMission>().Objective, Is.EqualTo(ForestObjective.FindWeapon));
        }

        [UnityTest]
        public IEnumerator DeadPlayerCannotPickUpOrCompleteObjective()
        {
            yield return OpenForest(); MoveToPickup(); yield return new WaitForSeconds(0.1f);
            equipment.GetComponent<Health>().TakeDamage(100); yield return PressF();
            Assert.That(equipment.HasWeapon, Is.False); Assert.That(pickup.CanInteract(equipment.gameObject), Is.False);
            Assert.That(equipment.TryEquipWeapon(), Is.False); Assert.That(mission.Objective, Is.EqualTo(ForestObjective.FindWeapon));
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            var current = SceneManager.GetActiveScene();
            if (current.path == GameScenes.Forest || current.path == GameScenes.MainMenu)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("AfterForestOpeningTest")); yield return SceneManager.UnloadSceneAsync(current);
            }
            Time.timeScale = 1;
        }

        private static IEnumerator Capture(string name)
        {
            var camera = Camera.main; var canvas = Object.FindFirstObjectByType<Canvas>(); var scaler = canvas.GetComponent<CanvasScaler>();
            var previousMode = canvas.renderMode; var previousCamera = canvas.worldCamera; float plane = canvas.planeDistance, scale = canvas.scaleFactor;
            bool enabled = scaler.enabled; var previousTarget = camera.targetTexture; var active = RenderTexture.active;
            var target = new RenderTexture(1280, 720, 24); target.Create(); var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = target; scaler.enabled = false; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera;
                // Match overlay depth when capturing a live level: world geometry must not hide the UI.
                canvas.planeDistance = camera.nearClipPlane + 0.001f; canvas.scaleFactor = 2f / 3;
                Canvas.ForceUpdateCanvases(); yield return null; yield return null; Canvas.ForceUpdateCanvases();
                foreach (var text in canvas.GetComponentsInChildren<TMP_Text>()) { text.ForceMeshUpdate(); Assert.That(text.isTextOverflowing, Is.False, text.name + ": " + text.text); }
                camera.Render(); RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                Directory.CreateDirectory("Logs/OpeningFlow"); File.WriteAllBytes("Logs/OpeningFlow/" + name + ".png", pixels.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = previousMode; canvas.worldCamera = previousCamera; canvas.planeDistance = plane; canvas.scaleFactor = scale; scaler.enabled = enabled;
                camera.targetTexture = previousTarget; RenderTexture.active = active; target.Release(); Object.Destroy(target); Object.Destroy(pixels); Canvas.ForceUpdateCanvases();
            }
            yield return null;
        }
    }
}
#endif
