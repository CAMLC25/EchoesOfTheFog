using UnityEngine;

namespace EchoesOfTheFog.Interaction
{
    // The interactor is the player's root GameObject. Implementations must recheck
    // CanInteract inside Interact, since mission state may change after UI polling.
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        bool CanInteract(GameObject interactor);
        void Interact(GameObject interactor);
    }
}
