using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System.Reflection;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal interface ITaskProcessor : IAnvilDisposable
    {
        internal static readonly BulkScheduleDelegate<ITaskProcessor> CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<ITaskProcessor>(nameof(BulkConsolidateForFrame), BindingFlags.Static | BindingFlags.NonPublic);
        internal static readonly BulkScheduleDelegate<ITaskProcessor> PREPARE_AND_SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<ITaskProcessor>(nameof(BulkPrepareAndSchedule), BindingFlags.Static | BindingFlags.NonPublic);

        public JobHandle ConsolidateForFrame(JobHandle dependsOn);
        public JobHandle PrepareAndSchedule(JobHandle dependsOn);
        
        private static JobHandle BulkConsolidateForFrame(ITaskProcessor taskProcessor, JobHandle dependsOn)
        {
            return taskProcessor.ConsolidateForFrame(dependsOn);
        }

        private static JobHandle BulkPrepareAndSchedule(ITaskProcessor taskProcessor, JobHandle dependsOn)
        {
            return taskProcessor.PrepareAndSchedule(dependsOn);
        }
    }
}
