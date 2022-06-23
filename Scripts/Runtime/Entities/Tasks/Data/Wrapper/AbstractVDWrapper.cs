using Anvil.Unity.DOTS.Data;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal abstract class AbstractVDWrapper : IDataWrapper
    {
        public IVirtualData Data
        {
            get;
        }

        protected AbstractVDWrapper(IVirtualData data)
        {
            Data = data;
        }

        public abstract JobHandle Acquire();
        public abstract void Release(JobHandle releaseAccessDependency);
    }
}
