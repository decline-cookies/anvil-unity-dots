using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class EntityQueryComponentScheduleInfo<T> : AbstractScheduleInfo,
                                                         IScheduleInfo
        where T : struct, IComponentData
    {
        private readonly JobConfigScheduleDelegates.ScheduleJobDelegate m_ScheduleJobFunction;
        private readonly EntityQueryComponentJobData<T> m_JobData;

        public int BatchSize { get; }

        public EntityQueryComponentNativeArray<T> EntityQueryComponentNativeArray { get; }

        public int Length
        {
            get => EntityQueryComponentNativeArray.Length;
        }

        public EntityQueryComponentScheduleInfo(EntityQueryComponentJobData<T> jobData,
                                                EntityQueryComponentNativeArray<T> entityQueryComponentNativeArray,
                                                BatchStrategy batchStrategy,
                                                JobConfigScheduleDelegates.ScheduleJobDelegate scheduleJobFunction) : base(scheduleJobFunction.Method)
        {
            EntityQueryComponentNativeArray = entityQueryComponentNativeArray;
            m_JobData = jobData;

            BatchSize = batchStrategy == BatchStrategy.MaximizeChunk
                ? ChunkUtil.MaxElementsPerChunk<T>()
                : 1;

            m_ScheduleJobFunction = scheduleJobFunction;
        }

        public sealed override JobHandle CallScheduleFunction(JobHandle dependsOn)
        {
            return m_ScheduleJobFunction(dependsOn, m_JobData, this);
        }
    }
}
