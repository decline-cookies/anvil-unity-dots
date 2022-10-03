using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class EntityQueryNativeArray : AbstractEntityQueryNativeArray
    {
        public NativeArray<Entity> NativeArray { get; private set; }

        public sealed override int Length
        {
            get => NativeArray.Length;
        }

        public EntityQueryNativeArray(EntityQuery entityQuery) : base(entityQuery)
        {
        }

        public sealed override JobHandle Acquire()
        {
            NativeArray = EntityQuery.ToEntityArrayAsync(Allocator.TempJob, out JobHandle dependsOn);
            return dependsOn;
        }

        public sealed override void Release(JobHandle releaseAccessDependency)
        {
            NativeArray.Dispose(releaseAccessDependency);
        }
    }
}
