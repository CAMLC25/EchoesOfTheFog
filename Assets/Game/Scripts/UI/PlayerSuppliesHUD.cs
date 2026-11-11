using EchoesOfTheFog.Combat;
using EchoesOfTheFog.Mission;
using EchoesOfTheFog.Player;
using TMPro;
using UnityEngine;

namespace EchoesOfTheFog.UI
{
    public sealed class PlayerSuppliesHUD : MonoBehaviour
    {
        public PlayerSupplies supplies;
        public HitscanRifle rifle;
        public TMP_Text inventoryLabel, feedbackLabel;
        public GameObject inventoryPanel;
        private float feedbackUntil;
        private ForestMission mission;
        private void OnEnable() { if (supplies != null) supplies.Feedback += OnFeedback; }
        private void Start() => mission = FindFirstObjectByType<ForestMission>();
        private void OnDisable() { if (supplies != null) supplies.Feedback -= OnFeedback; }
        private void OnFeedback(string message) { feedbackLabel.text = message; feedbackUntil = Time.unscaledTime + 2.5f; }
        private void Update()
        {
            bool completed = mission != null && mission.Objective == ForestObjective.Completed;
            inventoryPanel.SetActive(!completed);
            feedbackLabel.gameObject.SetActive(!completed && Time.unscaledTime < feedbackUntil);
            inventoryLabel.text = (rifle.IsEquipped ? $"ĐẠN DỰ TRỮ  {rifle.Reserve} / {rifle.ReserveCapacity}" : "NHẶT SÚNG ĐỂ NHẬN ĐẠN")
                + $"\nCỨU THƯƠNG  {supplies.Medkits} / {supplies.MedkitCapacity}\nE — Dùng cứu thương (+40 máu)";
        }
    }
}
