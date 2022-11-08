using Anvil.Unity.DOTS.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelProgressLookupAccessWrapper : AbstractAccessWrapper
    {
        private readonly AccessControlledValue<UnsafeParallelHashMap<EntityProxyInstanceID, bool>> m_CancelProgressLookup;
        private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_ProgressLookup;

        public UnsafeParallelHashMap<EntityProxyInstanceID, bool> ProgressLookup
        {
            get => m_ProgressLookup;
        }
        

        public CancelProgressLookupAccessWrapper(AccessControlledValue<UnsafeParallelHashMap<EntityProxyInstanceID, bool>> cancelProgressLookup, AccessType accessType, AbstractJobConfig.Usage usage) : base(accessType, usage)
        {
            m_CancelProgressLookup = cancelProgressLookup;
        }

        public override JobHandle Acquire()
        {
            return m_CancelProgressLookup.AcquireAsync(AccessType, out m_ProgressLookup);
        }

        public override void Release(JobHandle releaseAccessDependency)
        {
            m_CancelProgressLookup.ReleaseAsync(releaseAccessDependency);
        }
    }
}
