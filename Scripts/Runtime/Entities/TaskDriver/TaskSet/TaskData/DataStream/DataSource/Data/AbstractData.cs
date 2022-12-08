using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System.Runtime.CompilerServices;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractData : AbstractAnvilBase
    {
        public uint ID { get; }
        private readonly AccessController m_AccessController;

        protected AbstractData(uint id)
        {
            ID = id;
            m_AccessController = new AccessController();
        }

        protected sealed override void DisposeSelf()
        {
            m_AccessController.Acquire(AccessType.Disposal);
            DisposeData();
            m_AccessController.Dispose();
            base.DisposeSelf();
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
