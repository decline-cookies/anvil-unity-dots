using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class EntityQueryComponentNativeList<T> : AbstractEntityQueryNativeList<T>
        where T : unmanaged, IComponentData
    {
        public sealed override int Length
        {
            get => Results.Length;
        }

        public EntityQueryComponentNativeList(EntityQuery entityQuery) : base(entityQuery) { }

        public sealed override JobHandle Acquire()
        {
            Results = EntityQuery.ToComponentDataListAsync<T>(Allocator.TempJob, out JobHandle dependsOn);
            return dependsOn;
        }

        public sealed override void Release(JobHandle releaseAccessDependency)
        {
            Results.Dispose(releaseAccessDependency);
        }
    }
}