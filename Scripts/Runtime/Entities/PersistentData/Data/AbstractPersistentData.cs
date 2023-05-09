using Anvil.CSharp.Core;
using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Jobs;
using System.Runtime.CompilerServices;
using Unity.Jobs;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    internal abstract class AbstractPersistentData : AbstractAnvilBase,
                                                     IWorldUniqueID<DataTargetID>
    {
        private readonly AccessController m_AccessController;
        private readonly string m_UniqueContextIdentifier;
        private readonly IDataOwner m_DataOwner;
        private DataTargetID m_WorldUniqueID;
        
        public DataTargetID WorldUniqueID
        {
            get
            {
                if (!m_WorldUniqueID.IsValid)
                {
                    m_WorldUniqueID = GenerateWorldUniqueID();
                }
                return m_WorldUniqueID;
            }
        }

        protected AbstractPersistentData(IDataOwner dataOwner, string uniqueContextIdentifier)
        {
            m_DataOwner = dataOwner;
            m_UniqueContextIdentifier = uniqueContextIdentifier;
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

        public override string ToString()
        {
            return $"{GetType().GetReadableName()}";
        }

        private DataTargetID GenerateWorldUniqueID()
        {
            Debug.Assert(m_DataOwner == null || m_DataOwner.WorldUniqueID.IsValid);
            string idPath = $"{(m_DataOwner != null ? m_DataOwner.WorldUniqueID : string.Empty)}/{GetType().AssemblyQualifiedName}{m_UniqueContextIdentifier}";
            return new DataTargetID(idPath.GetBurstHashCode32());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JobHandle AcquireAsync(AccessType accessType)
        {
            return m_AccessController.AcquireAsync(accessType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleaseAsync(JobHandle dependsOn)
        {
            m_AccessController.ReleaseAsync(dependsOn);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public AccessController.AccessHandle AcquireWithHandle(AccessType accessType)
        {
            return m_AccessController.AcquireWithHandle(accessType);
        }
    }
}