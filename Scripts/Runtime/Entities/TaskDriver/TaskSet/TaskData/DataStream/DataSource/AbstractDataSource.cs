using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractDataSource<T> : AbstractAnvilBase,
                                                    IDataSource
        where T : unmanaged, IEquatable<T>
    {
        private readonly TaskDriverManagementSystem m_TaskDriverManagementSystem;
        private bool m_IsHardened;
        
        private NativeArray<JobHandle> m_ConsolidationDependencies;
        private readonly List<DataAccessWrapper> m_ConsolidationData;

        public UnsafeTypedStream<T>.Writer PendingWriter { get; }
        public unsafe void* PendingWriterPointer { get; }
        
        protected PendingData<T> PendingData { get; }
        protected Dictionary<uint, AbstractData> ActiveDataLookupByID { get; }

        protected unsafe AbstractDataSource(TaskDriverManagementSystem taskDriverManagementSystem)
        {
            m_TaskDriverManagementSystem = taskDriverManagementSystem;
            PendingData = new PendingData<T>(m_TaskDriverManagementSystem.GetNextID());
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

        public ActiveArrayData<T> CreateActiveArrayData(ITaskSetOwner taskSetOwner, CancelBehaviour cancelBehaviour)
        {
            ActiveArrayData<T> activeArrayData = new ActiveArrayData<T>(m_TaskDriverManagementSystem.GetNextID(), taskSetOwner, cancelBehaviour);
            ActiveDataLookupByID.Add(activeArrayData.ID, activeArrayData);
            return activeArrayData;
        }

        public ActiveLookupData<T> CreateActiveLookupData(ITaskSetOwner taskSetOwner, CancelBehaviour cancelBehaviour)
        {
            ActiveLookupData<T> activeLookupData = new ActiveLookupData<T>(m_TaskDriverManagementSystem.GetNextID(), taskSetOwner, cancelBehaviour);
            ActiveDataLookupByID.Add(activeLookupData.ID, activeLookupData);
            return activeLookupData;
        }

        public JobHandle AcquirePendingAsync(AccessType accessType)
        {
            return PendingData.AcquireAsync(accessType);
        }

        public void ReleasePendingAsync(JobHandle dependsOn)
        {
            PendingData.ReleaseAsync(dependsOn);
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
                m_ConsolidationData.Add(new DataAccessWrapper(data, AccessType.ExclusiveWrite));
            }
            
            //One job handle for each Consolidation Data, one for the Pending, one for the incoming dependency
            m_ConsolidationDependencies = new NativeArray<JobHandle>(m_ConsolidationData.Count + 2, Allocator.Persistent);
        }

        protected virtual void HardenSelf()
        {
        }

        protected void AddConsolidationData(AbstractData data, AccessType accessType)
        {
            m_ConsolidationData.Add(new DataAccessWrapper(data, accessType));
        }

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

            m_ConsolidationDependencies[dependencyIndex] = PendingData.AcquireAsync(AccessType.ExclusiveWrite);
            dependencyIndex++;
            m_ConsolidationDependencies[dependencyIndex] = dependsOn;

            return JobHandle.CombineDependencies(m_ConsolidationDependencies);
        }

        private void ReleaseAsync(JobHandle dependsOn)
        {
            foreach (DataAccessWrapper dataAccessWrapper in m_ConsolidationData)
            {
                dataAccessWrapper.Data.ReleaseAsync(dependsOn);
            }

            PendingData.ReleaseAsync(dependsOn);
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNotHardened()
        {
            if (m_IsHardened)
            {
                throw new InvalidOperationException($"Trying to Harden {this} but {nameof(Harden)} has already been called!");
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
