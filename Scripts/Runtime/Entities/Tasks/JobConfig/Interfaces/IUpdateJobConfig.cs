using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IUpdateJobConfig
    {
        //TODO: Docs
        public delegate JobHandle ScheduleJobDelegate<TInstance>(JobHandle jobHandle, JobData jobData, UpdateTaskStreamScheduleInfo<TInstance> scheduleInfo)
            where TInstance : unmanaged, IProxyInstance;
    }
}
