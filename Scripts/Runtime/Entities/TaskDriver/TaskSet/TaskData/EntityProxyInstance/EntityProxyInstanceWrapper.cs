using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    [BurstCompatible]
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct EntityProxyInstanceWrapper<TInstance> : IEquatable<EntityProxyInstanceWrapper<TInstance>>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        //NOTE: Be careful messing with this - See Debug_EnsureOffsetsAreCorrect
        // ReSharper disable once StaticMemberInGenericType
        public const int INSTANCE_ID_OFFSET = 0;

        public static bool operator ==(EntityProxyInstanceWrapper<TInstance> lhs, EntityProxyInstanceWrapper<TInstance> rhs)
        {
            //Note that we are not checking if the Payload is equal because the wrapper is only for origin and lookup
            //checks.
            Debug_EnsurePayloadsAreTheSame(lhs, rhs);
            return lhs.InstanceID == rhs.InstanceID;
        }

        public static bool operator !=(EntityProxyInstanceWrapper<TInstance> lhs, EntityProxyInstanceWrapper<TInstance> rhs)
        {
            return !(lhs == rhs);
        }

        public readonly EntityProxyInstanceID InstanceID;
        public readonly TInstance Payload;

        public EntityProxyInstanceWrapper(Entity entity, DataOwnerID dataOwnerID, DataTargetID dataTargetID, ref TInstance payload)
        {
            InstanceID = new EntityProxyInstanceID(entity, dataOwnerID, dataTargetID);
            Payload = payload;
        }

        public EntityProxyInstanceWrapper(ref EntityProxyInstanceWrapper<TInstance> original, DataTargetID newDataTargetID)
        {
            EntityProxyInstanceID originalInstanceID = original.InstanceID;
            InstanceID = new EntityProxyInstanceID(originalInstanceID.Entity, originalInstanceID.DataOwnerID, newDataTargetID);
            Payload = original.Payload;
        }

        public bool Equals(EntityProxyInstanceWrapper<TInstance> other)
        {
            return this == other;
        }

        public override bool Equals(object compare)
        {
            return compare is EntityProxyInstanceWrapper<TInstance> id && Equals(id);
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
        private static void Debug_EnsurePayloadsAreTheSame(
            EntityProxyInstanceWrapper<TInstance> lhs,
            EntityProxyInstanceWrapper<TInstance> rhs)
        {
            if (lhs.InstanceID == rhs.InstanceID && !lhs.Payload.Equals(rhs.Payload))
            {
                throw new InvalidOperationException($"Equality check for {typeof(EntityProxyInstanceWrapper<TInstance>)} where the ID's are the same but the Payloads are different. This should never happen!");
            }
        }
        
        [Conditional("ANVIL_DEBUG_SAFETY")]
        public static void Debug_EnsureOffsetsAreCorrect()
        {
            int actualOffset = UnsafeUtility.GetFieldOffset(typeof(EntityProxyInstanceWrapper<TInstance>).GetField(nameof(InstanceID)));
            if (actualOffset != INSTANCE_ID_OFFSET)
            {
                throw new InvalidOperationException($"{nameof(InstanceID)} has changed location in the struct. The hardcoded burst compatible offset of {nameof(INSTANCE_ID_OFFSET)} = {INSTANCE_ID_OFFSET} needs to be changed to {actualOffset}!");
            }
        }
    }
}
