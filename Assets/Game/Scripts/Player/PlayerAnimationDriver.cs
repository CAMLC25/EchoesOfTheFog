using EchoesOfTheFog.Combat;
using EchoesOfTheFog.Core;
using UnityEngine;

namespace EchoesOfTheFog.Player
{
    [RequireComponent(typeof(PlayerMotor), typeof(Health))]
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        private Rigidbody body;
        private PlayerMotor motor;
        private Health health;
        private HitscanRifle rifle;
        private PlayerCameraRig view;
        private float previousHealth;
        private static readonly int MoveX = Animator.StringToHash("MoveX"), MoveY = Animator.StringToHash("MoveY"),
            Grounded = Animator.StringToHash("Grounded"), Dead = Animator.StringToHash("Dead"),
            Reloading = Animator.StringToHash("Reloading"), Fire = Animator.StringToHash("Fire"), Hit = Animator.StringToHash("Hit");

        private void Awake()
        {
            body = GetComponent<Rigidbody>(); motor = GetComponent<PlayerMotor>();
            health = GetComponent<Health>(); rifle = GetComponent<HitscanRifle>(); previousHealth = health.CurrentHealth;
            view = GetComponent<PlayerCameraRig>();
            if (animator == null || animator.runtimeAnimatorController == null)
            { Debug.LogError("PlayerAnimationDriver needs the character Animator and controller.", this); enabled = false; }
        }
        private void OnEnable()
        {
            if (health != null) health.HealthChanged += OnHealthChanged;
            if (rifle != null) rifle.ShotFired += OnShot;
        }
        private void OnDisable()
        {
            if (health != null) health.HealthChanged -= OnHealthChanged;
            if (rifle != null) rifle.ShotFired -= OnShot;
        }
        private void Update()
        {
            Vector3 local = transform.InverseTransformDirection(body.linearVelocity) / motor.WalkSpeed;
            animator.SetFloat(MoveX, health.IsDead ? 0 : local.x, 0.12f, Time.deltaTime);
            animator.SetFloat(MoveY, health.IsDead ? 0 : local.z, 0.12f, Time.deltaTime);
            animator.SetBool(Grounded, motor.IsGrounded);
            animator.SetBool(Dead, health.IsDead);
            bool inFirePose = rifle != null && rifle.IsInFirePose;
            bool aiming = view != null && view.IsAiming;
            animator.SetBool("Aiming", aiming || inFirePose);
            animator.SetFloat("Armed", rifle != null && rifle.isActiveAndEnabled ? 1 : 0);
            animator.SetFloat("JumpPhase", Mathf.Clamp01(0.5f - body.linearVelocity.y / (2 * motor.JumpSpeed)));
            animator.SetBool(Reloading, rifle != null && rifle.IsReloading);
            if (rifle != null && rifle.IsReloading) animator.ResetTrigger(Fire);
            float upperWeight = health.IsDead || (!motor.IsGrounded && !aiming && !inFirePose && !(rifle != null && rifle.IsReloading)) ? 0 : 1;
            animator.SetLayerWeight(1, health.IsDead ? 0 : Mathf.MoveTowards(animator.GetLayerWeight(1), upperWeight, Time.deltaTime * 10));
        }
        private void OnShot() { if (animator != null) animator.SetTrigger(Fire); }
        private void OnHealthChanged(float current, float maximum)
        {
            if (current < previousHealth && current > 0 && !(rifle != null && rifle.IsReloading) && animator != null) animator.SetTrigger(Hit);
            previousHealth = current;
        }
    }
}
