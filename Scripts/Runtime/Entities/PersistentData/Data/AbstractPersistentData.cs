using Anvil.CSharp.Core;
using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Runtime.CompilerServices;
using Unity.Jobs;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    internal abstract class AbstractPersistentData : AbstractAnvilBase,
                                                     IWorldUniqueID<DataTargetID>
    {
        private static readonly Type ABSTRACT_PERSISTENT_DATA_TYPE = typeof(AbstractPersistentData);
        
        public static DataTargetID GetWorldUniqueID(IDataOwner dataOwner, Type persistentDataType, string uniqueContextIdentifier)
        {
            Debug.Assert(dataOwner == null || dataOwner.WorldUniqueID.IsValid);
            Debug.Assert(ABSTRACT_PERSISTENT_DATA_TYPE.IsAssignableFrom(persistentDataType));
            string idPath = $"{(dataOwner != null ? dataOwner.WorldUniqueID : string.Empty)}/{persistentDataType.AssemblyQualifiedName}{uniqueContextIdentifier}";
            return new DataTargetID(idPath.GetBurstHashCode32());
        }
        
        private readonly AccessController m_AccessController;
        private DataTargetID m_WorldUniqueID;
        
        public DataTargetID WorldUniqueID { get; }

        protected AbstractPersistentData(IDataOwner dataOwner, string uniqueContextIdentifier)
        {
            WorldUniqueID = GetWorldUniqueID(dataOwner, GetType(), uniqueContextIdentifier);
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