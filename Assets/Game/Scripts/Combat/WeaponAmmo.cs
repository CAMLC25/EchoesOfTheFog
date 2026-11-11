using System;

namespace EchoesOfTheFog.Combat
{
    public sealed class WeaponAmmo
    {
        public int Capacity { get; }
        public int Loaded { get; private set; }
        public int Reserve { get; private set; }
        public int ReserveCapacity { get; }
        public bool CanReload => Loaded < Capacity && Reserve > 0;

        public WeaponAmmo(int capacity, int reserve, int reserveCapacity = int.MaxValue)
        {
            if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
            if (reserve < 0) throw new ArgumentOutOfRangeException(nameof(reserve));
            if (reserveCapacity < reserve) throw new ArgumentOutOfRangeException(nameof(reserveCapacity));
            ReserveCapacity = reserveCapacity;
            Capacity = capacity; Loaded = capacity; Reserve = reserve;
        }

        public bool TryConsume()
        {
            if (Loaded == 0) return false;
            Loaded--; return true;
        }

        public int AddReserve(int amount)
        {
            if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
            int accepted = Math.Min(amount, ReserveCapacity - Reserve);
            Reserve += accepted;
            return accepted;
        }

        public void CompleteReload()
        {
            int transferred = Math.Min(Capacity - Loaded, Reserve);
            Loaded += transferred; Reserve -= transferred;
        }
    }
}
