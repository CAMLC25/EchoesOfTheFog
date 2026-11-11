using EchoesOfTheFog.Core;
using UnityEngine;

namespace EchoesOfTheFog.Combat
{
    [RequireComponent(typeof(Health), typeof(Renderer))]
    public sealed class DamageTargetFeedback : MonoBehaviour
    {
        private Health health;
        private Renderer targetRenderer;
        private MaterialPropertyBlock properties;
        private void Awake() { health = GetComponent<Health>(); targetRenderer = GetComponent<Renderer>(); properties = new MaterialPropertyBlock(); }
        private void OnEnable() { health.HealthChanged += OnChanged; }
        private void OnDisable() { if (health != null) health.HealthChanged -= OnChanged; }
        private void Start() => OnChanged(health.CurrentHealth, health.MaxHealth);
        private void OnChanged(float current, float maximum)
        {
            targetRenderer.GetPropertyBlock(properties);
            properties.SetColor("_Color", Color.Lerp(new Color(0.65f, 0.1f, 0.08f), new Color(0.15f, 0.55f, 0.3f), current / maximum));
            targetRenderer.SetPropertyBlock(properties);
        }
    }
}
