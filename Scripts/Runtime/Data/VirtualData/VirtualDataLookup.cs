using Anvil.CSharp.Core;
using System;
using System.Collections.Generic;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// A lookup collection of <see cref="VirtualData{TKey,TInstance}"/> by <see cref="Type"/>
    /// </summary>
    internal class VirtualDataLookup<TKey> : AbstractAnvilBase
        where TKey : unmanaged, IEquatable<TKey>
    {
        private readonly Dictionary<Type, AbstractVirtualData<TKey>> m_DataLookup;

        public VirtualDataLookup()
        {
            m_DataLookup = new Dictionary<Type, AbstractVirtualData<TKey>>();
        }

        protected override void DisposeSelf()
        {
            foreach (AbstractVirtualData<TKey> data in m_DataLookup.Values)
            {
                data.Dispose();
            }

            m_DataLookup.Clear();

            base.DisposeSelf();
        }

        /// <summary>
        /// Adds <see cref="VirtualData{TKey,TInstance}"/> to the lookup
        /// </summary>
        /// <param name="data">The <see cref="VirtualData{TKey,TInstance}"/> to add</param>
        /// <typeparam name="TKey">The type of Key</typeparam>
        /// <typeparam name="TInstance">The type of Instance data</typeparam>
        public void AddData<TInstance>(VirtualData<TKey, TInstance> data)
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
        }

        /// <summary>
        /// Returns <see cref="VirtualData{TKey,TInstance}"/> from the lookup
        /// </summary>
        /// <typeparam name="TKey">The type of Key</typeparam>
        /// <typeparam name="TInstance">The type of Instance data</typeparam>
        /// <returns>The <see cref="VirtualData{TKey,TInstance}"/> instance</returns>
        public VirtualData<TKey, TInstance> GetData<TInstance>()
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
        public JobHandle ConsolidateForFrame(JobHandle dependsOn, CancelVirtualData<TKey> cancelData)
        {
            return m_DataLookup.Values.BulkScheduleParallel(dependsOn, cancelData, AbstractVirtualData<TKey>.CONSOLIDATE_FOR_FRAME_SCHEDULE_DELEGATE);
        }
    }
}
