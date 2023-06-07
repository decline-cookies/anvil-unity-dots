using Anvil.CSharp.Core;
using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Runtime.CompilerServices;
using Unity.Jobs;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    //TODO: #136 - Too many responsibilities
    internal abstract class AbstractData : AbstractAnvilBase,
                                           IWorldUniqueID<DataTargetID>
    {
        private static readonly Type ABSTRACT_DATA_TYPE = typeof(AbstractData);

        public static DataTargetID GenerateWorldUniqueID(IDataOwner dataOwner, Type abstractDataType, string uniqueContextIdentifier)
        {
            Debug.Assert(dataOwner == null || dataOwner.WorldUniqueID.IsValid);
            Debug.Assert(ABSTRACT_DATA_TYPE.IsAssignableFrom(abstractDataType));
            string idPath = $"{(dataOwner != null ? dataOwner.WorldUniqueID : string.Empty)}/{abstractDataType.AssemblyQualifiedName}{uniqueContextIdentifier ?? string.Empty}";
            return new DataTargetID(idPath.GetBurstHashCode32());
        }

        private readonly AccessController m_AccessController;
        private JobHandle m_LastSharedReadAccessJobHandle;

        public DataTargetID WorldUniqueID { get; }
        public CancelRequestBehaviour CancelRequestBehaviour { get; }

        public IDataOwner DataOwner { get; }

        public AbstractData PendingCancelActiveData { get; }

        private DataTargetID m_WorldUniqueID;

        /// <summary>
        /// Whether the underlying data has potentially been updated by something getting write access to it.
        /// </summary>
        public virtual bool IsDataInvalidated
        {
            //If the current Read dependency has changed from what we last stored, then someone has written here
            get => m_AccessController
                .GetDependencyFor(AccessType.SharedRead)
                .Equals_NoBox(m_LastSharedReadAccessJobHandle);
        }
        
        protected AbstractData(IDataOwner dataOwner, CancelRequestBehaviour cancelRequestBehaviour, AbstractData pendingCancelActiveData, string uniqueContextIdentifier)
        {
            m_AccessController = new AccessController();
            DataOwner = dataOwner;
            CancelRequestBehaviour = cancelRequestBehaviour;
            PendingCancelActiveData = pendingCancelActiveData;
            WorldUniqueID = GenerateWorldUniqueID(DataOwner, GetType(), uniqueContextIdentifier);
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
            JobHandle dependsOn = m_AccessController.AcquireAsync(accessType);
            //Store the dependency for the last time we read from this data
            if (accessType == AccessType.SharedRead)
            {
                m_LastSharedReadAccessJobHandle = dependsOn;
            }
            return dependsOn;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JobHandle GetDependency(AccessType accessType)
        {
            return m_AccessController.GetDependencyFor(accessType);
        }
    }
}