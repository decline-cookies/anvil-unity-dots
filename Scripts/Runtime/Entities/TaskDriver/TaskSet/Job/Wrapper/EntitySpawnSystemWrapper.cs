using Anvil.Unity.DOTS.Jobs;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class EntitySpawnSystemWrapper : AbstractAccessWrapper
    {
        private readonly EntitySpawnSystem m_EntitySpawnSystem;
        private EntitySpawner m_EntitySpawner;

        public EntitySpawner EntitySpawner
        {
            get => m_EntitySpawner;
        }

        public EntitySpawnSystemWrapper(
            EntitySpawnSystem entitySpawnSystem,
            AccessType accessType,
            AbstractJobConfig.Usage usage) : base(accessType, usage)
        {
            m_EntitySpawnSystem = entitySpawnSystem;
        }
        
        public override JobHandle AcquireAsync()
        {
            return m_EntitySpawnSystem.AcquireAsync(out m_EntitySpawner);
        }

        public override void ReleaseAsync(JobHandle dependsOn)
        {
            m_EntitySpawnSystem.ReleaseAsync(dependsOn);
        }
    }
}
