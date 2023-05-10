using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class EntityProxyDataSource<TInstance> : AbstractDataSource<EntityProxyInstanceWrapper<TInstance>>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        private EntityProxyDataSourceConsolidator<TInstance> m_Consolidator;

        public EntityProxyDataSource(
            TaskDriverManagementSystem taskDriverManagementSystem) 
            : base(taskDriverManagementSystem)
        {
            EntityWorldMigrationSystem.RegisterForEntityPatching<EntityProxyInstanceWrapper<TInstance>>();
            EntityProxyInstanceWrapper<TInstance>.Debug_EnsureOffsetsAreCorrect();
        }

        protected override void DisposeSelf()
        {
            m_Consolidator.Dispose();
            base.DisposeSelf();
        }

        protected override void HardenSelf()
        {
            base.HardenSelf();

            //We need to ensure we get the right access to any of the cancel data structures
            foreach (AbstractData data in DataTargets)
            {
                //If this piece of data can be cancelled, we need to be able to read the associated Cancel Request lookup
                if (data.CancelRequestBehaviour is CancelRequestBehaviour.Delete or CancelRequestBehaviour.Unwind)
                {
                    AddConsolidationData(((ITaskSetOwner)data.DataOwner).TaskSet.CancelRequestsDataStream.ActiveLookupData, AccessType.SharedRead);
                }
            }

            m_Consolidator = new EntityProxyDataSourceConsolidator<TInstance>(PendingData, DataTargets);
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
            EntityProxyDataSource<TInstance> destination = destinationDataSource as EntityProxyDataSource<TInstance>;
            UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.Writer destinationWriter = default;

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
                ref remapArray);
            dependsOn = migrateJob.Schedule(dependsOn);

            PendingData.ReleaseAsync(dependsOn);
            destination?.PendingData.ReleaseAsync(dependsOn);

            return dependsOn;
        }

        [BurstCompile]
        private struct MigrateJob : IJob
        {
            private const int UNSET_ID = -1;

            private UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> m_CurrentStream;
            private readonly UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.Writer m_DestinationStreamWriter;
            [ReadOnly] private NativeArray<EntityRemapUtility.EntityRemapInfo> m_RemapArray;
            [NativeSetThreadIndex] private readonly int m_NativeThreadIndex;

            public MigrateJob(
                UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>> currentStream,
                UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.Writer destinationStreamWriter,
                ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray)
            {
                m_CurrentStream = currentStream;
                m_DestinationStreamWriter = destinationStreamWriter;
                m_RemapArray = remapArray;

                m_NativeThreadIndex = UNSET_ID;
            }

            public void Execute()
            {
                //Can't modify while iterating so we collapse down to a single array and clean the underlying stream.
                //We'll build this stream back up if anything should still remain
                NativeArray<EntityProxyInstanceWrapper<TInstance>> currentInstanceArray = m_CurrentStream.ToNativeArray(Allocator.Temp);
                m_CurrentStream.Clear();

                int laneIndex = ParallelAccessUtil.CollectionIndexForThread(m_NativeThreadIndex);

                UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.LaneWriter currentLaneWriter = m_CurrentStream.AsLaneWriter(laneIndex);
                UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.LaneWriter destinationLaneWriter =
                    m_DestinationStreamWriter.IsCreated
                        ? m_DestinationStreamWriter.AsLaneWriter(laneIndex)
                        : default;

                for (int i = 0; i < currentInstanceArray.Length; ++i)
                {
                    EntityProxyInstanceWrapper<TInstance> instance = currentInstanceArray[i];
                    EntityProxyInstanceID instanceID = instance.InstanceID;

                    //If we don't exist in the new world then we stayed in this world and we need to rewrite ourselves 
                    //to our own stream
                    if (!instanceID.Entity.TryGetRemappedEntity(ref m_RemapArray, out Entity remappedEntity))
                    {
                        currentLaneWriter.Write(ref instance);
                        continue;
                    }

                    //If we don't have a destination in the new world, then we can just let these cease to exist
                    if (!destinationLaneWriter.IsCreated)
                    {
                        continue;
                    }

                    //If we do have a destination, then we will want to patch the entity references
                    instance.PatchEntityReferences(ref m_RemapArray);

                    //Write to the destination stream
                    destinationLaneWriter.Write(instance);
                }
            }
        }

        //*************************************************************************************************************
        // EXECUTION
        //*************************************************************************************************************
        protected sealed override JobHandle ConsolidateSelf(JobHandle dependsOn)
        {
            ConsolidateEntityProxyDataSourceJob consolidateEntityProxyDataSourceJob = new ConsolidateEntityProxyDataSourceJob(m_Consolidator);
            dependsOn = consolidateEntityProxyDataSourceJob.Schedule(dependsOn);

            return dependsOn;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct ConsolidateEntityProxyDataSourceJob : IJob
        {
            private EntityProxyDataSourceConsolidator<TInstance> m_Consolidator;

            public ConsolidateEntityProxyDataSourceJob(EntityProxyDataSourceConsolidator<TInstance> dataSourceConsolidator) : this()
            {
                m_Consolidator = dataSourceConsolidator;
            }

            public void Execute()
            {
                m_Consolidator.Consolidate();
            }
        }
    }
}
