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
                                             IWorldEntityPersistentData<T>
        where T : unmanaged, IEntityPersistentDataInstance
    {
        public EntityPersistentData()
            : base(new UnsafeParallelHashMap<Entity, T>(ChunkUtil.MaxElementsPerChunk<Entity>(), Allocator.Persistent))
        {
            //We don't know what will be stored in here, but if there are Entity references we want to be able to patch them
            MigrationUtil.RegisterTypeForEntityPatching<T>();
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

        public JobHandle AcquireReaderAsync(out EntityPersistentDataReader<T> reader)
        {
            JobHandle dependsOn = AcquireAsync(AccessType.SharedRead);
            reader = CreateEntityPersistentDataReader();
            return dependsOn;
        }

        public void ReleaseReaderAsync(JobHandle dependsOn)
        {
            ReleaseAsync(dependsOn);
        }

        public EntityPersistentDataReader<T> AcquireReader()
        {
            Acquire(AccessType.SharedRead);
            return CreateEntityPersistentDataReader();
        }

        public void ReleaseReader()
        {
            Release();
        }

        public JobHandle AcquireWriterAsync(out EntityPersistentDataWriter<T> writer)
        {
            JobHandle dependsOn = AcquireAsync(AccessType.SharedWrite);
            writer = CreateEntityPersistentDataWriter();
            return dependsOn;
        }

        public void ReleaseWriterAsync(JobHandle dependsOn)
        {
            ReleaseAsync(dependsOn);
        }

        public EntityPersistentDataWriter<T> AcquireWriter()
        {
            Acquire(AccessType.SharedWrite);
            return CreateEntityPersistentDataWriter();
        }

        public void ReleaseWriter()
        {
            Release();
        }
        
        //*************************************************************************************************************
        // MIGRATION
        //*************************************************************************************************************

        public override JobHandle MigrateTo(JobHandle dependsOn, PersistentDataSystem destinationPersistentDataSystem, ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray)
        {
            //This ensures there is a target on the other world to go to
            EntityPersistentData<T> destinationPersistentData = destinationPersistentDataSystem.GetOrCreateEntityPersistentData<T>();
            
            //Launch the migration job to get that burst speed
            dependsOn = JobHandle.CombineDependencies(dependsOn,
                AcquireWriterAsync(out EntityPersistentDataWriter<T> currentData),
                destinationPersistentData.AcquireWriterAsync(out EntityPersistentDataWriter<T> destinationData));

            MigrateJob migrateJob = new MigrateJob(
                currentData,
                destinationData,
                ref remapArray);
            dependsOn = migrateJob.Schedule(dependsOn);
            
            destinationPersistentData.ReleaseWriterAsync(dependsOn);
            ReleaseWriterAsync(dependsOn);

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
                //Can't remove while iterating so we collapse to an array first of our current keys/values
                NativeKeyValueArrays<Entity, T> currentEntries = m_CurrentData.GetKeyValueArrays(Allocator.Temp);

                for (int i = 0; i < currentEntries.Length; ++i)
                {
                    Entity currentEntity = currentEntries.Keys[i];
                    //If we don't exist in the new world we can just skip, we stayed in this world
                    if (!currentEntity.IfEntityIsRemapped(ref m_RemapArray, out Entity remappedEntity))
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
