using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using Unity.Burst;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Copy a <see cref="NativeArray{T}" /> to a singleton <see cref="DynamicBuffer{T}" />.
    /// </summary>
    // <remarks>
    /// <see cref="DynamicBuffer{T}" /> and <see cref="NativeArray{T}" /> must have the same length.
    /// </remarks>
    [BurstCompile]
    public struct CopyToSingletonBuffer<T> : IJobEntityBatch where T : struct, IBufferElementData
    {
        /// <summary>
        /// The <see cref="NativeArray{T}" /> to copy from.
        /// </summary>
        [ReadOnly] public NativeArray<T> InputBuffer;
        /// <summary>
        /// The type handle for the <see cref="DynamicBuffer{T}" /> to copy to.
        /// </summary>
        [WriteOnly] public BufferTypeHandle<T> OutputBufferTypeHandle;

        /// <inheritdoc/>
        public void Execute(ArchetypeChunk batchInChunk, int batchIndex)
        {
            BufferAccessor<T> buffers = batchInChunk.GetBufferAccessor(OutputBufferTypeHandle);
            Debug.Assert(buffers.Length == 1, "Expected singleton buffer");
            // Could be modified to support mismatched lengths using `NativeArray<T>.Copy()` but it's not
            // necessary for the indended use case and adds complexity. Add support if the need arises.
            Debug.Assert(buffers[0].Length == InputBuffer.Length, "Output buffer must be the same size as the singleton buffer");
            NativeArray<T>.Copy(InputBuffer, buffers[0].AsNativeArray());
        }
    }
}