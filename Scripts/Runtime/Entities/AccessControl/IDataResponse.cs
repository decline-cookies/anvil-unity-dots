using Anvil.Unity.DOTS.Data;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IDataResponse<T>
        where T : struct, ICompleteData<T>
    {
        JobHandle AcquireForResponse();
        void ReleaseForResponse(JobHandle releaseAccessDependency);

        UnsafeTypedStream<T>.Writer GetResponseChannel();
    }
}
