using Anvil.Unity.DOTS.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelDataAccessWrapper : AbstractAccessWrapper
    {
        private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_ProgressLookup;
        
        public CancelData CancelData { get; }

        public UnsafeParallelHashMap<EntityProxyInstanceID, bool> ProgressLookup
        {
            get => m_ProgressLookup;
        }
        

        public CancelDataAccessWrapper(CancelData cancelData, AccessType accessType, AbstractJobConfig.Usage usage) : base(accessType, usage)
        {
            CancelData = cancelData;
        }

        public override JobHandle Acquire()
        {
            return CancelData.AcquireProgressLookup(AccessType, out m_ProgressLookup);
        }

        public override void Release(JobHandle releaseAccessDependency)
        {
            CancelData.ReleaseProgressLookup(releaseAccessDependency);
        }
    }
}
