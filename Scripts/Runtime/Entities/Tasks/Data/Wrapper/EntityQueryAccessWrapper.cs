using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class EntityQueryAccessWrapper
    {
        internal class EntityQueryType<TType>
        {
            
        }
        
        public EntityQuery EntityQuery
        {
            get;
        }

        private NativeArray<Entity> m_NativeArray;
        
        public EntityQueryAccessWrapper(EntityQuery entityQuery)
        {
            EntityQuery = entityQuery;
        }

        public JobHandle Acquire()
        {
            m_NativeArray = EntityQuery.ToEntityArrayAsync(Allocator.TempJob, out JobHandle acquireHandle);
            return acquireHandle;
        }

        public void Release(JobHandle releaseAccessDependency)
        {
            m_NativeArray.Dispose(releaseAccessDependency);
        }
    }
}
