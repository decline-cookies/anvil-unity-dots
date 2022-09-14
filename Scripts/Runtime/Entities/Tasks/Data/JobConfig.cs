using Anvil.Unity.DOTS.Jobs;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public class JobConfig<TInstance> : AbstractJobConfig<TInstance>
        where TInstance : unmanaged, IProxyInstance
    {
        public delegate JobHandle ScheduleJobDelegate(JobHandle jobHandle, JobData<TInstance> jobData, IScheduleInfo scheduleInfo);

        private readonly ProxyDataStreamScheduleInfo<TInstance> m_ScheduleInfo;
        private readonly ScheduleJobDelegate m_ScheduleJobFunction;
        private readonly JobData<TInstance> m_JobData;

        //TODO: Remove once wrapped
        private readonly ProxyDataStream<TInstance> m_UpdateProxyDataStream;

        public JobConfig(World world,
                         byte context,
                         ScheduleJobDelegate scheduleJobFunction,
                         BatchStrategy batchStrategy,
                         ProxyDataStream<TInstance> updateProxyDataStream)
        {
            m_ScheduleJobFunction = scheduleJobFunction;

            m_UpdateProxyDataStream = updateProxyDataStream;

            m_ScheduleInfo = new ProxyDataStreamScheduleInfo<TInstance>(updateProxyDataStream, batchStrategy);
            m_JobData = new JobData<TInstance>(world,
                                               context,
                                               updateProxyDataStream);
        }

        //TODO: Cross reference with JobTaskWorkConfig to include safety checks and other data
        public override JobHandle PrepareAndSchedule(JobHandle dependsOn)
        {
            //TODO Get access to all data we need based on the config

            JobHandle exclusiveWrite = m_UpdateProxyDataStream.AccessController.AcquireAsync(AccessType.ExclusiveWrite);
            dependsOn = JobHandle.CombineDependencies(exclusiveWrite, dependsOn);

            dependsOn = m_ScheduleJobFunction(dependsOn, m_JobData, m_ScheduleInfo);

            m_UpdateProxyDataStream.AccessController.ReleaseAsync(dependsOn);

            return dependsOn;
        }
    }
}
