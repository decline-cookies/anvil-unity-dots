using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal abstract class AbstractEntityProxyInstanceIDDataSource : AbstractDataSource<EntityProxyInstanceID>
    {
        protected AbstractEntityProxyInstanceIDDataSource(TaskDriverManagementSystem taskDriverManagementSystem) : base(taskDriverManagementSystem)
        {
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
            CancelRequestsDataSource destination = destinationDataSource as CancelRequestsDataSource;
            UnsafeTypedStream<EntityProxyInstanceID>.Writer destinationWriter = default;
            
            if (destination == null)
            {
                dependsOn = JobHandle.CombineDependencies(
                    dependsOn,
                    PendingData.AcquireAsync(AccessType.ExclusiveWrite));
            }
            else
            {
                dependsOn = JobHandle.CombineDependencies(
                    dependsOn,
                    PendingData.AcquireAsync(AccessType.ExclusiveWrite),
                    destination.PendingData.AcquireAsync(AccessType.ExclusiveWrite));

                destinationWriter = destination.PendingWriter;
            }
            
            
            MigrateJob migrateJob = new MigrateJob(
                PendingData.Pending,
                destinationWriter,
                remapArray,
                destinationWorldDataMap.TaskSetOwnerIDMapping,
                destinationWorldDataMap.ActiveIDMapping);
            dependsOn = migrateJob.Schedule(dependsOn);

            PendingData.ReleaseAsync(dependsOn);
            destination?.PendingData.ReleaseAsync(dependsOn);

            return dependsOn;
        }
        
        [BurstCompile]
        private struct MigrateJob : IJob
        {
            private const int UNSET_ID = -1;

            private UnsafeTypedStream<EntityProxyInstanceID> m_CurrentStream;
            private readonly UnsafeTypedStream<EntityProxyInstanceID>.Writer m_DestinationStreamWriter;
            [ReadOnly] private NativeArray<EntityRemapUtility.EntityRemapInfo> m_RemapArray;
            [ReadOnly] private readonly NativeParallelHashMap<uint, uint> m_TaskSetOwnerIDMapping;
            [ReadOnly] private readonly NativeParallelHashMap<uint, uint> m_ActiveIDMapping;
            [NativeSetThreadIndex] private readonly int m_NativeThreadIndex;

            public MigrateJob(
                UnsafeTypedStream<EntityProxyInstanceID> currentStream,
                UnsafeTypedStream<EntityProxyInstanceID>.Writer destinationStreamWriter,
                NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray,
                NativeParallelHashMap<uint, uint> taskSetOwnerIDMapping,
                NativeParallelHashMap<uint, uint> activeIDMapping)
            {
                m_CurrentStream = currentStream;
                m_DestinationStreamWriter = destinationStreamWriter;
                m_RemapArray = remapArray;
                m_TaskSetOwnerIDMapping = taskSetOwnerIDMapping;
                m_ActiveIDMapping = activeIDMapping;

                m_NativeThreadIndex = UNSET_ID;
            }

            public void Execute()
            {
                //Can't modify while iterating so we collapse down to a single array and clean the underlying stream.
                //We'll build this stream back up if anything should still remain
                NativeArray<EntityProxyInstanceID> currentInstanceArray = m_CurrentStream.ToNativeArray(Allocator.Temp);
                m_CurrentStream.Clear();

                int laneIndex = ParallelAccessUtil.CollectionIndexForThread(m_NativeThreadIndex);

                UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter currentLaneWriter = m_CurrentStream.AsLaneWriter(laneIndex);
                UnsafeTypedStream<EntityProxyInstanceID>.LaneWriter destinationLaneWriter =
                    m_DestinationStreamWriter.IsCreated
                        ? m_DestinationStreamWriter.AsLaneWriter(laneIndex)
                        : default;

                for (int i = 0; i < currentInstanceArray.Length; ++i)
                {
                    EntityProxyInstanceID instanceID = currentInstanceArray[i];

                    //If we don't exist in the new world then we stayed in this world and we need to rewrite ourselves 
                    //to our own stream
                    if (!instanceID.Entity.IfEntityIsRemapped(ref m_RemapArray, out Entity remappedEntity))
                    {
                        currentLaneWriter.Write(ref instanceID);
                        continue;
                    }

                    //If we don't have a destination in the new world, then we can just let these cease to exist
                    if (!destinationLaneWriter.IsCreated
                        || !m_TaskSetOwnerIDMapping.TryGetValue(instanceID.TaskSetOwnerID, out uint destinationTaskSetOwnerID)
                        || !m_ActiveIDMapping.TryGetValue(instanceID.ActiveID, out uint destinationActiveID))
                    {
                        continue;
                    }

                    //If we do have a destination, then we will want to patch the entity references
                    instanceID.PatchEntityReferences(ref m_RemapArray);

                    //Rewrite the memory for the TaskSetOwnerID and ActiveID
                    instanceID.PatchIDs(
                        destinationTaskSetOwnerID,
                        destinationActiveID);

                    //Write to the destination stream
                    destinationLaneWriter.Write(instanceID);
                }
            }
        }
    }
}
