using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    internal readonly struct ProxyDataWrapper<TData> : IEquatable<ProxyDataWrapper<TData>>
        where TData : unmanaged, IProxyData
    {
        public static bool operator ==(ProxyDataWrapper<TData> lhs, ProxyDataWrapper<TData> rhs)
        {
            //Note that we are not checking if the Payload is equal because the wrapper is only for origin and lookup
            //checks. 
            Debug_EnsurePayloadsAreTheSame(lhs, rhs);
            return lhs.ID == rhs.ID;
        }

        public static bool operator !=(ProxyDataWrapper<TData> lhs, ProxyDataWrapper<TData> rhs)
        {
            return !(lhs == rhs);
        }

        public readonly ProxyDataID ID;
        public readonly TData Payload;

        public ProxyDataWrapper(Entity entity, byte context, ref TData payload)
        {
            ID = new ProxyDataID(entity, context);
            Payload = payload;
        }

        public bool Equals(ProxyDataWrapper<TData> other)
        {
            return this == other;
        }

        public override bool Equals(object compare)
        {
            return compare is ProxyDataWrapper<TData> id && Equals(id);
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


        [Conditional("ANVIL_DEBUG_SAFETY_EXPENSIVE")]
        private static void Debug_EnsurePayloadsAreTheSame(ProxyDataWrapper<TData> lhs, ProxyDataWrapper<TData> rhs)
        {
#if ANVIL_DEBUG_SAFETY_EXPENSIVE
            if (lhs.ID == rhs.ID
             && !lhs.Payload.Equals(rhs.Payload))
            {
                throw new InvalidOperationException($"Equality check for {typeof(ProxyDataWrapper<TData>)} where the ID's are the same but the Payloads are different. This should never happen!");
            }
#endif
        }
    }
}
