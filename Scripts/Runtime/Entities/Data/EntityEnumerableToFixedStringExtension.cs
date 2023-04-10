using Anvil.Unity.DOTS.Core;
using Anvil.Unity.DOTS.Data;
using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A collection of extension methods for generating burst compatible strings from burst compatible collections of
    /// <see cref="Entity"/> elements.
    /// This is an <see cref="Entity"/> specific version of what is offered in <see cref="EnumerableToFixedStringExtension"/>/
    /// </summary>
    [BurstCompile]
    public static class EntityEnumerableToFixedStringExtension
    {
        /// <summary>
        /// Returns a burst compatible string of the collection using
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </summary>
        /// <param name="collection">The collection to generate the string from.</param>
        /// <typeparam name="TOutputString">
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </typeparam>
        /// <returns>
        /// <inheritdoc cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TOutputString>(ref this UnsafeParallelHashSet<Entity> collection)
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            return UnsafeUtility.As<UnsafeParallelHashSet<Entity>, UnsafeParallelHashSet<EntityWrapper>>(ref collection)
                .ToFixedString<EntityWrapper, FixedString64Bytes, TOutputString>();
        }

        /// <summary>
        /// Returns a burst compatible string of the collection using
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </summary>
        /// <param name="collection">The collection to generate the string from.</param>
        /// <typeparam name="TOutputString">
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </typeparam>
        /// <returns>
        /// <inheritdoc cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TOutputString>(ref this NativeParallelHashSet<Entity> collection)
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            return UnsafeUtility.As<NativeParallelHashSet<Entity>, NativeParallelHashSet<EntityWrapper>>(ref collection)
                .ToFixedString<EntityWrapper, FixedString64Bytes, TOutputString>();
        }

        /// <summary>
        /// Returns a burst compatible string of the collection using
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </summary>
        /// <param name="collection">The collection to generate the string from.</param>
        /// <typeparam name="TOutputString">
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </typeparam>
        /// <returns>
        /// <inheritdoc cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>
        /// </returns>
        public static TOutputString ToFixedString<TOutputString>(ref this UnsafeList<Entity> collection)
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            return UnsafeUtility.As<UnsafeList<Entity>, UnsafeList<EntityWrapper>>(ref collection)
                .ToFixedString<EntityWrapper, FixedString64Bytes, TOutputString>();
        }

        /// <summary>
        /// Returns a burst compatible string of the collection using
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </summary>
        /// <param name="collection">The collection to generate the string from.</param>
        /// <typeparam name="TOutputString">
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </typeparam>
        /// <returns>
        /// <inheritdoc cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TOutputString>(ref this NativeList<Entity> collection)
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            return UnsafeUtility.As<NativeList<Entity>, NativeList<EntityWrapper>>(ref collection)
                .ToFixedString<EntityWrapper, FixedString64Bytes, TOutputString>();
        }

        /// <summary>
        /// Returns a burst compatible string of the collection using
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </summary>
        /// <param name="collection">The collection to generate the string from.</param>
        /// <typeparam name="TOutputString">
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </typeparam>
        /// <returns>
        /// <inheritdoc cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TOutputString>(ref this UnsafeArray<Entity> collection)
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            return UnsafeUtility.As<UnsafeArray<Entity>, UnsafeArray<EntityWrapper>>(ref collection)
                .ToFixedString<EntityWrapper, FixedString64Bytes, TOutputString>();
        }

        /// <summary>
        /// Returns a burst compatible string of the collection using
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </summary>
        /// <param name="collection">The collection to generate the string from.</param>
        /// <typeparam name="TOutputString">
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </typeparam>
        /// <returns>
        /// <inheritdoc cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TOutputString>(ref this NativeArray<Entity> collection)
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            return UnsafeUtility.As<NativeArray<Entity>, NativeArray<EntityWrapper>>(ref collection)
                .ToFixedString<EntityWrapper, FixedString64Bytes, TOutputString>();
        }

        /// <summary>
        /// Returns a burst compatible string of the collection using
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </summary>
        /// <param name="collection">The collection to generate the string from.</param>
        /// <typeparam name="TOutputString">
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </typeparam>
        /// <returns>
        /// <inheritdoc cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TOutputString>(ref this NativeArray<Entity>.ReadOnly collection)
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            return UnsafeUtility.As<NativeArray<Entity>.ReadOnly, NativeArray<EntityWrapper>.ReadOnly>(ref collection)
                .ToFixedString<EntityWrapper, FixedString64Bytes, TOutputString>();
        }

        /// <summary>
        /// Returns a burst compatible string of the collection using
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </summary>
        /// <param name="collection">The collection to generate the string from.</param>
        /// <returns>
        /// <inheritdoc cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>
        /// </returns>
        /// <remarks>The outputted string size assumes each element's string is 64-bytes</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedString512Bytes ToFixedString(ref this FixedList32Bytes<Entity> collection)
        {
            return UnsafeUtility.As<FixedList32Bytes<Entity>, FixedList32Bytes<EntityWrapper>>(ref collection)
                .ToFixedString<EntityWrapper, FixedString64Bytes, FixedString512Bytes>();
        }

        /// <summary>
        /// Returns a burst compatible string of the collection using
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </summary>
        /// <param name="collection">The collection to generate the string from.</param>
        /// <returns>
        /// <inheritdoc cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>
        /// </returns>
        /// <remarks>The outputted string size assumes each element's string is 64-bytes</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedString512Bytes ToFixedString(ref this FixedList64Bytes<Entity> collection)
        {
            return UnsafeUtility.As<FixedList64Bytes<Entity>, FixedList64Bytes<EntityWrapper>>(ref collection)
                .ToFixedString<EntityWrapper, FixedString64Bytes, FixedString512Bytes>();
        }

        /// <summary>
        /// Returns a burst compatible string of the collection using
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </summary>
        /// <param name="collection">The collection to generate the string from.</param>
        /// <returns>
        /// <inheritdoc cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>
        /// </returns>
        /// <remarks>The outputted string size assumes each element's string is 64-bytes</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedString4096Bytes ToFixedString(ref this FixedList128Bytes<Entity> collection)
        {
            return UnsafeUtility.As<FixedList128Bytes<Entity>, FixedList128Bytes<EntityWrapper>>(ref collection)
                .ToFixedString<EntityWrapper, FixedString64Bytes, FixedString4096Bytes>();
        }

        /// <summary>
        /// Returns a burst compatible string of the collection using
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </summary>
        /// <param name="collection">The collection to generate the string from.</param>
        /// <returns>
        /// <inheritdoc cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>
        /// </returns>
        /// <remarks>The outputted string size assumes each element's string is 64-bytes</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedString4096Bytes ToFixedString(ref this FixedList512Bytes<Entity> collection)
        {
            return UnsafeUtility.As<FixedList512Bytes<Entity>, FixedList512Bytes<EntityWrapper>>(ref collection)
                .ToFixedString<EntityWrapper, FixedString64Bytes, FixedString4096Bytes>();
        }

        /// <summary>
        /// Returns a burst compatible string of the collection using
        /// <see cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>.
        /// </summary>
        /// <param name="collection">The collection to generate the string from.</param>
        /// <returns>
        /// <inheritdoc cref="EnumerableToFixedStringExtension.ToFixedString{TCollection, TElement, TElementString, TOutputString}"/>
        /// </returns>
        /// <remarks>The outputted string size assumes each element's string is 64-bytes</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FixedString4096Bytes ToFixedString(ref this FixedList4096Bytes<Entity> collection)
        {
            return UnsafeUtility.As<FixedList4096Bytes<Entity>, FixedList4096Bytes<EntityWrapper>>(ref collection)
                .ToFixedString<EntityWrapper, FixedString64Bytes, FixedString4096Bytes>();
        }

        /// <summary>
        /// Wraps an entity and implements <see cref="IToFixedString{T}"/>.
        /// </summary>
        private struct EntityWrapper : IToFixedString<FixedString64Bytes>, IEquatable<EntityWrapper>
        {
            private Entity Entity;

            public FixedString64Bytes ToFixedString() => Entity.ToFixedString();

            public bool Equals(EntityWrapper other) => other.Entity == Entity;
        }
    }
}