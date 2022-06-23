using Anvil.Unity.DOTS.Data;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class VDWrapperAsResultsDestination : AbstractVDWrapper
    {
        public VDWrapperAsResultsDestination(IVirtualData data) : base(data)
        {
        }

        public override JobHandle Acquire()
        {
            return default;
        }

        public override void Release(JobHandle releaseAccessDependency)
        {
            //Does nothing
        }
    }
}
