using System;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IUpdatableJobConfigRequirements : IJobConfigRequirements
    {
        public IUpdatableJobConfigRequirements RequireResolveTarget<TResolveTarget>(TResolveTarget resolveTarget)
            where TResolveTarget : Enum;
    }
}
