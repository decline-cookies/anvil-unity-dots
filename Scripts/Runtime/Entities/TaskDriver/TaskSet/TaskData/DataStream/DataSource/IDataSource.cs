using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal interface IDataSource : IAnvilDisposable
    {
        public static readonly BulkScheduleDelegate<IDataSource> CONSOLIDATE_SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<IDataSource>(nameof(Consolidate), BindingFlags.Instance | BindingFlags.Public);
        
        public void Harden();

        public JobHandle Consolidate(JobHandle dependsOn);
        
        public DataTargetID PendingWorldUniqueID { get; }

        public JobHandle MigrateTo(JobHandle dependsOn, TaskDriverManagementSystem destinationTaskDriverManagementSystem, IDataSource destinationDataSource, ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray);
    }
}
