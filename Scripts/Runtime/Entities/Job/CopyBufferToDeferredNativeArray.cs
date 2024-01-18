using Anvil.Unity.DOTS.Data;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Copy a <see cref="DynamicBuffer{T}" /> to a <see cref="DeferredNativeArray{T}" />.
    /// </summary>
    [BurstCompile]
    public struct CopyBufferToDeferredNativeArray<T> : IJob where T : unmanaged, IBufferElementData
    {
        /// <summary>
        /// The type handle for the <see cref="DynamicBuffer{T}" /> to copy from.
        /// </summary>
        [ReadOnly] public BufferFromSingleEntity<T> InputBufferLookup;

        /// <summary>
        /// The <see cref="NativeArray{T}" to copy to.
        /// </summary>
        [WriteOnly] public DeferredNativeArray<T> OutputBuffer;

        /// <inheritdoc/>
        public void Execute()
        {
            NativeArray<T> inputBuffer = InputBufferLookup.GetBuffer().AsNativeArray();
            NativeArray<T> outputBuffer = OutputBuffer.DeferredCreate(inputBuffer.Length, NativeArrayOptions.UninitializedMemory);
            outputBuffer.CopyFrom(inputBuffer);
        }
    }
}