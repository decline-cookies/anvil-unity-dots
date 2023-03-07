using Anvil.CSharp.Logging;
using System;
using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Debug = UnityEngine.Debug;

namespace Anvil.Unity.Collections
{
    /// <summary>
    /// A collection of extension methods for <see cref="FixedList32Bytes{T}"/> (and friends) that require internal
    /// access to function.
    /// </summary>
    [BurstCompatible]
    public static unsafe class FixedListInternalExtension
    {
        /// <summary>
        /// Finds the index of the first occurrence of a particular value interpreting the lists elements as a specific
        /// type.
        /// </summary>
        /// <typeparam name="TEnum">The type of elements in this list.</typeparam>
        /// <typeparam name="TUnderlying">
        /// The value type and the type that list elements will be reinterpreted as.
        /// </typeparam>
        /// <param name="list">The list to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>The index of the first occurrence of the value. Returns -1 if no occurrence is found.</returns>
        /// <remarks>
        /// This is a copy of <see cref="FixedList32Bytes{T}.IndexOf{T,U}"/> that supports <see cref="Enum"/> elements
        /// </remarks>
        public static int IndexOf<TEnum, TUnderlying>(this in FixedList32Bytes<TEnum> list, TUnderlying value)
            where TEnum : unmanaged, Enum
            where TUnderlying : unmanaged, IEquatable<TUnderlying>
        {
            DEBUG_AssertEnumUnderlyingType<TEnum, TUnderlying>();
            // The types don't really need to be compatible just the same size
            Debug.Assert(UnsafeUtility.SizeOf<TEnum>() == UnsafeUtility.SizeOf<TUnderlying>());

            return NativeArrayExtensions.IndexOf<TUnderlying, TUnderlying>(list.Buffer, list.length, value);
        }

        /// <summary>
        /// Finds the index of the first occurrence of a particular value interpreting the lists elements as a specific
        /// type.
        /// </summary>
        /// <typeparam name="TEnum">The type of elements in this list.</typeparam>
        /// <typeparam name="TUnderlying">
        /// The value type and the type that list elements will be reinterpreted as.
        /// </typeparam>
        /// <param name="list">The list to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>The index of the first occurrence of the value. Returns -1 if no occurrence is found.</returns>
        /// <remarks>
        /// This is a copy of <see cref="FixedList32Bytes{T}.IndexOf{T,U}"/> that supports <see cref="Enum"/> elements
        /// </remarks>
        public static int IndexOf<TEnum, TUnderlying>(this in FixedList64Bytes<TEnum> list, TUnderlying value)
            where TEnum : unmanaged, Enum
            where TUnderlying : unmanaged, IEquatable<TUnderlying>
        {
            DEBUG_AssertEnumUnderlyingType<TEnum, TUnderlying>();
            // The types don't really need to be compatible just the same size
            Debug.Assert(UnsafeUtility.SizeOf<TEnum>() == UnsafeUtility.SizeOf<TUnderlying>());

            return NativeArrayExtensions.IndexOf<TUnderlying, TUnderlying>(list.Buffer, list.length, value);
        }

        /// <summary>
        /// Finds the index of the first occurrence of a particular value interpreting the lists elements as a specific
        /// type.
        /// </summary>
        /// <typeparam name="TEnum">The type of elements in this list.</typeparam>
        /// <typeparam name="TUnderlying">
        /// The value type and the type that list elements will be reinterpreted as.
        /// </typeparam>
        /// <param name="list">The list to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>The index of the first occurrence of the value. Returns -1 if no occurrence is found.</returns>
        /// <remarks>
        /// This is a copy of <see cref="FixedList32Bytes{T}.IndexOf{T,U}"/> that supports <see cref="Enum"/> elements
        /// </remarks>
        public static int IndexOf<TEnum, TUnderlying>(this in FixedList128Bytes<TEnum> list, TUnderlying value)
            where TEnum : unmanaged, Enum
            where TUnderlying : unmanaged, IEquatable<TUnderlying>
        {
            DEBUG_AssertEnumUnderlyingType<TEnum, TUnderlying>();
            // The types don't really need to be compatible just the same size
            Debug.Assert(UnsafeUtility.SizeOf<TEnum>() == UnsafeUtility.SizeOf<TUnderlying>());

            return NativeArrayExtensions.IndexOf<TUnderlying, TUnderlying>(list.Buffer, list.length, value);
        }

        /// <summary>
        /// Finds the index of the first occurrence of a particular value interpreting the lists elements as a specific
        /// type.
        /// </summary>
        /// <typeparam name="TEnum">The type of elements in this list.</typeparam>
        /// <typeparam name="TUnderlying">
        /// The value type and the type that list elements will be reinterpreted as.
        /// </typeparam>
        /// <param name="list">The list to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>The index of the first occurrence of the value. Returns -1 if no occurrence is found.</returns>
        /// <remarks>
        /// This is a copy of <see cref="FixedList32Bytes{T}.IndexOf{T,U}"/> that supports <see cref="Enum"/> elements
        /// </remarks>
        public static int IndexOf<TEnum, TUnderlying>(this in FixedList512Bytes<TEnum> list, TUnderlying value)
            where TEnum : unmanaged, Enum
            where TUnderlying : unmanaged, IEquatable<TUnderlying>
        {
            DEBUG_AssertEnumUnderlyingType<TEnum, TUnderlying>();
            // The types don't really need to be compatible just the same size
            Debug.Assert(UnsafeUtility.SizeOf<TEnum>() == UnsafeUtility.SizeOf<TUnderlying>());

            return NativeArrayExtensions.IndexOf<TUnderlying, TUnderlying>(list.Buffer, list.length, value);
        }

        /// <summary>
        /// Finds the index of the first occurrence of a particular value interpreting the lists elements as a specific
        /// type.
        /// </summary>
        /// <typeparam name="TEnum">The type of elements in this list.</typeparam>
        /// <typeparam name="TUnderlying">
        /// The value type and the type that list elements will be reinterpreted as.
        /// </typeparam>
        /// <param name="list">The list to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>The index of the first occurrence of the value. Returns -1 if no occurrence is found.</returns>
        /// <remarks>
        /// This is a copy of <see cref="FixedList32Bytes{T}.IndexOf{T,U}"/> that supports <see cref="Enum"/> elements
        /// </remarks>
        public static int IndexOf<TEnum, TUnderlying>(this in FixedList4096Bytes<TEnum> list, TUnderlying value)
            where TEnum : unmanaged, Enum
            where TUnderlying : unmanaged, IEquatable<TUnderlying>
        {
            DEBUG_AssertEnumUnderlyingType<TEnum, TUnderlying>();
            // The types don't really need to be compatible just the same size
            Debug.Assert(UnsafeUtility.SizeOf<TEnum>() == UnsafeUtility.SizeOf<TUnderlying>());

            return NativeArrayExtensions.IndexOf<TUnderlying, TUnderlying>(list.Buffer, list.length, value);
        }

        /// <summary>
        /// Returns true if a particular value is present in this list.
        /// </summary>
        /// <typeparam name="TEnum">The type of elements in this list.</typeparam>
        /// <typeparam name="TUnderlying">
        /// The value type and the type that list elements will be reinterpreted as.
        /// </typeparam>
        /// <param name="list">The list to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>True if the value is present in this list.</returns>
        /// /// <remarks>
        /// This is a copy of <see cref="FixedList32Bytes{T}.Contains{T,U}"/> that supports <see cref="Enum"/> elements
        /// </remarks>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        public static bool Contains<TEnum, TUnderlying>(this in FixedList32Bytes<TEnum> list, TUnderlying value)
            where TEnum : unmanaged, Enum
            where TUnderlying : unmanaged, IEquatable<TUnderlying>
        {
            return list.IndexOf(value) != -1;
        }

        /// <summary>
        /// Returns true if a particular value is present in this list.
        /// </summary>
        /// <typeparam name="TEnum">The type of elements in this list.</typeparam>
        /// <typeparam name="TUnderlying">
        /// The value type and the type that list elements will be reinterpreted as.
        /// </typeparam>
        /// <param name="list">The list to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>True if the value is present in this list.</returns>
        /// /// <remarks>
        /// This is a copy of <see cref="FixedList32Bytes{T}.Contains{T,U}"/> that supports <see cref="Enum"/> elements
        /// </remarks>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        public static bool Contains<TEnum, TUnderlying>(this in FixedList64Bytes<TEnum> list, TUnderlying value)
            where TEnum : unmanaged, Enum
            where TUnderlying : unmanaged, IEquatable<TUnderlying>
        {
            return list.IndexOf(value) != -1;
        }

        /// <summary>
        /// Returns true if a particular value is present in this list.
        /// </summary>
        /// <typeparam name="TEnum">The type of elements in this list.</typeparam>
        /// <typeparam name="TUnderlying">
        /// The value type and the type that list elements will be reinterpreted as.
        /// </typeparam>
        /// <param name="list">The list to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>True if the value is present in this list.</returns>
        /// /// <remarks>
        /// This is a copy of <see cref="FixedList32Bytes{T}.Contains{T,U}"/> that supports <see cref="Enum"/> elements
        /// </remarks>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        public static bool Contains<TEnum, TUnderlying>(this in FixedList128Bytes<TEnum> list, TUnderlying value)
            where TEnum : unmanaged, Enum
            where TUnderlying : unmanaged, IEquatable<TUnderlying>
        {
            return list.IndexOf(value) != -1;
        }

        /// <summary>
        /// Returns true if a particular value is present in this list.
        /// </summary>
        /// <typeparam name="TEnum">The type of elements in this list.</typeparam>
        /// <typeparam name="TUnderlying">
        /// The value type and the type that list elements will be reinterpreted as.
        /// </typeparam>
        /// <param name="list">The list to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>True if the value is present in this list.</returns>
        /// /// <remarks>
        /// This is a copy of <see cref="FixedList32Bytes{T}.Contains{T,U}"/> that supports <see cref="Enum"/> elements
        /// </remarks>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        public static bool Contains<TEnum, TUnderlying>(this in FixedList512Bytes<TEnum> list, TUnderlying value)
            where TEnum : unmanaged, Enum
            where TUnderlying : unmanaged, IEquatable<TUnderlying>
        {
            return list.IndexOf(value) != -1;
        }

        /// <summary>
        /// Returns true if a particular value is present in this list.
        /// </summary>
        /// <typeparam name="TEnum">The type of elements in this list.</typeparam>
        /// <typeparam name="TUnderlying">
        /// The value type and the type that list elements will be reinterpreted as.
        /// </typeparam>
        /// <param name="list">The list to search.</param>
        /// <param name="value">The value to locate.</param>
        /// <returns>True if the value is present in this list.</returns>
        /// /// <remarks>
        /// This is a copy of <see cref="FixedList32Bytes{T}.Contains{T,U}"/> that supports <see cref="Enum"/> elements
        /// </remarks>
        [BurstCompatible(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
        public static bool Contains<TEnum, TUnderlying>(this in FixedList4096Bytes<TEnum> list, TUnderlying value)
            where TEnum : unmanaged, Enum
            where TUnderlying : unmanaged, IEquatable<TUnderlying>
        {
            return list.IndexOf(value) != -1;
        }

        [BurstDiscard]
        [Conditional("ANVIL_DEBUG_SAFETY")]
        private static void DEBUG_AssertEnumUnderlyingType<TEnum, TExpectedUnderlying>() where TEnum : unmanaged, Enum
        {
            Type actualUnderlyingEnumType = Enum.GetUnderlyingType(typeof(TEnum));
            if (actualUnderlyingEnumType != typeof(TExpectedUnderlying))
            {
                throw new Exception($"Enum's underlying type does not match the type specified. Enum: {typeof(TEnum).GetReadableName()}, UnderlyingType: {actualUnderlyingEnumType.GetReadableName()}, ExpectedUnderlyingType: {typeof(TExpectedUnderlying).GetReadableName()}");
            }
        }
    }
}