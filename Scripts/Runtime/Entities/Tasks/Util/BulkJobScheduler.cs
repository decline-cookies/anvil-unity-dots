using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class BulkJobScheduler<T> : AbstractAnvilBase
    {
        private readonly T[] m_Data;
        private NativeArray<JobHandle> m_Dependencies;

        public BulkJobScheduler(T[] data)
        {
            m_Data = data;
            m_Dependencies = new NativeArray<JobHandle>(data.Length, Allocator.Persistent);
        }

        protected override void DisposeSelf()
        {
            m_Dependencies.Dispose();
            base.DisposeSelf();
        }

        public JobHandle Schedule(JobHandle dependsOn,
                                  BulkScheduleDelegate<T> scheduleFunction)
        {
            return m_Data.BulkScheduleParallel(dependsOn, 
                                               ref m_Dependencies, 
                                               scheduleFunction);
        }
    }
}
