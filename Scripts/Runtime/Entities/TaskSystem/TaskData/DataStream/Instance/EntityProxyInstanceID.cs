using Anvil.Unity.DOTS.Util;
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    internal readonly struct EntityProxyInstanceID : IEquatable<EntityProxyInstanceID>
    {
        public static bool operator ==(EntityProxyInstanceID lhs, EntityProxyInstanceID rhs)
        {
            return lhs.Entity == rhs.Entity && lhs.Context == rhs.Context;
        }

        public static bool operator !=(EntityProxyInstanceID lhs, EntityProxyInstanceID rhs)
        {
            return !(lhs == rhs);
        }

        public readonly Entity Entity;
        public readonly byte Context;

        public EntityProxyInstanceID(Entity entity, byte context)
        {
            Entity = entity;
            Context = context;
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
            return HashCodeUtil.GetHashCode(Context, Entity.Index);
        }

        public override string ToString()
        {
            return $"{Entity.ToString()} - Context: {Context}";
        }

        [BurstCompatible]
        public FixedString64Bytes ToFixedString()
        {
            FixedString64Bytes fs = new FixedString64Bytes();
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            fs.Append(Entity.ToFixedString());
            fs.Append((FixedString32Bytes)" - Context: ");
            fs.Append(Context);
            return fs;
        }
    }
}
