using Anvil.Unity.DOTS.Data;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal interface IDataWrapper
    {
        public IVirtualData Data
        {
            get;
        }
        JobHandle Acquire();
        void Release(JobHandle releaseAccessDependency);
    }
}
