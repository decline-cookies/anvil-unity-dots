using System;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IUpdateJobConfigRequirements : IJobConfigRequirements
    {
        public IUpdateJobConfigRequirements RequireResolveChannel<TResolveChannel>(TResolveChannel resolveChannel)
            where TResolveChannel : Enum;
    }
}
