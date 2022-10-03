using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class EntityQueryComponentNativeArray<T> : AbstractEntityQueryNativeArray
        where T : struct, IComponentData
    {
        public NativeArray<T> NativeArray { get; private set; }
        
        public sealed override int Length
        {
            get => NativeArray.Length;
        }
        public EntityQueryComponentNativeArray(EntityQuery entityQuery) : base(entityQuery)
        {
        }
        
        protected sealed override void DisposeSelf()
        {
            if (NativeArray.IsCreated)
            {
                NativeArray.Dispose();
            }
            base.DisposeSelf();
        }

        public sealed override JobHandle Acquire()
        {
            NativeArray = EntityQuery.ToComponentDataArrayAsync<T>(Allocator.TempJob, out JobHandle dependsOn);
            return dependsOn;
        }

        public sealed override void Release(JobHandle releaseAccessDependency)
        {
            NativeArray.Dispose(releaseAccessDependency);
        }
    }
}
