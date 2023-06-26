using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Helper class to allow for easy job scheduling on a <see cref="IAbstractDataStream"/> when not running through
    /// a <see cref="AbstractTaskDriver"/>. Ex. A regular Unity <see cref="ComponentSystemBase"/>.
    /// </summary>
    /// <typeparam name="TInstance">The data inside the data stream.</typeparam>
    public class DataStreamJobScheduler<TInstance>
        where TInstance : unmanaged, IEntityKeyedTask
    {
        /// <summary>
        /// A scheduling delegate to perform custom scheduling of the job that will the use the data stream
        /// to run off of.
        /// </summary>
        public delegate JobHandle ScheduleFunction(IAbstractDataStream<TInstance> dataStream, JobHandle dependsOn);
        
        private uint m_LastVersion;
        private readonly IAbstractDataStream<TInstance> m_DataStream;
        private readonly ScheduleFunction m_ScheduleFunction;
        
        public DataStreamJobScheduler(IAbstractDataStream<TInstance> dataStream, ScheduleFunction scheduleFunction) 
        {
            m_DataStream = dataStream;
            m_ScheduleFunction = scheduleFunction;
            m_LastVersion = m_DataStream.ActiveDataVersion;
        }

        /// <summary>
        /// Will schedule the job if the underlying data stream has potentially been written to since the last
        /// time this function was called. 
        /// </summary>
        /// <param name="dependsOn">The <see cref="JobHandle"/> to wait upon</param>
        /// <returns>The <see cref="JobHandle"/> that represents the work that was scheduled</returns>
        public JobHandle ScheduleIfNecessary(JobHandle dependsOn)
        {
            if (!m_DataStream.IsActiveDataInvalidated(m_LastVersion))
            {
                return dependsOn;
            }

            dependsOn = m_ScheduleFunction(m_DataStream, dependsOn);

            m_LastVersion = m_DataStream.ActiveDataVersion;

            return dependsOn;
        }
    }
}
