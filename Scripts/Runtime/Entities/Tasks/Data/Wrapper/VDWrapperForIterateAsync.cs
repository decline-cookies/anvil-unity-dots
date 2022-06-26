using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class VDWrapperForIterateAsync : AbstractVDWrapper
    {
        public VDWrapperForIterateAsync(AbstractVirtualData data) : base(data)
        {
        }

        public override JobHandle Acquire()
        {
            return Data.AccessController.AcquireAsync(AccessType.SharedRead);
        }

        public override void Release(JobHandle releaseAccessDependency)
        {
            Data.AccessController.ReleaseAsync(releaseAccessDependency);
        }
    }
}
