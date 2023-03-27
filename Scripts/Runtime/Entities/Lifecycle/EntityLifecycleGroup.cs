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

        public EntityLifecycleFilteredGroup CreationFilteredGroup { get; }
        public EntityLifecycleFilteredGroup DestructionFilteredGroup { get; }

        public EntityLifecycleGroup(AbstractAnvilSystemBase owningSystem, params ComponentType[] queryComponents)
        {
            EntityQuery entityQuery = owningSystem.GetEntityQuery(queryComponents);
            EntityQueryMask entityQueryMask = entityQuery.GetEntityQueryMask();
            
            UnsafeList<Entity> creationList = new UnsafeList<Entity>(
                ChunkUtil.MaxElementsPerChunk<Entity>(),
                Allocator.Persistent);
            m_CreatedEntities = new AccessControlledValue<UnsafeList<Entity>>(creationList);
            CreationFilteredGroup = new EntityLifecycleFilteredGroup(entityQueryMask, creationList);


            UnsafeList<Entity> destructionList = new UnsafeList<Entity>(
                ChunkUtil.MaxElementsPerChunk<Entity>(),
                Allocator.Persistent);
            m_DestroyedEntities = new AccessControlledValue<UnsafeList<Entity>>(destructionList);
            DestructionFilteredGroup = new EntityLifecycleFilteredGroup(entityQueryMask, destructionList);
        }

        protected override void DisposeSelf()
        {
            m_CreatedEntities.Dispose();
            m_DestroyedEntities.Dispose();
            base.DisposeSelf();
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
