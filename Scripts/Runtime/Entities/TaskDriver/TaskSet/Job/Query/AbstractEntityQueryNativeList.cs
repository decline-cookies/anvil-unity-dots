using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// A NativeArray doesn't exist from an EntityQuery until you resolve it and you need to resolve it each
    /// frame that you use it. This (and it's derived classes) act as a handle to that Array and manage its lifecycle.
    /// Consumers can then get the length of the array to be able to do things like scheduling and everyone can share
    /// the same NativeArray instead of resolving the struct EntityQuery multiple times.
    /// </summary>
    internal abstract class AbstractEntityQueryNativeList<T>
        where T : unmanaged
    {
        public abstract int Length { get; }
        public NativeList<T> Results { get; protected set; }
        internal EntityQuery EntityQuery { get; }

        protected AbstractEntityQueryNativeList(EntityQuery entityQuery)
        {
            EntityQuery = entityQuery;
        }

        public abstract JobHandle Acquire();
        public abstract void Release(JobHandle releaseAccessDependency);
    }
}