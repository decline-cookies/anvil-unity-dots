using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Data
{
    [BurstCompatible]
    public static unsafe class DeferredNativeArrayUnsafeUtility
    {
        /// <summary>
        /// Gets the pointer to <see cref="DeferredNativeArray{T}.BufferInfo"/> struct.
        /// Will check the safety handle
        /// </summary>
        /// <param name="deferredNativeArray">The <see cref="DeferredNativeArray{T}"/> to get the pointer from</param>
        /// <typeparam name="T">The type of <see cref="DeferredNativeArray{T}"/> it is.</typeparam>
        /// <returns>The pointer</returns>
        public static void* GetBufferInfo<T>(ref DeferredNativeArray<T> deferredNativeArray)
            where T : struct
        {
            AtomicSafetyHandle.CheckWriteAndThrow(deferredNativeArray.m_Safety);
            return deferredNativeArray.m_BufferInfo;
        }

        /// <summary>
        /// Gets the pointer to <see cref="DeferredNativeArray{T}.BufferInfo"/> struct.
        /// Will NOT check the safety handle
        /// </summary>
        /// <param name="deferredNativeArray">The <see cref="DeferredNativeArray{T}"/> to get the pointer from</param>
        /// <typeparam name="T">The type of <see cref="DeferredNativeArray{T}"/> it is.</typeparam>
        /// <returns>The pointer</returns>
        public static void* GetBufferInfoUnchecked<T>(ref DeferredNativeArray<T> deferredNativeArray)
            where T : struct
        {
            return deferredNativeArray.m_BufferInfo;
        }

        /// <summary>
        /// Gets the safety handle for a <see cref="DeferredNativeArray{T}"/>
        /// </summary>
        /// <param name="deferredNativeArray">The instance to get the safety handle from.</param>
        /// <typeparam name="T">The type of <see cref="DeferredNativeArray{T}"/></typeparam>
        /// <returns>An <see cref="AtomicSafetyHandle"/> instance</returns>
        public static AtomicSafetyHandle GetSafetyHandle<T>(ref DeferredNativeArray<T> deferredNativeArray)
            where T : struct
        {
            return deferredNativeArray.m_Safety;
        }

        /// <summary>
        /// Gets the pointer to the safety handle for a <see cref="DeferredNativeArray{T}"/>
        /// </summary>
        /// <param name="deferredNativeArray">The instance to get the safety handle pointer from.</param>
        /// <typeparam name="T">The type of <see cref="DeferredNativeArray{T}"/></typeparam>
        /// <returns>The pointer to the <see cref="AtomicSafetyHandle"/> instance</returns>
        public static void* GetSafetyHandlePointer<T>(ref DeferredNativeArray<T> deferredNativeArray)
            where T : struct
        {
            return UnsafeUtility.AddressOf(ref deferredNativeArray.m_Safety);
        }
    }
}
