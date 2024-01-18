using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Specific scheduling information for a <see cref="EntityQueryJobConfig"/>
    /// </summary>
    public class EntityQueryScheduleInfo : AbstractScheduleInfo
    {
        private readonly EntityQueryJobData m_JobData;
        private readonly EntityQueryNativeList m_EntityQueryNativeList;
        private readonly JobConfigScheduleDelegates.ScheduleEntityQueryJobDelegate m_ScheduleJobFunction;

        /// <summary>
        /// The total number of instances to process.
        /// </summary>
        public int Length
        {
            get => m_EntityQueryNativeList.Length;
        }

        internal EntityQueryScheduleInfo(
            EntityQueryJobData jobData,
            EntityQueryNativeList entityQueryNativeList,
            BatchStrategy batchStrategy,
            JobConfigScheduleDelegates.ScheduleEntityQueryJobDelegate scheduleJobFunction)
            : base(scheduleJobFunction.Method, batchStrategy, ChunkUtil.MaxElementsPerChunk<Entity>())
        {
            m_JobData = jobData;
            m_EntityQueryNativeList = entityQueryNativeList;
            m_ScheduleJobFunction = scheduleJobFunction;
        }

        internal sealed override JobHandle CallScheduleFunction(JobHandle dependsOn)
        {
            return m_ScheduleJobFunction(dependsOn, m_JobData, this);
        }
        
        internal override bool ShouldSchedule()
        {
            //If the query won't match anything, no need to schedule. SystemState.ShouldRunSystem does this
            return !m_EntityQueryNativeList.EntityQuery.IsEmptyIgnoreFilter;
        }
    }
}