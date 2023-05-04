using Anvil.CSharp.Core;
using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Jobs;
using System.Runtime.CompilerServices;
using Unity.Jobs;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    //TODO: #136 - Too many responsibilities
    internal abstract class AbstractData : AbstractAnvilBase
    {
        private readonly AccessController m_AccessController;
        private readonly string m_UniqueContextIdentifier;

        public DataTargetID DataTargetID
        {
            get
            {
                Debug.Assert(m_DataTargetID.IsValid);
                return m_DataTargetID;
            }
        }
        public CancelRequestBehaviour CancelRequestBehaviour { get; }

        public ITaskSetOwner TaskSetOwner { get; }

        public AbstractData PendingCancelActiveData { get; }

        private DataTargetID m_DataTargetID;


        protected AbstractData(ITaskSetOwner taskSetOwner, CancelRequestBehaviour cancelRequestBehaviour, AbstractData pendingCancelActiveData, string uniqueContextIdentifier)
        {
            m_AccessController = new AccessController();
            m_UniqueContextIdentifier = uniqueContextIdentifier;
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
        
        public void GenerateWorldUniqueID()
        {
            Debug.Assert(TaskSetOwner == null || TaskSetOwner.WorldUniqueID.IsValid);
            string idPath = $"{(TaskSetOwner != null ? TaskSetOwner.WorldUniqueID : string.Empty)}/{GetType().AssemblyQualifiedName}{m_UniqueContextIdentifier}";
            m_DataTargetID = new DataTargetID(idPath.GetBurstHashCode32());
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
