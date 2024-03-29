using Anvil.Unity.DOTS.Entities.TaskDriver;
using Anvil.Unity.DOTS.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class EntityPersistentData<T> : AbstractTypedPersistentData<UnsafeParallelHashMap<Entity, T>>,
                                             IDriverEntityPersistentData<T>,
                                             ISystemEntityPersistentData<T>,
                                             IWorldEntityPersistentData<T>,
                                             IMigratablePersistentData
        where T : unmanaged, IEntityPersistentDataInstance
    {
        public EntityPersistentData(IDataOwner dataOwner, string uniqueContextIdentifier)
            : base(
                dataOwner, 
                new UnsafeParallelHashMap<Entity, T>(ChunkUtil.MaxElementsPerChunk<Entity>(), Allocator.Persistent), 
                uniqueContextIdentifier)
        {
            //We don't know what will be stored in here, but if there are Entity references we want to be able to patch them
            EntityWorldMigrationSystem.RegisterForEntityPatching<T>();
        }

        protected override void DisposeData()
        {
            ref UnsafeParallelHashMap<Entity, T> data = ref Data;
            foreach (KeyValue<Entity, T> entry in data)
            {
                entry.Value.Dispose();
            }
            base.DisposeData();
        }

        public EntityPersistentDataReader<T> CreateEntityPersistentDataReader()
        {
            return new EntityPersistentDataReader<T>(ref Data);
        }

        public EntityPersistentDataWriter<T> CreateEntityPersistentDataWriter()
        {
            return new EntityPersistentDataWriter<T>(ref Data);
        }

        public JobHandle AcquireReadAsync(out EntityPersistentDataReader<T> reader)
        {
            reader = CreateEntityPersistentDataReader();
            return AcquireAsync(AccessType.SharedRead);
            ;
        }

        public EntityPersistentDataReader<T> AcquireRead()
        {
            Acquire(AccessType.SharedRead);
            return CreateEntityPersistentDataReader();
        }

        public AccessControlledValue<EntityPersistentDataReader<T>>.AccessHandle AcquireWithReadHandle()
        {
            return new AccessControlledValue<EntityPersistentDataReader<T>>.AccessHandle(
                AcquireWithHandle(AccessType.SharedRead),
                CreateEntityPersistentDataReader());
        }

        public JobHandle AcquireSharedWriteAsync(out EntityPersistentDataWriter<T> writer)
        {
            writer = CreateEntityPersistentDataWriter();
            return AcquireAsync(AccessType.SharedWrite);
        }

        public EntityPersistentDataWriter<T> AcquireSharedWrite()
        {
            Acquire(AccessType.SharedWrite);
            return CreateEntityPersistentDataWriter();
        }

        public AccessControlledValue<EntityPersistentDataWriter<T>>.AccessHandle AcquireWithSharedWriteHandle()
        {
            return new AccessControlledValue<EntityPersistentDataWriter<T>>.AccessHandle(
                AcquireWithHandle(AccessType.SharedWrite),
                CreateEntityPersistentDataWriter());
        }

        public JobHandle AcquireExclusiveWriteAsync(out EntityPersistentDataWriter<T> writer)
        {
            writer = CreateEntityPersistentDataWriter();
            return AcquireAsync(AccessType.ExclusiveWrite);
        }

        public EntityPersistentDataWriter<T> AcquireExclusiveWrite()
        {
            Acquire(AccessType.ExclusiveWrite);
            return CreateEntityPersistentDataWriter();
        }

        public AccessControlledValue<EntityPersistentDataWriter<T>>.AccessHandle AcquireWithExclusiveWriteHandle()
        {
            return new AccessControlledValue<EntityPersistentDataWriter<T>>.AccessHandle(
                AcquireWithHandle(AccessType.ExclusiveWrite),
                CreateEntityPersistentDataWriter());
        }


        //*************************************************************************************************************
        // MIGRATION
        //*************************************************************************************************************

        public JobHandle MigrateTo(JobHandle dependsOn, IMigratablePersistentData destinationPersistentData, ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray)
        {
            EntityPersistentData<T> destinationEntityPersistentData = (EntityPersistentData<T>)destinationPersistentData;

            //Launch the migration job to get that burst speed
            dependsOn = JobHandle.CombineDependencies(
                dependsOn,
                AcquireExclusiveWriteAsync(out EntityPersistentDataWriter<T> currentData),
                destinationEntityPersistentData.AcquireExclusiveWriteAsync(out EntityPersistentDataWriter<T> destinationData));

            MigrateJob migrateJob = new MigrateJob(
                currentData,
                destinationData,
                ref remapArray);
            dependsOn = migrateJob.Schedule(dependsOn);

            destinationEntityPersistentData.ReleaseAsync(dependsOn);
            ReleaseAsync(dependsOn);

            return dependsOn;
        }

        [BurstCompile]
        private struct MigrateJob : IJob
        {
            private EntityPersistentDataWriter<T> m_CurrentData;
            private EntityPersistentDataWriter<T> m_DestinationData;
            [ReadOnly] private NativeArray<EntityRemapUtility.EntityRemapInfo> m_RemapArray;

            public MigrateJob(
                EntityPersistentDataWriter<T> currentData,
                EntityPersistentDataWriter<T> destinationData,
                ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray)
            {
                m_CurrentData = currentData;
                m_DestinationData = destinationData;
                m_RemapArray = remapArray;
            }

            public void Execute()
            {
                //TODO: Optimization: Could pass through the array of entities that were moving to avoid the copy. See: https://github.com/decline-cookies/anvil-unity-dots/pull/232#discussion_r1181697951

                //Can't remove while iterating so we collapse to an array first of our current keys/values
                NativeKeyValueArrays<Entity, T> currentEntries = m_CurrentData.GetKeyValueArrays(Allocator.Temp);

                for (int i = 0; i < currentEntries.Length; ++i)
                {
                    Entity currentEntity = currentEntries.Keys[i];
                    //If we don't exist in the new world we can just skip, we stayed in this world
                    if (!currentEntity.TryGetRemappedEntity(ref m_RemapArray, out Entity remappedEntity))
                    {
                        continue;
                    }

                    //Otherwise, remove us from this world's lookup
                    m_CurrentData.Remove(currentEntity);

                    //Get our data and patch it
                    T currentValue = currentEntries.Values[i];
                    currentValue.PatchEntityReferences(ref m_RemapArray);

                    //Then write the newly remapped data to the new world's lookup
                    m_DestinationData[remappedEntity] = currentValue;
                }
            }
        }
    }
}