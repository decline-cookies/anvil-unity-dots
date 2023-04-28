using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class CancelProgressDataSource : AbstractDataSource<EntityProxyInstanceID>
    {
        public CancelProgressDataSource(TaskDriverManagementSystem taskDriverManagementSystem) : base(taskDriverManagementSystem) { }

        protected override JobHandle ConsolidateSelf(JobHandle dependsOn)
        {
            throw new InvalidOperationException($"CancelProgress Data Never needs to be consolidated");
        }
        
        //*************************************************************************************************************
        // MIGRATION
        //*************************************************************************************************************
        
        public override JobHandle MigrateTo(JobHandle dependsOn, IDataSource destinationDataSource, ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray, DestinationWorldDataMap destinationWorldDataMap)
        {
            CancelProgressDataSource destination = destinationDataSource as CancelProgressDataSource;

            return dependsOn;
        }
    }
}
