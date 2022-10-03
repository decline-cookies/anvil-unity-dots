using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class EntityQueryAccessWrapper : IAccessWrapper
    {
        internal class EntityQueryType<TType>
        {
            
        }

        private readonly EntityQueryNativeArray m_EntityQueryNativeArray;

        public NativeArray<Entity> NativeArray
        {
            get => m_EntityQueryNativeArray.NativeArray;
        }


        public EntityQueryAccessWrapper(EntityQueryNativeArray entityQueryNativeArray)
        {
            m_EntityQueryNativeArray = entityQueryNativeArray;
        }
        
        public void Dispose()
        {
            //Not needed
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
