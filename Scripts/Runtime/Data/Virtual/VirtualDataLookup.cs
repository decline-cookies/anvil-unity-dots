using Anvil.CSharp.Core;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Data
{
    public class VirtualDataLookup : AbstractAnvilBase
    {
        private readonly Dictionary<Type, IVirtualData> m_Data = new Dictionary<Type, IVirtualData>();

        protected override void DisposeSelf()
        {
            foreach (IVirtualData data in m_Data.Values)
            {
                data.Dispose();
            }

            m_Data.Clear();
            base.DisposeSelf();
        }

        public void AddData<TKey, TInstance>(VirtualData<TKey, TInstance> data)
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            m_Data.Add(typeof(VirtualData<TKey, TInstance>), data);
        }

        public VirtualData<TKey, TInstance> GetData<TKey, TInstance>()
            where TKey : struct, IEquatable<TKey>
            where TInstance : struct, ILookupData<TKey>
        {
            return (VirtualData<TKey, TInstance>)m_Data[typeof(VirtualData<TKey, TInstance>)];
        }

        public JobHandle ConsolidateForFrame(JobHandle dependsOn)
        {
            int len = m_Data.Count;
            if (len == 0)
            {
                return dependsOn;
            }
            
            NativeArray<JobHandle> consolidateDependencies = new NativeArray<JobHandle>(len, Allocator.Temp);
            int index = 0;
            foreach (IVirtualData data in m_Data.Values)
            {
                consolidateDependencies[index] = data.ConsolidateForFrame(dependsOn);
                index++;
            }
            
            return JobHandle.CombineDependencies(consolidateDependencies);
        }
    }
}
