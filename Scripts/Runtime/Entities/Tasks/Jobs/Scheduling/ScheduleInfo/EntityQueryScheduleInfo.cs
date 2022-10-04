using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class EntityQueryScheduleInfo : AbstractScheduleInfo,
                                             IEntityQueryScheduleInfo
    {
        private readonly JobConfigScheduleDelegates.ScheduleEntityQueryJobDelegate m_ScheduleJobFunction;
        private readonly EntityQueryJobData m_JobData;

        public int BatchSize { get; }

        public EntityQueryNativeArray EntityQueryNativeArray { get; }

        public int Length
        {
            get => EntityQueryNativeArray.Length;
        }

        public EntityQueryScheduleInfo(EntityQueryJobData jobData,
                                       EntityQueryNativeArray entityQueryNativeArray,
                                       BatchStrategy batchStrategy,
                                       JobConfigScheduleDelegates.ScheduleEntityQueryJobDelegate scheduleJobFunction) : base(scheduleJobFunction.Method)
        {
            m_JobData = jobData;
            EntityQueryNativeArray = entityQueryNativeArray;

            BatchSize = batchStrategy == BatchStrategy.MaximizeChunk
                ? ChunkUtil.MaxElementsPerChunk<Entity>()
                : 1;

            m_ScheduleJobFunction = scheduleJobFunction;
        }

        public sealed override JobHandle CallScheduleFunction(JobHandle dependsOn)
        {
            return m_ScheduleJobFunction(dependsOn, m_JobData, this);
        }
    }
}
