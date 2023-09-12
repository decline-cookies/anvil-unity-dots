using Anvil.Unity.DOTS.Core;
using Anvil.Unity.DOTS.Util;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A typed wrapper for an <see cref="int"/> to denote a variant for a <see cref="IEntitySpawnDefinition"/>'s a
    /// associated prototype. 
    /// </summary>
    public readonly struct PrototypeVariant : IEquatable<PrototypeVariant>,
                                              IToFixedString<FixedString32Bytes>
    {
        public static readonly int UNSET_VALUE = default;
        
        /// <summary>
        /// Converts an enum's underlying value into a <see cref="PrototypeVariant"/> instance.
        /// </summary>
        /// <param name="enumValue">The enum value to convert.</param>
        /// <typeparam name="T">The type of enum</typeparam>
        /// <returns>A <see cref="PrototypeVariant"/> that represents that enum.</returns>
        //TODO: Possibly remove this and just use an ID provider to pass in raw ints. See: https://github.com/decline-cookies/anvil-unity-dots/pull/271/files#r1265569116
        public static PrototypeVariant FromEnum<T>(T enumValue)
            where T : unmanaged, Enum
        {
            return new PrototypeVariant((int)enumValue.ToBurstValue());
        }

        public static bool operator ==(PrototypeVariant lhs, PrototypeVariant rhs)
        {
            return lhs.VariantID == rhs.VariantID;
        }

        public static bool operator !=(PrototypeVariant lhs, PrototypeVariant rhs)
        {
            return lhs.VariantID != rhs.VariantID;
        }

        static PrototypeVariant()
        {
            // Make sure that the default actor ID matches an unset ID. Otherwise
            // an uninitialized PrototypeVariant may conflict with a valid ID.
            Debug.Assert(new PrototypeVariant().VariantID == UNSET_VALUE);
        }
        
        /// <summary>
        /// The backing int ID the prototype is associated with
        /// </summary>
        public readonly int VariantID;

        /// <summary>
        /// Whether this instance has been created with a valid ID value
        /// </summary>
        public bool IsUnset
        {
            get => VariantID == UNSET_VALUE;
        }

        /// <summary>
        /// Creates a new instance with a specific variant ID
        /// </summary>
        /// <param name="variantID">The variant ID to use</param>
        private PrototypeVariant(int variantID)
        {
            Debug.Assert(variantID != UNSET_VALUE);
            VariantID = variantID;
        }
        
        /// <summary>
        /// Helper function to calculate a unique has for a
        /// <see cref="IEntitySpawnDefinition"/>/<see cref="PrototypeVariant"/> pair.
        /// </summary>
        /// <typeparam name="TEntitySpawnDefinition">The type of <see cref="IEntitySpawnDefinition"/></typeparam>
        /// <returns>The hash that represents the pair.</returns>
        public int GetVariantHashForDefinition<TEntitySpawnDefinition>()
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            return HashCodeUtil.GetHashCode(BurstRuntime.GetHashCode32<TEntitySpawnDefinition>(), GetHashCode());
        }

        public bool Equals(PrototypeVariant other) => other.VariantID == VariantID;

        public override bool Equals(object compare) => compare is PrototypeVariant variant && Equals(variant);

        public override int GetHashCode() => VariantID;

        public override string ToString()
        {
            return VariantID.ToString();
        }

        public FixedString32Bytes ToFixedString()
        {
            return $"{VariantID}";
        }
    }
}
