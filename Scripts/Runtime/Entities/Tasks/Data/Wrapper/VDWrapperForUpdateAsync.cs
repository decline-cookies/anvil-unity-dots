using Anvil.Unity.DOTS.Data;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class VDWrapperForUpdateAsync : AbstractVDWrapper
    {
        public VDWrapperForUpdateAsync(IVirtualData data) : base(data)
        {
        }

        public override JobHandle Acquire()
        {
            return Data.AcquireForUpdate();
        }

        public override void Release(JobHandle releaseAccessDependency)
        {
            Data.ReleaseForUpdate(releaseAccessDependency);
        }
    }
}
