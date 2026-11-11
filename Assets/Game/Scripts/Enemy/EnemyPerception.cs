using EchoesOfTheFog.Core;
using UnityEngine;

namespace EchoesOfTheFog.Enemy
{
    public sealed class EnemyPerception : MonoBehaviour
    {
        [SerializeField] private RunnerTuning tuning;
        [SerializeField] private LayerMask occlusionMask = ~0;

        public bool CanSee(Health target, float radius, bool tracking)
        {
            if (target == null || target.IsDead || !target.gameObject.activeInHierarchy) return false;
            Vector3 direction = target.transform.position - transform.position;
            if (direction.sqrMagnitude > radius * radius) return false;
            Vector3 planar = Vector3.ProjectOnPlane(direction, Vector3.up);
            if (!tracking && planar.sqrMagnitude > 0.01f && Vector3.Angle(transform.forward, planar) > tuning.fieldOfView * 0.5f) return false;
            return HasClearSight(target);
        }

        public bool HasClearSight(Health target)
        {
            if (target == null || target.IsDead || !target.gameObject.activeInHierarchy) return false;
            Vector3 origin = transform.position + Vector3.up * tuning.eyeHeight;
            Vector3 destination = target.transform.position + Vector3.up * tuning.targetHeight;
            Vector3 delta = destination - origin;
            return !Physics.Raycast(origin, delta.normalized, delta.magnitude, occlusionMask, QueryTriggerInteraction.Ignore);
        }
    }
}
