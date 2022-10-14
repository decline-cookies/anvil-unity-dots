using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Specific scheduling information for a <see cref="NativeArrayJobConfig{T}"/>
    /// </summary>
    /// <typeparam name="T">The type of struct in the <see cref="NativeArray{T}"/></typeparam>
    public class NativeArrayScheduleInfo<T> : AbstractScheduleInfo
        where T : struct
    {
        private readonly NativeArrayJobData<T> m_JobData;
        private readonly JobConfigScheduleDelegates.ScheduleNativeArrayJobDelegate<T> m_ScheduleJobFunction;
        
        private NativeArray<T> m_Array;

        /// <summary>
        /// The total number of instances to process.
        /// </summary>
        public int Length
        {
            get => m_Array.Length;
        }

        internal NativeArrayScheduleInfo(NativeArrayJobData<T> jobData,
                                         BatchStrategy batchStrategy,
                                         JobConfigScheduleDelegates.ScheduleNativeArrayJobDelegate<T> scheduleJobFunction)
            : base(scheduleJobFunction.Method,
                   batchStrategy,
                   ChunkUtil.MaxElementsPerChunk<T>())
        {
            m_JobData = jobData;
            m_ScheduleJobFunction = scheduleJobFunction;
        }

        internal override JobHandle CallScheduleFunction(JobHandle dependsOn)
        {
            m_Array = m_JobData.GetNativeCollectionForReading<NativeArray<T>>();
            return m_ScheduleJobFunction(dependsOn, m_JobData, this);
        }
    }
}
