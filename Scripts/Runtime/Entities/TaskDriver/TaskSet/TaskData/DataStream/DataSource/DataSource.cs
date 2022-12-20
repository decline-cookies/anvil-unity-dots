using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using Anvil.CSharp.Data;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class DataSource<TInstance> : AbstractAnvilBase,
                                           IDataSource
        where TInstance : unmanaged, IEntityProxyInstance
    {
        private readonly IDProvider m_IDProvider;
        private readonly PendingData<TInstance> m_PendingData;
        private readonly Dictionary<uint, ActiveArrayData<TInstance>> m_ActiveDataLookupByID;

        private bool m_IsHardened;
        private DataSourceConsolidator<TInstance> m_DataSourceConsolidator;
        private NativeArray<JobHandle> m_ConsolidationDependencies;
        private ActiveArrayData<TInstance>[] m_ActiveData;

        public UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.Writer PendingWriter { get; }

        public DataSource()
        {
            m_IDProvider = new IDProvider();
            m_PendingData = new PendingData<TInstance>(m_IDProvider.GetNextID());
            PendingWriter = m_PendingData.PendingWriter;
            m_ActiveDataLookupByID = new Dictionary<uint, ActiveArrayData<TInstance>>();
        }

        protected sealed override void DisposeSelf()
        {
            m_IDProvider.Dispose();
            m_PendingData.Dispose();
            m_ActiveDataLookupByID.DisposeAllValuesAndClear();
            m_DataSourceConsolidator.Dispose();

            if (m_ConsolidationDependencies.IsCreated)
            {
                m_ConsolidationDependencies.Dispose();
            }

            base.DisposeSelf();
        }

        public ActiveArrayData<TInstance> CreateActiveArrayData()
        {
            ActiveArrayData<TInstance> activeArrayData = new ActiveArrayData<TInstance>(m_IDProvider.GetNextID());
            m_ActiveDataLookupByID.Add(activeArrayData.ID, activeArrayData);
            return activeArrayData;
        }

        public JobHandle AcquirePendingAsync(AccessType accessType)
        {
            return m_PendingData.AcquireAsync(accessType);
        }

        public void ReleasePendingAsync(JobHandle dependsOn)
        {
            m_PendingData.ReleaseAsync(dependsOn);
        }

        public void Harden()
        {
            Debug_EnsureNotHardened();
            m_IsHardened = true;

            //Build up hardened collections
            m_ActiveData = m_ActiveDataLookupByID.Values.ToArray();
            //One job handle for each Active Data, one for the Pending, one for the incoming dependency
            m_ConsolidationDependencies = new NativeArray<JobHandle>(m_ActiveDataLookupByID.Count + 2, Allocator.Persistent);

            m_DataSourceConsolidator = new DataSourceConsolidator<TInstance>(m_PendingData, m_ActiveDataLookupByID);
        }
        
        //*************************************************************************************************************
        // EXECUTION
        //*************************************************************************************************************

        public JobHandle Consolidate(JobHandle dependsOn)
        {

            dependsOn = Acquire(dependsOn);

            ConsolidatePendingToActiveJob consolidatePendingToActiveJob = new ConsolidatePendingToActiveJob(m_DataSourceConsolidator);
            dependsOn = consolidatePendingToActiveJob.Schedule(dependsOn);

            Release(dependsOn);

            return dependsOn;
        }

        private JobHandle Acquire(JobHandle dependsOn)
        {
            int dependencyIndex = 0;
            for (; dependencyIndex < m_ActiveData.Length; ++dependencyIndex)
            {
                m_ConsolidationDependencies[dependencyIndex] = m_ActiveData[dependencyIndex].AcquireAsync(AccessType.ExclusiveWrite);
            }
            m_ConsolidationDependencies[dependencyIndex] = m_PendingData.AcquireAsync(AccessType.ExclusiveWrite);
            dependencyIndex++;
            m_ConsolidationDependencies[dependencyIndex] = dependsOn;
            
            return JobHandle.CombineDependencies(m_ConsolidationDependencies);
        }

        private void Release(JobHandle dependsOn)
        {
            foreach (ActiveArrayData<TInstance> activeData in m_ActiveData)
            {
                activeData.ReleaseAsync(dependsOn);
            }
            m_PendingData.ReleaseAsync(dependsOn);
        }


        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct ConsolidatePendingToActiveJob : IJob
        {
            private DataSourceConsolidator<TInstance> m_DataSourceConsolidator;

            public ConsolidatePendingToActiveJob(DataSourceConsolidator<TInstance> dataSourceConsolidator) : this()
            {
                m_DataSourceConsolidator = dataSourceConsolidator;
            }

            public unsafe void Execute()
            {
                m_DataSourceConsolidator.Consolidate();
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
                throw new InvalidOperationException($"Trying to Harden {this} but {nameof(Harden)} has already been called!");
            }
        }
    }
}
