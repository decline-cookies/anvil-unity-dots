using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Sets all elements of a <see cref="DynamicBuffer{T}"/> to a given value.
    /// </summary>
    /// <typeparam name="T">The element type of the <see cref="DynamicBuffer{T}"/></typeparam>
    /// <remarks>The <see cref="DynamicBuffer{T}"/> compliment to <see cref="MemsetNativeArray{T}"/>.</remarks>
    [BurstCompile]
    public struct MemsetBufferJob<T> : IJobParallelForBatch where T : struct, IBufferElementData
    {
        //TODO: Measuere an ideal value.
        private const float MIN_BATCH_SIZE_PER_THREAD = 50_000;

        /// <summary>
        /// Calculate an ideal batch size per thread.
        /// Aim to spread work across as many threads as possible unless the batch is too small to 
        /// overcome the prformance cost of a batch's initialization.
        /// </summary>
        /// <param name="length"></param>
        /// <returns></returns>
        public static int CalculateOptimalBatchSize(int length)
        {
            float optimalWorkerCount = math.clamp(length / MIN_BATCH_SIZE_PER_THREAD, 1, JobsUtility.JobWorkerCount);
            return (int)math.ceil(length / optimalWorkerCount);
        }

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
            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
            {
                buffer[i] = Value;
            }
        }

        /// <summary>
        /// Schedule the job with an optimimal batch size
        /// </summary>
        /// <param name="arrayLength">The length of the buffer</param>
        /// <param name="dependsOn">The <see cref="JobHandle"/> to wait for.</param>
        /// <returns>A <see cref="JobHandle"/> that represents when the work is complete.</returns>
        public JobHandle ScheduleWithOptimalBatchSize(int arrayLength, JobHandle dependsOn = default)
        {
            int batchSize = CalculateOptimalBatchSize(arrayLength);
            return this.ScheduleBatch(arrayLength, batchSize, dependsOn);
        }
    }
}