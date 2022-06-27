using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// Helper class for getting the dependencies for a collection of items.
    /// </summary>
    /// <typeparam name="T">The type of item to get dependencies for.</typeparam>
    public abstract class AbstractBulkScheduler<T>
    {
        private readonly List<T> m_List;

        protected AbstractBulkScheduler(List<T> list)
        {
            m_List = list;
        }

        protected abstract JobHandle ScheduleItem(T item, JobHandle dependsOn);
        
        /// <summary>
        /// Schedules each item in the collection with the passed in <see cref="JobHandle"/>
        /// and combines the results into one <see cref="JobHandle"/> dependency to return.
        /// </summary>
        /// <param name="dependsOn">The incoming dependency</param>
        /// <returns>
        /// The combined dependency of all the elements doing whatever work they need to schedule in the derived class.
        /// </returns>
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
