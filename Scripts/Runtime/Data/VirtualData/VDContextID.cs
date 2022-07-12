using System;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Data
{
    public struct VDContextID : IEquatable<VDContextID>
    {
        public const int UNSET_CONTEXT = -1;
        
        public static bool operator==(VDContextID lhs, VDContextID rhs)
        {
            return lhs.Entity == rhs.Entity && lhs.Context == rhs.Context;
        }
        
        public static bool operator!=(VDContextID lhs, VDContextID rhs)
        {
            return !(lhs == rhs);
        }
        
        public Entity Entity
        {
            get;
        }

        public int Context
        {
            get;
            internal set;
        }

        public VDContextID(Entity entity)
        {
            Entity = entity;
            Context = UNSET_CONTEXT;
        }

        internal VDContextID(VDContextID contextID, int context)
        {
            Entity = contextID.Entity;
            Context = context;
        }

        public bool Equals(VDContextID other)
        {
            return Entity == other.Entity && Context == other.Context;
        }
        
        public override bool Equals(object compare)
        {
            return compare is VDContextID id && Equals(id);
        }

        public override int GetHashCode()
        {
            return (Entity, Context).GetHashCode();
        }
    }
}
