using System;
using System.Reflection;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Scheduling info for a <see cref="AbstractJobConfig"/> for use with a <see cref="JobConfigScheduleDelegates"/>
    /// delegate.
    /// </summary>
    public abstract class AbstractScheduleInfo
    {
        private const string UNKNOWN_DECLARING_TYPE = "unknown";
        
        private static int ResolveBatchSize(BatchStrategy batchStrategy, int maxElementsPerChunk)
        {
            return batchStrategy switch
            {
                BatchStrategy.MaximizeChunk   => maxElementsPerChunk,
                BatchStrategy.MaximizeThreads => 1,
                _                             => throw new InvalidOperationException($"Tried to resolve batch size for {nameof(BatchStrategy)}.{batchStrategy} but no code path satisfies!")
            };
        }
        
        /// <summary>
        /// The number of instances to process per batch.
        /// </summary>
        public int BatchSize { get; }
        
        internal string ScheduleJobFunctionInfo { get; }

        protected AbstractScheduleInfo(MemberInfo scheduleJobFunctionMethodInfo, BatchStrategy batchStrategy, int maxElementsPerChunk)
        {
            ScheduleJobFunctionInfo = $"{scheduleJobFunctionMethodInfo.DeclaringType?.Name ?? UNKNOWN_DECLARING_TYPE}.{scheduleJobFunctionMethodInfo.Name}";
            BatchSize = ResolveBatchSize(batchStrategy, maxElementsPerChunk);
        }

        internal abstract JobHandle CallScheduleFunction(JobHandle dependsOn);
        
    }
}
