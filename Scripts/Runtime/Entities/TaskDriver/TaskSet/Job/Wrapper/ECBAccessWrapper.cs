using Anvil.Unity.DOTS.Jobs;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class ECBAccessWrapper : AbstractAccessWrapper
    {
        private readonly EntityCommandBufferSystem m_ECBSystem;

        public EntityCommandBuffer CommandBuffer
        {
            get => m_ECBSystem.CreateCommandBuffer();
        }


        // Note: The AccessType value doesn't really matter since there isn't access control on an ECB. You're getting
        // one instance per Acquire and the system is aggregating dependencies on release.
        public ECBAccessWrapper(EntityCommandBufferSystem ecbSystem, AbstractJobConfig.Usage usage) : base(AccessType.ExclusiveWrite, usage)
        {
            m_ECBSystem = ecbSystem;
        }

        public override JobHandle AcquireAsync()
        {
            return default;
        }

        public override void ReleaseAsync(JobHandle dependsOn)
        {
            m_ECBSystem.AddJobHandleForProducer(dependsOn);
        }
    }
}