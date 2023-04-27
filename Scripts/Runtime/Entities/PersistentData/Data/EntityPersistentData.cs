using Anvil.Unity.DOTS.Entities.TaskDriver;
using Anvil.Unity.DOTS.Jobs;
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
        where T : struct, IEntityPersistentDataInstance
    {
        public EntityPersistentData()
            : base(new UnsafeParallelHashMap<Entity, T>(ChunkUtil.MaxElementsPerChunk<Entity>(), Allocator.Persistent))
        {
            MigrationReflectionHelper.RegisterTypeForEntityPatching<T>();
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

        public override void MigrateTo(AbstractPersistentData destinationPersistentData, ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray)
        {
            //Our destination in the other world could be null... TODO: Should we create?
            if (destinationPersistentData is not EntityPersistentData<T> destination)
            {
                return;
            }
            
            EntityPersistentDataWriter<T> currentData = AcquireWriter();
            EntityPersistentDataWriter<T> destinationData = destination.AcquireWriter();

            NativeKeyValueArrays<Entity, T> currentElements = currentData.GetKeyValueArrays(Allocator.Temp);

            for (int i = 0; i < currentElements.Length; ++i)
            {
                Entity currentEntity = currentElements.Keys[i];
                Entity remappedEntity = EntityRemapUtility.RemapEntity(ref remapArray, currentEntity);
                //We don't exist in the new world, we should just stay here.
                if (remappedEntity == Entity.Null)
                {
                    continue;
                }
                
                //Otherwise, prepare us in the migration data
                currentData.Remove(currentEntity);
                T currentValue = currentElements.Values[i];

                currentValue.PatchEntityReferences(ref remappedEntity);

                //TODO: Could this be a problem? Is there data already here that wasn't moved?
                destinationData[remappedEntity] = currentValue;
            }
            
            destination.ReleaseWriter();
            ReleaseWriter();
        }
    }
}
