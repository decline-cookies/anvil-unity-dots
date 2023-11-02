using Anvil.Unity.Collections;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// A collection of extension methods for <see cref="FixedList32Bytes{T}"/> (and friends).
    /// </summary>
    public static class FixedListExtension
    {
        /// <inheritdoc cref="FixedList32BytesExtensions.IndexOf{T,U}"/>
        /// <remarks>
        /// A version of <see cref="FixedList32BytesExtensions.IndexOf{T,U}"/> that is compatible with readonly references.
        /// </remarks>
        public static int IndexOfReadOnly<T, U>(this in FixedList32Bytes<T> list, U value) where T : unmanaged, IEquatable<U>
        {
            return UnsafeUtilityExtensions.AsRef(list).IndexOf(value);
        }

        /// <inheritdoc cref="FixedList64BytesExtensions.IndexOf{T,U}"/>
        /// <remarks>
        /// A version of <see cref="FixedList64BytesExtensions.IndexOf{T,U}"/> that is compatible with readonly references.
        /// </remarks>
        public static int IndexOfReadOnly<T, U>(this in FixedList64Bytes<T> list, U value) where T : unmanaged, IEquatable<U>
        {
            return UnsafeUtilityExtensions.AsRef(list).IndexOf(value);
        }

        /// <inheritdoc cref="FixedList128BytesExtensions.IndexOf{T,U}"/>
        /// <remarks>
        /// A version of <see cref="FixedList128BytesExtensions.IndexOf{T,U}"/> that is compatible with readonly references.
        /// </remarks>
        public static int IndexOfReadOnly<T, U>(this in FixedList128Bytes<T> list, U value) where T : unmanaged, IEquatable<U>
        {
            return UnsafeUtilityExtensions.AsRef(list).IndexOf(value);
        }

        /// <inheritdoc cref="FixedList512BytesExtensions.IndexOf{T,U}"/>
        /// <remarks>
        /// A version of <see cref="FixedList512BytesExtensions.IndexOf{T,U}"/> that is compatible with readonly references.
        /// </remarks>
        public static int IndexOfReadOnly<T, U>(this in FixedList512Bytes<T> list, U value) where T : unmanaged, IEquatable<U>
        {
            return UnsafeUtilityExtensions.AsRef(list).IndexOf(value);
        }

        /// <inheritdoc cref="FixedList4096BytesExtensions.IndexOf{T,U}"/>
        /// <remarks>
        /// A version of <see cref="FixedList4096BytesExtensions.IndexOf{T,U}"/> that is compatible with readonly references.
        /// </remarks>
        public static int IndexOfReadOnly<T, U>(this in FixedList4096Bytes<T> list, U value) where T : unmanaged, IEquatable<U>
        {
            return UnsafeUtilityExtensions.AsRef(list).IndexOf(value);
        }

        /// <inheritdoc cref="FixedList32BytesExtensions.Contains{T,U}"/>
        /// <remarks>
        /// A version of <see cref="FixedList32BytesExtensions.Contains{T,U}"/> that is compatible with readonly references.
        /// </remarks>
        public static bool ContainsReadOnly<T, U>(this in FixedList32Bytes<T> list, U value) where T : unmanaged, IEquatable<U>
        {
            return UnsafeUtilityExtensions.AsRef(list).Contains(value);
        }

        /// <inheritdoc cref="FixedList64BytesExtensions.Contains{T,U}"/>
        /// <remarks>
        /// A version of <see cref="FixedList64BytesExtensions.Contains{T,U}"/> that is compatible with readonly references.
        /// </remarks>
        public static bool ContainsReadOnly<T, U>(this in FixedList64Bytes<T> list, U value) where T : unmanaged, IEquatable<U>
        {
            return UnsafeUtilityExtensions.AsRef(list).Contains(value);
        }

        /// <inheritdoc cref="FixedList128BytesExtensions.Contains{T,U}"/>
        /// <remarks>
        /// A version of <see cref="FixedList128BytesExtensions.Contains{T,U}"/> that is compatible with readonly
        /// references.
        /// </remarks>
        public static bool ContainsReadOnly<T, U>(this in FixedList128Bytes<T> list, U value) where T : unmanaged, IEquatable<U>
        {
            return UnsafeUtilityExtensions.AsRef(list).Contains(value);
        }

        /// <inheritdoc cref="FixedList512BytesExtensions.Contains{T,U}"/>
        /// <remarks>
        /// A version of <see cref="FixedList512BytesExtensions.Contains{T,U}"/> that is compatible with readonly
        /// references.
        /// </remarks>
        public static bool ContainsReadOnly<T, U>(this in FixedList512Bytes<T> list, U value) where T : unmanaged, IEquatable<U>
        {
            return UnsafeUtilityExtensions.AsRef(list).Contains(value);
        }

        /// <inheritdoc cref="FixedList4096BytesExtensions.Contains{T,U}"/>
        /// <remarks>
        /// A version of <see cref="FixedList4096BytesExtensions.Contains{T,U}"/> that is compatible with readonly
        /// references.
        /// </remarks>
        public static bool ContainsReadOnly<T, U>(this in FixedList4096Bytes<T> list, U value) where T : unmanaged, IEquatable<U>
        {
            return UnsafeUtilityExtensions.AsRef(list).Contains(value);
        }

        /// <inheritdoc cref="FixedList32BytesExtensions.Remove{T,U}"/>
        /// <remarks>
        /// A version of <see cref="FixedList32BytesExtensions.Remove{T,U}"/> that is compatible with Enums.
        /// </remarks>
        public static bool Remove<TEnum, TUnderlying>(this ref FixedList32Bytes<TEnum> list, TUnderlying value)
            where TEnum : unmanaged, Enum
            where TUnderlying : unmanaged, IEquatable<TUnderlying>
        {
            int index = list.IndexOf(value);
            if (index == -1)
            {
                return false;
            }

            list.RemoveAt(index);

            return true;
        }

        /// <inheritdoc cref="FixedList64BytesExtensions.Remove{T,U}"/>
        /// <remarks>
        /// A version of <see cref="FixedList64BytesExtensions.Remove{T,U}"/> that is compatible with Enums.
        /// </remarks>
        public static bool Remove<TEnum, TUnderlying>(this ref FixedList64Bytes<TEnum> list, TUnderlying value)
            where TEnum : unmanaged, Enum
            where TUnderlying : unmanaged, IEquatable<TUnderlying>
        {
            int index = list.IndexOf(value);
            if (index == -1)
            {
                return false;
            }

            list.RemoveAt(index);

            return true;
        }

        /// <inheritdoc cref="FixedList128BytesExtensions.Remove{T,U}"/>
        /// <remarks>
        /// A version of <see cref="FixedList128BytesExtensions.Remove{T,U}"/> that is compatible with Enums.
        /// </remarks>
        public static bool Remove<TEnum, TUnderlying>(this ref FixedList128Bytes<TEnum> list, TUnderlying value)
            where TEnum : unmanaged, Enum
            where TUnderlying : unmanaged, IEquatable<TUnderlying>
        {
            int index = list.IndexOf(value);
            if (index == -1)
            {
                return false;
            }

            list.RemoveAt(index);

            return true;
        }

        /// <inheritdoc cref="FixedList512BytesExtensions.Remove{T,U}"/>
        /// <remarks>
        /// A version of <see cref="FixedList512BytesExtensions.Remove{T,U}"/> that is compatible with Enums.
        /// </remarks>
        public static bool Remove<TEnum, TUnderlying>(this ref FixedList512Bytes<TEnum> list, TUnderlying value)
            where TEnum : unmanaged, Enum
            where TUnderlying : unmanaged, IEquatable<TUnderlying>
        {
            int index = list.IndexOf(value);
            if (index == -1)
            {
                return false;
            }

            list.RemoveAt(index);

            return true;
        }

        /// <inheritdoc cref="FixedList4096BytesExtensions.Remove{T,U}"/>
        /// <remarks>
        /// A version of <see cref="FixedList4096BytesExtensions.Remove{T,U}"/> that is compatible with Enums.
        /// </remarks>
        public static bool Remove<TEnum, TUnderlying>(this ref FixedList4096Bytes<TEnum> list, TUnderlying value)
            where TEnum : unmanaged, Enum
            where TUnderlying : unmanaged, IEquatable<TUnderlying>
        {
            int index = list.IndexOf(value);
            if (index == -1)
            {
                return false;
            }

            list.RemoveAt(index);

            return true;
        }

        /// <inheritdoc cref="FixedList32BytesExtensions.RemoveSwapBack{T,U}"/>
        /// <remarks>
        /// A version of <see cref="FixedList32BytesExtensions.RemoveSwapBack{T,U}"/> that is compatible with Enums.
        /// </remarks>
        public static bool RemoveSwapBack<TEnum, TUnderlying>(this ref FixedList32Bytes<TEnum> list, TUnderlying value)
            where TEnum : unmanaged, Enum
            where TUnderlying : unmanaged, IEquatable<TUnderlying>
        {
            int index = list.IndexOf(value);
            if (index == -1)
            {
                return false;
            }

            list.RemoveAtSwapBack(index);

            return true;
        }

        /// <inheritdoc cref="FixedList64BytesExtensions.RemoveSwapBack{T,U}"/>
        /// <remarks>
        /// A version of <see cref="FixedList64BytesExtensions.RemoveSwapBack{T,U}"/> that is compatible with Enums.
        /// </remarks>
        public static bool RemoveSwapBack<TEnum, TUnderlying>(this ref FixedList64Bytes<TEnum> list, TUnderlying value)
            where TEnum : unmanaged, Enum
            where TUnderlying : unmanaged, IEquatable<TUnderlying>
        {
            int index = list.IndexOf(value);
            if (index == -1)
            {
                return false;
            }

            list.RemoveAtSwapBack(index);

            return true;
        }

        /// <inheritdoc cref="FixedList128BytesExtensions.RemoveSwapBack{T,U}"/>
        /// <remarks>
        /// A version of <see cref="FixedList128BytesExtensions.RemoveSwapBack{T,U}"/> that is compatible with Enums.
        /// </remarks>
        public static bool RemoveSwapBack<TEnum, TUnderlying>(this ref FixedList128Bytes<TEnum> list, TUnderlying value)
            where TEnum : unmanaged, Enum
            where TUnderlying : unmanaged, IEquatable<TUnderlying>
        {
            int index = list.IndexOf(value);
            if (index == -1)
            {
                return false;
            }

            list.RemoveAtSwapBack(index);

            return true;
        }

        /// <inheritdoc cref="FixedList512BytesExtensions.RemoveSwapBack{T,U}"/>
        /// <remarks>
        /// A version of <see cref="FixedList512BytesExtensions.RemoveSwapBack{T,U}"/> that is compatible with Enums.
        /// </remarks>
        public static bool RemoveSwapBack<TEnum, TUnderlying>(this ref FixedList512Bytes<TEnum> list, TUnderlying value)
            where TEnum : unmanaged, Enum
            where TUnderlying : unmanaged, IEquatable<TUnderlying>
        {
            int index = list.IndexOf(value);
            if (index == -1)
            {
                return false;
            }

            list.RemoveAtSwapBack(index);

            return true;
        }

        /// <inheritdoc cref="FixedList4096BytesExtensions.RemoveSwapBack{T,U}"/>
        /// <remarks>
        /// A version of <see cref="FixedList4096BytesExtensions.RemoveSwapBack{T,U}"/> that is compatible with Enums.
        /// </remarks>
        public static bool RemoveSwapBack<TEnum, TUnderlying>(this ref FixedList4096Bytes<TEnum> list, TUnderlying value)
            where TEnum : unmanaged, Enum
            where TUnderlying : unmanaged, IEquatable<TUnderlying>
        {
            int index = list.IndexOf(value);
            if (index == -1)
            {
                return false;
            }

            list.RemoveAtSwapBack(index);

            return true;
        }

        /// <summary>
        /// Create a <see cref="FixedList32Bytes{T}"/> from an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="enumerable">The enumerable to add elements from.</param>
        /// <typeparam name="T">The element type.</typeparam>
        /// <returns>A <see cref="FixedList32Bytes{T}"/> instance populated with the elements of <see cref="enumerable"/>.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when the contents of <see cref="enumerable"/> don't fit in the fixed list.
        /// </exception>
        public static FixedList32Bytes<T> ToFixedList32<T>(this IEnumerable<T> enumerable)
            where T : unmanaged
        {
            FixedList32Bytes<T> fixedList = new FixedList32Bytes<T>();
            foreach (T element in enumerable)
            {
                fixedList.Add(element);
            }

            return fixedList;
        }

        /// <summary>
        /// Create a <see cref="FixedList64Bytes{T}"/> from an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="enumerable">The enumerable to add elements from.</param>
        /// <typeparam name="T">The element type.</typeparam>
        /// <returns>A <see cref="FixedList64Bytes{T}"/> instance populated with the elements of <see cref="enumerable"/>.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when the contents of <see cref="enumerable"/> don't fit in the fixed list.
        /// </exception>
        public static FixedList64Bytes<T> ToFixedList64<T>(this IEnumerable<T> enumerable)
            where T : unmanaged
        {
            FixedList64Bytes<T> fixedList = new FixedList64Bytes<T>();
            foreach (T element in enumerable)
            {
                fixedList.Add(element);
            }

            return fixedList;
        }

        /// <summary>
        /// Create a <see cref="FixedList128Bytes{T}"/> from an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="enumerable">The enumerable to add elements from.</param>
        /// <typeparam name="T">The element type.</typeparam>
        /// <returns>A <see cref="FixedList128Bytes{T}"/> instance populated with the elements of <see cref="enumerable"/>.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when the contents of <see cref="enumerable"/> don't fit in the fixed list.
        /// </exception>
        public static FixedList128Bytes<T> ToFixedList128<T>(this IEnumerable<T> enumerable)
            where T : unmanaged
        {
            FixedList128Bytes<T> fixedList = new FixedList128Bytes<T>();
            foreach (T element in enumerable)
            {
                fixedList.Add(element);
            }

            return fixedList;
        }

        /// <summary>
        /// Create a <see cref="FixedList512Bytes{T}"/> from an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="enumerable">The enumerable to add elements from.</param>
        /// <typeparam name="T">The element type.</typeparam>
        /// <returns>A <see cref="FixedList512Bytes{T}"/> instance populated with the elements of <see cref="enumerable"/>.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when the contents of <see cref="enumerable"/> don't fit in the fixed list.
        /// </exception>
        public static FixedList512Bytes<T> ToFixedList512<T>(this IEnumerable<T> enumerable)
            where T : unmanaged
        {
            FixedList512Bytes<T> fixedList = new FixedList512Bytes<T>();
            foreach (T element in enumerable)
            {
                fixedList.Add(element);
            }

            return fixedList;
        }

        /// <summary>
        /// Create a <see cref="FixedList4096Bytes{T}"/> from an <see cref="IEnumerable{T}"/>.
        /// </summary>
        /// <param name="enumerable">The enumerable to add elements from.</param>
        /// <typeparam name="T">The element type.</typeparam>
        /// <returns>A <see cref="FixedList4096Bytes{T}"/> instance populated with the elements of <see cref="enumerable"/>.</returns>
        /// <exception cref="IndexOutOfRangeException">
        /// Thrown when the contents of <see cref="enumerable"/> don't fit in the fixed list.
        /// </exception>
        public static FixedList4096Bytes<T> ToFixedList4096<T>(this IEnumerable<T> enumerable)
            where T : unmanaged
        {
            FixedList4096Bytes<T> fixedList = new FixedList4096Bytes<T>();
            foreach (T element in enumerable)
            {
                fixedList.Add(element);
            }

            return fixedList;
        }
    }
}