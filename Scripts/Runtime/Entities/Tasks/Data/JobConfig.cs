using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public class JobConfig<TInstance> : AbstractJobConfig
        where TInstance : unmanaged, IProxyInstance
    {
        public delegate JobHandle ScheduleJobDelegate(JobHandle jobHandle, JobData<TInstance> jobData, IScheduleInfo scheduleInfo);


        private readonly ScheduleJobDelegate m_ScheduleJobFunction;
        private readonly JobData<TInstance> m_JobData;


        internal JobConfig(ITaskFlowGraph taskFlowGraph,
                           ITaskSystem taskSystem,
                           ITaskDriver taskDriver,
                           ScheduleJobDelegate scheduleJobFunction) : base(taskFlowGraph, taskSystem, taskDriver)
        {
            m_ScheduleJobFunction = scheduleJobFunction;

            // m_JobData = new JobData<TInstance>(world,
            //                                    context,
            //                                    updateProxyDataStream);
        }


        //TODO: Cross reference with JobTaskWorkConfig to include safety checks and other data
        protected override JobHandle PrepareAndSchedule(JobHandle dependsOn)
        {
            //TODO Get access to all data we need based on the config

            // JobHandle exclusiveWrite = m_UpdateProxyDataStream.AccessController.AcquireAsync(AccessType.ExclusiveWrite);
            // dependsOn = JobHandle.CombineDependencies(exclusiveWrite, dependsOn);
            //
            // dependsOn = m_ScheduleJobFunction(dependsOn, m_JobData, m_ScheduleInfo);
            //
            // m_UpdateProxyDataStream.AccessController.ReleaseAsync(dependsOn);

            return dependsOn;
        }
    }
}
