using Anvil.CSharp.Core;
using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Jobs;
using System.Runtime.CompilerServices;
using Unity.Jobs;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    //TODO: #136 - Too many responsibilities
    internal abstract class AbstractData : AbstractAnvilBase,
                                           IWorldUniqueID<DataTargetID>
    {
        private readonly AccessController m_AccessController;

        public DataTargetID WorldUniqueID { get; }
        public CancelRequestBehaviour CancelRequestBehaviour { get; }

        public IDataOwner DataOwner { get; }

        public AbstractData PendingCancelActiveData { get; }

        private DataTargetID m_WorldUniqueID;


        protected AbstractData(IDataOwner dataOwner, CancelRequestBehaviour cancelRequestBehaviour, AbstractData pendingCancelActiveData, string uniqueContextIdentifier)
        {
            m_AccessController = new AccessController();
            DataOwner = dataOwner;
            CancelRequestBehaviour = cancelRequestBehaviour;
            PendingCancelActiveData = pendingCancelActiveData;
            WorldUniqueID = GenerateWorldUniqueID(uniqueContextIdentifier);
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
        
        private DataTargetID GenerateWorldUniqueID(string uniqueContextIdentifier)
        {
            Debug.Assert(DataOwner == null || DataOwner.WorldUniqueID.IsValid);
            string idPath = $"{(DataOwner != null ? DataOwner.WorldUniqueID : string.Empty)}/{GetType().AssemblyQualifiedName}{uniqueContextIdentifier}";
            return new DataTargetID(idPath.GetBurstHashCode32());
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
