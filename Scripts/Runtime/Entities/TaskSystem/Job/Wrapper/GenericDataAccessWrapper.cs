using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class GenericDataAccessWrapper<TData> : AbstractAccessWrapper
        where TData : struct
    {
        private readonly AccessControlledValue<TData> m_AccessControlledCollection;
        private TData m_Collection;
        
        public TData Collection
        {
            get => m_Collection;
        }

        public GenericDataAccessWrapper(AccessControlledValue<TData> collection, AccessType accessType) : base(accessType)
        {
            m_AccessControlledCollection = collection;
        }
        
        public sealed override JobHandle Acquire()
        {
            return m_AccessControlledCollection.AcquireAsync(AccessType, out m_Collection);
        }

        public sealed override void Release(JobHandle releaseAccessDependency)
        {
            m_AccessControlledCollection.ReleaseAsync(releaseAccessDependency);
        }
    }
}
