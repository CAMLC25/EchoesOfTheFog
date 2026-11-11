using UnityEngine;

namespace EchoesOfTheFog.Enemy
{
    [CreateAssetMenu(menuName = "Echoes of the Fog/Runner Tuning")]
    public sealed class RunnerTuning : ScriptableObject
    {
        [Min(0.1f)] public float patrolSpeed = 1.4f;
        [Range(1, 360)] public float fieldOfView = 120;
        [Min(0.1f)] public float eyeHeight = 1.45f;
        [Min(0.1f)] public float targetHeight = 1;
        [Min(0.05f)] public float senseInterval = 0.15f;
        [Min(0.05f)] public float repathInterval = 0.25f;
        [Min(0)] public float sightMemory = 0.5f;
        [Min(0)] public float patrolWait = 1;
        [Min(0.05f)] public float hitStun = 0.35f;
        [Min(0.1f)] public float attack1Duration = 1;
        [Min(0.1f)] public float attack2Duration = 1.2f;
        [Range(0.05f, 0.95f)] public float attack1Impact = 0.4f;
        [Range(0.05f, 0.95f)] public float attack2Impact = 0.45f;
        [Range(1, 180)] public float attackArc = 100;
        [Min(1)] public float turnSpeed = 720;
        [Min(0.1f)] public float spawnSnapDistance = 1.5f;
        [Min(0.1f)] public float hearingMultiplier = 1;

        public float AttackDuration(int index) => index == 0 ? attack1Duration : attack2Duration;
        public float ImpactFraction(int index) => index == 0 ? attack1Impact : attack2Impact;
    }
}
