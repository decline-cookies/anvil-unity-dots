using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class VDWrapperForIterate : AbstractVDWrapper
    {
        public VDWrapperForIterate(IVirtualData data) : base(data)
        {
        }

        public override JobHandle Acquire()
        {
            Data.AccessController.Acquire(AccessType.SharedRead);
            return default;
        }

        public override void Release(JobHandle releaseAccessDependency)
        {
            Data.AccessController.Release();
        }
    }
}
