using EchoesOfTheFog.Core;
using EchoesOfTheFog.Mission;
using EchoesOfTheFog.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EchoesOfTheFog.UI
{
    public enum ForestPlayState { Playing, Paused, GameOver, Completed, Loading }

    public sealed class ForestGameFlow : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private Health health;
        [SerializeField] private ForestMission mission;
        [SerializeField] private GameObject pausePanel, gameOverPanel;
        [SerializeField] private Button resumeButton, pauseRestartButton, pauseMenuButton, retryButton, deathMenuButton;
        [SerializeField] private Canvas[] gameplayCanvases;
        private PlayerPrototypeHUD prototypeHUD;
        private bool ownsPause, oldHUDEnabled;
        private float previousTimeScale;
        public ForestPlayState State { get; private set; }

        private void Awake()
        {
            prototypeHUD = input.GetComponent<PlayerPrototypeHUD>();
            pausePanel.SetActive(false); gameOverPanel.SetActive(false);
        }
        private void OnEnable()
        {
            input.PauseRequested += TogglePause; health.Died += OnDeath;
            if (mission != null) mission.ObjectiveChanged += OnObjective;
            resumeButton.onClick.AddListener(Resume); pauseRestartButton.onClick.AddListener(Restart);
            pauseMenuButton.onClick.AddListener(ReturnToMenu); retryButton.onClick.AddListener(Restart); deathMenuButton.onClick.AddListener(ReturnToMenu);
        }
        private void OnDisable()
        {
            input.PauseRequested -= TogglePause; health.Died -= OnDeath;
            if (mission != null) mission.ObjectiveChanged -= OnObjective;
            resumeButton.onClick.RemoveListener(Resume); pauseRestartButton.onClick.RemoveListener(Restart);
            pauseMenuButton.onClick.RemoveListener(ReturnToMenu); retryButton.onClick.RemoveListener(Restart); deathMenuButton.onClick.RemoveListener(ReturnToMenu);
            RestoreTime();
        }
        public void TogglePause()
        {
            if (State == ForestPlayState.Paused) Resume();
            else if (State == ForestPlayState.Playing && !health.IsDead && (mission == null || mission.Objective != ForestObjective.Completed))
            {
                State = ForestPlayState.Paused; Freeze(); SetGameplayVisible(false);
                pausePanel.SetActive(true); Select(resumeButton);
            }
        }
        public void Resume()
        {
            if (State != ForestPlayState.Paused || health.IsDead) return;
            State = ForestPlayState.Playing; pausePanel.SetActive(false); RestoreTime(); SetGameplayVisible(true);
            input.SetControlsEnabled(true); input.SetCursorCaptured(true); input.SuppressFireUntilRelease();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
        }
        private void OnDeath()
        {
            if (State == ForestPlayState.Completed || State == ForestPlayState.Loading || State == ForestPlayState.GameOver) return;
            State = ForestPlayState.GameOver; Freeze(); SetGameplayVisible(false);
            pausePanel.SetActive(false); gameOverPanel.SetActive(true); Select(retryButton);
        }
        private void OnObjective(ForestObjective objective)
        {
            if (objective != ForestObjective.Completed || State == ForestPlayState.GameOver || State == ForestPlayState.Loading) return;
            State = ForestPlayState.Completed; pausePanel.SetActive(false); gameOverPanel.SetActive(false); SetGameplayVisible(true); Freeze();
        }
        private void Freeze()
        {
            if (!ownsPause)
            {
                previousTimeScale = Time.timeScale; ownsPause = true;
                oldHUDEnabled = prototypeHUD != null && prototypeHUD.enabled;
            }
            Time.timeScale = 0; input.SetControlsEnabled(false);
            if (prototypeHUD != null) prototypeHUD.enabled = false;
        }
        private void RestoreTime() { if (ownsPause) { Time.timeScale = previousTimeScale; ownsPause = false; } }
        private void SetGameplayVisible(bool visible)
        {
            foreach (var canvas in gameplayCanvases) if (canvas != null) canvas.enabled = visible;
            if (prototypeHUD != null) prototypeHUD.enabled = visible && oldHUDEnabled;
        }
        public void Restart() => Load(SceneManager.GetActiveScene().path);
        public void ContinueToVillage() { if (State == ForestPlayState.Completed) Load(GameScenes.Village); }
        public void ReturnToMenu() => Load(GameScenes.MainMenu);
        private void Load(string path)
        {
            if (State == ForestPlayState.Playing || State == ForestPlayState.Loading || !Application.CanStreamedLevelBeLoaded(path)) return;
            State = ForestPlayState.Loading; input.SetControlsEnabled(false);
            foreach (var button in GetComponentsInChildren<Button>(true)) button.interactable = false;
            RestoreTime(); SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
        }
        private static void Select(Button button) { if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(button.gameObject); }
    }
}
