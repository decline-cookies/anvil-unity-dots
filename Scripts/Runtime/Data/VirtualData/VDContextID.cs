using System;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Data
{
    internal readonly struct VDContextID : IEquatable<VDContextID>
    {
        public static bool operator==(VDContextID lhs, VDContextID rhs)
        {
            return lhs.Entity == rhs.Entity && lhs.Context == rhs.Context;
        }

        public static bool operator!=(VDContextID lhs, VDContextID rhs)
        {
            return !(lhs == rhs);
        }
        
        public readonly Entity Entity;
        public readonly uint Context;
        
        internal VDContextID(Entity entity, uint context)
        {
            Entity = entity;
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
            //Taken from ValueTuple.cs GetHashCode. 
            //https://github.com/dotnet/roslyn/blob/main/src/Compilers/Test/Resources/Core/NetFX/ValueTuple/ValueTuple.cs
            //Licence is a-ok as per the top of the linked file:
            // - Licensed to the .NET Foundation under one or more agreements.
            // - The .NET Foundation licenses this file to you under the MIT license.
            // - See the LICENSE file in the project root for more information.
            //Unfortunately we can't use directly because it has a static Random class it creates which doesn't jive with Burst
            uint rol5 = (Context << 5) | (Context >> 27);
            return ((int)rol5 + (int)Context) ^ Entity.Index;
        }

        public override string ToString()
        {
            return $"{Entity.ToString()} - Context: {Context}";
        }

        [BurstCompatible]
        public FixedString64Bytes ToFixedString()
        {
            FixedString64Bytes fs = new FixedString64Bytes();
            fs.Append(Entity.ToFixedString());
            fs.Append((FixedString32Bytes)" - Context: ");
            fs.Append(Context);
            return fs;
        }
    }
}
