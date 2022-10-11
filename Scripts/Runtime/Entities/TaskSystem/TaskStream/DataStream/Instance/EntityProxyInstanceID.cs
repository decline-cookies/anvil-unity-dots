using Anvil.Unity.DOTS.Util;
using System;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal readonly struct EntityProxyInstanceID : IEquatable<EntityProxyInstanceID>
    {
        public static bool operator ==(EntityProxyInstanceID lhs, EntityProxyInstanceID rhs)
        {
            return lhs.m_Entity == rhs.m_Entity && lhs.Context == rhs.Context;
        }

        public static bool operator !=(EntityProxyInstanceID lhs, EntityProxyInstanceID rhs)
        {
            return !(lhs == rhs);
        }

        private readonly Entity m_Entity;
        public readonly byte Context;

        public EntityProxyInstanceID(Entity entity, byte context)
        {
            m_Entity = entity;
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
            return HashCodeUtil.GetHashCode(Context, m_Entity.Index);
        }

        public override string ToString()
        {
            return $"{m_Entity.ToString()} - Context: {Context}";
        }

        [BurstCompatible]
        public FixedString64Bytes ToFixedString()
        {
            FixedString64Bytes fs = new FixedString64Bytes();
            fs.Append(m_Entity.ToFixedString());
            fs.Append(" - Context: ");
            fs.Append(Context);
            return fs;
        }
    }
}
