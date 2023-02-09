using Anvil.Unity.Collections;
using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Data
{
    public static class UnsafeArrayUtility
    {
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckConvertArguments<T>(int length, Allocator allocator) where T : unmanaged
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be >= 0");
            }
            UnsafeArray<T>.IsUnmanagedAndThrow();
        }

        public static unsafe UnsafeArray<T> ConvertExistingDataToUnsafeArray<T>(void* dataPointer, int length, Allocator allocator)
            where T : unmanaged
        {
            CheckConvertArguments<T>(length, allocator);
            return new UnsafeArray<T>()
            {
                m_Buffer = dataPointer,
                m_Length = length,
                m_AllocatorLabel = allocator,
                m_MinIndex = 0,
                m_MaxIndex = length - 1
            };
        }

        public static unsafe NativeArray<T> AsNativeArray<T>(this UnsafeArray<T> unsafeArray) where T : unmanaged
        {
            return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(
                unsafeArray.m_Buffer,
                unsafeArray.m_Length,
                unsafeArray.m_AllocatorLabel);
        }

        public static unsafe UnsafeArray<T> AsUnsafeArray<T>(this NativeArray<T> nativeArray) where T : unmanaged
        {
            return ConvertExistingDataToUnsafeArray<T>(
                NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(nativeArray),
                nativeArray.Length,
                nativeArray.GetAllocator());
        }

        public static unsafe void* GetUnsafePtr<T>(this UnsafeArray<T> nativeArray) where T : unmanaged
        {
            return nativeArray.m_Buffer;
        }

        public static unsafe void* GetUnsafeReadOnlyPtr<T>(this UnsafeArray<T>.ReadOnly nativeArray) where T : unmanaged
        {
            return nativeArray.m_Buffer;
        }
    }
}