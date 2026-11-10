using System;
using UnityEngine;

namespace EchoesOfTheFog.Core
{
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;

        public float MaxHealth => maxHealth;
        public float CurrentHealth { get; private set; }
        public bool IsDead => CurrentHealth <= 0f;

        // Subscribe after spawning; read CurrentHealth/MaxHealth for the initial UI state.
        public event Action<float, float> HealthChanged;
        public event Action Died;

        private void Awake() => ResetHealth(maxHealth);

        public void TakeDamage(float amount)
        {
            ValidatePositive(amount, nameof(amount));
            if (IsDead) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            bool died = IsDead;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
            if (died) Died?.Invoke();
        }

        public void Heal(float amount)
        {
            ValidatePositive(amount, nameof(amount));
            if (IsDead || CurrentHealth >= maxHealth) return;

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        // Explicit spawn/restart operation; Heal never revives a dead actor.
        public void ResetHealth(float maximum)
        {
            ValidatePositive(maximum, nameof(maximum));
            maxHealth = maximum;
            CurrentHealth = maximum;
            HealthChanged?.Invoke(CurrentHealth, maxHealth);
        }

        private void OnValidate()
        {
            if (float.IsNaN(maxHealth) || float.IsInfinity(maxHealth) || maxHealth < 1f)
                maxHealth = 1f;
        }

        private static void ValidatePositive(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
                throw new ArgumentOutOfRangeException(name, "Value must be finite and greater than zero.");
        }
    }
}
