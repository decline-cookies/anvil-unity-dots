using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Specific scheduling information for a <see cref="EntityQueryComponentJobConfig{T}"/>
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IComponentData"/> data</typeparam>
    //TODO: #82 - See if this can be consolidated.
    public class EntityQueryComponentScheduleInfo<T> : AbstractScheduleInfo
        where T : unmanaged, IComponentData
    {
        private readonly EntityQueryComponentJobData<T> m_JobData;
        private readonly EntityQueryComponentNativeList<T> m_EntityQueryComponentNativeList;
        private readonly JobConfigScheduleDelegates.ScheduleEntityQueryComponentJobDelegate<T> m_ScheduleJobFunction;

        /// <summary>
        /// The total number of instances to process.
        /// </summary>
        public int Length
        {
            get => m_EntityQueryComponentNativeList.Length;
        }

        internal EntityQueryComponentScheduleInfo(
            EntityQueryComponentJobData<T> jobData,
            EntityQueryComponentNativeList<T> entityQueryComponentNativeList,
            BatchStrategy batchStrategy,
            JobConfigScheduleDelegates.ScheduleEntityQueryComponentJobDelegate<T> scheduleJobFunction)
            : base(scheduleJobFunction.Method, batchStrategy, ChunkUtil.MaxElementsPerChunk<T>())
        {
            m_JobData = jobData;
            m_EntityQueryComponentNativeList = entityQueryComponentNativeList;
            m_ScheduleJobFunction = scheduleJobFunction;
        }

        internal sealed override JobHandle CallScheduleFunction(JobHandle dependsOn)
        {
            return m_ScheduleJobFunction(dependsOn, m_JobData, this);
        }

        internal override bool ShouldSchedule()
        {
            //If the query won't match anything, no need to schedule. SystemState.ShouldRunSystem does this
            return !m_EntityQueryComponentNativeList.EntityQuery.IsEmptyIgnoreFilter;
        }
    }
}