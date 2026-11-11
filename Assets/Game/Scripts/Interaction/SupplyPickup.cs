using EchoesOfTheFog.Combat;
using EchoesOfTheFog.Core;
using EchoesOfTheFog.Player;
using UnityEngine;

namespace EchoesOfTheFog.Interaction
{
    public enum SupplyKind { Ammo, Medkit }
    public sealed class SupplyPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private SupplyKind kind;
        [SerializeField, Min(1)] private int quantity = 30;
        public SupplyKind Kind => kind;
        public int Remaining { get; private set; }
        public string InteractionPrompt => kind == SupplyKind.Ammo ? $"F — Nhặt {Remaining} viên đạn" : $"F — Nhặt {Remaining} cứu thương";
        private void Awake() => Remaining = quantity;
        public bool CanInteract(GameObject actor)
        {
            if (!isActiveAndEnabled || Remaining <= 0 || actor == null) return false;
            var hp = actor.GetComponent<Health>(); var input = actor.GetComponent<PlayerInputReader>();
            if (hp == null || hp.IsDead || input == null || !input.CanControl || Time.timeScale <= 0
                || Vector3.Distance(transform.position + Vector3.up * .25f, actor.transform.position + Vector3.up) > 2.2f) return false;
            return kind == SupplyKind.Ammo ? actor.GetComponent<HitscanRifle>()?.CanReceiveAmmo == true
                : actor.GetComponent<PlayerSupplies>()?.CanReceiveMedkit == true;
        }
        public void Interact(GameObject actor)
        {
            if (!CanInteract(actor)) return;
            var supplies = actor.GetComponent<PlayerSupplies>();
            int accepted = kind == SupplyKind.Ammo ? actor.GetComponent<HitscanRifle>().AddReserveAmmo(Remaining) : supplies.AddMedkits(Remaining);
            if (accepted <= 0) return;
            Remaining -= accepted;
            if (kind == SupplyKind.Ammo) supplies?.Notify($"+{accepted} viên đạn dự trữ");
            if (Remaining == 0) gameObject.SetActive(false);
        }
    }
}
