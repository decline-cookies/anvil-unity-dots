using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public static class JobConfigScheduleDelegates
    {
        //TODO: Docs
        public delegate JobHandle ScheduleTaskStreamJobDelegate<TInstance>(JobHandle jobHandle,
                                                                           TaskStreamJobData<TInstance> jobData,
                                                                           ITaskStreamScheduleInfo<TInstance> scheduleInfo)
            where TInstance : unmanaged, IProxyInstance;


        public delegate JobHandle ScheduleEntityQueryJobDelegate(JobHandle jobHandle,
                                                                 EntityQueryJobData jobData,
                                                                 IEntityQueryScheduleInfo scheduleInfo);

        public delegate JobHandle ScheduleEntityQueryComponentJobDelegate<T>(JobHandle jobHandle,
                                                                             EntityQueryComponentJobData<T> jobData,
                                                                             IEntityQueryComponentScheduleInfo<T> scheduleInfo)
            where T : struct, IComponentData;


        public delegate JobHandle ScheduleNativeArrayJobDelegate<T>(JobHandle jobHandle,
                                                                    NativeArrayJobData<T> jobData,
                                                                    INativeArrayScheduleInfo<T> scheduleInfo)
            where T : struct;


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
