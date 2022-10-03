using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class NativeArrayScheduleInfo<T> : AbstractScheduleInfo,
                                                IScheduleInfo
        where T : struct
    {
        private readonly JobConfigScheduleDelegates.ScheduleJobDelegate m_ScheduleJobFunction;
        private readonly NativeArray<T> m_Array;
        private readonly NativeArrayJobData<T> m_JobData;

        public int BatchSize { get; }

        public int Length
        {
            get => m_Array.Length;
        }


        public NativeArrayScheduleInfo(NativeArrayJobData<T> jobData,
                                       NativeArray<T> array,
                                       BatchStrategy batchStrategy,
                                       JobConfigScheduleDelegates.ScheduleJobDelegate scheduleJobFunction) : base(scheduleJobFunction.Method)
        {
            m_Array = array;
            m_ScheduleJobFunction = scheduleJobFunction;
            m_JobData = jobData;

            BatchSize = batchStrategy == BatchStrategy.MaximizeChunk
                ? ChunkUtil.MaxElementsPerChunk<T>()
                : 1;
        }

        public override JobHandle CallScheduleFunction(JobHandle dependsOn)
        {
            return m_ScheduleJobFunction(dependsOn, m_JobData, this);
        }
    }
}
