using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class EntityQueryComponentAccessWrapper<T> : AbstractAccessWrapper
        where T : struct, IComponentData
    {
        private readonly EntityQueryComponentNativeArray<T> m_EntityQueryNativeArray;

        public NativeArray<T> NativeArray
        {
            get => m_EntityQueryNativeArray.Results;
        }

        public EntityQueryComponentAccessWrapper(EntityQueryComponentNativeArray<T> entityQueryNativeArray, AbstractJobConfig.Usage usage) : base(AccessType.SharedRead, usage)
        {
            m_EntityQueryNativeArray = entityQueryNativeArray;
        }

        public sealed override JobHandle AcquireAsync()
        {
            return m_EntityQueryNativeArray.Acquire();
        }

        public sealed override void ReleaseAsync(JobHandle dependsOn)
        {
            m_EntityQueryNativeArray.Release(dependsOn);
        }
    }
}
