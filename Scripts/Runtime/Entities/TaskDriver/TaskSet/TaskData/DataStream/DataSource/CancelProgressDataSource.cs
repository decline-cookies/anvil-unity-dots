using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class CancelProgressDataSource : AbstractEntityProxyInstanceIDDataSource
    {

        // ReSharper disable once InconsistentNaming
        private NativeArray<JobHandle> m_MigrationDependencies_ScratchPad;
        
        public CancelProgressDataSource(TaskDriverManagementSystem taskDriverManagementSystem) : base(taskDriverManagementSystem, "CANCEL_PROGRESS") { }

        protected override void DisposeSelf()
        {
            if (m_MigrationDependencies_ScratchPad.IsCreated)
            {
                m_MigrationDependencies_ScratchPad.Dispose();
            }
            base.DisposeSelf();
        }

        protected override void HardenSelf()
        {
            base.HardenSelf();

            //One extra for base dependency
            m_MigrationDependencies_ScratchPad = new NativeArray<JobHandle>(ActiveDataLookupByDataTargetID.Count + 1, Allocator.Persistent);
        }
        
        protected override JobHandle ConsolidateSelf(JobHandle dependsOn)
        {
            throw new InvalidOperationException($"CancelProgress Data Never needs to be consolidated");
        }

        //*************************************************************************************************************
        // MIGRATION
        //*************************************************************************************************************
        
        public override JobHandle MigrateTo(
            JobHandle dependsOn, 
            IDataSource destinationDataSource, 
            ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray, 
            DestinationWorldDataMap destinationWorldDataMap)
        {
            //TODO: Optimization by using a list of entities that moved and iterating through that instead. See: https://github.com/decline-cookies/anvil-unity-dots/pull/232#discussion_r1181717999
            int index = 0;
            foreach (KeyValuePair<DataTargetID, AbstractData> entry in ActiveDataLookupByDataTargetID)
            {
                ActiveLookupData<EntityProxyInstanceID> activeLookupData = (ActiveLookupData<EntityProxyInstanceID>)entry.Value;

                m_MigrationDependencies_ScratchPad[index] = MigrateTo(
                    dependsOn,
                    activeLookupData,
                    destinationDataSource,
                    ref remapArray,
                    destinationWorldDataMap);
                index++;
            }
            m_MigrationDependencies_ScratchPad[index] = base.MigrateTo(
                dependsOn,
                destinationDataSource,
                ref remapArray,
                destinationWorldDataMap);

            return JobHandle.CombineDependencies(m_MigrationDependencies_ScratchPad);
        }

        private JobHandle MigrateTo(
            JobHandle dependsOn,
            ActiveLookupData<EntityProxyInstanceID> currentLookupData,
            IDataSource destinationDataSource,
            ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray,
            DestinationWorldDataMap destinationWorldDataMap)
        {
            ActiveLookupData<EntityProxyInstanceID> destinationLookupData = null;
            
            //If we don't have a destination or a mapping to a destination active ID...
            if (destinationDataSource is not CancelProgressDataSource destination 
                || !destinationWorldDataMap.DataTargetIDMapping.TryGetValue(
                    currentLookupData.DataTargetID, 
                    out DataTargetID destinationDataTargetID))
            {
                //Then we can only deal with ourselves
                dependsOn = JobHandle.CombineDependencies(
                    dependsOn,
                    currentLookupData.AcquireAsync(AccessType.ExclusiveWrite));
            }
            else
            {
                destinationLookupData = (ActiveLookupData<EntityProxyInstanceID>)destination.ActiveDataLookupByDataTargetID[destinationDataTargetID];
                
                dependsOn = JobHandle.CombineDependencies(
                    dependsOn,
                    currentLookupData.AcquireAsync(AccessType.ExclusiveWrite),
                    destinationLookupData.AcquireAsync(AccessType.ExclusiveWrite));
            }

            MigrateJob migrateJob = new MigrateJob(
                currentLookupData.Lookup,
                destinationLookupData?.Lookup ?? default,
                ref remapArray,
                destinationWorldDataMap.DataOwnerIDMapping,
                destinationWorldDataMap.DataTargetIDMapping);

            dependsOn = migrateJob.Schedule(dependsOn);

            currentLookupData.ReleaseAsync(dependsOn);
            destinationLookupData?.ReleaseAsync(dependsOn);

            return dependsOn;
        }

        [BurstCompile]
        private struct MigrateJob : IJob
        {
            private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_CurrentLookup;
            private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_DestinationLookup;
            [ReadOnly] private NativeArray<EntityRemapUtility.EntityRemapInfo> m_RemapArray;
            [ReadOnly] private readonly NativeParallelHashMap<DataOwnerID, DataOwnerID> m_DataOwnerIDMapping;
            [ReadOnly] private readonly NativeParallelHashMap<DataTargetID, DataTargetID> m_DataTargetIDMapping;

            public MigrateJob(
                UnsafeParallelHashMap<EntityProxyInstanceID, bool> currentLookup, 
                UnsafeParallelHashMap<EntityProxyInstanceID, bool> destinationLookup, 
                ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray, 
                NativeParallelHashMap<DataOwnerID, DataOwnerID> dataOwnerIDMapping, 
                NativeParallelHashMap<DataTargetID, DataTargetID> dataTargetIDMapping)
            {
                m_CurrentLookup = currentLookup;
                m_DestinationLookup = destinationLookup;
                m_RemapArray = remapArray;
                m_DataOwnerIDMapping = dataOwnerIDMapping;
                m_DataTargetIDMapping = dataTargetIDMapping;
            }

            public void Execute()
            {
                //Can't remove while iterating so we collapse to an array first of our current keys/values
                NativeKeyValueArrays<EntityProxyInstanceID, bool> currentEntries = m_CurrentLookup.GetKeyValueArrays(Allocator.Temp);

                for (int i = 0; i < currentEntries.Length; ++i)
                {
                    EntityProxyInstanceID currentID = currentEntries.Keys[i];
                    //If we don't exist in the new world, we can just skip, we stayed in this world
                    if (!currentID.Entity.TryGetRemappedEntity(ref m_RemapArray, out Entity remappedEntity))
                    {
                        continue;
                    }
                    
                    //Otherwise, remove us from this world's lookup
                    m_CurrentLookup.Remove(currentID);

                    //If we don't have a destination in the new world, then we can just let these cease to exist
                    if (!m_DataOwnerIDMapping.TryGetValue(currentID.DataOwnerID, out DataOwnerID destinationDataOwnerID)
                        || !m_DataTargetIDMapping.TryGetValue(currentID.DataTargetID, out DataTargetID destinationDataTargetID))
                    {
                        continue;
                    }
                    
                    //Patch our ID with new values
                    currentID.PatchEntityReferences(ref m_RemapArray);

                    //Write to the destination lookup
                    m_DestinationLookup.Add(currentID, currentEntries.Values[i]);
                }
            }
        }
    }
}
