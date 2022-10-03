using System;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    public interface IResolvableJobConfigRequirements : IJobConfigRequirements
    {
        public IResolvableJobConfigRequirements RequireResolveTarget<TResolveTarget>(TResolveTarget resolveTarget)
            where TResolveTarget : Enum;
    }
}
