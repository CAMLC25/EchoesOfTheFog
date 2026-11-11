using EchoesOfTheFog.Core;
using EchoesOfTheFog.Player;
using UnityEngine;

namespace EchoesOfTheFog.Interaction
{
    public sealed class WeaponPickup : MonoBehaviour, IInteractable
    {
        [SerializeField, Min(0.1f)] private float pickupRange = 2.2f;
        private bool taken;
        public string InteractionPrompt => "Nhấn F để nhặt súng ức chế";
        public bool CanInteract(GameObject interactor)
        {
            if (taken || !isActiveAndEnabled || interactor == null) return false;
            var equipment = interactor.GetComponent<PlayerEquipment>(); var health = interactor.GetComponent<Health>();
            var input = interactor.GetComponent<PlayerInputReader>();
            return equipment != null && !equipment.HasWeapon && health != null && !health.IsDead && input != null && input.CanControl
                && Vector3.Distance(transform.position + Vector3.up * 0.25f, interactor.transform.position + Vector3.up) <= pickupRange;
        }
        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor) || !interactor.GetComponent<PlayerEquipment>().TryEquipWeapon()) return;
            taken = true; gameObject.SetActive(false);
        }
    }
}
