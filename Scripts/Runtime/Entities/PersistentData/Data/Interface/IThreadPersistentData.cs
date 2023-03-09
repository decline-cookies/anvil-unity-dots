using Anvil.Unity.DOTS.Entities.TaskDriver;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IThreadPersistentData<T> : IAbstractPersistentData
        where T : unmanaged, IThreadPersistentDataInstance
    {
        public JobHandle AcquireAsync(out ThreadPersistentDataAccessor<T> accessor);
        public void ReleaseAsync(JobHandle dependsOn);

        public ThreadPersistentDataAccessor<T> Acquire();
        public void Release();
    }
}
