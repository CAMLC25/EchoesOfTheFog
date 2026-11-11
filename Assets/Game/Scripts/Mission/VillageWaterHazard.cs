using EchoesOfTheFog.Core;
using UnityEngine;

namespace EchoesOfTheFog.Mission
{
    public sealed class VillageWaterHazard : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other) => Check(other);
        private void OnTriggerStay(Collider other) => Check(other);
        private void Check(Collider other)
        {
            var health = other.GetComponentInParent<Health>();
            if (health != null && health.CompareTag("Player") && !health.IsDead) health.TakeDamage(health.CurrentHealth);
        }
    }
}
