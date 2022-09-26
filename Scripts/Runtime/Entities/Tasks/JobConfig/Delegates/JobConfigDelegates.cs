using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public static class JobConfigDelegates
    {
        //TODO: Docs
        public delegate JobHandle ScheduleJobDelegate(JobHandle jobHandle, JobData jobData, IScheduleInfo scheduleInfo);
        
        //TODO: Docs
        public delegate JobHandle ScheduleUpdateJobDelegate<TInstance>(JobHandle jobHandle, JobData jobData, UpdateTaskStreamScheduleInfo<TInstance> scheduleInfo)
            where TInstance : unmanaged, IProxyInstance;
    }
}
