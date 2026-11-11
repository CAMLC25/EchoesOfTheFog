using EchoesOfTheFog.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EchoesOfTheFog.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private GameObject mainPanel, guidePanel;
        [SerializeField] private GameObject storyPanel;
        [SerializeField] private Button startButton, guideButton, backButton;
        [SerializeField] private Button continueButton, skipButton;
        [SerializeField] private TMP_Text startLabel, statusLabel;
        public bool IsStarting { get; private set; }
        public bool GuideVisible => guidePanel != null && guidePanel.activeSelf;
        public bool StoryVisible => storyPanel != null && storyPanel.activeSelf;

        private void Awake()
        {
            Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            mainPanel.SetActive(true); guidePanel.SetActive(false);
            if (storyPanel != null) storyPanel.SetActive(false);
        }
        private void OnEnable()
        {
            startButton.onClick.AddListener(StartGame); guideButton.onClick.AddListener(OpenGuide); backButton.onClick.AddListener(CloseGuide);
            if (continueButton != null) continueButton.onClick.AddListener(EnterForest);
            if (skipButton != null) skipButton.onClick.AddListener(EnterForest);
        }
        private void Start() => Select(startButton);
        private void OnDisable()
        {
            startButton.onClick.RemoveListener(StartGame); guideButton.onClick.RemoveListener(OpenGuide); backButton.onClick.RemoveListener(CloseGuide);
            if (continueButton != null) continueButton.onClick.RemoveListener(EnterForest);
            if (skipButton != null) skipButton.onClick.RemoveListener(EnterForest);
        }
        private void Update()
        {
            bool cancel = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
            if (cancel && GuideVisible) CloseGuide();
            else if (cancel && StoryVisible && !IsStarting) { storyPanel.SetActive(false); mainPanel.SetActive(true); Select(startButton); }
        }
        public void OpenGuide()
        {
            if (IsStarting || StoryVisible) return;
            mainPanel.SetActive(false); guidePanel.SetActive(true); Select(backButton);
        }
        public void CloseGuide()
        {
            if (IsStarting) return;
            guidePanel.SetActive(false); mainPanel.SetActive(true); Select(guideButton);
        }
        public void StartGame()
        {
            if (IsStarting || GuideVisible || StoryVisible) return;
            if (storyPanel == null) { Debug.LogError("Main menu needs its story intro panel.", this); return; }
            mainPanel.SetActive(false); storyPanel.SetActive(true); Select(continueButton);
        }
        public void EnterForest()
        {
            if (IsStarting || !StoryVisible) return;
            if (!Application.CanStreamedLevelBeLoaded(GameScenes.Forest))
            {
                storyPanel.SetActive(false); mainPanel.SetActive(true); Select(startButton);
                statusLabel.text = "Không thể mở màn chơi. Vui lòng khởi động lại trò chơi.";
                return;
            }
            IsStarting = true; startButton.interactable = false; guideButton.interactable = false;
            continueButton.interactable = false; skipButton.interactable = false;
            continueButton.GetComponentInChildren<TMP_Text>().text = "ĐANG VÀO RỪNG…";
            startLabel.text = "ĐANG VÀO RỪNG…"; statusLabel.text = "";
            SceneManager.LoadSceneAsync(GameScenes.Forest, LoadSceneMode.Single);
        }
        private static void Select(Button button)
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }
}
