using System;
using EchoesOfTheFog.Combat;
using EchoesOfTheFog.Core;
using UnityEngine;

namespace EchoesOfTheFog.Player
{
    [DisallowMultipleComponent, RequireComponent(typeof(Health), typeof(PlayerInputReader))]
    public sealed class PlayerSupplies : MonoBehaviour
    {
        [SerializeField, Min(1)] private int medkitCapacity = 3;
        [SerializeField, Min(1)] private float healing = 40;
        private Health health;
        private PlayerInputReader input;
        public int Medkits { get; private set; }
        public int MedkitCapacity => medkitCapacity;
        public bool CanReceiveMedkit => isActiveAndEnabled && health != null && !health.IsDead && Medkits < medkitCapacity;
        public event Action<string> Feedback;
        private void Awake() { health = GetComponent<Health>(); input = GetComponent<PlayerInputReader>(); }
        private void Update() { if (input.UseMedkitPressed && Time.timeScale > 0) TryUseMedkit(); }
        public int AddMedkits(int count)
        {
            if (count <= 0 || !CanReceiveMedkit) return 0;
            int accepted = Math.Min(count, medkitCapacity - Medkits);
            Medkits += accepted; Notify($"+{accepted} cứu thương"); return accepted;
        }
        public void Notify(string message) => Feedback?.Invoke(message);
        public bool TryUseMedkit()
        {
            if (!isActiveAndEnabled || health.IsDead || !input.CanControl || Time.timeScale <= 0) return false;
            if (Medkits == 0) { Notify("Bạn chưa có cứu thương"); return false; }
            if (health.CurrentHealth >= health.MaxHealth) { Notify("Máu đã đầy"); return false; }
            float before = health.CurrentHealth;
            health.Heal(healing); Medkits--;
            Notify($"Đã hồi {health.CurrentHealth - before:0} máu"); return true;
        }
    }
}
