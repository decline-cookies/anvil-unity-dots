using System;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Data
{
    public struct VDID : IEquatable<VDID>
    {
        public const int UNSET_CONTEXT = -1;
        
        public static bool operator==(VDID lhs, VDID rhs)
        {
            return lhs.Entity == rhs.Entity && lhs.Context == rhs.Context;
        }
        
        public static bool operator!=(VDID lhs, VDID rhs)
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

        public VDID(Entity entity)
        {
            Entity = entity;
            Context = UNSET_CONTEXT;
        }

        internal VDID(Entity entity, int context)
        {
            Entity = entity;
            Context = context;
        }

        public bool Equals(VDID other)
        {
            return Entity == other.Entity && Context == other.Context;
        }
        
        public override bool Equals(object compare)
        {
            return compare is VDID id && Equals(id);
        }

        public override int GetHashCode()
        {
            return (Entity, Context).GetHashCode();
        }
    }
}
