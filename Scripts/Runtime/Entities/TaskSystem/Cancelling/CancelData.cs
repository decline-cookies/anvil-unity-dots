using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CancelData : AbstractAnvilBase
    {
        public CancelRequestDataStream RequestDataStream { get; }
        public CancelCompleteDataStream CompleteDataStream { get; }
        private readonly AccessControlledValue<UnsafeParallelHashMap<EntityProxyInstanceID, bool>> m_ProgressLookup;
        
        public CancelData(AbstractTaskSystem taskSystem) : this(null, taskSystem)
        {
        }

        public CancelData(AbstractTaskDriver taskDriver) : this(taskDriver, taskDriver.TaskSystem)
        {
        }
        
        private CancelData(AbstractTaskDriver taskDriver, AbstractTaskSystem taskSystem)
        {
            m_ProgressLookup = new AccessControlledValue<UnsafeParallelHashMap<EntityProxyInstanceID, bool>>(new UnsafeParallelHashMap<EntityProxyInstanceID, bool>(ChunkUtil.MaxElementsPerChunk<EntityProxyInstanceID>(),
                                                                                                                                                                    Allocator.Persistent));
            CompleteDataStream = new CancelCompleteDataStream(taskDriver, taskSystem);
            RequestDataStream = new CancelRequestDataStream(this, taskDriver, taskSystem);
        }

        protected override void DisposeSelf()
        {
            //TODO: Should this get baked into AccessControlledValue's Dispose method?
            m_ProgressLookup.Acquire(AccessType.Disposal);
            m_ProgressLookup.Dispose();
            RequestDataStream.Dispose();
            CompleteDataStream.Dispose();
            base.DisposeSelf();
        }

        public JobHandle AcquireProgressLookup(AccessType accessType, out UnsafeParallelHashMap<EntityProxyInstanceID, bool> progressLookup)
        {
            return m_ProgressLookup.AcquireAsync(accessType, out progressLookup);
        }

        public void ReleaseProgressLookup(JobHandle releaseAccessDependency)
        {
            m_ProgressLookup.ReleaseAsync(releaseAccessDependency);
        }
    }
}
