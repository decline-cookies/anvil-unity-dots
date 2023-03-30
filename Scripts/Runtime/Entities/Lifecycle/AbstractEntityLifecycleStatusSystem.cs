using Anvil.CSharp.Collections;
using Anvil.Unity.DOTS.Jobs;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    [AlwaysUpdateSystem]
    public abstract partial class AbstractEntityLifecycleStatusSystem : AbstractAnvilSystemBase
    {
        private readonly List<EntityLifecycleStatus> m_EntityLifecycleStatus;
        private WorldEntityState m_WorldEntityState;
        private NativeArray<JobHandle> m_Dependencies;

        protected AbstractEntityLifecycleStatusSystem()
        {
            m_EntityLifecycleStatus = new List<EntityLifecycleStatus>();
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_WorldEntityState = WorldEntityState.GetOrCreate(World, this);

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

            m_WorldEntityState.RemoveLifecycleStatusSystem(this);

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
            JobHandle dependsOn = Dependency;
            dependsOn = m_WorldEntityState.AcquireCreatedAndDestroyedEntities(
                dependsOn,
                out NativeArray<Entity>.ReadOnly createdEntities,
                out NativeArray<Entity>.ReadOnly destroyedEntities);

            if (createdEntities.Length == 0
                && destroyedEntities.Length == 0)
            {
                m_WorldEntityState.ReleaseCreatedAndDestroyedEntities(dependsOn);
                Dependency = dependsOn;
                return;
            }

            Logger.Debug($"{World} | {UnityEngine.Time.frameCount} - Created: {createdEntities.Length} - Destroyed: {destroyedEntities.Length}");
            dependsOn = UpdateAsync(
                dependsOn,
                ref destroyedEntities);
            
            m_WorldEntityState.ReleaseCreatedAndDestroyedEntities(dependsOn);
            Dependency = dependsOn;
        }

        private JobHandle UpdateAsync(
            JobHandle dependsOn,
            ref NativeArray<Entity>.ReadOnly destroyedEntities)
        {
            for (int i = 0; i < m_EntityLifecycleStatus.Count; ++i)
            {
                m_Dependencies[i] = m_EntityLifecycleStatus[i]
                    .UpdateAsync(
                        dependsOn,
                        ref destroyedEntities);
            }
            return JobHandle.CombineDependencies(m_Dependencies);
        }

        private class WorldEntityState
        {
            private static readonly Dictionary<World, WorldEntityState> s_WorldEntityStates = new Dictionary<World, WorldEntityState>();

            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
            private static void Init()
            {
                s_WorldEntityStates.Clear();
            }

            public static WorldEntityState GetOrCreate(World world, AbstractEntityLifecycleStatusSystem lifecycleStatusSystem)
            {
                if (!s_WorldEntityStates.TryGetValue(world, out WorldEntityState worldEntityState))
                {
                    worldEntityState = new WorldEntityState(world);
                    s_WorldEntityStates.Add(world, worldEntityState);
                }
                worldEntityState.AddLifecycleStatusSystem(lifecycleStatusSystem);
                return worldEntityState;
            }

            private readonly World m_World;
            private readonly HashSet<AbstractEntityLifecycleStatusSystem> m_LifecycleStatusSystems;
            private readonly AccessController m_AccessController;

            private NativeList<int> m_WorldEntitiesState;
            private NativeList<Entity> m_WorldCreatedEntities;
            private NativeList<Entity> m_WorldDestroyedEntities;

            private WorldEntityState(World world)
            {
                m_World = world;
                m_LifecycleStatusSystems = new HashSet<AbstractEntityLifecycleStatusSystem>();
                m_AccessController = new AccessController();

                m_WorldEntitiesState = new NativeList<int>(ChunkUtil.MaxElementsPerChunk<Entity>(), Allocator.Persistent);
                m_WorldCreatedEntities = new NativeList<Entity>(ChunkUtil.MaxElementsPerChunk<Entity>(), Allocator.Persistent);
                m_WorldDestroyedEntities = new NativeList<Entity>(ChunkUtil.MaxElementsPerChunk<Entity>(), Allocator.Persistent);
            }

            private void Dispose()
            {
                m_AccessController.Dispose();
                m_WorldEntitiesState.Dispose();
                m_WorldCreatedEntities.Dispose();
                m_WorldDestroyedEntities.Dispose();
            }

            private void AddLifecycleStatusSystem(AbstractEntityLifecycleStatusSystem lifecycleStatusSystem)
            {
                m_LifecycleStatusSystems.Add(lifecycleStatusSystem);
            }

            public void RemoveLifecycleStatusSystem(AbstractEntityLifecycleStatusSystem lifecycleStatusSystem)
            {
                m_LifecycleStatusSystems.Remove(lifecycleStatusSystem);
                if (m_LifecycleStatusSystems.Count == 0)
                {
                    Dispose();
                }
            }

            public JobHandle AcquireCreatedAndDestroyedEntities(
                JobHandle dependsOn,
                out NativeArray<Entity>.ReadOnly createdEntities,
                out NativeArray<Entity>.ReadOnly destroyedEntities)
            {
                m_AccessController.Acquire(AccessType.ExclusiveWrite);
                
                m_World.EntityManager.GetCreatedAndDestroyedEntities(m_WorldEntitiesState, m_WorldCreatedEntities, m_WorldDestroyedEntities);
                createdEntities = m_WorldCreatedEntities.AsArray().AsReadOnly();
                destroyedEntities = m_WorldDestroyedEntities.AsArray().AsReadOnly();

                return dependsOn;
            }

            public void ReleaseCreatedAndDestroyedEntities(JobHandle dependsOn)
            {
                m_AccessController.ReleaseAsync(dependsOn);
            }
        }
    }
}
