using System;
using EchoesOfTheFog.Core;
using EchoesOfTheFog.Player;
using UnityEngine;

namespace EchoesOfTheFog.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputReader), typeof(Health))]
    public sealed class HitscanRifle : MonoBehaviour
    {
        [SerializeField] private RifleConfig config;
        [SerializeField] private Transform muzzle;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private LineRenderer tracer;
        [SerializeField] private Light muzzleLight;
        private PlayerInputReader input;
        private Health health;
        private Camera aimCamera;
        private WeaponAmmo ammo;
        private float nextShotTime, reloadFinishTime, effectEndTime;
        private float firePoseEndTime;

        public int Loaded => ammo?.Loaded ?? 0;
        public int Reserve => ammo?.Reserve ?? 0;
        public bool IsReloading { get; private set; }
        public bool IsInFirePose => isActiveAndEnabled && input != null && input.CanControl && health != null && !health.IsDead && !IsReloading
            && ((input.FireHeld && Loaded > 0) || Time.time < firePoseEndTime);
        public float LastDamageHitTime { get; private set; } = -100;
        public event Action ShotFired;

        private void Awake()
        {
            input = GetComponent<PlayerInputReader>(); health = GetComponent<Health>();
            if (config == null || muzzle == null)
            {
                Debug.LogError("HitscanRifle needs its RifleConfig and muzzle.", this); enabled = false; return;
            }
            ammo = new WeaponAmmo(config.magazineSize, config.startingReserve);
        }

        private void OnEnable() { if (health != null) health.Died += StopReload; }
        private void OnDisable()
        {
            if (health != null) health.Died -= StopReload;
            StopReload();
            if (tracer != null) tracer.enabled = false;
            if (muzzleLight != null) muzzleLight.enabled = false;
        }

        private void Update()
        {
            if (tracer != null) tracer.enabled = Time.time < effectEndTime;
            if (muzzleLight != null) muzzleLight.enabled = Time.time < effectEndTime;
            if (health.IsDead) return;
            if (IsReloading && Time.time >= reloadFinishTime) { ammo.CompleteReload(); IsReloading = false; }
            if (input.ReloadPressed) TryStartReload();
            if (input.FireHeld) TryFire();
        }

        public bool TryStartReload()
        {
            if (!isActiveAndEnabled || ammo == null || health.IsDead || !input.CanControl || IsReloading || !ammo.CanReload) return false;
            IsReloading = true; reloadFinishTime = Time.time + config.reloadDuration; return true;
        }

        public bool TryFire()
        {
            if (!isActiveAndEnabled || ammo == null || health.IsDead || !input.CanControl || IsReloading || Time.time < nextShotTime || Loaded == 0) return false;
            if (aimCamera == null) aimCamera = Camera.main;
            if (aimCamera == null) return false;
            ammo.TryConsume(); nextShotTime = Time.time + config.fireInterval;
            firePoseEndTime = Time.time + 0.45f;
            Ray cameraRay = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            Vector3 target = cameraRay.GetPoint(config.range);
            if (Physics.Raycast(cameraRay, out var cameraHit, config.range, hitMask, QueryTriggerInteraction.Ignore)) target = cameraHit.point;
            Vector3 origin = muzzle.position;
            Vector3 direction = target - origin;
            float distance = Mathf.Min(config.range, direction.magnitude + 0.02f);
            Vector3 end = origin + direction.normalized * distance;
            RaycastHit hit;
            Vector3 chest = transform.position + Vector3.up * 1.3f;
            Vector3 toMuzzle = origin - chest;
            // A barrel protruding through a wall must not bypass cover just because
            // the camera can see around it. Check both chest-to-muzzle and muzzle-to-target.
            bool didHit = Physics.Raycast(chest, toMuzzle.normalized, out hit, toMuzzle.magnitude, hitMask, QueryTriggerInteraction.Ignore);
            if (!didHit) didHit = Physics.Raycast(origin, direction.normalized, out hit, distance, hitMask, QueryTriggerInteraction.Ignore);
            if (didHit)
            {
                end = hit.point;
                var damageable = hit.collider.GetComponentInParent<IDamageable>();
                if (damageable != null) { damageable.TakeDamage(config.damage); LastDamageHitTime = Time.time; }
            }
            effectEndTime = Time.time + 0.045f;
            if (tracer != null) { tracer.SetPosition(0, origin); tracer.SetPosition(1, end); tracer.enabled = true; }
            if (muzzleLight != null) muzzleLight.enabled = true;
            ShotFired?.Invoke();
            return true;
        }

        private void StopReload() => IsReloading = false;
    }
}
