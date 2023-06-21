using Anvil.CSharp.Core;
using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal abstract class AbstractDataSource<T> : AbstractAnvilBase,
                                                    IDataSource
        where T : unmanaged, IEquatable<T>
    {
        private bool m_IsHardened;

        private uint m_Consolidation_LastPendingDataVersion;
        private bool m_Consolidation_IsFollowUpRequired;
        private NativeArray<JobHandle> m_ConsolidationDependencies;
        private readonly List<DataAccessWrapper> m_ConsolidationData;

        public UnsafeTypedStream<T>.Writer PendingWriter { get; }
        public unsafe void* PendingWriterPointer { get; }

        public PendingData<T> PendingData { get; }
        protected HashSet<AbstractData> DataTargets { get; }

        protected TaskDriverManagementSystem TaskDriverManagementSystem { get; }

        public DataTargetID PendingWorldUniqueID
        {
            get => PendingData.WorldUniqueID;
        }

        protected unsafe AbstractDataSource(TaskDriverManagementSystem taskDriverManagementSystem)
        {
            TaskDriverManagementSystem = taskDriverManagementSystem;

            PendingData = taskDriverManagementSystem.CreatePendingData<T>(GetType().AssemblyQualifiedName);
            PendingWriter = PendingData.PendingWriter;
            PendingWriterPointer = PendingData.PendingWriterPointer;
            m_ConsolidationData = new List<DataAccessWrapper>();

            DataTargets = new HashSet<AbstractData>
            {
                PendingData
            };
        }

        protected override void DisposeSelf()
        {
            //DataTargets are Disposed by TaskDriverManagementSystem

            if (m_ConsolidationDependencies.IsCreated)
            {
                m_ConsolidationDependencies.Dispose();
            }

            base.DisposeSelf();
        }

        public override string ToString()
        {
            return $"{GetType().GetReadableName()}";
        }

        public ActiveArrayData<T> CreateActiveArrayData(ITaskSetOwner taskSetOwner, CancelRequestBehaviour cancelRequestBehaviour, string uniqueContextIdentifier)
        {
            Debug_EnsureNotHardened();
            //TODO: #136 - Kinda gross, we shouldn't know about Cancelling here.

            //If we need to have an explicit unwinding to cancel, we need to create a second hidden piece of data to serve as the trigger
            ActiveArrayData<T> activeCancelArrayData = null;
            if (cancelRequestBehaviour is CancelRequestBehaviour.Unwind)
            {
                activeCancelArrayData = TaskDriverManagementSystem.CreateActiveArrayData<T>(
                    taskSetOwner,
                    CancelRequestBehaviour.Ignore,
                    null,
                    $"{uniqueContextIdentifier}PENDING-CANCEL");
                DataTargets.Add(activeCancelArrayData);
            }

            ActiveArrayData<T> activeArrayData = TaskDriverManagementSystem.CreateActiveArrayData<T>(
                taskSetOwner,
                cancelRequestBehaviour,
                activeCancelArrayData,
                uniqueContextIdentifier);
            DataTargets.Add(activeArrayData);
            return activeArrayData;
        }

        public ActiveLookupData<T> CreateActiveLookupData(ITaskSetOwner taskSetOwner, string uniqueContextIdentifier)
        {
            Debug_EnsureNotHardened();
            ActiveLookupData<T> activeLookupData = TaskDriverManagementSystem.CreateActiveLookupData<T>(
                taskSetOwner,
                CancelRequestBehaviour.Ignore,
                uniqueContextIdentifier);
            DataTargets.Add(activeLookupData);
            return activeLookupData;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public JobHandle AcquirePendingAsync(AccessType accessType)
        {
            return PendingData.AcquireAsync(accessType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleasePendingAsync(JobHandle dependsOn)
        {
            PendingData.ReleaseAsync(dependsOn);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AcquirePending(AccessType accessType)
        {
            PendingData.Acquire(accessType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ReleasePending()
        {
            PendingData.Release();
        }

        public void Harden()
        {
            Debug_EnsureNotHardened();
            m_IsHardened = true;

            //Allow derived classes to add to the Consolidation Data if they need to.
            HardenSelf();

            //For each piece of active data, we want exclusive access to it when consolidating. We're going to be writing to it via one thread.
            //We'll also add our own Pending data. We want exclusive access because we'll be reading from it and then clearing the collection.
            foreach (AbstractData data in DataTargets)
            {
                AddConsolidationData(data, AccessType.ExclusiveWrite);
            }

            //One job handle for each Consolidation Data and one for the incoming dependency
            m_ConsolidationDependencies = new NativeArray<JobHandle>(m_ConsolidationData.Count + 1, Allocator.Persistent);
        }

        protected virtual void HardenSelf() { }

        protected void AddConsolidationData(AbstractData data, AccessType accessType)
        {
            m_ConsolidationData.Add(new DataAccessWrapper(data, accessType));
        }

        //*************************************************************************************************************
        // MIGRATION
        //*************************************************************************************************************

        public abstract JobHandle MigrateTo(
            JobHandle dependsOn,
            TaskDriverManagementSystem destinationTaskDriverManagementSystem,
            IDataSource destinationDataSource,
            ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray);

        //*************************************************************************************************************
        // EXECUTION
        //*************************************************************************************************************

        public JobHandle Consolidate(JobHandle dependsOn)
        {
            bool hasPendingDataChanged = PendingData.IsDataInvalidated(m_Consolidation_LastPendingDataVersion);
            if (!hasPendingDataChanged && !m_Consolidation_IsFollowUpRequired)
            {
                return dependsOn;
            }

            // One additional consolidation pass is required after the data has stopped changing so that the active
            // lookup is cleared. Without this followup mechanism data in the data source will contain already processed
            // data when the PendingData version doesn't change.
            // This is important for jobs that read from data sources in addition to the one they were scheduled on.
            m_Consolidation_IsFollowUpRequired = hasPendingDataChanged;

            dependsOn = AcquireAsync(dependsOn);
            dependsOn = ConsolidateSelf(dependsOn);
            ReleaseAsync(dependsOn);

            m_Consolidation_LastPendingDataVersion = PendingData.Version;

            return dependsOn;
        }

        protected abstract JobHandle ConsolidateSelf(JobHandle dependsOn);

        private JobHandle AcquireAsync(JobHandle dependsOn)
        {
            int dependencyIndex = 0;
            for (; dependencyIndex < m_ConsolidationData.Count; ++dependencyIndex)
            {
                DataAccessWrapper dataAccessWrapper = m_ConsolidationData[dependencyIndex];
                m_ConsolidationDependencies[dependencyIndex] = dataAccessWrapper.Data.AcquireAsync(dataAccessWrapper.AccessType);
            }

            m_ConsolidationDependencies[dependencyIndex] = dependsOn;

            return JobHandle.CombineDependencies(m_ConsolidationDependencies);
        }

        private void ReleaseAsync(JobHandle dependsOn)
        {
            foreach (DataAccessWrapper dataAccessWrapper in m_ConsolidationData)
            {
                dataAccessWrapper.Data.ReleaseAsync(dependsOn);
            }
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void Debug_EnsureNotHardened()
        {
            if (m_IsHardened)
            {
                throw new InvalidOperationException($"Expected {this} to not be hardened but {nameof(Harden)} has already been called!");
            }
        }

        //*************************************************************************************************************
        // INNER CLASS
        //*************************************************************************************************************

        private class DataAccessWrapper
        {
            public readonly AbstractData Data;
            public readonly AccessType AccessType;

            public DataAccessWrapper(AbstractData data, AccessType accessType)
            {
                Data = data;
                AccessType = accessType;
            }
        }
    }
}