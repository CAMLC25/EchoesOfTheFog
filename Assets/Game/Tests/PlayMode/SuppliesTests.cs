#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using EchoesOfTheFog.Combat;
using EchoesOfTheFog.Core;
using EchoesOfTheFog.Interaction;
using EchoesOfTheFog.Player;
using EchoesOfTheFog.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace EchoesOfTheFog.Tests
{
    public sealed class SuppliesTests : InputTestFixture
    {
        private GameObject arena, actor;
        private Keyboard keyboard;
        private PlayerInputReader input;
        private PlayerSupplies supplies;
        private HitscanRifle rifle;
        private Health health;
        private IEnumerator CreateArena()
        {
            keyboard = InputSystem.AddDevice<Keyboard>(); InputSystem.AddDevice<Mouse>();
            arena = new GameObject("Supply test arena");
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.transform.SetParent(arena.transform);
            floor.transform.position = new Vector3(600, -.5f, 0); floor.transform.localScale = new Vector3(20, 1, 20);
            actor = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Player/PlayerCapsule.prefab"));
            actor.GetComponent<Rigidbody>().position = new Vector3(600, .04f, 0);
            yield return new WaitForSeconds(.15f);
            input = actor.GetComponent<PlayerInputReader>(); input.SetCursorCaptured(true);
            supplies = actor.GetComponent<PlayerSupplies>(); rifle = actor.GetComponent<HitscanRifle>(); health = actor.GetComponent<Health>();
        }
        private SupplyPickup Spawn(SupplyKind kind)
        {
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Items/" + kind + "Pickup.prefab"), arena.transform);
            go.transform.position = actor.transform.position + new Vector3(0, .7f, 1);
            Physics.SyncTransforms(); return go.GetComponent<SupplyPickup>();
        }
        private IEnumerator F()
        { Press(keyboard.fKey); yield return null; Release(keyboard.fKey); yield return null; }
        [Test]
        public void ReserveCapacityHandlesPartialPickupAndIntegerLimit()
        {
            var ammo = new WeaponAmmo(30, 170, 180);
            Assert.That(ammo.AddReserve(30), Is.EqualTo(10)); Assert.That(ammo.AddReserve(1), Is.Zero);
            Assert.That(ammo.Loaded, Is.EqualTo(30)); Assert.That(ammo.Reserve, Is.EqualTo(180));
            var unlimited = new WeaponAmmo(1, int.MaxValue - 1);
            Assert.That(unlimited.AddReserve(int.MaxValue), Is.EqualTo(1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => ammo.AddReserve(-1));
        }
        [UnityTest]
        public IEnumerator FAmmoPickupKeepsRemainderAndRequiresWeapon()
        {
            yield return CreateArena(); var pickup = Spawn(SupplyKind.Ammo);
            rifle.AddReserveAmmo(80); Assert.That(rifle.Reserve, Is.EqualTo(170));
            yield return new WaitForSeconds(.15f); yield return F();
            Assert.That(rifle.Reserve, Is.EqualTo(180)); Assert.That(pickup.Remaining, Is.EqualTo(20)); Assert.That(pickup.gameObject.activeSelf, Is.True);
            yield return F(); Assert.That(pickup.Remaining, Is.EqualTo(20));
            var camera = new GameObject("Supply test camera", typeof(Camera)); camera.transform.SetParent(arena.transform); camera.tag = "MainCamera";
            camera.transform.position = actor.transform.position + new Vector3(0, 1.4f, -3);
            Assert.That(rifle.TryFire(), Is.True); Assert.That(rifle.TryStartReload(), Is.True);
            yield return new WaitForSeconds(3.6f);
            Assert.That(rifle.Reserve, Is.EqualTo(179)); Assert.That(rifle.Loaded, Is.EqualTo(30));
            yield return F(); Assert.That(rifle.Reserve, Is.EqualTo(180)); Assert.That(pickup.Remaining, Is.EqualTo(19));
            rifle.enabled = false; Assert.That(pickup.CanInteract(actor), Is.False); rifle.enabled = true;
            health.TakeDamage(100); Assert.That(pickup.CanInteract(actor), Is.False);
        }
        [UnityTest]
        public IEnumerator MedkitsStoreAtFullHealthHealOncePerPressAndNeverRevive()
        {
            yield return CreateArena(); var pickup = Spawn(SupplyKind.Medkit);
            yield return new WaitForSeconds(.15f); yield return F();
            Assert.That(supplies.Medkits, Is.EqualTo(1)); Assert.That(pickup.gameObject.activeSelf, Is.False);
            pickup.Interact(actor); Assert.That(supplies.Medkits, Is.EqualTo(1));
            Assert.That(supplies.TryUseMedkit(), Is.False); Assert.That(supplies.Medkits, Is.EqualTo(1));
            Assert.That(supplies.AddMedkits(9), Is.EqualTo(2)); Assert.That(supplies.AddMedkits(1), Is.Zero);
            var extra = Spawn(SupplyKind.Medkit); Assert.That(extra.CanInteract(actor), Is.False); Assert.That(extra.Remaining, Is.EqualTo(1));
            health.TakeDamage(90); Press(keyboard.eKey); yield return new WaitForSeconds(.2f);
            Assert.That(health.CurrentHealth, Is.EqualTo(50)); Assert.That(supplies.Medkits, Is.EqualTo(2)); Release(keyboard.eKey); yield return null;
            input.SetCursorCaptured(false); Press(keyboard.eKey); yield return null; Release(keyboard.eKey); yield return null;
            Assert.That(health.CurrentHealth, Is.EqualTo(50)); input.SetCursorCaptured(true);
            Assert.That(supplies.TryUseMedkit(), Is.True); Assert.That(health.CurrentHealth, Is.EqualTo(90));
            Assert.That(supplies.TryUseMedkit(), Is.True); Assert.That(health.CurrentHealth, Is.EqualTo(100)); Assert.That(supplies.Medkits, Is.Zero);
            supplies.AddMedkits(1); health.TakeDamage(100); Assert.That(supplies.TryUseMedkit(), Is.False);
            Assert.That(supplies.Medkits, Is.EqualTo(1)); Assert.That(health.IsDead, Is.True);
        }
        [UnityTest]
        public IEnumerator PickupsRespectCoverAndReleasedControls()
        {
            yield return CreateArena(); var pickup = Spawn(SupplyKind.Medkit);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.transform.SetParent(arena.transform);
            wall.transform.position = actor.transform.position + new Vector3(0, 1, .5f); wall.transform.localScale = new Vector3(2, 2, .1f); Physics.SyncTransforms();
            yield return new WaitForSeconds(.15f); yield return F(); Assert.That(supplies.Medkits, Is.Zero);
            Object.Destroy(wall); yield return null; input.SetCursorCaptured(false); yield return F(); Assert.That(supplies.Medkits, Is.Zero);
            input.SetCursorCaptured(true); yield return new WaitForSeconds(.15f); yield return F(); Assert.That(supplies.Medkits, Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator ForestContainsBothSupplyStationsAndReadableInventory()
        {
            keyboard = InputSystem.AddDevice<Keyboard>(); InputSystem.AddDevice<Mouse>();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenes.Forest, new LoadSceneParameters(LoadSceneMode.Single));
            yield return new WaitForSeconds(.2f);
            var pickups = Object.FindObjectsByType<SupplyPickup>(FindObjectsSortMode.None);
            Assert.That(pickups.Count(p => p.Kind == SupplyKind.Ammo), Is.EqualTo(2)); Assert.That(pickups.Count(p => p.Kind == SupplyKind.Medkit), Is.EqualTo(2));
            var hud = Object.FindFirstObjectByType<PlayerSuppliesHUD>(); Assert.That(hud.supplies.Medkits, Is.Zero);
            Assert.That(hud.inventoryLabel.text, Does.Contain("0 / 3"));
            var medkit = pickups.OrderBy(p => p.transform.position.z).First(p => p.Kind == SupplyKind.Medkit);
            var player = hud.supplies.gameObject; var body = player.GetComponent<Rigidbody>();
            body.position = medkit.transform.position + new Vector3(0, -.65f, -1); body.linearVelocity = Vector3.zero; Physics.SyncTransforms();
            player.GetComponent<PlayerInputReader>().SetCursorCaptured(true);
            var ammoPickup = pickups.OrderBy(p => p.transform.position.z).First(p => p.Kind == SupplyKind.Ammo);
            Assert.That(ammoPickup.CanInteract(player), Is.False, "Ammo requires first picking up the weapon.");
            yield return new WaitForSeconds(.15f); yield return F();
            Assert.That(hud.supplies.Medkits, Is.EqualTo(1), "Actual station must be reachable with F despite its table collider.");
            medkit.gameObject.SetActive(true); // Restore the model only for the overview render below.
            foreach (var pickup in pickups)
                foreach (var renderer in pickup.GetComponentsInChildren<Renderer>())
                    Assert.That(renderer.sharedMaterials.All(m => m != null && m.shader.isSupported), Is.True);
            // Capture the actual station and the SDF inventory together.
            var target = pickups.OrderBy(p => p.transform.position.z).First().transform.position;
            var camera = Camera.main; camera.GetComponent<Unity.Cinemachine.CinemachineBrain>().enabled = false;
            camera.transform.position = target + new Vector3(1.3f, 1.1f, -1.8f); camera.transform.LookAt(target + new Vector3(.15f, 0, 0));
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas).ToArray();
            var targetTexture = new RenderTexture(1280, 720, 24); targetTexture.Create(); camera.targetTexture = targetTexture;
            foreach (var canvas in canvases)
            {
                canvas.GetComponent<CanvasScaler>().enabled = false; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera;
                canvas.planeDistance = camera.nearClipPlane + .001f; canvas.scaleFactor = 2f / 3;
            }
            yield return null; Canvas.ForceUpdateCanvases();
            foreach (var label in hud.GetComponentsInChildren<TMP_Text>()) { label.ForceMeshUpdate(); Assert.That(label.isTextOverflowing, Is.False, label.name); }
            camera.Render(); RenderTexture.active = targetTexture;
            var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false); pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
            Directory.CreateDirectory("Logs/Supplies"); File.WriteAllBytes("Logs/Supplies/Station.png", pixels.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = null; targetTexture.Release(); Object.Destroy(targetTexture); Object.Destroy(pixels);
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Object.Destroy(actor); Object.Destroy(arena); Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            var scene = SceneManager.GetActiveScene();
            if (scene.path == GameScenes.Forest) { SceneManager.SetActiveScene(SceneManager.CreateScene("AfterSupplyTest")); yield return SceneManager.UnloadSceneAsync(scene); }
            yield return null;
        }
    }
}
#endif
