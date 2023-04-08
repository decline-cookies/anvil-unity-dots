using Anvil.Unity.DOTS.Core;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// A collection of extension methods for working with burst compatible implementations of <see cref="IEnumerable{T}"/>.
    /// </summary>
    [BurstCompile]
    public static class EnumerableToFixedStringExtension
    {
        /// <summary>
        /// Generates a burst compatible, comma separated, string of an <see cref="IEnumerable{T}"/>'s elements.
        /// Each element must implement <see cref="IToFixedString{T}"/>
        /// </summary>
        /// <param name="collection">The collection of elements.</param>
        /// <typeparam name="TCollection">The <see cref="IEnumerable{T}"/>'s concrete type.</typeparam>
        /// <typeparam name="TElement">The element's type. Must implement <see cref="IToFixedString{T}"/>.</typeparam>
        /// <typeparam name="TElementString">The element's string type.</typeparam>
        /// <typeparam name="TOutputString">
        /// The output's string type. This must be large enough to contain the strings of all elements plus one byte
        /// per element for the comma.
        /// </typeparam>
        /// <returns>The generated string instance.</returns>
        public static unsafe TOutputString ToFixedString<TCollection, TElement, TElementString, TOutputString>(ref this TCollection collection)
            where TCollection : struct, IEnumerable<TElement>
            where TElement : struct, IToFixedString<TElementString>
            where TElementString : struct, INativeList<byte>, IUTF8Bytes
            where TOutputString : struct, INativeList<byte>, IUTF8Bytes
        {
            TOutputString output = default;
            foreach (TElement element in collection)
            {
                TElementString elementString = element.ToFixedString();
                output.Append(elementString.GetUnsafePtr(), elementString.Length);
                output.Append(',');
            }

            return output;
        }
    }
}