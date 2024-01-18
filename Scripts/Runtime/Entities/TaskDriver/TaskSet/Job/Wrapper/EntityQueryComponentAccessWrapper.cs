using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class EntityQueryComponentAccessWrapper<T> : AbstractAccessWrapper
        where T : unmanaged, IComponentData
    {
        private readonly EntityQueryComponentNativeList<T> m_EntityQueryNativeList;

        public NativeList<T> NativeList
        {
            get => m_EntityQueryNativeList.Results;
        }

        public EntityQueryComponentAccessWrapper(EntityQueryComponentNativeList<T> entityQueryNativeList, AbstractJobConfig.Usage usage) : base(AccessType.SharedRead, usage)
        {
            m_EntityQueryNativeList = entityQueryNativeList;
        }

        public sealed override JobHandle AcquireAsync()
        {
            return m_EntityQueryNativeList.Acquire();
        }

        public sealed override void ReleaseAsync(JobHandle dependsOn)
        {
            m_EntityQueryNativeList.Release(dependsOn);
        }
    }
}