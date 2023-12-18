using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct EntityKeyedTaskWrapper<TInstance> : IEquatable<EntityKeyedTaskWrapper<TInstance>>
        where TInstance : unmanaged, IEntityKeyedTask
    {
        //NOTE: Be careful messing with this - See Debug_EnsureOffsetsAreCorrect
        // ReSharper disable once StaticMemberInGenericType
        public const int INSTANCE_ID_OFFSET = 0;

        public static bool operator ==(EntityKeyedTaskWrapper<TInstance> lhs, EntityKeyedTaskWrapper<TInstance> rhs)
        {
            //Note that we are not checking if the Payload is equal because the wrapper is only for origin and lookup
            //checks.
            Debug_EnsurePayloadsAreTheSame(lhs, rhs);
            return lhs.InstanceID == rhs.InstanceID;
        }

        public static bool operator !=(EntityKeyedTaskWrapper<TInstance> lhs, EntityKeyedTaskWrapper<TInstance> rhs)
        {
            return !(lhs == rhs);
        }

        public readonly EntityKeyedTaskID InstanceID;
        public readonly TInstance Payload;

        public EntityKeyedTaskWrapper(Entity entity, DataOwnerID dataOwnerID, DataTargetID dataTargetID, ref TInstance payload)
        {
            InstanceID = new EntityKeyedTaskID(entity, dataOwnerID, dataTargetID);
            Payload = payload;
        }

        public EntityKeyedTaskWrapper(ref EntityKeyedTaskWrapper<TInstance> original, DataTargetID newDataTargetID)
        {
            EntityKeyedTaskID originalInstanceID = original.InstanceID;
            InstanceID = new EntityKeyedTaskID(originalInstanceID.Entity, originalInstanceID.DataOwnerID, newDataTargetID);
            Payload = original.Payload;
        }

        public bool Equals(EntityKeyedTaskWrapper<TInstance> other)
        {
            return this == other;
        }

        public override bool Equals(object compare)
        {
            return compare is EntityKeyedTaskWrapper<TInstance> id && Equals(id);
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

        public FixedString64Bytes ToFixedString()
        {
            return InstanceID.ToFixedString();
        }


        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ANVIL_DEBUG_SAFETY_EXPENSIVE")]
        private static void Debug_EnsurePayloadsAreTheSame(
            EntityKeyedTaskWrapper<TInstance> lhs,
            EntityKeyedTaskWrapper<TInstance> rhs)
        {
            if (lhs.InstanceID == rhs.InstanceID && !lhs.Payload.Equals(rhs.Payload))
            {
                throw new InvalidOperationException($"Equality check for {typeof(EntityKeyedTaskWrapper<TInstance>)} where the ID's are the same but the Payloads are different. This should never happen!");
            }
        }

        [Conditional("ANVIL_DEBUG_SAFETY")]
        public static void Debug_EnsureOffsetsAreCorrect()
        {
            int actualOffset = UnsafeUtility.GetFieldOffset(typeof(EntityKeyedTaskWrapper<TInstance>).GetField(nameof(InstanceID)));
            if (actualOffset != INSTANCE_ID_OFFSET)
            {
                throw new InvalidOperationException($"{nameof(InstanceID)} has changed location in the struct. The hardcoded burst compatible offset of {nameof(INSTANCE_ID_OFFSET)} = {INSTANCE_ID_OFFSET} needs to be changed to {actualOffset}!");
            }
        }
    }
}