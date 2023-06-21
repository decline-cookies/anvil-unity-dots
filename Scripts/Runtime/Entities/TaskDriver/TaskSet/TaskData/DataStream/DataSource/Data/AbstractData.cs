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

        public DataTargetID WorldUniqueID { get; }
        public CancelRequestBehaviour CancelRequestBehaviour { get; }

        public IDataOwner DataOwner { get; }

        public AbstractData ActiveCancelData { get; }

        /// <summary>
        /// The version of the data. This value is incremented each time write access is granted to the data allowing
        /// consumers to check whether the data may have changed since the last time the consumer read it.
        /// </summary>
        /// <remarks>
        /// Store this value at the time of read access and on next potential read use <see cref="IsDataInvalidated"/>
        /// to check whether the data has potentially changed. This is the safest option as it accounts for other potential
        /// conditions in the data.
        ///
        /// If comparing version numbers directly make sure to always use an equality check. The version number can
        /// theoretically wrap and overflow so it's possible for the current value to be less than the previous value.
        /// </remarks>
        public uint Version { get; private set; }

        private DataTargetID m_WorldUniqueID;

        protected AbstractData(IDataOwner dataOwner, CancelRequestBehaviour cancelRequestBehaviour, AbstractData activeCancelData, string uniqueContextIdentifier)
        {
            m_AccessController = new AccessController();
            DataOwner = dataOwner;
            CancelRequestBehaviour = cancelRequestBehaviour;
            ActiveCancelData = activeCancelData;
            WorldUniqueID = GenerateWorldUniqueID(DataOwner, GetType(), uniqueContextIdentifier);
            Version = 0;
        }

        protected sealed override void DisposeSelf()
        {
            m_AccessController.Acquire(AccessType.Disposal);
            DisposeData();
            ActiveCancelData?.Dispose();
            m_AccessController.Dispose();
            base.DisposeSelf();
        }

        public override string ToString()
        {
            return $"{GetType().GetReadableName()}";
        }

        protected abstract void DisposeData();

        /// <summary>
        /// Whether the underlying data has potentially been updated by something getting write access to it.
        /// </summary>
        /// <param name="lastVersion">
        /// A write version at the last read.
        /// </param>
        public virtual bool IsDataInvalidated(uint lastVersion)
        {
            // If the current data version has changed from what we last stored, then someone has written here
            return lastVersion != Version;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JobHandle AcquireAsync(AccessType accessType)
        {
            IncrementVersionIfWrite(accessType);
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
            IncrementVersionIfWrite(accessType);
            m_AccessController.Acquire(accessType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Release()
        {
            m_AccessController.Release();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JobHandle GetDependencyFor(AccessType accessType)
        {
            return m_AccessController.GetDependencyFor(accessType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void IncrementVersionIfWrite(AccessType accessType)
        {
            if (accessType is AccessType.ExclusiveWrite or AccessType.SharedWrite)
            {
                unchecked
                {
                    Version++;
                }
            }
        }
    }
}
