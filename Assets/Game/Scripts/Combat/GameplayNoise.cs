using System;
using EchoesOfTheFog.Core;
using UnityEngine;

namespace EchoesOfTheFog.Combat
{
    // Logical hearing events; independent of whether audio clips have been supplied.
    public static class GameplayNoise
    {
        public static event Action<Vector3, float, Health> Emitted;
        public static void Emit(Vector3 position, float radius, Health source)
        {
            if (source != null && !source.IsDead && radius > 0) Emitted?.Invoke(position, radius, source);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Emitted = null;
    }
}
