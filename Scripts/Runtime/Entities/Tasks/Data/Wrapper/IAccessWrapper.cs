using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal interface IAccessWrapper
    {
        public JobHandle Acquire();
        public void Release(JobHandle releaseAccessDependency);
    }
}
