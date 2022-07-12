using Anvil.Unity.DOTS.Data;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class VDWrapperAsResultsDestination : AbstractVDWrapper
    {
        public VDWrapperAsResultsDestination(AbstractVirtualData data) : base(data)
        {
        }

        public override JobHandle AcquireAsync()
        {
            //Does nothing because we're just passing back the data as pointer for where to write to later on. We're
            //not actually writing at this time so we don't care if we have access or not.
            return default;
        }

        public override void ReleaseAsync(JobHandle releaseAccessDependency)
        {
            //Does nothing because since we didn't need to actually acquire, we don't need to actually release.
        }

        public override void Acquire()
        {
            //Does nothing because we're just passing back the data as pointer for where to write to later on. We're
            //not actually writing at this time so we don't care if we have access or not.
        }

        public override void Release()
        {
            //Does nothing because since we didn't need to actually acquire, we don't need to actually release.
        }
    }
}
