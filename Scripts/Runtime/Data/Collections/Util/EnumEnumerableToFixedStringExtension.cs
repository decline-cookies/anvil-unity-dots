using Anvil.Unity.DOTS.Core;
using Anvil.Unity.DOTS.Util;
using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// A collection of extension methods for generating burst compatible strings from burst compatible collections of
    /// Enum elements.
    /// This is an Enum specific version of what is offered in <see cref="EnumerableToFixedStringExtension"/>/
    /// </summary>
    [BurstCompile]
    public static class EnumEnumerableToFixedStringExtension
    {
        /// <summary>
        /// Returns a burst compatible string of the collection using
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </summary>
        /// <param name="collection">The collection to generate the string from.</param>
        /// <typeparam name="TElement">The element's type. Must be an enum.</typeparam>
        /// <typeparam name="TOutputString">
        /// The output's string type. This must be large enough to contain the strings of all elements plus one byte
        /// per element for the comma.
        /// </typeparam>
        /// <returns>
        /// <inheritdoc cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TElement, TOutputString>(ref this UnsafeList<TElement> collection)
            where TElement : unmanaged, Enum
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            return UnsafeUtility.As<UnsafeList<TElement>, UnsafeList<EnumWrapper<TElement>>>(ref collection)
                .ToFixedString<EnumWrapper<TElement>, FixedString32Bytes, TOutputString>();
        }

        /// <summary>
        /// Returns a burst compatible string of the collection using
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </summary>
        /// <param name="collection">The collection to generate the string from.</param>
        /// <typeparam name="TElement">The element's type. Must be an enum.</typeparam>
        /// <typeparam name="TOutputString">
        /// The output's string type. This must be large enough to contain the strings of all elements plus one byte
        /// per element for the comma.
        /// </typeparam>
        /// <returns>
        /// <inheritdoc cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TElement, TOutputString>(ref this NativeList<TElement> collection)
            where TElement : unmanaged, Enum
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            return UnsafeUtility.As<NativeList<TElement>, NativeList<EnumWrapper<TElement>>>(ref collection)
                .ToFixedString<EnumWrapper<TElement>, FixedString32Bytes, TOutputString>();
        }

        /// <summary>
        /// Returns a burst compatible string of the collection using
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </summary>
        /// <param name="collection">The collection to generate the string from.</param>
        /// <typeparam name="TElement">The element's type. Must be an enum.</typeparam>
        /// <typeparam name="TOutputString">
        /// The output's string type. This must be large enough to contain the strings of all elements plus one byte
        /// per element for the comma.
        /// </typeparam>
        /// <returns>
        /// <inheritdoc cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>
        /// </returns>
        public static TOutputString ToFixedString<TElement, TOutputString>(ref this UnsafeArray<TElement> collection)
            where TElement : unmanaged, Enum
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            return UnsafeUtility.As<UnsafeArray<TElement>, UnsafeArray<EnumWrapper<TElement>>>(ref collection)
                .ToFixedString<EnumWrapper<TElement>, FixedString32Bytes, TOutputString>();
        }

        /// <summary>
        /// Returns a burst compatible string of the collection using
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </summary>
        /// <param name="collection">The collection to generate the string from.</param>
        /// <typeparam name="TElement">The element's type. Must be an enum.</typeparam>
        /// <typeparam name="TOutputString">
        /// The output's string type. This must be large enough to contain the strings of all elements plus one byte
        /// per element for the comma.
        /// </typeparam>
        /// <returns>
        /// <inheritdoc cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>
        /// </returns>
        public static TOutputString ToFixedString<TElement, TOutputString>(ref this NativeArray<TElement> collection)
            where TElement : unmanaged, Enum
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            return UnsafeUtility.As<NativeArray<TElement>, NativeArray<EnumWrapper<TElement>>>(ref collection)
                .ToFixedString<EnumWrapper<TElement>, FixedString32Bytes, TOutputString>();
        }

        /// <summary>
        /// Returns a burst compatible string of the collection using
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </summary>
        /// <param name="collection">The collection to generate the string from.</param>
        /// <typeparam name="TElement">The element's type. Must be an enum.</typeparam>
        /// <typeparam name="TOutputString">
        /// The output's string type. This must be large enough to contain the strings of all elements plus one byte
        /// per element for the comma.
        /// </typeparam>
        /// <returns>
        /// <inheritdoc cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>
        /// </returns>
        public static TOutputString ToFixedString<TElement, TOutputString>(ref this NativeArray<TElement>.ReadOnly collection)
            where TElement : unmanaged, Enum
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            return UnsafeUtility.As<NativeArray<TElement>.ReadOnly, NativeArray<EnumWrapper<TElement>>.ReadOnly>(ref collection)
                .ToFixedString<EnumWrapper<TElement>, FixedString32Bytes, TOutputString>();
        }

        /// <summary>
        /// Returns a burst compatible string of the collection using
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </summary>
        /// <param name="collection">The collection to generate the string from.</param>
        /// <typeparam name="TElement">The element's type. Must be an enum.</typeparam>
        /// <typeparam name="TOutputString">
        /// The output's string type. This must be large enough to contain the strings of all elements plus one byte
        /// per element for the comma.
        /// </typeparam>
        /// <returns>
        /// <inheritdoc cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TElement, TOutputString>(ref this FixedList32Bytes<TElement> collection)
            where TElement : unmanaged, Enum
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            return UnsafeUtility.As<FixedList32Bytes<TElement>, FixedList32Bytes<EnumWrapper<TElement>>>(ref collection)
                .ToFixedString<EnumWrapper<TElement>, FixedString32Bytes, TOutputString>();
        }

        /// <summary>
        /// Returns a burst compatible string of the collection using
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </summary>
        /// <param name="collection">The collection to generate the string from.</param>
        /// <typeparam name="TElement">The element's type. Must be an enum.</typeparam>
        /// <typeparam name="TOutputString">
        /// The output's string type. This must be large enough to contain the strings of all elements plus one byte
        /// per element for the comma.
        /// </typeparam>
        /// <returns>
        /// <inheritdoc cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TElement, TOutputString>(ref this FixedList64Bytes<TElement> collection)
            where TElement : unmanaged, Enum
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            return UnsafeUtility.As<FixedList64Bytes<TElement>, FixedList64Bytes<EnumWrapper<TElement>>>(ref collection)
                .ToFixedString<EnumWrapper<TElement>, FixedString32Bytes, TOutputString>();
        }

        /// <summary>
        /// Returns a burst compatible string of the collection using
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </summary>
        /// <param name="collection">The collection to generate the string from.</param>
        /// <typeparam name="TElement">The element's type. Must be an enum.</typeparam>
        /// <typeparam name="TOutputString">
        /// The output's string type. This must be large enough to contain the strings of all elements plus one byte
        /// per element for the comma.
        /// </typeparam>
        /// <returns>
        /// <inheritdoc cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TElement, TOutputString>(ref this FixedList128Bytes<TElement> collection)
            where TElement : unmanaged, Enum
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            return UnsafeUtility.As<FixedList128Bytes<TElement>, FixedList128Bytes<EnumWrapper<TElement>>>(ref collection)
                .ToFixedString<EnumWrapper<TElement>, FixedString32Bytes, TOutputString>();
        }

        /// <summary>
        /// Returns a burst compatible string of the collection using
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </summary>
        /// <param name="collection">The collection to generate the string from.</param>
        /// <typeparam name="TElement">The element's type. Must be an enum.</typeparam>
        /// <typeparam name="TOutputString">
        /// The output's string type. This must be large enough to contain the strings of all elements plus one byte
        /// per element for the comma.
        /// </typeparam>
        /// <returns>
        /// <inheritdoc cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TElement, TOutputString>(ref this FixedList512Bytes<TElement> collection)
            where TElement : unmanaged, Enum
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            return UnsafeUtility.As<FixedList512Bytes<TElement>, FixedList512Bytes<EnumWrapper<TElement>>>(ref collection)
                .ToFixedString<EnumWrapper<TElement>, FixedString32Bytes, TOutputString>();
        }

        /// <summary>
        /// Returns a burst compatible string of the collection using
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </summary>
        /// <param name="collection">The collection to generate the string from.</param>
        /// <typeparam name="TElement">The element's type. Must be an enum.</typeparam>
        /// <typeparam name="TOutputString">
        /// The output's string type. This must be large enough to contain the strings of all elements plus one byte
        /// per element for the comma.
        /// </typeparam>
        /// <returns>
        /// <inheritdoc cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TElement, TOutputString>(ref this FixedList4096Bytes<TElement> collection)
            where TElement : unmanaged, Enum
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            return UnsafeUtility.As<FixedList4096Bytes<TElement>, FixedList4096Bytes<EnumWrapper<TElement>>>(ref collection)
                .ToFixedString<EnumWrapper<TElement>, FixedString32Bytes, TOutputString>();
        }

        /// <summary>
        /// Wraps an enum of type <see cref="T"/> and implements <see cref="IToFixedString{T}"/>.
        /// </summary>
        private struct EnumWrapper<T> : IToFixedString<FixedString32Bytes>, IEquatable<EnumWrapper<T>>
            where T : unmanaged, Enum
        {
            private T m_Value;

            public FixedString32Bytes ToFixedString() => $"{m_Value.ToBurstValue()}";

            public bool Equals(EnumWrapper<T> other) => other.m_Value.ToBurstValue() == m_Value.ToBurstValue();
        }
    }
}
