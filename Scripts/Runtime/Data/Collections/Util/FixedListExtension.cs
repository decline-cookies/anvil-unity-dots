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
    }
}