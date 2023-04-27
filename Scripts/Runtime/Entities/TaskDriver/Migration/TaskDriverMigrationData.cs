using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class TaskDriverMigrationData : AbstractAnvilBase
    {
        private readonly Dictionary<string, uint> m_MigrationTaskSetOwnerIDLookup;
        private readonly Dictionary<string, uint> m_MigrationActiveIDLookup;
        private readonly Dictionary<World, DestinationWorldDataMap> m_DestinationWorldDataMaps;

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
        }

        protected override void DisposeSelf()
        {
            m_DestinationWorldDataMaps.DisposeAllValuesAndClear();
            base.DisposeSelf();
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

        public DestinationWorldDataMap GetOrCreateDestinationWorldDataMapFor(World destinationWorld, TaskDriverMigrationData destinationMigrationData)
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
    }
}
