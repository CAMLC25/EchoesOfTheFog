using EchoesOfTheFog.Player;
using UnityEngine;

namespace EchoesOfTheFog.Mission
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class ForestExitTrigger : MonoBehaviour
    {
        [SerializeField] private ForestMission mission;
        private void OnTriggerEnter(Collider other) => Check(other);
        private void OnTriggerStay(Collider other) => Check(other);
        private void Check(Collider other)
        {
            var equipment = other.GetComponentInParent<PlayerEquipment>();
            if (equipment != null) mission.ReachVillage(equipment);
        }
    }
}
