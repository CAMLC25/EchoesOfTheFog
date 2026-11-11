using EchoesOfTheFog.Combat;
using EchoesOfTheFog.Core;
using UnityEngine;

namespace EchoesOfTheFog.Player
{
    // Must live on the same GameObject as Animator. The UpperBody layer enables IK Pass.
    [RequireComponent(typeof(Animator))]
    public sealed class PlayerRifleIK : MonoBehaviour
    {
        [SerializeField] private Transform leftHandGrip;
        private Animator animator;
        private PlayerCameraRig view;
        private HitscanRifle rifle;
        private Health health;
        private Transform rifleSocket;

        private void Awake()
        {
            animator = GetComponent<Animator>(); view = GetComponentInParent<PlayerCameraRig>();
            rifle = GetComponentInParent<HitscanRifle>(); health = GetComponentInParent<Health>();
            if (leftHandGrip != null) rifleSocket = leftHandGrip.GetComponentInParent<RifleView>(true)?.transform.parent;
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (animator == null) return;
            bool active = leftHandGrip != null && leftHandGrip.gameObject.activeInHierarchy && health != null && !health.IsDead && !(rifle != null && rifle.IsReloading);
            bool aiming = active && view != null && (view.IsAiming || (rifle != null && rifle.IsInFirePose));
            // Imported clips use different wrist rotations. Keep the barrel facing
            // the player's heading before solving the support hand against its grip.
            if (active && rifleSocket != null)
                rifleSocket.rotation = Quaternion.Euler(aiming ? view.Pitch : 0, aiming ? view.Yaw : health.transform.eulerAngles.y, 0);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, active ? 1 : 0);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, active ? 1 : 0);
            if (active)
            {
                animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandGrip.position);
                animator.SetIKRotation(AvatarIKGoal.LeftHand, leftHandGrip.rotation);
            }
            animator.SetLookAtWeight(aiming ? 0.8f : 0, 0, 0.8f, 0.6f, 0.55f);
            if (aiming)
                animator.SetLookAtPosition(health.transform.position + Vector3.up * 1.45f + Quaternion.Euler(view.Pitch, view.Yaw, 0) * Vector3.forward * 15);
        }
    }
}
