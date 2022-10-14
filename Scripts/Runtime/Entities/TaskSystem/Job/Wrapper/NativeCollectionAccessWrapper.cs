using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class NativeCollectionAccessWrapper<TCollection> : AbstractAccessWrapper
        where TCollection : struct
    {
        private readonly AccessControlledValue<TCollection> m_AccessControlledCollection;
        private TCollection m_Collection;
        
        public TCollection Collection
        {
            get => m_Collection;
        }

        public NativeCollectionAccessWrapper(AccessControlledValue<TCollection> collection, AccessType accessType) : base(accessType)
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
