using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Specific scheduling information for a <see cref="EntityQueryComponentJobConfig{T}"/>
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IComponentData"/> data</typeparam>
    //TODO: #82 - See if this can be consolidated.
    public class EntityQueryComponentScheduleInfo<T> : AbstractScheduleInfo
        where T : struct, IComponentData
    {
        private readonly EntityQueryComponentJobData<T> m_JobData;
        private readonly EntityQueryComponentNativeArray<T> m_EntityQueryComponentNativeArray;
        private readonly JobConfigScheduleDelegates.ScheduleEntityQueryComponentJobDelegate<T> m_ScheduleJobFunction;

        /// <summary>
        /// The total number of instances to process.
        /// </summary>
        public int Length
        {
            get => m_EntityQueryComponentNativeArray.Length;
        }

        internal EntityQueryComponentScheduleInfo(EntityQueryComponentJobData<T> jobData,
                                                  EntityQueryComponentNativeArray<T> entityQueryComponentNativeArray,
                                                  BatchStrategy batchStrategy,
                                                  JobConfigScheduleDelegates.ScheduleEntityQueryComponentJobDelegate<T> scheduleJobFunction) 
            : base(scheduleJobFunction.Method,
                   batchStrategy,
                   ChunkUtil.MaxElementsPerChunk<T>())
        {
            m_JobData = jobData;
            m_EntityQueryComponentNativeArray = entityQueryComponentNativeArray;
            m_ScheduleJobFunction = scheduleJobFunction;
        }

        internal sealed override JobHandle CallScheduleFunction(JobHandle dependsOn)
        {
            return m_ScheduleJobFunction(dependsOn, m_JobData, this);
        }
    }
}
