using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class EntityLifecycleGroup : AbstractAnvilBase,
                                          IReadOnlyEntityLifecycleGroup
    {
        private readonly AccessControlledValue<UnsafeList<Entity>> m_CreatedEntities;
        private readonly AccessControlledValue<UnsafeList<Entity>> m_DestroyedEntities;
        private readonly AbstractAnvilSystemBase m_OwningSystem;
        private readonly ComponentType[] m_QueryComponents;

        public EntityLifecycleGroup(AbstractAnvilSystemBase owningSystem, params ComponentType[] queryComponents)
        {
            m_OwningSystem = owningSystem;
            m_QueryComponents = queryComponents;
            
            m_CreatedEntities = new AccessControlledValue<UnsafeList<Entity>>(new UnsafeList<Entity>(
                ChunkUtil.MaxElementsPerChunk<Entity>(),
                Allocator.Persistent));

            m_DestroyedEntities = new AccessControlledValue<UnsafeList<Entity>>(new UnsafeList<Entity>(
                ChunkUtil.MaxElementsPerChunk<Entity>(),
                Allocator.Persistent));
        }

        protected override void DisposeSelf()
        {
            m_CreatedEntities.Dispose();
            m_DestroyedEntities.Dispose();
            base.DisposeSelf();
        }

        public void Harden(
            out EntityLifecycleFilteredGroup creationFilteredGroup, 
            out EntityLifecycleFilteredGroup destructionFilteredGroup)
        {
            EntityQuery entityQuery = m_OwningSystem.GetEntityQuery(m_QueryComponents);
            EntityQueryMask entityQueryMask = entityQuery.GetEntityQueryMask();

            using var creationHandle = m_CreatedEntities.AcquireWithHandle(AccessType.SharedRead);
            creationFilteredGroup = new EntityLifecycleFilteredGroup(entityQueryMask, creationHandle.Value);

            using var destructionHandle = m_DestroyedEntities.AcquireWithHandle(AccessType.SharedRead);
            destructionFilteredGroup = new EntityLifecycleFilteredGroup(entityQueryMask, destructionHandle.Value);
        }
        
        //*************************************************************************************************************
        // INTERNAL ACQUIRE/RELEASE FOR UPDATE
        //*************************************************************************************************************

        public JobHandle AcquireCreationForUpdate()
        {
            //No need to actually use the returned value since it's been baked into the CreationFilteredGroup
            return m_CreatedEntities.AcquireAsync(AccessType.ExclusiveWrite, out UnsafeList<Entity> _);
        }

        public JobHandle AcquireDestructionForUpdate()
        {
            //No need to actually use the returned value since it's been baked into the DestructionFilteredGroup
            return m_DestroyedEntities.AcquireAsync(AccessType.ExclusiveWrite, out UnsafeList<Entity> _);
        }

        //*************************************************************************************************************
        // PUBLIC ACQUIRE/RELEASE - IReadOnlyEntityLifecycleGroup
        //*************************************************************************************************************
        
        /// <inheritdoc cref="IReadOnlyEntityLifecycleGroup.AcquireCreationAsync"/>
        public JobHandle AcquireCreationAsync(out UnsafeList<Entity> createdEntities)
        {
            return m_CreatedEntities.AcquireAsync(AccessType.SharedRead, out createdEntities);
        }

        /// <inheritdoc cref="IReadOnlyEntityLifecycleGroup.ReleaseCreationAsync"/>
        public void ReleaseCreationAsync(JobHandle dependsOn)
        {
            m_CreatedEntities.ReleaseAsync(dependsOn);
        }

        /// <inheritdoc cref="IReadOnlyEntityLifecycleGroup.AcquireCreation"/>
        public AccessControlledValue<UnsafeList<Entity>>.AccessHandle AcquireCreation()
        {
            return m_CreatedEntities.AcquireWithHandle(AccessType.SharedRead);
        }

        /// <inheritdoc cref="IReadOnlyEntityLifecycleGroup.AcquireDestructionAsync"/>
        public JobHandle AcquireDestructionAsync(out UnsafeList<Entity> destroyedEntities)
        {
            return m_DestroyedEntities.AcquireAsync(AccessType.SharedRead, out destroyedEntities);
        }

        /// <inheritdoc cref="IReadOnlyEntityLifecycleGroup.ReleaseDestructionAsync"/>
        public void ReleaseDestructionAsync(JobHandle dependsOn)
        {
            m_DestroyedEntities.ReleaseAsync(dependsOn);
        }

        /// <inheritdoc cref="IReadOnlyEntityLifecycleGroup.AcquireDestruction"/>
        public AccessControlledValue<UnsafeList<Entity>>.AccessHandle AcquireDestruction()
        {
            return m_DestroyedEntities.AcquireWithHandle(AccessType.SharedRead);
        }
        
    }
}
