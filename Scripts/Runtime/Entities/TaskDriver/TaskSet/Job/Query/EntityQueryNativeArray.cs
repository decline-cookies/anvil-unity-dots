using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class EntityQueryNativeArray : AbstractEntityQueryNativeArray<Entity>
    {
        public sealed override int Length
        {
            get => Results.Length;
        }

        public EntityQueryNativeArray(EntityQuery entityQuery) : base(entityQuery) { }

        public sealed override JobHandle Acquire()
        {
            Results = EntityQuery.ToEntityArrayAsync(Allocator.TempJob, out JobHandle dependsOn);
            return dependsOn;
        }

        public sealed override void Release(JobHandle releaseAccessDependency)
        {
            Results.Dispose(releaseAccessDependency);
        }
    }
}
