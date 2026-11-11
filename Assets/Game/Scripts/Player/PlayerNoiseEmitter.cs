using EchoesOfTheFog.Combat;
using EchoesOfTheFog.Core;
using UnityEngine;

namespace EchoesOfTheFog.Player
{
    [RequireComponent(typeof(PlayerMotor), typeof(Health))]
    public sealed class PlayerNoiseEmitter : MonoBehaviour
    {
        [SerializeField, Min(0)] private float gunshotRadius = 26;
        [SerializeField, Min(0)] private float sprintRadius = 12;
        [SerializeField, Min(0.1f)] private float footstepInterval = 0.45f;
        private PlayerMotor motor;
        private Health health;
        private HitscanRifle rifle;
        private float nextStep;

        private void Awake() { motor = GetComponent<PlayerMotor>(); health = GetComponent<Health>(); rifle = GetComponent<HitscanRifle>(); }
        private void OnEnable() { if (rifle != null) rifle.ShotFired += OnShot; }
        private void OnDisable() { if (rifle != null) rifle.ShotFired -= OnShot; }
        private void OnShot() => GameplayNoise.Emit(transform.position, gunshotRadius, health);
        private void Update()
        {
            if (Time.timeScale > 0 && !health.IsDead && motor.IsSprinting && motor.IsGrounded && Time.time >= nextStep)
            {
                nextStep = Time.time + footstepInterval;
                GameplayNoise.Emit(transform.position, sprintRadius, health);
            }
        }
    }
}
