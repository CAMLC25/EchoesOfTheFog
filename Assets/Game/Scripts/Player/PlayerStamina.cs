using System;
using UnityEngine;

namespace EchoesOfTheFog.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerStamina : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maximum = 100;
        [SerializeField, Min(0.1f)] private float drainPerSecond = 22;
        [SerializeField, Min(0.1f)] private float recoveryPerSecond = 18;
        [SerializeField, Min(0)] private float recoveryDelay = 1.2f;
        [SerializeField, Range(0.05f, 1)] private float exhaustedRecoveryFraction = 0.25f;
        private float recoveryTimer;
        private bool exhausted;

        public float Current { get; private set; }
        public float Maximum => maximum;
        public bool CanSprint => !exhausted && Current > 0;
        public event Action<float, float> Changed;

        private void Awake() => ResetStamina();

        public void ResetStamina()
        {
            Current = maximum;
            exhausted = false;
            recoveryTimer = 0;
            Changed?.Invoke(Current, maximum);
        }

        // Called exactly once per physics step by the motor; movement decides whether sprinting occurred.
        public void Tick(bool sprinting, float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            float previous = Current;
            if (sprinting && CanSprint)
            {
                Current = Mathf.Max(0, Current - drainPerSecond * deltaTime);
                recoveryTimer = recoveryDelay;
                if (Current <= 0) exhausted = true;
            }
            else
            {
                // A long frame must not consume both the full cooldown and full regeneration interval.
                float recoveryTime = Mathf.Max(0, deltaTime - recoveryTimer);
                recoveryTimer = Mathf.Max(0, recoveryTimer - deltaTime);
                Current = Mathf.Min(maximum, Current + recoveryPerSecond * recoveryTime);
                if (Current >= maximum * exhaustedRecoveryFraction) exhausted = false;
            }
            if (!Mathf.Approximately(previous, Current)) Changed?.Invoke(Current, maximum);
        }
    }
}
