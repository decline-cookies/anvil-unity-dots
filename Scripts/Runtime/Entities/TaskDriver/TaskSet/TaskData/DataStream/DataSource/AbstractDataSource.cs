using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using Anvil.CSharp.Data;
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
        private readonly IDProvider m_IDProvider;
        

        private bool m_IsHardened;
        
        private NativeArray<JobHandle> m_ConsolidationDependencies;
        private AbstractData[] m_ActiveData;

        public UnsafeTypedStream<T>.Writer PendingWriter { get; }
        public unsafe void* PendingWriterPointer { get; }
        
        protected PendingData<T> PendingData { get; }
        protected Dictionary<uint, AbstractData> ActiveDataLookupByID { get; }

        protected unsafe AbstractDataSource()
        {
            m_IDProvider = new IDProvider();
            PendingData = new PendingData<T>(m_IDProvider.GetNextID());
            PendingWriter = PendingData.PendingWriter;
            PendingWriterPointer = PendingData.PendingWriterPointer;
            ActiveDataLookupByID = new Dictionary<uint, AbstractData>();
        }

        protected override void DisposeSelf()
        {
            m_IDProvider.Dispose();
            PendingData.Dispose();
            ActiveDataLookupByID.DisposeAllValuesAndClear();

            if (m_ConsolidationDependencies.IsCreated)
            {
                m_ConsolidationDependencies.Dispose();
            }

            base.DisposeSelf();
        }

        public ActiveArrayData<T> CreateActiveArrayData()
        {
            ActiveArrayData<T> activeArrayData = new ActiveArrayData<T>(m_IDProvider.GetNextID());
            ActiveDataLookupByID.Add(activeArrayData.ID, activeArrayData);
            return activeArrayData;
        }

        public ActiveLookupData<T> CreateActiveLookupData()
        {
            ActiveLookupData<T> activeLookupData = new ActiveLookupData<T>(m_IDProvider.GetNextID());
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

            //Build up hardened collections
            m_ActiveData = ActiveDataLookupByID.Values.ToArray();
            //One job handle for each Active Data, one for the Pending, one for the incoming dependency
            m_ConsolidationDependencies = new NativeArray<JobHandle>(ActiveDataLookupByID.Count + 2, Allocator.Persistent);

            HardenSelf();
        }

        protected virtual void HardenSelf()
        {
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
            for (; dependencyIndex < m_ActiveData.Length; ++dependencyIndex)
            {
                m_ConsolidationDependencies[dependencyIndex] = m_ActiveData[dependencyIndex].AcquireAsync(AccessType.ExclusiveWrite);
            }

            m_ConsolidationDependencies[dependencyIndex] = PendingData.AcquireAsync(AccessType.ExclusiveWrite);
            dependencyIndex++;
            m_ConsolidationDependencies[dependencyIndex] = dependsOn;

            return JobHandle.CombineDependencies(m_ConsolidationDependencies);
        }

        private void ReleaseAsync(JobHandle dependsOn)
        {
            foreach (AbstractData activeData in m_ActiveData)
            {
                activeData.ReleaseAsync(dependsOn);
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
    }
}
