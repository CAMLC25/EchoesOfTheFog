using System;
using EchoesOfTheFog.Core;
using UnityEngine;

namespace EchoesOfTheFog.Player
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Health))]
    public sealed class PlayerEquipment : MonoBehaviour
    {
        [SerializeField] private bool startEquipped = true;
        [SerializeField] private GameObject weaponVisual;
        private Health health;
        public bool HasWeapon { get; private set; }
        public GameObject WeaponVisual => weaponVisual;
        public event Action WeaponEquipped;

        private void Awake()
        {
            health = GetComponent<Health>(); HasWeapon = startEquipped;
            if (weaponVisual == null) { Debug.LogError("PlayerEquipment needs its weapon visual.", this); enabled = false; return; }
            weaponVisual.SetActive(HasWeapon);
        }
        public bool TryEquipWeapon()
        {
            if (!isActiveAndEnabled || HasWeapon || health.IsDead || weaponVisual == null) return false;
            HasWeapon = true; weaponVisual.SetActive(true);
            GetComponent<PlayerInputReader>()?.SuppressFireUntilRelease();
            WeaponEquipped?.Invoke(); return true;
        }
    }
}
