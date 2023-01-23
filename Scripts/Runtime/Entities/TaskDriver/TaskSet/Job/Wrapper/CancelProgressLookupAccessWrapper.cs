using Anvil.Unity.DOTS.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class CancelProgressLookupAccessWrapper : AbstractAccessWrapper
    {
        private readonly ActiveLookupData<EntityProxyInstanceID> m_CancelProgressLookupData;

        public UnsafeParallelHashMap<EntityProxyInstanceID, bool> ProgressLookup { get; }


        public CancelProgressLookupAccessWrapper(ActiveLookupData<EntityProxyInstanceID> cancelProgressLookupData, AccessType accessType, AbstractJobConfig.Usage usage) : base(accessType, usage)
        {
            m_CancelProgressLookupData = cancelProgressLookupData;
            ProgressLookup = m_CancelProgressLookupData.Lookup;
        }

        public override JobHandle AcquireAsync()
        {
            return m_CancelProgressLookupData.AcquireAsync(AccessType);
        }

        public override void ReleaseAsync(JobHandle dependsOn)
        {
            m_CancelProgressLookupData.ReleaseAsync(dependsOn);
        }
    }
}
