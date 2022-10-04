using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Specific scheduling information for a <see cref="EntityQueryJobConfig"/>
    /// </summary>
    public class EntityQueryScheduleInfo : AbstractScheduleInfo
    {
        private readonly EntityQueryJobData m_JobData;
        private readonly EntityQueryNativeArray m_EntityQueryNativeArray;
        private readonly JobConfigScheduleDelegates.ScheduleEntityQueryJobDelegate m_ScheduleJobFunction;
        
        /// <summary>
        /// The number of instances to process per batch.
        /// </summary>
        public int BatchSize { get; }
        
        /// <summary>
        /// The total number of instances to process.
        /// </summary>
        public int Length
        {
            get => m_EntityQueryNativeArray.Length;
        }

        internal EntityQueryScheduleInfo(EntityQueryJobData jobData,
                                         EntityQueryNativeArray entityQueryNativeArray,
                                         BatchStrategy batchStrategy,
                                         JobConfigScheduleDelegates.ScheduleEntityQueryJobDelegate scheduleJobFunction) : base(scheduleJobFunction.Method)
        {
            m_JobData = jobData;
            m_EntityQueryNativeArray = entityQueryNativeArray;
            m_ScheduleJobFunction = scheduleJobFunction;

            BatchSize = batchStrategy == BatchStrategy.MaximizeChunk
                ? ChunkUtil.MaxElementsPerChunk<Entity>()
                : 1;
        }

        internal sealed override JobHandle CallScheduleFunction(JobHandle dependsOn)
        {
            return m_ScheduleJobFunction(dependsOn, m_JobData, this);
        }
    }
}
