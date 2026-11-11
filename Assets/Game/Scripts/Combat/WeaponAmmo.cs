using System;

namespace EchoesOfTheFog.Combat
{
    public sealed class WeaponAmmo
    {
        public int Capacity { get; }
        public int Loaded { get; private set; }
        public int Reserve { get; private set; }
        public bool CanReload => Loaded < Capacity && Reserve > 0;

        public WeaponAmmo(int capacity, int reserve)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (reserve < 0) throw new ArgumentOutOfRangeException(nameof(reserve));
            Capacity = capacity; Loaded = capacity; Reserve = reserve;
        }

        public bool TryConsume()
        {
            if (Loaded == 0) return false;
            Loaded--; return true;
        }

        public void CompleteReload()
        {
            int transferred = Math.Min(Capacity - Loaded, Reserve);
            Loaded += transferred; Reserve -= transferred;
        }
    }
}
