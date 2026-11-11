#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using EchoesOfTheFog.Core;
using EchoesOfTheFog.Enemy;
using EchoesOfTheFog.Interaction;
using EchoesOfTheFog.Mission;
using EchoesOfTheFog.Player;
using EchoesOfTheFog.UI;
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
    public sealed class VillageMapTests : InputTestFixture
    {
        private Keyboard keyboard;
        private VillageRoute route;
        private PlayerInputReader input;
        private IEnumerator Open()
        {
            keyboard = InputSystem.AddDevice<Keyboard>(); InputSystem.AddDevice<Mouse>();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenes.Village, new LoadSceneParameters(LoadSceneMode.Single));
            yield return new WaitForSeconds(.6f);
            route = Object.FindFirstObjectByType<VillageRoute>(); input = route.player; input.SetCursorCaptured(true);
        }
        private void Isolate()
        { foreach (var runner in Object.FindObjectsByType<RunnerBrain>(FindObjectsSortMode.None)) runner.gameObject.SetActive(false); }
        [UnityTest]
        public IEnumerator VillageStartsArmedWithFourValidRunnersAndClosedCanalCrossing()
        {
            yield return Open(); Assert.That(input.GetComponent<PlayerEquipment>().HasWeapon, Is.True); Assert.That(input.GetComponent<Health>().CurrentHealth, Is.EqualTo(100));
            Assert.That(route.PowerOn, Is.False); Assert.That(route.BridgeOpen, Is.False); Assert.That(route.bridgeFloor.enabled, Is.False);
            var runners = Object.FindObjectsByType<RunnerBrain>(FindObjectsSortMode.None); Assert.That(runners.Length, Is.EqualTo(4));
            foreach (var runner in runners) { Assert.That(runner.NavigationReady, Is.True); Assert.That(runner.Target, Is.EqualTo(input.GetComponent<Health>())); }
            Assert.That(NavMesh.SamplePosition(new Vector3(0, 0, 30), out var from, 2, NavMesh.AllAreas), Is.True);
            Assert.That(NavMesh.SamplePosition(new Vector3(0, 0, 50), out var to, 2, NavMesh.AllAreas), Is.True);
            var path = new NavMeshPath(); NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path);
            Assert.That(path.status, Is.Not.EqualTo(NavMeshPathStatus.PathComplete), "Raised bridge must prevent crossing by AI.");
            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                foreach (var material in renderer.sharedMaterials) Assert.That(material != null && material.shader != null && material.shader.isSupported, Is.True, renderer.name);
            yield return Capture("GameView_Start");
        }
        [UnityTest]
        public IEnumerator FPowerSwitchLowersBridgeAndPlayerCanWalkAcrossTheActualDeck()
        {
            yield return Open(); Isolate(); var body = input.GetComponent<Rigidbody>();
            body.position = route.generatorTarget.position + new Vector3(0, -1.1f, -1.2f); body.linearVelocity = Vector3.zero; Physics.SyncTransforms();
            yield return new WaitForSeconds(.15f); Press(keyboard.fKey); yield return null; Release(keyboard.fKey); yield return null;
            Assert.That(route.PowerOn, Is.True); Assert.That(route.ActivatePower(), Is.False);
            var flow = Object.FindFirstObjectByType<ForestGameFlow>(); flow.TogglePause(); float rotation = route.bridgePivot.localEulerAngles.x;
            yield return new WaitForSecondsRealtime(.2f); Assert.That(route.bridgePivot.localEulerAngles.x, Is.EqualTo(rotation).Within(.001f)); flow.Resume();
            yield return new WaitForSeconds(2.8f); Assert.That(route.BridgeOpen, Is.True); Assert.That(route.bridgeFloor.enabled, Is.True); Assert.That(route.bridgeBlocker.enabled, Is.False);
            yield return new WaitForSeconds(.3f);
            Assert.That(NavMesh.SamplePosition(new Vector3(0, 0, 30), out var from, 2, NavMesh.AllAreas), Is.True);
            Assert.That(NavMesh.SamplePosition(route.clinicTarget.position, out var to, 2, NavMesh.AllAreas), Is.True);
            var path = new NavMeshPath(); Assert.That(NavMesh.CalculatePath(from.position, to.position, NavMesh.AllAreas, path), Is.True); Assert.That(path.status, Is.EqualTo(NavMeshPathStatus.PathComplete));
            body.position = new Vector3(0, .04f, 31); body.linearVelocity = Vector3.zero; input.GetComponent<PlayerCameraRig>().SetViewAngles(0, 12); Physics.SyncTransforms();
            Press(keyboard.wKey); float deadline = Time.time + 10;
            while (body.position.z < 50 && Time.time < deadline && !input.GetComponent<Health>().IsDead) yield return null;
            Release(keyboard.wKey); Assert.That(body.position.z, Is.GreaterThan(50)); Assert.That(body.position.y, Is.GreaterThan(-.1f)); Assert.That(input.GetComponent<Health>().CurrentHealth, Is.EqualTo(100));
            body.position = new Vector3(0, .04f, 90); body.linearVelocity = Vector3.zero; Physics.SyncTransforms();
            yield return new WaitForSeconds(.4f); Assert.That(route.AtClinic, Is.True);
            yield return Capture("GameView_Clinic");
        }
        [UnityTest]
        public IEnumerator CanalFallShowsGameOverAndRetryStaysInVillage()
        {
            yield return Open(); Isolate(); var body = input.GetComponent<Rigidbody>(); body.position = new Vector3(10, -1.4f, 40); body.linearVelocity = Vector3.zero; Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.25f);
            var flow = Object.FindFirstObjectByType<ForestGameFlow>(); Assert.That(flow.State, Is.EqualTo(ForestPlayState.GameOver)); Assert.That(Time.timeScale, Is.Zero);
            flow.Restart(); yield return new WaitForSecondsRealtime(.7f);
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(GameScenes.Village)); Assert.That(Object.FindFirstObjectByType<VillageRoute>().PowerOn, Is.False); Assert.That(Time.timeScale, Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator ForestCompletionButtonLoadsPlayableVillage()
        {
            keyboard = InputSystem.AddDevice<Keyboard>(); InputSystem.AddDevice<Mouse>();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenes.Forest, new LoadSceneParameters(LoadSceneMode.Single)); yield return new WaitForSeconds(.2f);
            var equipment = Object.FindFirstObjectByType<PlayerEquipment>(); equipment.TryEquipWeapon(); Object.FindFirstObjectByType<ForestMission>().ReachVillage(equipment); yield return null;
            var button = GameObject.Find("ContinueVillageButton").GetComponent<Button>(); Assert.That(button, Is.Not.Null); button.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.7f);
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(GameScenes.Village)); Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(Object.FindFirstObjectByType<VillageRoute>().player.CanControl, Is.True);
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            var scene = SceneManager.GetActiveScene(); SceneManager.SetActiveScene(SceneManager.CreateScene("AfterVillageTest")); yield return SceneManager.UnloadSceneAsync(scene); Time.timeScale = 1;
        }
        private static IEnumerator Capture(string name)
        {
            var camera = Camera.main; var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c => c.isRootCanvas).ToArray();
            var texture = new RenderTexture(1280, 720, 24); texture.Create(); var pixels = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            var previous = RenderTexture.active;
            try
            {
                camera.targetTexture = texture;
                foreach (var canvas in canvases)
                {
                    canvas.GetComponent<CanvasScaler>().enabled = false; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera;
                    canvas.planeDistance = camera.nearClipPlane + .001f; canvas.scaleFactor = 2f / 3;
                }
                yield return null; Canvas.ForceUpdateCanvases();
                foreach (var label in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
                { label.ForceMeshUpdate(); Assert.That(label.isTextOverflowing, Is.False, label.text); }
                camera.Render(); RenderTexture.active = texture; pixels.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); pixels.Apply();
                Directory.CreateDirectory("Logs/Village"); File.WriteAllBytes("Logs/Village/" + name + ".png", pixels.EncodeToPNG());
            }
            finally
            {
                foreach (var canvas in canvases) { canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.GetComponent<CanvasScaler>().enabled = true; }
                camera.targetTexture = null; RenderTexture.active = previous; texture.Release(); Object.Destroy(texture); Object.Destroy(pixels);
            }
        }
    }
}
#endif
