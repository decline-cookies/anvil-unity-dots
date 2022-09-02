using System;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Data
{
    internal readonly struct PDWrapper<T> : IEquatable<PDWrapper<T>>
        where T : unmanaged, IEntityProxyData
    {
        public static bool operator==(PDWrapper<T> lhs, PDWrapper<T> rhs)
        {
            //Note that we are not checking if the Payload is equal because the wrapper is only for origin and lookup
            //checks. 
            return lhs.ID == rhs.ID;
        }

        public static bool operator!=(PDWrapper<T> lhs, PDWrapper<T> rhs)
        {
            return !(lhs == rhs);
        }

        public readonly PDID ID;
        public readonly T Payload;

        public PDWrapper(Entity entity, byte context, ref T payload)
        {
            ID = new PDID(entity, context);
            Payload = payload;
        }

        public bool Equals(PDWrapper<T> other)
        {
            return this == other;
        }

        public override bool Equals(object compare)
        {
            return compare is PDWrapper<T> id && Equals(id);
        }
        public override int GetHashCode()
        {
            //We ignore the Payload because the wrapper is only for origin and lookup checks.
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
