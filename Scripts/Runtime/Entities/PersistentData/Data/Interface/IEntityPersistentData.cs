using Anvil.Unity.DOTS.Entities.TaskDriver;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IEntityPersistentData<T> : IAbstractPersistentData
        where T : struct, IEntityPersistentDataInstance
    {
        public JobHandle AcquireReaderAsync(out EntityPersistentDataReader<T> reader);
        public void ReleaseReaderAsync(JobHandle dependsOn);

        public EntityPersistentDataReader<T> AcquireReader();
        public void ReleaseReader();

        public JobHandle AcquireWriterAsync(out EntityPersistentDataWriter<T> writer);
        public void ReleaseWriterAsync(JobHandle dependsOn);

        public EntityPersistentDataWriter<T> AcquireWriter();
        public void ReleaseWriter();
    }
}
