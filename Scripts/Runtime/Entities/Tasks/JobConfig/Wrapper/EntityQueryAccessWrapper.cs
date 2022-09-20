using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class EntityQueryAccessWrapper : IAccessWrapper
    {
        internal class EntityQueryType<TType>
        {
            
        }
        
        public EntityQuery EntityQuery
        {
            get;
        }

        public NativeArray<Entity> NativeArray
        {
            get;
            private set;
        }
        
        public EntityQueryAccessWrapper(EntityQuery entityQuery)
        {
            EntityQuery = entityQuery;
        }

        public JobHandle Acquire()
        {
            NativeArray = EntityQuery.ToEntityArrayAsync(Allocator.TempJob, out JobHandle acquireHandle);
            return acquireHandle;
        }

        public void Release(JobHandle releaseAccessDependency)
        {
            NativeArray.Dispose(releaseAccessDependency);
        }
    }
}
