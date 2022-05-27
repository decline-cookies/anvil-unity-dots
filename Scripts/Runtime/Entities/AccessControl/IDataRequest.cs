using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IDataRequest<T>
        where T : struct, ICompleteData<T>
    {
        void RegisterResponseChannel(IDataResponse<T> responseChannel);
        void UnregisterResponseChannel(IDataResponse<T> responseChannel);

        JobHandle AcquireForNew(out DataRequestJobStruct<T> jobData);
        void ReleaseForNew(JobHandle releaseAccessDependency);
    }
}
