using Anvil.Unity.DOTS.Jobs;
using System;
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

        public CancelProgressDataSource(TaskDriverManagementSystem taskDriverManagementSystem) : base(taskDriverManagementSystem) { }

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
            m_MigrationDependencies_ScratchPad = new NativeArray<JobHandle>(DataTargets.Count + 1, Allocator.Persistent);
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
            TaskDriverManagementSystem destinationTaskDriverManagementSystem,
            IDataSource destinationDataSource,
            ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray)
        {
            //TODO: Optimization by using a list of entities that moved and iterating through that instead. See: https://github.com/decline-cookies/anvil-unity-dots/pull/232#discussion_r1181717999
            int index = 0;
            foreach (AbstractData dataTarget in DataTargets)
            {
                if (dataTarget is not ActiveLookupData<EntityKeyedTaskID> activeLookupData)
                {
                    continue;
                }

                m_MigrationDependencies_ScratchPad[index] = MigrateTo(
                    dependsOn,
                    activeLookupData,
                    destinationTaskDriverManagementSystem,
                    destinationDataSource,
                    ref remapArray);
                index++;
            }
            m_MigrationDependencies_ScratchPad[index] = base.MigrateTo(
                dependsOn,
                destinationTaskDriverManagementSystem,
                destinationDataSource,
                ref remapArray);

            return JobHandle.CombineDependencies(m_MigrationDependencies_ScratchPad);
        }

        private JobHandle MigrateTo(
            JobHandle dependsOn,
            ActiveLookupData<EntityKeyedTaskID> currentLookupData,
            TaskDriverManagementSystem destinationTaskDriverManagementSystem,
            IDataSource destinationDataSource,
            ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray)
        {
            ActiveLookupData<EntityKeyedTaskID> destinationLookupData = null;

            //If we don't have a destination or a mapping to a destination active ID...
            if (destinationDataSource is not CancelProgressDataSource
                || !destinationTaskDriverManagementSystem.TryGetActiveLookupDataByID(
                    currentLookupData.WorldUniqueID,
                    out destinationLookupData))
            {
                //Then we can only deal with ourselves
                dependsOn = JobHandle.CombineDependencies(
                    dependsOn,
                    currentLookupData.AcquireAsync(AccessType.ExclusiveWrite));
            }
            else
            {
                dependsOn = JobHandle.CombineDependencies(
                    dependsOn,
                    currentLookupData.AcquireAsync(AccessType.ExclusiveWrite),
                    destinationLookupData.AcquireAsync(AccessType.ExclusiveWrite));
            }

            MigrateJob migrateJob = new MigrateJob(
                currentLookupData.Lookup,
                destinationLookupData?.Lookup ?? default,
                ref remapArray);

            dependsOn = migrateJob.Schedule(dependsOn);

            currentLookupData.ReleaseAsync(dependsOn);
            destinationLookupData?.ReleaseAsync(dependsOn);

            return dependsOn;
        }

        [BurstCompile]
        private struct MigrateJob : IJob
        {
            private UnsafeParallelHashMap<EntityKeyedTaskID, bool> m_CurrentLookup;
            private UnsafeParallelHashMap<EntityKeyedTaskID, bool> m_DestinationLookup;
            [ReadOnly] private NativeArray<EntityRemapUtility.EntityRemapInfo> m_RemapArray;

            public MigrateJob(
                UnsafeParallelHashMap<EntityKeyedTaskID, bool> currentLookup,
                UnsafeParallelHashMap<EntityKeyedTaskID, bool> destinationLookup,
                ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray)
            {
                m_CurrentLookup = currentLookup;
                m_DestinationLookup = destinationLookup;
                m_RemapArray = remapArray;
            }

            public void Execute()
            {
                //Can't remove while iterating so we collapse to an array first of our current keys/values
                NativeKeyValueArrays<EntityKeyedTaskID, bool> currentEntries = m_CurrentLookup.GetKeyValueArrays(Allocator.Temp);

                for (int i = 0; i < currentEntries.Length; ++i)
                {
                    EntityKeyedTaskID currentID = currentEntries.Keys[i];
                    //If we don't exist in the new world, we can just skip, we stayed in this world
                    if (!currentID.Entity.TryGetRemappedEntity(ref m_RemapArray, out Entity remappedEntity))
                    {
                        continue;
                    }

                    //Otherwise, remove us from this world's lookup
                    m_CurrentLookup.Remove(currentID);

                    //If we don't have a destination in the new world, then we can just let these cease to exist
                    if (!m_DestinationLookup.IsCreated)
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
