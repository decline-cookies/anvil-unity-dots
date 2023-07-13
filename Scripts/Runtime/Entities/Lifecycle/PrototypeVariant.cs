using Anvil.Unity.DOTS.Core;
using Anvil.Unity.DOTS.Util;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    public readonly struct PrototypeVariant : IEquatable<PrototypeVariant>,
                                              IComponentData,
                                              IToFixedString<FixedString32Bytes>
    {
        public static readonly int UNSET_VALUE = default;

        public static PrototypeVariant FromEnum<T>(T enumValue)
            where T : unmanaged, Enum
        {
            return new PrototypeVariant((int)enumValue.ToBurstValue());
        }

        public static bool operator ==(PrototypeVariant lhs, PrototypeVariant rhs)
        {
            return lhs.Value == rhs.Value;
        }

        public static bool operator !=(PrototypeVariant lhs, PrototypeVariant rhs)
        {
            return lhs.Value != rhs.Value;
        }

        static PrototypeVariant()
        {
            // Make sure that the default actor ID matches an unset ID. Otherwise
            // an uninitialized ActorID may conflict with a valid ID.
            Debug.Assert(new PrototypeVariant().Value == UNSET_VALUE);
        }

        public readonly int Value;

        public bool IsUnset
        {
            get => Value == UNSET_VALUE;
        }

        public PrototypeVariant(int variant)
        {
            Debug.Assert(variant != UNSET_VALUE);
            Value = variant;
        }
        
        public int GetVariantHashForDefinition<TEntitySpawnDefinition>()
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            return HashCodeUtil.GetHashCode(BurstRuntime.GetHashCode32<TEntitySpawnDefinition>(), GetHashCode());
        }

        public bool Equals(PrototypeVariant other) => other.Value == Value;

        public override bool Equals(object compare) => compare is PrototypeVariant variant && Equals(variant);

        public override int GetHashCode() => Value;

        public override string ToString()
        {
            return Value.ToString();
        }

        public FixedString32Bytes ToFixedString()
        {
            return $"{Value}";
        }
    }
}
