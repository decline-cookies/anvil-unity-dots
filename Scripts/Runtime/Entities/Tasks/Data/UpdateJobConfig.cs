using Anvil.Unity.DOTS.Jobs;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IUpdateJobConfig
    {
        
    }
    
    public class UpdateJobConfig<TData> : IUpdateJobConfig
        where TData : unmanaged, IProxyData
    {
        public delegate JobHandle ScheduleJobDelegate(JobHandle jobHandle, UpdateJobData<TData> jobData, IScheduleInfo scheduleInfo);
        
        private readonly IScheduleInfo m_ScheduleInfo;
        private readonly ScheduleJobDelegate m_ScheduleJobDelegate;
        private readonly UpdateJobData<TData> m_UpdateJobData;
        
        //TODO: Remove once wrapped
        private readonly ProxyDataStream<TData> m_UpdateProxyDataStream;

        public UpdateJobConfig(World world,
                               byte context,
                               ScheduleJobDelegate scheduleJobDelegate, 
                               ProxyDataStream<TData> dataStream, 
                               BatchStrategy batchStrategy)
        {
            m_ScheduleJobDelegate = scheduleJobDelegate;

            m_UpdateProxyDataStream = dataStream;
            
            m_ScheduleInfo = new ProxyDataStreamScheduleInfo<TData>(dataStream, batchStrategy);
            m_UpdateJobData = new UpdateJobData<TData>(world,
                                                       context,
                                                       dataStream);
        }
        
        //TODO: Cross reference with JobTaskWorkConfig to include safety checks and other data
        public JobHandle PrepareAndSchedule(JobHandle dependsOn)
        {
            //TODO Get access to all data we need based on the config

            JobHandle exclusiveWrite = m_UpdateProxyDataStream.AccessController.AcquireAsync(AccessType.ExclusiveWrite);
            dependsOn = JobHandle.CombineDependencies(exclusiveWrite, dependsOn);

            dependsOn = m_ScheduleJobDelegate(dependsOn, m_UpdateJobData, m_ScheduleInfo);
            
            m_UpdateProxyDataStream.AccessController.ReleaseAsync(dependsOn);

            return dependsOn;
        }
    }
}
