using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Data
{
    public class VirtualDataLookup : AbstractAnvilBase
    {
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
            foreach (AbstractVirtualData data in m_Data)
            {
                data.Dispose();
            }
            
            m_DataLookup.Clear();
            m_Data.Clear();
            
            base.DisposeSelf();
        }

        public void AddData<TKey, TInstance>(VirtualData<TKey, TInstance> data)
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            m_DataLookup.Add(typeof(VirtualData<TKey, TInstance>), data);
            m_Data.Add(data);
        }

        public VirtualData<TKey, TInstance> GetData<TKey, TInstance>()
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            return (VirtualData<TKey, TInstance>)m_DataLookup[typeof(VirtualData<TKey, TInstance>)];
        }

        public JobHandle ConsolidateForFrame(JobHandle dependsOn)
        {
            return m_VirtualDataBulkScheduler.BulkSchedule(dependsOn);
        }
    }
}
