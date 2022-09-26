using System;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IUpdateJobConfigRequirements : IJobConfigRequirements
    {
        public IUpdateJobConfigRequirements RequireResolveTarget<TResolveTarget>(TResolveTarget resolveTarget)
            where TResolveTarget : Enum;
    }
}