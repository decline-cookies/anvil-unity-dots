using Anvil.Unity.DOTS.Jobs;
using System.Reflection;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IJobConfig
    {
        internal static readonly BulkScheduleDelegate<IJobConfig> PREPARE_AND_SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<IJobConfig>(nameof(BulkPrepareAndSchedule), BindingFlags.Static | BindingFlags.NonPublic);
        
        private static JobHandle BulkPrepareAndSchedule(IJobConfig jobConfig, JobHandle dependsOn)
        {
            return jobConfig.PrepareAndSchedule(dependsOn);
        }

        public JobHandle PrepareAndSchedule(JobHandle jobHandle);
    }
}
