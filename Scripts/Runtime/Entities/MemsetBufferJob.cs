using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Sets all elements of a <see cref="DynamicBuffer{T}"/> to a given value.
    /// </summary>
    /// <typeparam name="T">The element type of the <see cref="DynamicBuffer{T}"/></typeparam>
    /// <remarks>The <see cref="DynamicBuffer{T}"/> compliement to <see cref="MemsetNativeArray{T}"/>.</remarks>
    [BurstCompile]
    public struct MemsetBufferJob<T> : IJobParallelForBatch where T : struct, IBufferElementData
    {
        /// <summary>
        /// The buffer to write to.
        /// </summary>
        [WriteOnly] public BufferFromSingleEntity<T> Source;
        /// <summary>
        /// The value to write to the buffer.
        /// </summary>
        [ReadOnly] public T Value;

        public void Execute(int startIndex, int count)
        {
            NativeArray<T> buffer = Source.GetBuffer().AsNativeArray();
            for (int i = startIndex; i < startIndex + count; i++)
            {
                buffer[i] = Value;
            }
        }
    }
}