using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System.Reflection;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal interface ISystemTaskProcessor : IAnvilDisposable
    {
        internal static readonly BulkScheduleDelegate<ISystemTaskProcessor> CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<ISystemTaskProcessor>(nameof(BulkConsolidateForFrame), BindingFlags.Static | BindingFlags.NonPublic);
        internal static readonly BulkScheduleDelegate<ISystemTaskProcessor> PREPARE_AND_SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<ISystemTaskProcessor>(nameof(BulkPrepareAndSchedule), BindingFlags.Static | BindingFlags.NonPublic);

        public JobHandle ConsolidateForFrame(JobHandle dependsOn);
        public JobHandle PrepareAndSchedule(JobHandle dependsOn);
        
        private static JobHandle BulkConsolidateForFrame(ISystemTaskProcessor systemTaskProcessor, JobHandle dependsOn)
        {
            return systemTaskProcessor.ConsolidateForFrame(dependsOn);
        }

        private static JobHandle BulkPrepareAndSchedule(ISystemTaskProcessor systemTaskProcessor, JobHandle dependsOn)
        {
            return systemTaskProcessor.PrepareAndSchedule(dependsOn);
        }
    }
}
