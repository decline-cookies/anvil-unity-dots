using Anvil.Unity.DOTS.Util;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    //TODO: #136 - Maybe have this implement IEntityProxyInstance. https://github.com/decline-cookies/anvil-unity-dots/pull/157#discussion_r1093730973
    [BurstCompatible]
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    internal readonly struct EntityProxyInstanceID : IEquatable<EntityProxyInstanceID>
    {
        public static readonly int TASK_SET_OWNER_ID_OFFSET = Marshal.OffsetOf<EntityProxyInstanceID>(nameof(TaskSetOwnerID)).ToInt32();
        public static readonly int ACTIVE_ID_OFFSET = Marshal.OffsetOf<EntityProxyInstanceID>(nameof(ActiveID)).ToInt32();

        public static bool operator ==(EntityProxyInstanceID lhs, EntityProxyInstanceID rhs)
        {
            return lhs.Entity == rhs.Entity && lhs.TaskSetOwnerID == rhs.TaskSetOwnerID;
        }

        public static bool operator !=(EntityProxyInstanceID lhs, EntityProxyInstanceID rhs)
        {
            return !(lhs == rhs);
        }

        public readonly Entity Entity;
        public readonly uint TaskSetOwnerID;
        public readonly uint ActiveID;

        public EntityProxyInstanceID(Entity entity, uint taskSetOwnerID, uint activeID)
        {
            Entity = entity;
            TaskSetOwnerID = taskSetOwnerID;
            ActiveID = activeID;
        }

        public EntityProxyInstanceID(EntityProxyInstanceID originalID, uint taskSetOwnerID)
        {
            Entity = originalID.Entity;
            TaskSetOwnerID = taskSetOwnerID;
            ActiveID = originalID.ActiveID;
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
            return HashCodeUtil.GetHashCode((int)TaskSetOwnerID, Entity.Index);
        }

        public override string ToString()
        {
            return $"{Entity.ToString()} - TaskSetOwnerID: {TaskSetOwnerID}, ActiveID: {ActiveID}";
        }

        [BurstCompatible]
        public FixedString64Bytes ToFixedString()
        {
            FixedString64Bytes fs = new FixedString64Bytes();
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            fs.Append(Entity.ToFixedString());
            fs.Append((FixedString32Bytes)" - TaskSetOwnerID: ");
            fs.Append(TaskSetOwnerID);
            fs.Append((FixedString32Bytes)", ActiveID: ");
            fs.Append(ActiveID);
            return fs;
        }
    }
}
