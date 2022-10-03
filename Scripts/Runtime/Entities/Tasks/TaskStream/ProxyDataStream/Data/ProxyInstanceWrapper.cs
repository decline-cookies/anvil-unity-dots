using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal readonly struct ProxyInstanceWrapper<TInstance> : IEquatable<ProxyInstanceWrapper<TInstance>>
        where TInstance : unmanaged, IProxyInstance
    {
        public static bool operator ==(ProxyInstanceWrapper<TInstance> lhs, ProxyInstanceWrapper<TInstance> rhs)
        {
            //Note that we are not checking if the Payload is equal because the wrapper is only for origin and lookup
            //checks. 
            Debug_EnsurePayloadsAreTheSame(lhs, rhs);
            return lhs.InstanceID == rhs.InstanceID;
        }

        public static bool operator !=(ProxyInstanceWrapper<TInstance> lhs, ProxyInstanceWrapper<TInstance> rhs)
        {
            return !(lhs == rhs);
        }

        public readonly ProxyInstanceID InstanceID;
        public readonly TInstance Payload;

        public ProxyInstanceWrapper(Entity entity, byte context, ref TInstance payload)
        {
            InstanceID = new ProxyInstanceID(entity, context);
            Payload = payload;
        }

        public bool Equals(ProxyInstanceWrapper<TInstance> other)
        {
            return this == other;
        }

        public override bool Equals(object compare)
        {
            return compare is ProxyInstanceWrapper<TInstance> id && Equals(id);
        }

        public override int GetHashCode()
        {
            //We ignore the Payload because the wrapper is only for origin and lookup checks.
            return InstanceID.GetHashCode();
        }

        public override string ToString()
        {
            return $"{InstanceID.ToString()})";
        }

        [BurstCompatible]
        public FixedString64Bytes ToFixedString()
        {
            return InstanceID.ToFixedString();
        }

        
        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ANVIL_DEBUG_SAFETY_EXPENSIVE")]
        private static void Debug_EnsurePayloadsAreTheSame(ProxyInstanceWrapper<TInstance> lhs, ProxyInstanceWrapper<TInstance> rhs)
        {
            if (lhs.InstanceID == rhs.InstanceID
             && !lhs.Payload.Equals(rhs.Payload))
            {
                throw new InvalidOperationException($"Equality check for {typeof(ProxyInstanceWrapper<TInstance>)} where the ID's are the same but the Payloads are different. This should never happen!");
            }
        }
    }
}
