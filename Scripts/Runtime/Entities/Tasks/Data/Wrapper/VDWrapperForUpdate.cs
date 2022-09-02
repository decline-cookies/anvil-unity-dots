using Anvil.Unity.DOTS.Data;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class VDWrapperForUpdate : AbstractVDWrapper
    {
        public VDWrapperForUpdate(AbstractProxyDataStream data) : base(data)
        {
        }
        public override JobHandle AcquireAsync()
        {
            return Data.AcquireForUpdateAsync();
        }

        public override void ReleaseAsync(JobHandle releaseAccessDependency)
        {
            Data.ReleaseForUpdateAsync(releaseAccessDependency);
        }

        public override void Acquire()
        {
            Data.AcquireForUpdate();
        }

        public override void Release()
        {
            Data.ReleaseForUpdate();
        }
    }
}
