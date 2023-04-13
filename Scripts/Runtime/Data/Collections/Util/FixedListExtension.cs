using Anvil.Unity.Collections;
using System;
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

        /// <inheritdoc cref="FixedList32BytesExtensions.RemoveSwapBack{T,U}"/>
        /// <remarks>
        /// A version of <see cref="FixedList32BytesExtensions.RemoveSwapBack{T,U}"/> that is compatible with Enums and
        /// readonly references.
        /// </remarks>
        public static bool RemoveSwapBack<TEnum, TUnderlying>(this in FixedList32Bytes<TEnum> list, TUnderlying value)
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
        /// A version of <see cref="FixedList64BytesExtensions.RemoveSwapBack{T,U}"/> that is compatible with Enums and
        /// readonly references.
        /// </remarks>
        public static bool RemoveSwapBack<TEnum, TUnderlying>(this in FixedList64Bytes<TEnum> list, TUnderlying value)
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
        /// A version of <see cref="FixedList128BytesExtensions.RemoveSwapBack{T,U}"/> that is compatible with Enums and
        /// readonly references.
        /// </remarks>
        public static bool RemoveSwapBack<TEnum, TUnderlying>(this in FixedList128Bytes<TEnum> list, TUnderlying value)
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
        /// A version of <see cref="FixedList512BytesExtensions.RemoveSwapBack{T,U}"/> that is compatible with Enums and readonly references.
        /// </remarks>
        public static bool RemoveSwapBack<TEnum, TUnderlying>(this in FixedList512Bytes<TEnum> list, TUnderlying value)
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
        /// A version of <see cref="FixedList4096BytesExtensions.RemoveSwapBack{T,U}"/> that is compatible with Enums
        /// and readonly references.
        /// </remarks>
        public static bool RemoveSwapBack<TEnum, TUnderlying>(this in FixedList4096Bytes<TEnum> list, TUnderlying value)
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
    }
}