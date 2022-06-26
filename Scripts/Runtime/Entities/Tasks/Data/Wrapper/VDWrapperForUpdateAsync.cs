using Anvil.Unity.DOTS.Data;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class VDWrapperForUpdateAsync : AbstractVDWrapper
    {
        public VDWrapperForUpdateAsync(AbstractVirtualData data) : base(data)
        {
        }

        public override JobHandle Acquire()
        {
            return Data.AcquireForUpdateAsync();
        }

        public override void Release(JobHandle releaseAccessDependency)
        {
            Data.ReleaseForUpdateAsync(releaseAccessDependency);
        }
    }
}
