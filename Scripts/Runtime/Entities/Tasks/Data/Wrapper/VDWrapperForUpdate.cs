using Anvil.Unity.DOTS.Data;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class VDWrapperForUpdate : AbstractVDWrapper
    {
        public VDWrapperForUpdate(AbstractVirtualData data) : base(data)
        {
        }

        public override JobHandle Acquire()
        {
            Data.AcquireForUpdate();
            return default;
        }

        public override void Release(JobHandle releaseAccessDependency)
        {
            Data.ReleaseForUpdate();
        }
    }
}
