using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class VDWrapperForAdd : AbstractVDWrapper
    {
        public VDWrapperForAdd(IVirtualData data) : base(data)
        {
        }

        public override JobHandle Acquire()
        {
            Data.AccessController.Acquire(AccessType.SharedWrite);
            return default;
        }

        public override void Release(JobHandle releaseAccessDependency)
        {
            Data.AccessController.Release();
        }
    }
}
