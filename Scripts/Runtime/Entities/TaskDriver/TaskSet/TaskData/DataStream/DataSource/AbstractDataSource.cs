using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
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

        private NativeArray<JobHandle> m_ConsolidationDependencies;
        private readonly List<DataAccessWrapper> m_ConsolidationData;

        public UnsafeTypedStream<T>.Writer PendingWriter { get; }
        public unsafe void* PendingWriterPointer { get; }

        public PendingData<T> PendingData { get; }
        protected Dictionary<uint, AbstractData> ActiveDataLookupByID { get; }

        protected TaskDriverManagementSystem TaskDriverManagementSystem { get; }

        protected unsafe AbstractDataSource(TaskDriverManagementSystem taskDriverManagementSystem)
        {
            TaskDriverManagementSystem = taskDriverManagementSystem;
            PendingData = new PendingData<T>(TaskDriverManagementSystem.GetNextID());
            PendingWriter = PendingData.PendingWriter;
            PendingWriterPointer = PendingData.PendingWriterPointer;
            ActiveDataLookupByID = new Dictionary<uint, AbstractData>();
            m_ConsolidationData = new List<DataAccessWrapper>();
        }

        protected override void DisposeSelf()
        {
            PendingData.Dispose();
            ActiveDataLookupByID.DisposeAllValuesAndClear();

            if (m_ConsolidationDependencies.IsCreated)
            {
                m_ConsolidationDependencies.Dispose();
            }

            base.DisposeSelf();
        }

        public ActiveArrayData<T> CreateActiveArrayData(ITaskSetOwner taskSetOwner, CancelRequestBehaviour cancelRequestBehaviour)
        {
            Debug_EnsureNotHardened();
            //TODO: #136 - Kinda gross, we shouldn't know about Cancelling here.

            //If we need to have an explicit unwinding to cancel, we need to create a second hidden piece of data to serve as the trigger
            ActiveArrayData<T> pendingCancelArrayData = null;
            if (cancelRequestBehaviour is CancelRequestBehaviour.Unwind)
            {
                pendingCancelArrayData = new ActiveArrayData<T>(TaskDriverManagementSystem.GetNextID(), taskSetOwner, CancelRequestBehaviour.Ignore, null);
            }

            ActiveArrayData<T> activeArrayData = new ActiveArrayData<T>(TaskDriverManagementSystem.GetNextID(), taskSetOwner, cancelRequestBehaviour, pendingCancelArrayData);
            ActiveDataLookupByID.Add(activeArrayData.ID, activeArrayData);

            return activeArrayData;
        }

        public ActiveLookupData<T> CreateActiveLookupData(ITaskSetOwner taskSetOwner)
        {
            Debug_EnsureNotHardened();
            ActiveLookupData<T> activeLookupData = new ActiveLookupData<T>(TaskDriverManagementSystem.GetNextID(), taskSetOwner, CancelRequestBehaviour.Ignore);
            ActiveDataLookupByID.Add(activeLookupData.ID, activeLookupData);
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
            foreach (AbstractData data in ActiveDataLookupByID.Values)
            {
                AddConsolidationData(data, AccessType.ExclusiveWrite);
                //Add any Pending Cancel Active data as well.
                if (data.PendingCancelActiveData != null)
                {
                    AddConsolidationData(data.PendingCancelActiveData, AccessType.ExclusiveWrite);
                }
            }
            //We'll also add our own Pending data. We want exclusive access because we'll be reading from it and then clearing the collection.
            AddConsolidationData(PendingData, AccessType.ExclusiveWrite);

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

        public abstract void MigrateTo(IDataSource destinationDataSource, ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray);

        //*************************************************************************************************************
        // EXECUTION
        //*************************************************************************************************************

        public JobHandle Consolidate(JobHandle dependsOn)
        {
            dependsOn = AcquireAsync(dependsOn);
            dependsOn = ConsolidateSelf(dependsOn);
            ReleaseAsync(dependsOn);
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

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
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
