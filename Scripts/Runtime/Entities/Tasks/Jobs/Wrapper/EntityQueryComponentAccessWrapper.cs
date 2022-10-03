using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class EntityQueryComponentAccessWrapper<T> : IAccessWrapper
        where T : struct, IComponentData
    {
        internal class EntityQueryComponentType
        {
        }

        private readonly EntityQueryComponentNativeArray<T> m_EntityQueryNativeArray;

        public NativeArray<T> NativeArray
        {
            get => m_EntityQueryNativeArray.NativeArray;
        }
        
        public EntityQueryComponentAccessWrapper(EntityQueryComponentNativeArray<T> entityQueryNativeArray)
        {
            m_EntityQueryNativeArray = entityQueryNativeArray;
        }

        public void Dispose()
        {
            m_EntityQueryNativeArray.Dispose();
        }

        public JobHandle Acquire()
        {
            return m_EntityQueryNativeArray.Acquire();
        }

        public void Release(JobHandle releaseAccessDependency)
        {
            m_EntityQueryNativeArray.Release(releaseAccessDependency);
        }
    }
}
