using EchoesOfTheFog.Core;
using EchoesOfTheFog.Interaction;
using EchoesOfTheFog.Mission;
using EchoesOfTheFog.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EchoesOfTheFog.UI
{
    public sealed class ForestMissionHUD : MonoBehaviour
    {
        [SerializeField] private ForestMission mission;
        [SerializeField] private PlayerInteractor interactor;
        [SerializeField] private TMP_Text objectiveLabel, detailLabel, interactionLabel;
        [SerializeField] private GameObject interactionPanel, completionPanel;
        [SerializeField] private Button menuButton;
        private bool paused, loading;
        private float previousTimeScale;
        private ForestGameFlow flow;
        private void Awake() => flow = FindFirstObjectByType<ForestGameFlow>();
        private void OnEnable() { mission.ObjectiveChanged += OnObjective; menuButton.onClick.AddListener(ReturnToMenu); }
        private void Start() { completionPanel.SetActive(false); OnObjective(mission.Objective); }
        private void OnDisable()
        {
            if (mission != null) mission.ObjectiveChanged -= OnObjective;
            if (menuButton != null) menuButton.onClick.RemoveListener(ReturnToMenu);
            if (paused) { Time.timeScale = previousTimeScale; paused = false; }
        }
        private void Update()
        {
            if (mission.Objective == ForestObjective.Completed) return;
            string prompt = interactor.CurrentPrompt; interactionPanel.SetActive(!string.IsNullOrEmpty(prompt)); interactionLabel.text = prompt;
            var target = mission.CurrentTarget;
            int distance = target == null ? 0 : Mathf.CeilToInt(Vector3.Distance(mission.Player.transform.position, target.position));
            detailLabel.text = mission.Objective == ForestObjective.FindWeapon ? $"Tìm ánh đèn bên lề đường  /  {distance} m" : $"Đi theo đường rừng về phía làng  /  {distance} m";
        }
        private void OnObjective(ForestObjective objective)
        {
            objectiveLabel.text = mission.ObjectiveText;
            if (objective != ForestObjective.Completed) return;
            detailLabel.text = "Chặng bìa rừng đã hoàn thành"; interactionPanel.SetActive(false); completionPanel.SetActive(true);
            mission.Player.GetComponent<PlayerInputReader>().SetControlsEnabled(false);
            var prototypeHUD = mission.Player.GetComponent<PlayerPrototypeHUD>();
            if (prototypeHUD != null) prototypeHUD.enabled = false;
            if (flow == null) { previousTimeScale = Time.timeScale; paused = true; Time.timeScale = 0; }
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(menuButton.gameObject);
        }
        private void ReturnToMenu()
        {
            if (flow != null) { flow.ReturnToMenu(); return; }
            if (loading) return; loading = true; menuButton.interactable = false;
            if (paused) { Time.timeScale = previousTimeScale; paused = false; }
            SceneManager.LoadSceneAsync(GameScenes.MainMenu, LoadSceneMode.Single);
        }
    }
}
