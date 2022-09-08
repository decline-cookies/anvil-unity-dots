using System;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    internal readonly struct ProxyDataWrapper<T> : IEquatable<ProxyDataWrapper<T>>
        where T : unmanaged, IProxyData
    {
        public static bool operator==(ProxyDataWrapper<T> lhs, ProxyDataWrapper<T> rhs)
        {
            //Note that we are not checking if the Payload is equal because the wrapper is only for origin and lookup
            //checks. 
            return lhs.ID == rhs.ID;
        }

        public static bool operator!=(ProxyDataWrapper<T> lhs, ProxyDataWrapper<T> rhs)
        {
            return !(lhs == rhs);
        }

        public readonly ProxyDataID ID;
        public readonly T Payload;

        public ProxyDataWrapper(Entity entity, byte context, ref T payload)
        {
            ID = new ProxyDataID(entity, context);
            Payload = payload;
        }

        public bool Equals(ProxyDataWrapper<T> other)
        {
            return this == other;
        }

        public override bool Equals(object compare)
        {
            return compare is ProxyDataWrapper<T> id && Equals(id);
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
