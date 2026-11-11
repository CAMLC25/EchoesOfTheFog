using System;
using EchoesOfTheFog.Core;
using EchoesOfTheFog.Player;
using UnityEngine;

namespace EchoesOfTheFog.Mission
{
    public enum ForestObjective { FindWeapon, ReachVillage, Completed }

    public sealed class ForestMission : MonoBehaviour
    {
        [SerializeField] private PlayerEquipment player;
        [SerializeField] private Transform weaponTarget, villageTarget;
        public PlayerEquipment Player => player;
        public ForestObjective Objective { get; private set; }
        public event Action<ForestObjective> ObjectiveChanged;
        public string ObjectiveText => Objective == ForestObjective.FindWeapon ? "Tìm khẩu súng ức chế sinh học"
            : Objective == ForestObjective.ReachVillage ? "Vượt qua khu rừng, tới cổng làng" : "Đã tới cổng làng Blackwood";
        public Transform CurrentTarget => Objective == ForestObjective.FindWeapon ? weaponTarget : Objective == ForestObjective.ReachVillage ? villageTarget : null;
        private void Awake() => Objective = player.HasWeapon ? ForestObjective.ReachVillage : ForestObjective.FindWeapon;
        private void OnEnable() => player.WeaponEquipped += OnEquipped;
        private void OnDisable() { if (player != null) player.WeaponEquipped -= OnEquipped; }
        private void OnEquipped()
        {
            if (Objective != ForestObjective.FindWeapon) return;
            Objective = ForestObjective.ReachVillage; ObjectiveChanged?.Invoke(Objective);
        }
        public bool ReachVillage(PlayerEquipment actor)
        {
            if (actor != player || Objective != ForestObjective.ReachVillage || !actor.HasWeapon || actor.GetComponent<Health>().IsDead) return false;
            Objective = ForestObjective.Completed; ObjectiveChanged?.Invoke(Objective); return true;
        }
    }
}
