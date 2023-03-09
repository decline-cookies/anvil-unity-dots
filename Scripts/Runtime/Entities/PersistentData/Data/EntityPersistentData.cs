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
        }

        protected override void DisposeData()
        {
            ref UnsafeParallelHashMap<Entity, T> data = ref Data;
            foreach (KeyValue<Entity, T> entry in data)
            {
                entry.Value.DisposeForEntity(entry.Key);
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
    }
}
