using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class TaskDriverMigrationData : AbstractAnvilBase
    {
        // ReSharper disable once InconsistentNaming
        private NativeArray<JobHandle> m_MigrationDependencies_ScratchPad;
        private Dictionary<DataTargetID, IDataSource> m_DataSources;
        
        public TaskDriverMigrationData(List<IDataSource> dataSources) 
        {
            m_MigrationDependencies_ScratchPad = new NativeArray<JobHandle>(dataSources.Count, Allocator.Persistent);
            m_DataSources = new Dictionary<DataTargetID, IDataSource>();
            foreach (IDataSource dataSource in dataSources)
            {
                m_DataSources.Add(dataSource.PendingWorldUniqueID, dataSource);
            }
        }

        protected override void DisposeSelf()
        {
            m_MigrationDependencies_ScratchPad.Dispose();
            base.DisposeSelf();
        }

        public JobHandle MigrateTo(
            JobHandle dependsOn, 
            TaskDriverMigrationData destinationTaskDriverMigrationData, 
            ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray)
        {
            Dictionary<DataTargetID, IDataSource> destinationDataSourcesByType = destinationTaskDriverMigrationData.m_DataSources;

            int index = 0;
            foreach (KeyValuePair<DataTargetID, IDataSource> entry in m_DataSources)
            {
                //We may not have a corresponding destination Data Source in the destination world but we still want to process the migration so that 
                //we remove any references in this world. If we do have the corresponding data source, we'll transfer over to the other world.
                destinationDataSourcesByType.TryGetValue(entry.Key, out IDataSource destinationDataSource);
                m_MigrationDependencies_ScratchPad[index] = entry.Value.MigrateTo(dependsOn, destinationDataSource, ref remapArray);
                index++;
            }

            return JobHandle.CombineDependencies(m_MigrationDependencies_ScratchPad);
        }
    }
}
