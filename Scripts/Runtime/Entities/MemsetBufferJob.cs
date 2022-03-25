using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

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
        // The minimum number of elements to process in a single batch.
        // Attempts to keep batch sizes large enough to prevent multiple cores from copying the same
        // data into cache. This job's operation is so simple the copy into cache is probably the most expensive part.
        //
        // Ex: We shouldn't split a set of 10 across 5 threads.
        //
        // Re CacheSize: If generalizing to a 16kb cache size is good enough for Unity it's good 
        // enough for us. If this isn't good enough #12 will allow us to get the actual cache size.
        private static readonly float MIN_BATCH_SIZE_PER_THREAD = math.max(1f, 16 * 1024 / (float)UnsafeUtility.SizeOf<T>());

        /// <summary>
        /// Calculate an ideal batch size per thread.
        /// Aim to spread work across as many threads as possible while satisfying (in order of importance):
        ///  - Aligning batch size to cache line size
        ///  - Keeping batch sizes large enough to overcome the cost of splitting.
        ///  - Minimizing the number of batches (there's overhead in each batch run)
        /// </summary>
        /// <remarks>
        /// This approach minimizes frame time consumed not total computation time. 
        /// Single thread will always consume the fewest CPU cycles.
        /// </remarks>
        /// <param name="length">The total length of the data set</param>
        /// <returns>The ideal batch size</returns>
        public static int CalculateOptimalBatchSize(int length)
        {
            float maxBatches = length / MIN_BATCH_SIZE_PER_THREAD;
            // Spread the max batches across all threads but round up to the nearest MIN_BATCH_SIZE_PER_THREAD.
            return (int)(math.ceil(maxBatches / (float)JobsUtility.JobWorkerCount) * MIN_BATCH_SIZE_PER_THREAD);
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
        /// Schedule the job with an optimal batch size
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