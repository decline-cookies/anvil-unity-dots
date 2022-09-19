using Anvil.Unity.DOTS.Jobs;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IUpdateJobConfig
    {
    }

    public class UpdateJobConfig<TInstance> : IUpdateJobConfig
        where TInstance : unmanaged, IProxyInstance
    {
        public delegate JobHandle ScheduleJobDelegate(JobHandle jobHandle, UpdateJobData<TInstance> jobData, IScheduleInfo scheduleInfo);

        private readonly ProxyDataStreamScheduleInfo<TInstance> m_ScheduleInfo;
        private readonly ScheduleJobDelegate m_ScheduleJobFunction;
        private readonly UpdateJobData<TInstance> m_UpdateJobData;

        //TODO: Remove once wrapped
        private readonly ProxyDataStream<TInstance> m_UpdateProxyDataStream;

        public UpdateJobConfig(World world,
                               byte context,
                               ScheduleJobDelegate scheduleJobFunction,
                               BatchStrategy batchStrategy,
                               ProxyDataStream<TInstance> updateProxyDataStream)
        {
            m_ScheduleJobFunction = scheduleJobFunction;

            m_UpdateProxyDataStream = updateProxyDataStream;

            m_ScheduleInfo = new ProxyDataStreamScheduleInfo<TInstance>(updateProxyDataStream, batchStrategy);
            m_UpdateJobData = new UpdateJobData<TInstance>(world,
                                                           context,
                                                           updateProxyDataStream);
        }

        //TODO: Cross reference with JobTaskWorkConfig to include safety checks and other data
        public JobHandle PrepareAndSchedule(JobHandle dependsOn)
        {
            //TODO Get access to all data we need based on the config

            JobHandle exclusiveWrite = m_UpdateProxyDataStream.AccessController.AcquireAsync(AccessType.ExclusiveWrite);
            dependsOn = JobHandle.CombineDependencies(exclusiveWrite, dependsOn);

            dependsOn = m_ScheduleJobFunction(dependsOn, m_UpdateJobData, m_ScheduleInfo);

            m_UpdateProxyDataStream.AccessController.ReleaseAsync(dependsOn);

            return dependsOn;
        }
    }
}
