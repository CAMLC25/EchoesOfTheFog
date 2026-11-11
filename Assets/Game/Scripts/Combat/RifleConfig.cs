using UnityEngine;

namespace EchoesOfTheFog.Combat
{
    [CreateAssetMenu(menuName = "Echoes of the Fog/Rifle Config")]
    public sealed class RifleConfig : ScriptableObject
    {
        [Min(1)] public int magazineSize = 30;
        [Min(0)] public int startingReserve = 90;
        [Min(0)] public int maximumReserve = 180;
        [Min(0.01f)] public float fireInterval = 0.2f;
        [Min(0.1f)] public float reloadDuration = 3.3f;
        [Min(0.1f)] public float damage = 25;
        [Min(1)] public float range = 100;
    }
}
