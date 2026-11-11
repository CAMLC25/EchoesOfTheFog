#if UNITY_EDITOR
using System.Collections;
using System.IO;
using EchoesOfTheFog.Combat;
using EchoesOfTheFog.Core;
using EchoesOfTheFog.Enemy;
using EchoesOfTheFog.Mission;
using EchoesOfTheFog.Player;
using EchoesOfTheFog.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace EchoesOfTheFog.Tests
{
    public sealed class ForestGameFlowTests : InputTestFixture
    {
        private Keyboard keyboard;
        private Mouse mouse;
        private ForestGameFlow flow;
        private PlayerInputReader input;
        private Health health;
        private IEnumerator Open()
        {
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenes.Forest, new LoadSceneParameters(LoadSceneMode.Single));
            yield return new WaitForSecondsRealtime(.25f);
            flow = Object.FindFirstObjectByType<ForestGameFlow>(); input = Object.FindFirstObjectByType<PlayerInputReader>(); health = input.GetComponent<Health>();
            input.SetCursorCaptured(true); Assert.That(flow.State, Is.EqualTo(ForestPlayState.Playing));
        }
        private IEnumerator Escape()
        { Press(keyboard.escapeKey); yield return null; Release(keyboard.escapeKey); yield return null; }
        [UnityTest]
        public IEnumerator EscapeFreezesGameplayAndResumeRestoresTimeWithoutAccidentalShot()
        {
            yield return Open(); input.GetComponent<PlayerEquipment>().TryEquipWeapon();
            input.GetComponent<PlayerSupplies>().AddMedkits(1); health.TakeDamage(20);
            Time.timeScale = .65f; yield return Escape();
            Assert.That(flow.State, Is.EqualTo(ForestPlayState.Paused)); Assert.That(Time.timeScale, Is.Zero);
            Assert.That(input.CanControl, Is.False); Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("ResumeButton"));
            var runner = Object.FindFirstObjectByType<RunnerBrain>(); Vector3 enemyPosition = runner.transform.position, playerPosition = input.transform.position;
            var rifle = input.GetComponent<HitscanRifle>(); int loaded = rifle.Loaded;
            Press(keyboard.wKey); Press(keyboard.eKey); Press(mouse.leftButton);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(Vector3.Distance(enemyPosition, runner.transform.position), Is.LessThan(.001f));
            Assert.That(Vector3.Distance(playerPosition, input.transform.position), Is.LessThan(.001f));
            Assert.That(health.CurrentHealth, Is.EqualTo(80)); Assert.That(rifle.Loaded, Is.EqualTo(loaded));
            yield return Capture("Pause");
            Release(keyboard.wKey); Release(keyboard.eKey);
            flow.transform.Find("Pause/Card/ResumeButton").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(Time.timeScale, Is.EqualTo(.65f).Within(.001f)); Assert.That(input.CanControl, Is.True);
            Assert.That(rifle.Loaded, Is.EqualTo(loaded), "The resume click must not fire a shot.");
            Release(mouse.leftButton); yield return null; Press(mouse.leftButton); yield return null; Release(mouse.leftButton); yield return null;
            Assert.That(rifle.Loaded, Is.EqualTo(loaded - 1));
            yield return Escape(); Assert.That(flow.State, Is.EqualTo(ForestPlayState.Paused));
            yield return Escape(); Assert.That(flow.State, Is.EqualTo(ForestPlayState.Playing));
        }
        [UnityTest]
        public IEnumerator DeathOverridesPauseEscapeCannotResumeAndRetryResetsTheLevel()
        {
            yield return Open(); input.GetComponent<PlayerEquipment>().TryEquipWeapon(); input.GetComponent<PlayerSupplies>().AddMedkits(2);
            yield return Escape(); health.TakeDamage(100); yield return null;
            Assert.That(flow.State, Is.EqualTo(ForestPlayState.GameOver)); Assert.That(Time.timeScale, Is.Zero);
            Assert.That(flow.transform.Find("Pause").gameObject.activeSelf, Is.False);
            Assert.That(flow.transform.Find("Game Over").gameObject.activeSelf, Is.True);
            Assert.That(EventSystem.current.currentSelectedGameObject.name, Is.EqualTo("RetryButton"));
            yield return Escape(); Assert.That(flow.State, Is.EqualTo(ForestPlayState.GameOver)); Assert.That(input.CanControl, Is.False);
            yield return Capture("GameOver");
            Press(keyboard.enterKey); yield return null; Release(keyboard.enterKey);
            yield return new WaitForSecondsRealtime(.65f);
            var restarted = Object.FindFirstObjectByType<ForestGameFlow>(); var player = Object.FindFirstObjectByType<PlayerInputReader>();
            Assert.That(restarted.State, Is.EqualTo(ForestPlayState.Playing)); Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(player.GetComponent<Health>().CurrentHealth, Is.EqualTo(100)); Assert.That(player.GetComponent<PlayerEquipment>().HasWeapon, Is.False);
            Assert.That(player.GetComponent<PlayerSupplies>().Medkits, Is.Zero); Assert.That(player.CanControl, Is.True);
            Assert.That(Object.FindObjectsByType<RunnerBrain>(FindObjectsSortMode.None).Length, Is.EqualTo(5));
        }
        [UnityTest]
        public IEnumerator PauseCanReturnToMenuWithoutLeavingTimeFrozen()
        {
            yield return Open(); yield return Escape();
            flow.transform.Find("Pause/Card/PauseMenuButton").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(.5f);
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(GameScenes.MainMenu)); Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(Object.FindFirstObjectByType<MainMenuController>(), Is.Not.Null); Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None));
        }
        [UnityTest]
        public IEnumerator DeathCanReturnToMenuAndCompletedLevelCannotBeResumedWithEscape()
        {
            yield return Open(); var mission = Object.FindFirstObjectByType<ForestMission>(); input.GetComponent<PlayerEquipment>().TryEquipWeapon();
            Assert.That(mission.ReachVillage(input.GetComponent<PlayerEquipment>()), Is.True); yield return null;
            Assert.That(flow.State, Is.EqualTo(ForestPlayState.Completed)); yield return Escape();
            Assert.That(flow.State, Is.EqualTo(ForestPlayState.Completed)); Assert.That(Time.timeScale, Is.Zero);
            flow.Restart(); yield return new WaitForSecondsRealtime(.6f);
            flow = Object.FindFirstObjectByType<ForestGameFlow>(); input = Object.FindFirstObjectByType<PlayerInputReader>();
            input.GetComponent<Health>().TakeDamage(100); yield return null;
            flow.transform.Find("Game Over/Card/DeathMenuButton").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(.5f);
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(GameScenes.MainMenu)); Assert.That(Time.timeScale, Is.EqualTo(1));
        }
        private IEnumerator Capture(string name)
        {
            var canvas = flow.GetComponent<Canvas>(); var scaler = canvas.GetComponent<CanvasScaler>(); var camera = Camera.main;
            var target = new RenderTexture(1360, 552, 24); target.Create(); var pixels = new Texture2D(1360, 552, TextureFormat.RGB24, false);
            var active = RenderTexture.active;
            try
            {
                camera.targetTexture = target; scaler.enabled = false; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera;
                canvas.planeDistance = camera.nearClipPlane + .001f; canvas.scaleFactor = Mathf.Sqrt(1360f / 1920 * 552f / 1080);
                yield return null; Canvas.ForceUpdateCanvases();
                foreach (var label in canvas.GetComponentsInChildren<TMP_Text>()) { label.ForceMeshUpdate(); Assert.That(label.isTextOverflowing, Is.False, label.text); }
                camera.Render(); RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, 1360, 552), 0, 0); pixels.Apply();
                Directory.CreateDirectory("Logs/Pause"); File.WriteAllBytes("Logs/Pause/" + name + ".png", pixels.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay; scaler.enabled = true; camera.targetTexture = null; RenderTexture.active = active;
                target.Release(); Object.Destroy(target); Object.Destroy(pixels);
            }
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            var scene = SceneManager.GetActiveScene(); SceneManager.SetActiveScene(SceneManager.CreateScene("AfterGameFlow")); yield return SceneManager.UnloadSceneAsync(scene);
            Time.timeScale = 1;
        }
    }
}
#endif
