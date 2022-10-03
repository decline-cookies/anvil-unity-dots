using System;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IResolvableJobConfigRequirements : IJobConfigRequirements
    {
        public IResolvableJobConfigRequirements RequireResolveTarget<TResolveTarget>(TResolveTarget resolveTarget)
            where TResolveTarget : Enum;
    }
}
