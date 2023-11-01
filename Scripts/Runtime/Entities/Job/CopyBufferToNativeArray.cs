using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using Unity.Burst;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Copy a <see cref="DynamicBuffer{T}" /> to a <see cref="NativeArray{T}" />.
    /// </summary>
    /// <remarks>
    /// <see cref="DynamicBuffer{T}" /> and <see cref="NativeArray{T}" /> must have the same length.
    /// </remarks>
    [BurstCompile]
    public struct CopyBufferToNativeArray<T> : IJob where T : unmanaged, IBufferElementData
    {
        /// <summary>
        /// The type handle for the <see cref="DynamicBuffer{T}" /> to copy from.
        /// </summary>
        [ReadOnly] public BufferFromSingleEntity<T> InputBufferFromEntity;

        /// <summary>
        /// The <see cref="NativeArray{T}" to copy to.
        /// </summary>
        [WriteOnly] public NativeArray<T> OutputBuffer;

        /// <inheritdoc/>
        public void Execute()
        {
            // Could be modified to support mismatched lengths using `NativeArray<T>.Copy()` but it's not
            // necessary for the intended use case and adds complexity. Add support if the need arises.
            NativeArray<T> inputBuffer = InputBufferFromEntity.GetBuffer().AsNativeArray();
            Debug.Assert(inputBuffer.Length == OutputBuffer.Length, "Output buffer must be the same size as the singleton buffer");
            OutputBuffer.CopyFrom(inputBuffer);
        }
    }
}