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
        private readonly Dictionary<string, uint> m_MigrationTaskSetOwnerIDLookup;
        private readonly Dictionary<string, uint> m_MigrationActiveIDLookup;
        private readonly Dictionary<World, DestinationWorldDataMap> m_DestinationWorldDataMaps;
        private readonly Dictionary<Type, IDataSource> m_AllDataSources;
        // ReSharper disable once InconsistentNaming
        private NativeList<JobHandle> m_MigrationDependencies_ScratchPad;

        public int TaskSetOwnerCount
        {
            get => m_MigrationTaskSetOwnerIDLookup.Count;
        }

        public int ActiveIDCount
        {
            get => m_MigrationActiveIDLookup.Count;
        }

        public TaskDriverMigrationData() 
        {
            m_MigrationTaskSetOwnerIDLookup = new Dictionary<string, uint>();
            m_MigrationActiveIDLookup = new Dictionary<string, uint>();
            m_DestinationWorldDataMaps = new Dictionary<World, DestinationWorldDataMap>();
            m_AllDataSources = new Dictionary<Type, IDataSource>();
            m_MigrationDependencies_ScratchPad = new NativeList<JobHandle>(32, Allocator.Persistent);
        }

        protected override void DisposeSelf()
        {
            m_DestinationWorldDataMaps.DisposeAllValuesAndClear();
            m_MigrationDependencies_ScratchPad.Dispose();
            base.DisposeSelf();
        }

        public void AddDataSource<T>(T dataSource)
            where T : class, IDataSource
        {
            Type type = dataSource.GetType();
            m_AllDataSources.Add(type, dataSource);
            m_MigrationDependencies_ScratchPad.ResizeUninitialized(m_AllDataSources.Count);
        }
        
        public void PopulateMigrationLookup(List<AbstractTaskDriver> topLevelTaskDrivers)
        {
            //Generate a World ID
            foreach (AbstractTaskDriver topLevelTaskDriver in topLevelTaskDrivers)
            {
                topLevelTaskDriver.AddToMigrationLookup(
                    string.Empty, 
                    m_MigrationTaskSetOwnerIDLookup, 
                    m_MigrationActiveIDLookup);
            }
        }

        private DestinationWorldDataMap GetOrCreateDestinationWorldDataMapFor(World destinationWorld, TaskDriverMigrationData destinationMigrationData)
        {
            if (!m_DestinationWorldDataMaps.TryGetValue(destinationWorld, out DestinationWorldDataMap destinationWorldDataMap))
            {
                //We're going to the Destination World so we can't have more than they have 
                NativeParallelHashMap<uint, uint> taskSetOwnerIDMapping = new NativeParallelHashMap<uint, uint>(destinationMigrationData.TaskSetOwnerCount, Allocator.Persistent);
                NativeParallelHashMap<uint, uint> activeIDMapping = new NativeParallelHashMap<uint, uint>(destinationMigrationData.ActiveIDCount, Allocator.Persistent);

                foreach (KeyValuePair<string, uint> entry in m_MigrationTaskSetOwnerIDLookup)
                {
                    if (!destinationMigrationData.m_MigrationTaskSetOwnerIDLookup.TryGetValue(entry.Key, out uint dstTaskSetOwnerID))
                    {
                        continue;
                    }
                    taskSetOwnerIDMapping.Add(entry.Value, dstTaskSetOwnerID);
                }
                
                foreach (KeyValuePair<string, uint> entry in m_MigrationActiveIDLookup)
                {
                    if (!destinationMigrationData.m_MigrationActiveIDLookup.TryGetValue(entry.Key, out uint dstActiveID))
                    {
                        continue;
                    }
                    activeIDMapping.Add(entry.Value, dstActiveID);
                }
                
                
                destinationWorldDataMap = new DestinationWorldDataMap(taskSetOwnerIDMapping, activeIDMapping);
                m_DestinationWorldDataMaps.Add(destinationWorld, destinationWorldDataMap);
            }
            return destinationWorldDataMap;
        }

        public JobHandle MigrateTo(JobHandle dependsOn, World destinationWorld, TaskDriverMigrationData destinationTaskDriverMigrationData, ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray)
        {
            //Lazy create a World to World mapping lookup for ActiveIDs and TaskSetOwnerIDs
            DestinationWorldDataMap destinationWorldDataMap = GetOrCreateDestinationWorldDataMapFor(destinationWorld, destinationTaskDriverMigrationData);

            Dictionary<Type, IDataSource> destinationDataSourcesByType = destinationTaskDriverMigrationData.m_AllDataSources;

            int index = 0;
            foreach (KeyValuePair<Type, IDataSource> entry in m_AllDataSources)
            {
                //We may not have a corresponding destination Data Source in the destination world but we still want to process the migration so that 
                //we remove any references in this world. If we do have the corresponding data source, we'll transfer over to the other world.
                destinationDataSourcesByType.TryGetValue(entry.Key, out IDataSource destinationDataSource);
                m_MigrationDependencies_ScratchPad[index] = entry.Value.MigrateTo(dependsOn, destinationDataSource, ref remapArray, destinationWorldDataMap);
                index++;
            }

            return JobHandle.CombineDependencies(m_MigrationDependencies_ScratchPad);
        }
    }
}
