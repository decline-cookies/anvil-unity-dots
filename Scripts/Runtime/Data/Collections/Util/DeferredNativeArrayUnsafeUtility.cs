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
        /// <param name="deferredNativeArray">The <see cref="IDeferredNativeArray"/> to get the pointer from</param>
        /// <returns>The pointer</returns>
        public static void* GetBufferInfo<TDeferredNativeArray>(ref TDeferredNativeArray deferredNativeArray)
            where TDeferredNativeArray : struct, IDeferredNativeArray
        {
            AtomicSafetyHandle.CheckWriteAndThrow(deferredNativeArray.SafetyHandle);
            return deferredNativeArray.BufferPtr;
        }

        /// <summary>
        /// Gets the pointer to <see cref="DeferredNativeArray{T}.BufferInfo"/> struct.
        /// Will NOT check the safety handle
        /// </summary>
        /// <param name="deferredNativeArray">The <see cref="IDeferredNativeArray"/> to get the pointer from</param>
        /// <returns>The pointer</returns>
        public static void* GetBufferInfoUnchecked<TDeferredNativeArray>(ref TDeferredNativeArray deferredNativeArray)
            where TDeferredNativeArray : struct, IDeferredNativeArray
        {
            return deferredNativeArray.BufferPtr;
        }

        /// <summary>
        /// Gets the safety handle for a <see cref="IDeferredNativeArray"/>
        /// </summary>
        /// <param name="deferredNativeArray">The instance to get the safety handle from.</param>
        /// <returns>An <see cref="AtomicSafetyHandle"/> instance</returns>
        public static AtomicSafetyHandle GetSafetyHandle<TDeferredNativeArray>(ref TDeferredNativeArray deferredNativeArray)
            where TDeferredNativeArray : struct, IDeferredNativeArray
        {
            return deferredNativeArray.SafetyHandle;
        }

        /// <summary>
        /// Gets the pointer to the safety handle for a <see cref="IDeferredNativeArray"/>
        /// </summary>
        /// <param name="deferredNativeArray">The instance to get the safety handle pointer from.</param>
        /// <returns>The pointer to the <see cref="AtomicSafetyHandle"/> instance</returns>
        public static void* GetSafetyHandlePointer<TDeferredNativeArray>(ref TDeferredNativeArray deferredNativeArray)
            where TDeferredNativeArray : struct, IDeferredNativeArray
        {
            return deferredNativeArray.SafetyHandlePtr;
        }
    }
}
