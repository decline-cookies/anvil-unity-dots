using Anvil.Unity.DOTS.Data;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IDataOwner<T>
        where T : struct, ICompleteData<T>
    {
        JobHandle AcquireForUpdate(JobHandle dependsOn, out DataOwnerJobStruct<T> jobData);
        void ReleaseForUpdate(JobHandle releaseAccessDependency);
    }
}
