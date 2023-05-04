using Anvil.Unity.DOTS.Util;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    //TODO: #136 - Maybe have this implement IEntityProxyInstance. https://github.com/decline-cookies/anvil-unity-dots/pull/157#discussion_r1093730973
    [BurstCompatible]
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    internal readonly struct EntityProxyInstanceID : IEquatable<EntityProxyInstanceID>
    {
        //NOTE: Be careful messing with these - See Debug_EnsureOffsetsAreCorrect
        public const int TASK_SET_OWNER_ID_OFFSET = 8;
        public const int DATA_TARGET_ID_OFFSET = 12;

        public static bool operator ==(EntityProxyInstanceID lhs, EntityProxyInstanceID rhs)
        {
            return lhs.Entity == rhs.Entity && lhs.TaskSetOwnerID == rhs.TaskSetOwnerID;
        }

        public static bool operator !=(EntityProxyInstanceID lhs, EntityProxyInstanceID rhs)
        {
            return !(lhs == rhs);
        }

        public readonly Entity Entity;
        public readonly TaskSetOwnerID TaskSetOwnerID;
        public readonly DataTargetID DataTargetID;

        public EntityProxyInstanceID(Entity entity, TaskSetOwnerID taskSetOwnerID, DataTargetID dataTargetID)
        {
            Entity = entity;
            TaskSetOwnerID = taskSetOwnerID;
            DataTargetID = dataTargetID;
        }

        public EntityProxyInstanceID(EntityProxyInstanceID originalID, TaskSetOwnerID taskSetOwnerID)
        {
            Entity = originalID.Entity;
            TaskSetOwnerID = taskSetOwnerID;
            DataTargetID = originalID.DataTargetID;
        }

        public bool Equals(EntityProxyInstanceID other)
        {
            return this == other;
        }

        public override bool Equals(object compare)
        {
            return compare is EntityProxyInstanceID id && Equals(id);
        }

        public override int GetHashCode()
        {
            return HashCodeUtil.GetHashCode(TaskSetOwnerID.GetHashCode(), Entity.Index);
        }

        public override string ToString()
        {
            return $"{Entity.ToString()} - TaskSetOwnerID: {TaskSetOwnerID}, DataTargetID: {DataTargetID}";
        }

        [BurstCompatible]
        public FixedString64Bytes ToFixedString()
        {
            FixedString64Bytes fs = new FixedString64Bytes();
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            fs.Append(Entity.ToFixedString());
            fs.Append((FixedString32Bytes)" - TaskSetOwnerID: ");
            fs.Append(TaskSetOwnerID.ToFixedString());
            fs.Append((FixedString32Bytes)", DataTargetID: ");
            fs.Append(DataTargetID.ToFixedString());
            return fs;
        }
        
        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************
        
        [Conditional("ANVIL_DEBUG_SAFETY")]
        public static void Debug_EnsureOffsetsAreCorrect()
        {
            int actualOffset = UnsafeUtility.GetFieldOffset(typeof(EntityProxyInstanceID).GetField(nameof(TaskSetOwnerID)));
            if (actualOffset != TASK_SET_OWNER_ID_OFFSET)
            {
                throw new InvalidOperationException($"{nameof(TaskSetOwnerID)} has changed location in the struct. The hardcoded burst compatible offset of {nameof(TASK_SET_OWNER_ID_OFFSET)} = {TASK_SET_OWNER_ID_OFFSET} needs to be changed to {actualOffset}!");
            }
            
            actualOffset = UnsafeUtility.GetFieldOffset(typeof(EntityProxyInstanceID).GetField(nameof(DataTargetID)));
            if (actualOffset != DATA_TARGET_ID_OFFSET)
            {
                throw new InvalidOperationException($"{nameof(DataTargetID)} has changed location in the struct. The hardcoded burst compatible offset of {nameof(DATA_TARGET_ID_OFFSET)} = {DATA_TARGET_ID_OFFSET} needs to be changed to {actualOffset}!");
            }
        }
    }
}
