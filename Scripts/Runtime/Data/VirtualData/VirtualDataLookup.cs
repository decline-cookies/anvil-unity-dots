using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// A lookup collection of <see cref="VirtualData{TKey,TInstance}"/> by <see cref="Type"/>
    /// </summary>
    internal class VirtualDataLookup : AbstractAnvilBase
    {
        private readonly Dictionary<Type, AbstractVirtualData> m_DataLookup;
        private readonly List<AbstractVirtualData> m_Data;
        private readonly VirtualDataBulkSchedulerForConsolidate m_VirtualDataBulkScheduler;

        public VirtualDataLookup()
        {
            m_DataLookup = new Dictionary<Type, AbstractVirtualData>();
            m_Data = new List<AbstractVirtualData>();
            m_VirtualDataBulkScheduler = new VirtualDataBulkSchedulerForConsolidate(m_Data);
        }

        protected override void DisposeSelf()
        {
            m_Data.DisposeAllAndClear();
            m_DataLookup.Clear();

            base.DisposeSelf();
        }
        
        /// <summary>
        /// Adds <see cref="VirtualData{TKey,TInstance}"/> to the lookup
        /// </summary>
        /// <param name="data">The <see cref="VirtualData{TKey,TInstance}"/> to add</param>
        /// <typeparam name="TKey">The type of Key</typeparam>
        /// <typeparam name="TInstance">The type of Instance data</typeparam>
        public void AddData<TKey, TInstance>(VirtualData<TKey, TInstance> data)
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            Type type = typeof(VirtualData<TKey, TInstance>);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_DataLookup.ContainsKey(type))
            {
                throw new InvalidOperationException($"VirtualData of type {type} has already been added!");
            }
#endif
            m_DataLookup.Add(type, data);
            m_Data.Add(data);
        }
        
        /// <summary>
        /// Returns <see cref="VirtualData{TKey,TInstance}"/> from the lookup
        /// </summary>
        /// <typeparam name="TKey">The type of Key</typeparam>
        /// <typeparam name="TInstance">The type of Instance data</typeparam>
        /// <returns>The <see cref="VirtualData{TKey,TInstance}"/> instance</returns>
        public VirtualData<TKey, TInstance> GetData<TKey, TInstance>()
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            Type type = typeof(VirtualData<TKey, TInstance>);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!m_DataLookup.ContainsKey(type))
            {
                throw new InvalidOperationException($"VirtualData of type {type} has not been added! Please ensure {nameof(AddData)} has been called.");
            }
#endif
            
            return (VirtualData<TKey, TInstance>)m_DataLookup[type];
        }
        
        /// <summary>
        /// Consolidates all <see cref="VirtualData{TKey,TInstance}"/> in the lookup in parallel.
        /// </summary>
        /// <param name="dependsOn">The <see cref="JobHandle"/> consolidation work depends on.</param>
        /// <returns>
        /// A <see cref="JobHandle"/> that represents when all <see cref="VirtualData{TKey,TInstance}"/>
        /// consolidation is complete.
        /// </returns>
        public JobHandle ConsolidateForFrame(JobHandle dependsOn)
        {
            return m_VirtualDataBulkScheduler.BulkSchedule(dependsOn);
        }
        
        //*************************************************************************************************************
        // BULK SCHEDULERS
        //*************************************************************************************************************
        
        /// <summary>
        /// <see cref="AbstractBulkScheduler{T}"/> to call ConsolidateForFrame on <see cref="VirtualData{TKey,TInstance}"/>
        /// </summary>
        private class VirtualDataBulkSchedulerForConsolidate : AbstractBulkScheduler<AbstractVirtualData>
        {
            public VirtualDataBulkSchedulerForConsolidate(List<AbstractVirtualData> list) : base(list)
            {
            }

            protected override JobHandle ScheduleItem(AbstractVirtualData item, JobHandle dependsOn)
            {
                return item.ConsolidateForFrame(dependsOn);
            }
        }
    }
}
