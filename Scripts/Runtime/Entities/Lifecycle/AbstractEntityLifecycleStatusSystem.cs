using Anvil.CSharp.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public abstract partial class AbstractEntityLifecycleStatusSystem : AbstractAnvilSystemBase
    {
        private readonly List<EntityLifecycleStatus> m_EntityLifecycleStatus;

        private NativeList<int> m_WorldEntitiesState;
        private NativeList<Entity> m_WorldCreatedEntities;
        private NativeList<Entity> m_WorldDestroyedEntities;

        private NativeArray<JobHandle> m_Dependencies;

        internal NativeArray<Entity>.ReadOnly CreatedEntities
        {
            get => m_WorldCreatedEntities.AsArray().AsReadOnly();
        }

        internal NativeArray<Entity>.ReadOnly DestroyedEntities
        {
            get => m_WorldDestroyedEntities.AsArray().AsReadOnly();
        }

        protected AbstractEntityLifecycleStatusSystem()
        {
            m_EntityLifecycleStatus = new List<EntityLifecycleStatus>();

            m_WorldEntitiesState = new NativeList<int>(ChunkUtil.MaxElementsPerChunk<Entity>(), Allocator.Persistent);
            m_WorldCreatedEntities = new NativeList<Entity>(ChunkUtil.MaxElementsPerChunk<Entity>(), Allocator.Persistent);
            m_WorldDestroyedEntities = new NativeList<Entity>(ChunkUtil.MaxElementsPerChunk<Entity>(), Allocator.Persistent);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            foreach (EntityLifecycleStatus entityLifecycleStatus in m_EntityLifecycleStatus)
            {
                entityLifecycleStatus.CreateQuery();
            }

            m_Dependencies = new NativeArray<JobHandle>(m_EntityLifecycleStatus.Count, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            m_EntityLifecycleStatus.DisposeAllAndTryClear();

            m_Dependencies.Dispose();

            m_WorldEntitiesState.Dispose();
            m_WorldCreatedEntities.Dispose();
            m_WorldDestroyedEntities.Dispose();
            base.OnDestroy();
        }

        protected IEntityLifecycleStatus CreateEntityLifecycleStatus(params ComponentType[] componentTypes)
        {
            EntityLifecycleStatus entityLifecycleStatus = new EntityLifecycleStatus(this, componentTypes);
            m_EntityLifecycleStatus.Add(entityLifecycleStatus);
            return entityLifecycleStatus;
        }

        protected override void OnUpdate()
        {
            EntityManager.GetCreatedAndDestroyedEntities(
                m_WorldEntitiesState,
                m_WorldCreatedEntities,
                m_WorldDestroyedEntities);

            if (m_WorldCreatedEntities.Length == 0
                && m_WorldDestroyedEntities.Length == 0)
            {
                return;
            }

            //TODO: If nothing has been created, nothing has been destroyed and nothing has been evicted... then we don't need to do anything
            Logger.Debug($"{World} | {UnityEngine.Time.frameCount} - Created: {m_WorldCreatedEntities.Length} - Destroyed: {m_WorldDestroyedEntities.Length} - ");
            Dependency = UpdateAsync(Dependency);
        }

        private JobHandle UpdateAsync(JobHandle dependsOn)
        {
            for (int i = 0; i < m_EntityLifecycleStatus.Count; ++i)
            {
                m_Dependencies[i] = m_EntityLifecycleStatus[i].UpdateAsync(dependsOn);
            }
            return JobHandle.CombineDependencies(m_Dependencies);
        }

        public void EvictEntitiesTo(World dstWorld, EntityQuery srcQuery)
        {
            dstWorld.EntityManager.MoveEntitiesFrom(EntityManager, srcQuery);
        }
    }
}
