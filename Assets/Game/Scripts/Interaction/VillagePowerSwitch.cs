using EchoesOfTheFog.Core;
using EchoesOfTheFog.Mission;
using EchoesOfTheFog.Player;
using UnityEngine;

namespace EchoesOfTheFog.Interaction
{
    public sealed class VillagePowerSwitch : MonoBehaviour, IInteractable
    {
        public VillageRoute route;
        public string InteractionPrompt => "F — Bật nguồn điện hạ cầu";
        public bool CanInteract(GameObject actor) => route != null && actor == route.player.gameObject && !route.PowerOn
            && isActiveAndEnabled && actor.GetComponent<PlayerInputReader>().CanControl && !actor.GetComponent<Health>().IsDead
            && Vector3.Distance(actor.transform.position + Vector3.up, transform.position) < 2.2f;
        public void Interact(GameObject actor) { if (CanInteract(actor)) route.ActivatePower(); }
    }
}
