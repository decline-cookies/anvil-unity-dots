using Unity.Entities;
using Unity.Collections;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Copy a <see cref="NativeArray{T}" /> to a <see cref="DynamicBuffer{T}" />.
    /// </summary>
    /// <remarks>
    /// <see cref="DynamicBuffer{T}" /> and <see cref="NativeArray{T}" /> must have the same length.
    /// </remarks>
    [BurstCompile]
    public struct CopyNativeArrayToBuffer<T> : IJob where T : unmanaged, IBufferElementData
    {
        /// <summary>
        /// The <see cref="NativeArray{T}" /> to copy from.
        /// </summary>
        [ReadOnly] public NativeArray<T> InputBuffer;

        /// <summary>
        /// The type handle for the <see cref="DynamicBuffer{T}" /> to copy to.
        /// </summary>
        [WriteOnly] public BufferFromSingleEntity<T> OutputBufferFromEntity;

        /// <inheritdoc/>
        public void Execute()
        {
            NativeArray<T> outputBuffer = OutputBufferFromEntity.GetBuffer().AsNativeArray();

            // Could be modified to support mismatched lengths using `NativeArray<T>.Copy()` but it's not
            // necessary for the intended use case and adds complexity. Add support if the need arises.
            Debug.Assert(outputBuffer.Length == InputBuffer.Length, "Output buffer must be the same size as the singleton buffer");
            NativeArray<T>.Copy(InputBuffer, outputBuffer);
        }
    }
}
