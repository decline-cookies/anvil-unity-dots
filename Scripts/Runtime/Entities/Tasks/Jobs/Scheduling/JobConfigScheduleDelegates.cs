using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public static class JobConfigScheduleDelegates
    {
        //TODO: Docs
        public delegate JobHandle ScheduleDeferredJobDelegate(JobHandle jobHandle,
                                                              AbstractJobData jobData,
                                                              IDeferredScheduleInfo scheduleInfo);

        public delegate JobHandle ScheduleJobDelegate(JobHandle jobHandle,
                                                      AbstractJobData jobData,
                                                      IScheduleInfo scheduleInfo);

        //TODO: Docs
        public delegate JobHandle ScheduleUpdateJobDelegate<TInstance>(JobHandle jobHandle,
                                                                       UpdateJobData<TInstance> jobData,
                                                                       IUpdateTaskStreamScheduleInfo<TInstance> scheduleInfo)
            where TInstance : unmanaged, IProxyInstance;

        //TODO: Docs
        public delegate JobHandle ScheduleCancelJobDelegate<TInstance>(JobHandle jobHandle,
                                                                       CancelJobData<TInstance> jobData,
                                                                       ICancelTaskStreamScheduleInfo<TInstance> scheduleInfo)
            where TInstance : unmanaged, IProxyInstance;
    }
}
