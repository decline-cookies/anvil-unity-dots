using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractEntityQueryNativeArray
    {
        public abstract int Length { get; }

        protected EntityQuery EntityQuery { get; }

        protected AbstractEntityQueryNativeArray(EntityQuery entityQuery)
        {
            EntityQuery = entityQuery;
        }
        
        public abstract JobHandle Acquire();
        public abstract void Release(JobHandle releaseAccessDependency);
    }
}
