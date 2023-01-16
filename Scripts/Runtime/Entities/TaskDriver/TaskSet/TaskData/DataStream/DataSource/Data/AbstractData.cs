using Anvil.CSharp.Core;
using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Jobs;
using System.Runtime.CompilerServices;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractData : AbstractAnvilBase
    {
        private readonly AccessController m_AccessController;
        
        public uint ID { get; }
        public CancelBehaviour CancelBehaviour { get; }
        
        public ITaskSetOwner TaskSetOwner { get; }
        
        public AbstractData PendingCancelActiveData { get; }
        

        protected AbstractData(uint id, ITaskSetOwner taskSetOwner, CancelBehaviour cancelBehaviour, AbstractData pendingCancelActiveData)
        {
            m_AccessController = new AccessController();
            ID = id;
            TaskSetOwner = taskSetOwner;
            CancelBehaviour = cancelBehaviour;
            PendingCancelActiveData = pendingCancelActiveData;
        }

        protected sealed override void DisposeSelf()
        {
            m_AccessController.Acquire(AccessType.Disposal);
            DisposeData();
            PendingCancelActiveData?.Dispose();
            m_AccessController.Dispose();
            base.DisposeSelf();
        }

        public override string ToString()
        {
            return $"{GetType().GetReadableName()}";
        }

        protected abstract void DisposeData();
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JobHandle AcquireAsync(AccessType accessType)
        {
            return m_AccessController.AcquireAsync(accessType);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseAsync(JobHandle releaseAccessDependency)
        {
            m_AccessController.ReleaseAsync(releaseAccessDependency);
        }
    }
}
