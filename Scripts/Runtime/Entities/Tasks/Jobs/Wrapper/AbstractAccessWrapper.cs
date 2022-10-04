using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractAccessWrapper : AbstractAnvilBase
    {
        protected AccessType AccessType { get; }

        protected AbstractAccessWrapper(AccessType accessType)
        {
            AccessType = accessType;
        }

        public abstract JobHandle Acquire();
        public abstract void Release(JobHandle releaseAccessDependency);
    }
}
