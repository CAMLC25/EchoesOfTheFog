using EchoesOfTheFog.Player;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

namespace EchoesOfTheFog.Mission
{
    public sealed class VillageRoute : MonoBehaviour
    {
        public PlayerInputReader player;
        public Transform bridgePivot, generatorTarget, clinicTarget;
        public NavMeshObstacle bridgeBlocker;
        public Collider bridgeFloor;
        public TMP_Text objectiveLabel, detailLabel, interactionLabel;
        public GameObject interactionPanel;
        public Light[] restoredLights;
        public bool PowerOn { get; private set; }
        public bool BridgeOpen { get; private set; }
        public bool AtClinic { get; private set; }
        private float lowering;
        private void Awake()
        {
            bridgePivot.localRotation = Quaternion.Euler(-78, 0, 0); bridgeFloor.enabled = false;
            foreach (var light in restoredLights) light.enabled = false;
        }
        public bool ActivatePower()
        {
            if (PowerOn || !player.CanControl || player.GetComponent<Core.Health>().IsDead) return false;
            PowerOn = true;
            foreach (var light in restoredLights) light.enabled = true;
            return true;
        }
        private void Update()
        {
            if (PowerOn && !BridgeOpen)
            {
                lowering = Mathf.Min(1, lowering + Time.deltaTime / 2.5f);
                bridgePivot.localRotation = Quaternion.Euler(Mathf.Lerp(-78, 0, Mathf.SmoothStep(0, 1, lowering)), 0, 0);
                if (lowering >= 1) { BridgeOpen = true; bridgeFloor.enabled = true; bridgeBlocker.enabled = false; }
            }
            if (BridgeOpen && Vector3.Distance(player.transform.position, clinicTarget.position) < 3) AtClinic = true;
            var target = PowerOn ? clinicTarget : generatorTarget;
            objectiveLabel.text = AtClinic ? "Đã tới trạm y tế Blackwood" : !PowerOn ? "Tìm và bật nguồn điện hạ cầu" : !BridgeOpen ? "Đang hạ cầu qua kênh" : "Qua cầu, tìm trạm y tế";
            detailLabel.text = AtClinic ? "Khu trú ẩn của người sống sót" : $"{(PowerOn ? "Trạm y tế" : "Sân máy phát")}  /  {Mathf.CeilToInt(Vector3.Distance(player.transform.position, target.position))} m";
            string prompt = player.GetComponent<Interaction.PlayerInteractor>().CurrentPrompt;
            interactionPanel.SetActive(!string.IsNullOrEmpty(prompt)); interactionLabel.text = prompt;
        }
    }
}
