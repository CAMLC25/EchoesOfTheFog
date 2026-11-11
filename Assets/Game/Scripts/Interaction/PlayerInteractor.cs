using EchoesOfTheFog.Core;
using EchoesOfTheFog.Player;
using UnityEngine;

namespace EchoesOfTheFog.Interaction
{
    [RequireComponent(typeof(PlayerInputReader), typeof(Health))]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float range = 1.8f;
        [SerializeField] private LayerMask interactableMask;
        [SerializeField] private LayerMask occlusionMask = ~0;
        private readonly Collider[] results = new Collider[16];
        private PlayerInputReader input;
        private Health health;
        private IInteractable current;
        private MonoBehaviour currentBehaviour;
        private Collider currentCollider;
        private float nextScan;
        public string CurrentPrompt => currentBehaviour != null && currentBehaviour.isActiveAndEnabled ? current.InteractionPrompt : "";
        private Vector3 Origin => transform.position + Vector3.up;

        private void Awake() { input = GetComponent<PlayerInputReader>(); health = GetComponent<Health>(); }
        private void Update()
        {
            if (!input.CanControl || health.IsDead) { Clear(); return; }
            if (Time.time >= nextScan) Scan();
            if (input.InteractPressed) TryInteract();
        }
        private void Clear() { current = null; currentBehaviour = null; currentCollider = null; }
        private void Scan()
        {
            Clear(); nextScan = Time.time + 0.1f;
            int count = Physics.OverlapSphereNonAlloc(Origin, range, results, interactableMask, QueryTriggerInteraction.Collide);
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var candidate = results[i].GetComponentInParent<IInteractable>();
                var behaviour = candidate as MonoBehaviour;
                if (behaviour == null || !behaviour.isActiveAndEnabled || !candidate.CanInteract(gameObject) || !ClearSight(results[i])) continue;
                float distance = (results[i].ClosestPoint(Origin) - Origin).sqrMagnitude;
                if (distance >= nearest) continue;
                nearest = distance; current = candidate; currentBehaviour = behaviour; currentCollider = results[i];
            }
        }
        private bool ClearSight(Collider collider)
        {
            Vector3 delta = collider.bounds.center - Origin;
            return !Physics.Raycast(Origin, delta.normalized, delta.magnitude, occlusionMask, QueryTriggerInteraction.Ignore);
        }
        public bool TryInteract()
        {
            if (!input.CanControl || health.IsDead) return false;
            Scan();
            if (currentBehaviour == null || currentCollider == null || !current.CanInteract(gameObject)
                || (currentCollider.ClosestPoint(Origin) - Origin).sqrMagnitude > range * range || !ClearSight(currentCollider)) return false;
            current.Interact(gameObject); Clear(); return true;
        }
    }
}
