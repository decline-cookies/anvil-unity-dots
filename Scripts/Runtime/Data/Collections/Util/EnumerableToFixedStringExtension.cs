using Anvil.Unity.DOTS.Core;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// A collection of extension methods for working with burst compatible implementations of <see cref="IEnumerable{T}"/>.
    /// </summary>
    [BurstCompile]
    public static class EnumerableToFixedStringExtension
    {
        /// <summary>
        /// Generates a burst compatible, comma separated, string of a <see cref="UnsafeParallelHashSet{T}"/>'s elements.
        /// Each element must implement <see cref="IToFixedString{T}"/>
        /// </summary>
        /// <param name="collection">The collection of elements</param>
        /// <typeparam name="TElement">The element's type. Must implement <see cref="IToFixedString{T}"/>.</typeparam>
        /// <typeparam name="TElementString">The element's string type.</typeparam>
        /// <typeparam name="TOutputString">
        /// The output's string type. This must be large enough to contain the strings of all elements plus one byte
        /// per element for the comma.
        /// </typeparam>
        /// <returns>The generated string instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TElement, TElementString, TOutputString>(in this UnsafeParallelHashSet<TElement> collection)
            where TElement : unmanaged, IToFixedString<TElementString>, IEquatable<TElement>
            where TElementString : struct, INativeList<byte>, IUTF8Bytes
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            ref var collectionAsRef = ref UnsafeUtilityExtensions.AsRef(in collection);
            UnsafeParallelHashSet<TElement>.Enumerator enumerator = collectionAsRef.GetEnumerator();
            return enumerator.ToFixedString<UnsafeParallelHashSet<TElement>.Enumerator, TElement, TElementString, TOutputString>();
        }

        /// <summary>
        /// Generates a burst compatible, comma separated, string of a <see cref="NativeParallelHashSet{T}"/>'s elements.
        /// Each element must implement <see cref="IToFixedString{T}"/>
        /// </summary>
        /// <param name="collection">The collection of elements</param>
        /// <typeparam name="TElement">The element's type. Must implement <see cref="IToFixedString{T}"/>.</typeparam>
        /// <typeparam name="TElementString">The element's string type.</typeparam>
        /// <typeparam name="TOutputString">
        /// The output's string type. This must be large enough to contain the strings of all elements plus one byte
        /// per element for the comma.
        /// </typeparam>
        /// <returns>The generated string instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TElement, TElementString, TOutputString>(in this NativeParallelHashSet<TElement> collection)
            where TElement : unmanaged, IToFixedString<TElementString>, IEquatable<TElement>
            where TElementString : struct, INativeList<byte>, IUTF8Bytes
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            ref var collectionAsRef = ref UnsafeUtilityExtensions.AsRef(in collection);
            NativeParallelHashSet<TElement>.Enumerator enumerator = collectionAsRef.GetEnumerator();
            return enumerator.ToFixedString<NativeParallelHashSet<TElement>.Enumerator, TElement, TElementString, TOutputString>();
        }

        /// <summary>
        /// Generates a burst compatible, comma separated, string of a <see cref="UnsafeList{T}"/>'s elements.
        /// Each element must implement <see cref="IToFixedString{T}"/>
        /// </summary>
        /// <param name="collection">The collection of elements</param>
        /// <typeparam name="TElement">The element's type. Must implement <see cref="IToFixedString{T}"/>.</typeparam>
        /// <typeparam name="TElementString">The element's string type.</typeparam>
        /// <typeparam name="TOutputString">
        /// The output's string type. This must be large enough to contain the strings of all elements plus one byte
        /// per element for the comma.
        /// </typeparam>
        /// <returns>The generated string instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TElement, TElementString, TOutputString>(in this UnsafeList<TElement> collection)
            where TElement : unmanaged, IToFixedString<TElementString>
            where TElementString : struct, INativeList<byte>, IUTF8Bytes
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            ref var collectionAsRef = ref UnsafeUtilityExtensions.AsRef(in collection);
            UnsafeList<TElement>.Enumerator enumerator = collectionAsRef.GetEnumerator();
            return enumerator.ToFixedString<UnsafeList<TElement>.Enumerator, TElement, TElementString, TOutputString>();
        }

        /// <summary>
        /// Generates a burst compatible, comma separated, string of a <see cref="NativeList{T}"/>'s elements.
        /// Each element must implement <see cref="IToFixedString{T}"/>
        /// </summary>
        /// <param name="collection">The collection of elements</param>
        /// <typeparam name="TElement">The element's type. Must implement <see cref="IToFixedString{T}"/>.</typeparam>
        /// <typeparam name="TElementString">The element's string type.</typeparam>
        /// <typeparam name="TOutputString">
        /// The output's string type. This must be large enough to contain the strings of all elements plus one byte
        /// per element for the comma.
        /// </typeparam>
        /// <returns>The generated string instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TElement, TElementString, TOutputString>(in this NativeList<TElement> collection)
            where TElement : unmanaged, IToFixedString<TElementString>
            where TElementString : struct, INativeList<byte>, IUTF8Bytes
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            ref var collectionAsRef = ref UnsafeUtilityExtensions.AsRef(in collection);
            NativeArray<TElement>.Enumerator enumerator = collectionAsRef.GetEnumerator();
            return enumerator.ToFixedString<NativeArray<TElement>.Enumerator, TElement, TElementString, TOutputString>();
        }

        /// <summary>
        /// Generates a burst compatible, comma separated, string of a <see cref="UnsafeArray{T}"/>'s elements.
        /// Each element must implement <see cref="IToFixedString{T}"/>
        /// </summary>
        /// <param name="collection">The collection of elements</param>
        /// <typeparam name="TElement">The element's type. Must implement <see cref="IToFixedString{T}"/>.</typeparam>
        /// <typeparam name="TElementString">The element's string type.</typeparam>
        /// <typeparam name="TOutputString">
        /// The output's string type. This must be large enough to contain the strings of all elements plus one byte
        /// per element for the comma.
        /// </typeparam>
        /// <returns>The generated string instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TElement, TElementString, TOutputString>(in this UnsafeArray<TElement> collection)
            where TElement : unmanaged, IToFixedString<TElementString>
            where TElementString : struct, INativeList<byte>, IUTF8Bytes
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            ref var collectionAsRef = ref UnsafeUtilityExtensions.AsRef(in collection);
            UnsafeArray<TElement>.Enumerator enumerator = collectionAsRef.GetEnumerator();
            return enumerator.ToFixedString<UnsafeArray<TElement>.Enumerator, TElement, TElementString, TOutputString>();
        }

        /// <summary>
        /// Generates a burst compatible, comma separated, string of a <see cref="NativeArray{T}"/>'s elements.
        /// Each element must implement <see cref="IToFixedString{T}"/>
        /// </summary>
        /// <param name="collection">The collection of elements</param>
        /// <typeparam name="TElement">The element's type. Must implement <see cref="IToFixedString{T}"/>.</typeparam>
        /// <typeparam name="TElementString">The element's string type.</typeparam>
        /// <typeparam name="TOutputString">
        /// The output's string type. This must be large enough to contain the strings of all elements plus one byte
        /// per element for the comma.
        /// </typeparam>
        /// <returns>The generated string instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TElement, TElementString, TOutputString>(in this NativeArray<TElement> collection)
            where TElement : unmanaged, IToFixedString<TElementString>
            where TElementString : struct, INativeList<byte>, IUTF8Bytes
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            ref var collectionAsRef = ref UnsafeUtilityExtensions.AsRef(in collection);
            NativeArray<TElement>.Enumerator enumerator = collectionAsRef.GetEnumerator();
            return enumerator.ToFixedString<NativeArray<TElement>.Enumerator, TElement, TElementString, TOutputString>();
        }

        /// <summary>
        /// Generates a burst compatible, comma separated, string of a <see cref="NativeArray{T}.ReadOnly"/>'s elements.
        /// Each element must implement <see cref="IToFixedString{T}"/>
        /// </summary>
        /// <param name="collection">The collection of elements</param>
        /// <typeparam name="TElement">The element's type. Must implement <see cref="IToFixedString{T}"/>.</typeparam>
        /// <typeparam name="TElementString">The element's string type.</typeparam>
        /// <typeparam name="TOutputString">
        /// The output's string type. This must be large enough to contain the strings of all elements plus one byte
        /// per element for the comma.
        /// </typeparam>
        /// <returns>The generated string instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TElement, TElementString, TOutputString>(in this NativeArray<TElement>.ReadOnly collection)
            where TElement : unmanaged, IToFixedString<TElementString>
            where TElementString : struct, INativeList<byte>, IUTF8Bytes
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            ref var collectionAsRef = ref UnsafeUtilityExtensions.AsRef(in collection);
            NativeArray<TElement>.ReadOnly.Enumerator enumerator = collectionAsRef.GetEnumerator();
            return enumerator.ToFixedString<NativeArray<TElement>.ReadOnly.Enumerator, TElement, TElementString, TOutputString>();
        }

        /// <summary>
        /// Generates a burst compatible, comma separated, string of a <see cref="FixedList32Bytes{T}"/>'s elements.
        /// Each element must implement <see cref="IToFixedString{T}"/>
        /// </summary>
        /// <param name="collection">The collection of elements</param>
        /// <typeparam name="TElement">The element's type. Must implement <see cref="IToFixedString{T}"/>.</typeparam>
        /// <typeparam name="TElementString">The element's string type.</typeparam>
        /// <typeparam name="TOutputString">
        /// The output's string type. This must be large enough to contain the strings of all elements plus one byte
        /// per element for the comma.
        /// </typeparam>
        /// <returns>The generated string instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TElement, TElementString, TOutputString>(in this FixedList32Bytes<TElement> collection)
            where TElement : unmanaged, IToFixedString<TElementString>
            where TElementString : struct, INativeList<byte>, IUTF8Bytes
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            ref var collectionAsRef = ref UnsafeUtilityExtensions.AsRef(in collection);
            FixedList32Bytes<TElement>.Enumerator enumerator = collectionAsRef.GetEnumerator();
            return enumerator.ToFixedString<FixedList32Bytes<TElement>.Enumerator, TElement, TElementString, TOutputString>();
        }

        /// <summary>
        /// Generates a burst compatible, comma separated, string of a <see cref="FixedList64Bytes{T}"/>'s elements.
        /// Each element must implement <see cref="IToFixedString{T}"/>
        /// </summary>
        /// <param name="collection">The collection of elements</param>
        /// <typeparam name="TElement">The element's type. Must implement <see cref="IToFixedString{T}"/>.</typeparam>
        /// <typeparam name="TElementString">The element's string type.</typeparam>
        /// <typeparam name="TOutputString">
        /// The output's string type. This must be large enough to contain the strings of all elements plus one byte
        /// per element for the comma.
        /// </typeparam>
        /// <returns>The generated string instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TElement, TElementString, TOutputString>(in this FixedList64Bytes<TElement> collection)
            where TElement : unmanaged, IToFixedString<TElementString>
            where TElementString : struct, INativeList<byte>, IUTF8Bytes
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            ref var collectionAsRef = ref UnsafeUtilityExtensions.AsRef(in collection);
            FixedList64Bytes<TElement>.Enumerator enumerator = collectionAsRef.GetEnumerator();
            return enumerator.ToFixedString<FixedList64Bytes<TElement>.Enumerator, TElement, TElementString, TOutputString>();
        }

        /// <summary>
        /// Generates a burst compatible, comma separated, string of a <see cref="FixedList128Bytes{T}"/>'s elements.
        /// Each element must implement <see cref="IToFixedString{T}"/>
        /// </summary>
        /// <param name="collection">The collection of elements</param>
        /// <typeparam name="TElement">The element's type. Must implement <see cref="IToFixedString{T}"/>.</typeparam>
        /// <typeparam name="TElementString">The element's string type.</typeparam>
        /// <typeparam name="TOutputString">
        /// The output's string type. This must be large enough to contain the strings of all elements plus one byte
        /// per element for the comma.
        /// </typeparam>
        /// <returns>The generated string instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TElement, TElementString, TOutputString>(in this FixedList128Bytes<TElement> collection)
            where TElement : unmanaged, IToFixedString<TElementString>
            where TElementString : struct, INativeList<byte>, IUTF8Bytes
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            ref var collectionAsRef = ref UnsafeUtilityExtensions.AsRef(in collection);
            FixedList128Bytes<TElement>.Enumerator enumerator = collectionAsRef.GetEnumerator();
            return enumerator.ToFixedString<FixedList128Bytes<TElement>.Enumerator, TElement, TElementString, TOutputString>();
        }

        /// <summary>
        /// Generates a burst compatible, comma separated, string of a <see cref="FixedList512Bytes{T}"/>'s elements.
        /// Each element must implement <see cref="IToFixedString{T}"/>
        /// </summary>
        /// <param name="collection">The collection of elements</param>
        /// <typeparam name="TElement">The element's type. Must implement <see cref="IToFixedString{T}"/>.</typeparam>
        /// <typeparam name="TElementString">The element's string type.</typeparam>
        /// <typeparam name="TOutputString">
        /// The output's string type. This must be large enough to contain the strings of all elements plus one byte
        /// per element for the comma.
        /// </typeparam>
        /// <returns>The generated string instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TElement, TElementString, TOutputString>(in this FixedList512Bytes<TElement> collection)
            where TElement : unmanaged, IToFixedString<TElementString>
            where TElementString : struct, INativeList<byte>, IUTF8Bytes
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            ref var collectionAsRef = ref UnsafeUtilityExtensions.AsRef(in collection);
            FixedList512Bytes<TElement>.Enumerator enumerator = collectionAsRef.GetEnumerator();
            return enumerator.ToFixedString<FixedList512Bytes<TElement>.Enumerator, TElement, TElementString, TOutputString>();
        }

        /// <summary>
        /// Generates a burst compatible, comma separated, string of a <see cref="FixedList4096Bytes{T}"/>'s elements.
        /// Each element must implement <see cref="IToFixedString{T}"/>
        /// </summary>
        /// <param name="collection">The collection of elements</param>
        /// <typeparam name="TElement">The element's type. Must implement <see cref="IToFixedString{T}"/>.</typeparam>
        /// <typeparam name="TElementString">The element's string type.</typeparam>
        /// <typeparam name="TOutputString">
        /// The output's string type. This must be large enough to contain the strings of all elements plus one byte
        /// per element for the comma.
        /// </typeparam>
        /// <returns>The generated string instance.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TOutputString ToFixedString<TElement, TElementString, TOutputString>(in this FixedList4096Bytes<TElement> collection)
            where TElement : unmanaged, IToFixedString<TElementString>
            where TElementString : struct, INativeList<byte>, IUTF8Bytes
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            ref var collectionAsRef = ref UnsafeUtilityExtensions.AsRef(in collection);
            FixedList4096Bytes<TElement>.Enumerator enumerator = collectionAsRef.GetEnumerator();
            return enumerator.ToFixedString<FixedList4096Bytes<TElement>.Enumerator, TElement, TElementString, TOutputString>();
        }

        /// <summary>
        /// Generates a burst compatible, comma separated, string of an <see cref="IEnumerator{T}"/>'s elements.
        /// Each element must implement <see cref="IToFixedString{T}"/>
        /// </summary>
        /// <param name="collection">The enumerator of the elements.</param>
        /// <typeparam name="TEnumerator">The <see cref="IEnumerator{T}"/>'s concrete type.</typeparam>
        /// <typeparam name="TElement">The element's type. Must implement <see cref="IToFixedString{T}"/>.</typeparam>
        /// <typeparam name="TElementString">The element's string type.</typeparam>
        /// <typeparam name="TOutputString">
        /// The output's string type. This must be large enough to contain the strings of all elements plus one byte
        /// per element for the comma.
        /// </typeparam>
        /// <returns>The generated string instance.</returns>
        public static unsafe TOutputString ToFixedString<TEnumerator, TElement, TElementString, TOutputString>(ref this TEnumerator enumerator)
            where TEnumerator : struct, IEnumerator<TElement>
            where TElement : struct, IToFixedString<TElementString>
            where TElementString : struct, INativeList<byte>, IUTF8Bytes
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            TOutputString output = default;
            output.Append('[');
            bool isFirst = true;
            while (enumerator.MoveNext())
            {
                if (!isFirst)
                {
                    output.Append(',');
                }
                else
                {
                    isFirst = false;
                }

                TElementString elementString = enumerator.Current.ToFixedString();
                output.Append(elementString.GetUnsafePtr(), elementString.Length);
            }
            output.Append(']');

            return output;
        }
    }
}