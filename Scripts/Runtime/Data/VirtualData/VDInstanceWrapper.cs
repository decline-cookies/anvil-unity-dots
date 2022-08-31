using System;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Data
{
    internal readonly struct VDInstanceWrapper<T> : IEquatable<VDInstanceWrapper<T>>
        where T : unmanaged, IKeyedData
    {
        public static bool operator==(VDInstanceWrapper<T> lhs, VDInstanceWrapper<T> rhs)
        {
            return lhs.ID == rhs.ID;
        }

        public static bool operator!=(VDInstanceWrapper<T> lhs, VDInstanceWrapper<T> rhs)
        {
            return !(lhs == rhs);
        }

        public readonly VDContextID ID;
        public readonly T Payload;

        public VDInstanceWrapper(Entity entity, uint context, ref T payload)
        {
            ID = new VDContextID(entity, context);
            Payload = payload;
        }

        public bool Equals(VDInstanceWrapper<T> other)
        {
            return ID == other.ID;
        }

        public override bool Equals(object compare)
        {
            return compare is VDInstanceWrapper<T> id && Equals(id);
        }
        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public override string ToString()
        {
            return $"{ID.ToString()})";
        }

        [BurstCompatible]
        public FixedString64Bytes ToFixedString()
        {
            return ID.ToFixedString();
        }
    }
}
