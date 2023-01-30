using Anvil.CSharp.Core;
using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Jobs;
using System.Runtime.CompilerServices;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    //TODO: #136 - Too many responsibilities
    internal abstract class AbstractData : AbstractAnvilBase
    {
        private readonly AccessController m_AccessController;
        
        public uint ID { get; }
        public CancelRequestBehaviour CancelRequestBehaviour { get; }
        
        public ITaskSetOwner TaskSetOwner { get; }
        
        public AbstractData PendingCancelActiveData { get; }
        

        protected AbstractData(uint id, ITaskSetOwner taskSetOwner, CancelRequestBehaviour cancelRequestBehaviour, AbstractData pendingCancelActiveData)
        {
            m_AccessController = new AccessController();
            ID = id;
            TaskSetOwner = taskSetOwner;
            CancelRequestBehaviour = cancelRequestBehaviour;
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
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Acquire(AccessType accessType)
        { 
            m_AccessController.Acquire(accessType);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release()
        {
            m_AccessController.Release();
        }
    }
}
