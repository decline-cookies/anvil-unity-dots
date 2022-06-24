using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Jobs
{
    public abstract class AbstractBulkScheduler<T>
    {
        private readonly List<T> m_List;

        protected AbstractBulkScheduler(List<T> list)
        {
            m_List = list;
        }

        protected abstract JobHandle ScheduleItem(T item, JobHandle dependsOn);
        public JobHandle BulkSchedule(JobHandle dependsOn)
        {
            int len = m_List.Count;
            if (len == 0)
            {
                return dependsOn;
            }

            NativeArray<JobHandle> dependencies = new NativeArray<JobHandle>(len, Allocator.Temp);
            for (int i = 0; i < len; ++i)
            {
                dependencies[i] = ScheduleItem(m_List[i], dependsOn);
            }

            return JobHandle.CombineDependencies(dependencies);
        }
    }
}
