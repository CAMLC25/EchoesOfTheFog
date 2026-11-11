#if UNITY_EDITOR
using System.Collections;
using System.IO;
using System.Linq;
using EchoesOfTheFog.Core;
using EchoesOfTheFog.Enemy;
using EchoesOfTheFog.Player;
using EchoesOfTheFog.UI;
using TMPro;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace EchoesOfTheFog.Tests
{
    public sealed class MainMenuTests : InputTestFixture
    {
        private Keyboard keyboard;
        private Mouse mouse;
        private MainMenuController menu;

        private IEnumerator OpenMenu()
        {
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(GameScenes.MainMenu, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null; yield return null;
            menu = Object.FindFirstObjectByType<MainMenuController>(); Assert.That(menu, Is.Not.Null);
            Assert.That(Cursor.lockState, Is.EqualTo(CursorLockMode.None)); Assert.That(Cursor.visible, Is.True);
        }
        private static Button FindButton(string name) => Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(button => button.name == name);
        private IEnumerator Click(Button button)
        {
            Canvas.ForceUpdateCanvases();
            var rect = button.GetComponent<RectTransform>();
            Set(mouse.position, RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)));
            yield return null; Press(mouse.leftButton); yield return null; Release(mouse.leftButton); yield return null; yield return null;
        }

        [UnityTest]
        public IEnumerator GuideOpensByMouseClosesByEscapeAndBackButton()
        {
            yield return OpenMenu();
            Assert.That(Object.FindObjectsByType<PlayerMotor>(FindObjectsSortMode.None), Is.Empty);
            yield return Capture("Menu_1920x1080", 1920, 1080);
            yield return Capture("Menu_4x3", 1280, 960);
            yield return Capture("Menu_Window_1360x552", 1360, 552);
            yield return Click(FindButton("GuideButton"));
            Assert.That(menu.GuideVisible, Is.True); Assert.That(FindButton("StartButton").gameObject.activeInHierarchy, Is.False);
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(FindButton("BackButton").gameObject));
            yield return Capture("Guide_1920x1080", 1920, 1080);
            yield return Capture("Guide_Window_1360x552", 1360, 552);
            Press(keyboard.escapeKey); yield return null; Release(keyboard.escapeKey); yield return null;
            Assert.That(menu.GuideVisible, Is.False);
            yield return Click(FindButton("GuideButton")); Assert.That(menu.GuideVisible, Is.True);
            yield return Click(FindButton("BackButton")); Assert.That(menu.GuideVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator KeyboardNavigationCanOpenGuideAndReturn()
        {
            yield return OpenMenu();
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(FindButton("StartButton").gameObject));
            Press(keyboard.downArrowKey); yield return null; Release(keyboard.downArrowKey); yield return null;
            Assert.That(EventSystem.current.currentSelectedGameObject, Is.EqualTo(FindButton("GuideButton").gameObject));
            Press(keyboard.enterKey); yield return null; Release(keyboard.enterKey); yield return null;
            Assert.That(menu.GuideVisible, Is.True);
            Press(keyboard.enterKey); yield return null; Release(keyboard.enterKey); yield return null;
            Assert.That(menu.GuideVisible, Is.False);
        }

        [UnityTest]
        public IEnumerator StartButtonLoadsForestWithOnePlayerAndUsableControls()
        {
            yield return OpenMenu();
            yield return Click(FindButton("StartButton"));
            Assert.That(menu.StoryVisible, Is.True);
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(GameScenes.MainMenu));
            Assert.That(Object.FindObjectsByType<PlayerMotor>(FindObjectsSortMode.None), Is.Empty);
            yield return Capture("Story_1920x1080", 1920, 1080);
            yield return Capture("Story_Window_1360x552", 1360, 552);
            yield return Click(FindButton("ContinueStoryButton"));
            float timeout = Time.realtimeSinceStartup + 8;
            while (SceneManager.GetActiveScene().path != GameScenes.Forest && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(GameScenes.Forest));
            yield return new WaitForSeconds(0.3f);
            Assert.That(Object.FindObjectsByType<MainMenuController>(FindObjectsSortMode.None), Is.Empty);
            var players = Object.FindObjectsByType<PlayerMotor>(FindObjectsSortMode.None);
            Assert.That(players.Length, Is.EqualTo(1)); Assert.That(Object.FindObjectsByType<RunnerBrain>(FindObjectsSortMode.None).Length, Is.EqualTo(5));
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Count(listener => listener.enabled), Is.EqualTo(1));
            Assert.That(players[0].GetComponent<PlayerInputReader>().CanControl, Is.True);
            var body = players[0].GetComponent<Rigidbody>(); Vector3 position = body.position;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W)); yield return new WaitForSeconds(0.4f);
            Assert.That(Vector3.ProjectOnPlane(body.position - position, Vector3.up).magnitude, Is.GreaterThan(0.5f));
        }

        [UnityTest]
        public IEnumerator StoryCanBeCancelledAndSkipped()
        {
            yield return OpenMenu(); yield return Click(FindButton("StartButton"));
            Assert.That(menu.StoryVisible, Is.True);
            Press(keyboard.escapeKey); yield return null; Release(keyboard.escapeKey); yield return null;
            Assert.That(menu.StoryVisible, Is.False); Assert.That(FindButton("StartButton").gameObject.activeInHierarchy, Is.True);
            yield return Click(FindButton("StartButton")); Assert.That(menu.StoryVisible, Is.True);
            yield return Click(FindButton("SkipStoryButton"));
            float timeout = Time.realtimeSinceStartup + 8;
            while (SceneManager.GetActiveScene().path != GameScenes.Forest && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(SceneManager.GetActiveScene().path, Is.EqualTo(GameScenes.Forest));
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            var current = SceneManager.GetActiveScene();
            if (current.path == GameScenes.MainMenu || current.path == GameScenes.Forest)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("AfterMainMenuTest"));
                yield return SceneManager.UnloadSceneAsync(current);
            }
        }

        private static IEnumerator Capture(string name, int width, int height)
        {
            var canvas = Object.FindFirstObjectByType<Canvas>(); var scaler = canvas.GetComponent<CanvasScaler>();
            var previousMode = canvas.renderMode; var previousCamera = canvas.worldCamera; float previousPlane = canvas.planeDistance, previousScale = canvas.scaleFactor;
            bool scalerEnabled = scaler.enabled;
            var item = new GameObject("UI capture camera"); var camera = item.AddComponent<Camera>(); camera.enabled = false;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            var target = new RenderTexture(width, height, 24); target.Create(); camera.targetTexture = target;
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false); var previousActive = RenderTexture.active;
            try
            {
                scaler.enabled = false; canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                canvas.scaleFactor = Mathf.Sqrt((width / 1920f) * (height / 1080f));
                Canvas.ForceUpdateCanvases(); yield return null; yield return null; Canvas.ForceUpdateCanvases();
                Assert.That(canvas.GetComponentsInChildren<Text>(true), Is.Empty, "Menu should no longer contain bitmap Legacy Text.");
                foreach (var label in canvas.GetComponentsInChildren<TMP_Text>().Where(label => label.gameObject.activeInHierarchy))
                {
                    label.ForceMeshUpdate();
                    Assert.That(label.isTextOverflowing, Is.False, $"Text overflows in {name}: {label.name}: {label.text}");
                    if (label.name == "Label" || label.name == "Key") Assert.That(label.textInfo.lineCount, Is.EqualTo(1), "Button/key labels must stay on one line.");
                    foreach (char character in label.text)
                        if (!char.IsWhiteSpace(character)) Assert.That(label.font.HasCharacter(character), Is.True, "Missing Vietnamese glyph: " + character);
                }
                foreach (var button in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
                {
                    var corners = new Vector3[4]; button.GetComponent<RectTransform>().GetWorldCorners(corners);
                    foreach (var corner in corners)
                    {
                        Vector3 point = camera.WorldToViewportPoint(corner);
                        Assert.That(point.x, Is.InRange(0f, 1f), "A menu button is outside the viewport at " + name);
                        Assert.That(point.y, Is.InRange(0f, 1f), "A menu button is outside the viewport at " + name);
                    }
                }
                camera.Render(); RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
                Directory.CreateDirectory("Logs/MainMenu"); File.WriteAllBytes("Logs/MainMenu/" + name + ".png", pixels.EncodeToPNG());
            }
            finally
            {
                canvas.renderMode = previousMode; canvas.worldCamera = previousCamera; canvas.planeDistance = previousPlane; canvas.scaleFactor = previousScale; scaler.enabled = scalerEnabled;
                camera.targetTexture = null; RenderTexture.active = previousActive; target.Release(); Object.Destroy(target); Object.Destroy(pixels); Object.Destroy(item);
                Canvas.ForceUpdateCanvases();
            }
            yield return null;
        }
    }
}
#endif
