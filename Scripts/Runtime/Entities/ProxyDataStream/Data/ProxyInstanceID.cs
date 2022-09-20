using System;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    internal readonly struct ProxyInstanceID : IEquatable<ProxyInstanceID>
    {
        public static bool operator ==(ProxyInstanceID lhs, ProxyInstanceID rhs)
        {
            return lhs.Entity == rhs.Entity && lhs.Context == rhs.Context;
        }

        public static bool operator !=(ProxyInstanceID lhs, ProxyInstanceID rhs)
        {
            return !(lhs == rhs);
        }

        public readonly Entity Entity;
        public readonly byte Context;

        public ProxyInstanceID(Entity entity, byte context)
        {
            Entity = entity;
            Context = context;
        }

        public bool Equals(ProxyInstanceID other)
        {
            return this == other;
        }

        public override bool Equals(object compare)
        {
            return compare is ProxyInstanceID id && Equals(id);
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
            uint uintContext = Context;
            uint rol5 = (uintContext << 5) | (uintContext >> 27);
            return ((int)rol5 + Context) ^ Entity.Index;
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
