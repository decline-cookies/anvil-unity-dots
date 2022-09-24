using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IJobConfig
    {
        //TODO: Docs
        public delegate JobHandle ScheduleJobDelegate(JobHandle jobHandle, JobData jobData, IScheduleInfo scheduleInfo);
    }
}
