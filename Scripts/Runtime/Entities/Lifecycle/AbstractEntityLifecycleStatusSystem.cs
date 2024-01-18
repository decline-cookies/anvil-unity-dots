using Anvil.CSharp.Collections;
using Anvil.Unity.DOTS.Jobs;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Provides information on the lifecycle of entities.
    /// </summary>
    /// <remarks>
    /// Best practice is to extend this system and place the newly created system at a point in time when structural
    /// changes have been made. Ex. Right after a <see cref="EntityCommandBufferSystem"/>.
    /// Created or Imported entities will be treated as "Arrivals" to the World.
    /// Destroyed or Evicted entities will be treated as "Departures" from the World.
    /// Entities that are in a "CleanUp" state can be treated as "Departures" before they are actually destroyed by
    /// Unity. This is the default behaviour.
    /// </remarks>
    public abstract partial class AbstractEntityLifecycleStatusSystem : AbstractAnvilSystemBase
    {
        private readonly List<EntityLifecycleStatus> m_EntityLifecycleStatus;
        private readonly bool m_ShouldCleanupEntitiesCountAsDepartures;
        private WorldEntityState m_WorldEntityState;
        private NativeArray<JobHandle> m_Dependencies;

        /// <summary>
        /// Creates a new <see cref="AbstractEntityLifecycleStatusSystem"/>
        /// </summary>
        /// <param name="shouldCleanupEntitiesCountAsDepartures">
        /// If true, Entities that are in a CleanUp state with <see cref="ISystemStateComponentData"/>
        /// will be considered as Departures. They will not show up later when they are actually destroyed by Unity.
        /// </param>
        protected AbstractEntityLifecycleStatusSystem(bool shouldCleanupEntitiesCountAsDepartures = true)
        {
            m_ShouldCleanupEntitiesCountAsDepartures = shouldCleanupEntitiesCountAsDepartures;
            m_EntityLifecycleStatus = new List<EntityLifecycleStatus>();
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_WorldEntityState = WorldEntityState.GetOrCreate(World);
            m_WorldEntityState.AddLifecycleStatusSystem(this);

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

        //TODO: Could consider getting rid of the Factory method and let you create your own Lifecycle Status.
        //For safety we'd check the parent of a given Lifecycle status when added to the system to make sure it's not
        //double added to multiple lifecycle systems. See: https://github.com/decline-cookies/anvil-unity-dots/pull/194/files#r1165757533
        /// <summary>
        /// Creates a new <see cref="IEntityLifecycleStatus"/> object to monitor Arrivals/Departures from a certain
        /// archetype.
        /// </summary>
        /// <param name="componentTypes">The components to filter on.</param>
        /// <returns>The <see cref="IEntityLifecycleStatus"/> object</returns>
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
                out NativeArray<Entity>.ReadOnly destroyedEntities,
                m_ShouldCleanupEntitiesCountAsDepartures);

            //If nothing was created or destroyed we can early exit
            //Even though we're not using the createdEntities that are populated here,
            //we want to still run the UpdateAsync method if any entities were created so that the queries
            //will pick up the new entities and populate them.
            if (createdEntities.Length == 0
                && destroyedEntities.Length == 0)
            {
                m_WorldEntityState.ReleaseCreatedAndDestroyedEntities(dependsOn);
                Dependency = dependsOn;
                return;
            }

            // Enable for helpful debugging.
            // Logger.Debug($"{World} | {UnityEngine.Time.frameCount} - Created: {createdEntities.Length} - Destroyed: {destroyedEntities.Length}");
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

        //*************************************************************************************************************
        // WORLD ENTITY STATE
        //*************************************************************************************************************

        private class WorldEntityState
        {
            private static readonly Dictionary<World, WorldEntityState> s_WorldEntityStates = new Dictionary<World, WorldEntityState>();

            [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
            private static void Init()
            {
                s_WorldEntityStates.Clear();
            }

            public static WorldEntityState GetOrCreate(World world)
            {
                if (!s_WorldEntityStates.TryGetValue(world, out WorldEntityState worldEntityState))
                {
                    worldEntityState = new WorldEntityState(world);
                    s_WorldEntityStates.Add(world, worldEntityState);
                }
                return worldEntityState;
            }

            private readonly World m_World;
            private readonly HashSet<AbstractEntityLifecycleStatusSystem> m_LifecycleStatusSystems;
            private readonly AccessController m_AccessController;

            private NativeList<int> m_WorldEntitiesState;
            private NativeList<Entity> m_WorldCreatedEntities;
            private NativeList<Entity> m_WorldDestroyedEntities;
            private NativeParallelHashSet<Entity> m_WorldCleanupEntities;
            private NativeReference<int> m_CleanupFrame;

            private EntityQuery m_CleanupEntityQuery;

            private WorldEntityState(World world)
            {
                m_World = world;
                m_LifecycleStatusSystems = new HashSet<AbstractEntityLifecycleStatusSystem>();
                m_AccessController = new AccessController();

                m_WorldEntitiesState = new NativeList<int>(ChunkUtil.MaxElementsPerChunk<Entity>(), Allocator.Persistent);
                m_WorldCreatedEntities = new NativeList<Entity>(ChunkUtil.MaxElementsPerChunk<Entity>(), Allocator.Persistent);
                m_WorldDestroyedEntities = new NativeList<Entity>(ChunkUtil.MaxElementsPerChunk<Entity>(), Allocator.Persistent);
                m_WorldCleanupEntities = new NativeParallelHashSet<Entity>(ChunkUtil.MaxElementsPerChunk<Entity>(), Allocator.Persistent);
                m_CleanupFrame = new NativeReference<int>(-1, Allocator.Persistent);

                m_CleanupEntityQuery = m_World.EntityManager.CreateEntityQuery(EntityCleanupHelper.CLEAN_UP_ENTITY_COMPONENT_TYPE);
            }

            private void Dispose()
            {
                m_AccessController.Dispose();
                m_WorldEntitiesState.Dispose();
                m_WorldCreatedEntities.Dispose();
                m_WorldDestroyedEntities.Dispose();
                m_WorldCleanupEntities.Dispose();
                m_CleanupFrame.Dispose();
                m_CleanupEntityQuery.Dispose();
            }

            public void AddLifecycleStatusSystem(AbstractEntityLifecycleStatusSystem lifecycleStatusSystem)
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
                out NativeArray<Entity>.ReadOnly destroyedEntities,
                bool shouldIncludeCleanupEntities)
            {
                dependsOn = JobHandle.CombineDependencies(
                    dependsOn,
                    m_AccessController.AcquireAsync(AccessType.ExclusiveWrite));
                //We have to ensure that any jobs that were in flight that were using the m_WorldCreatedEntities or
                //m_WorldDestroyedEntities lists are finished because Unity will modify them from under us.
                dependsOn.Complete();

                m_World.EntityManager.GetCreatedAndDestroyedEntities(m_WorldEntitiesState, m_WorldCreatedEntities, m_WorldDestroyedEntities);

                if (shouldIncludeCleanupEntities)
                {
                    NativeList<Entity> cleanupEntities = m_CleanupEntityQuery.ToEntityListAsync(Allocator.TempJob, out JobHandle cleanupDependsOn);

                    IncludeCleanupEntitiesJob includeCleanupEntitiesJob = new IncludeCleanupEntitiesJob(
                        m_WorldCleanupEntities,
                        m_WorldDestroyedEntities,
                        cleanupEntities,
                        m_CleanupFrame,
                        UnityEngine.Time.frameCount);
                    cleanupDependsOn = includeCleanupEntitiesJob.Schedule(cleanupDependsOn);
                    cleanupEntities.Dispose(cleanupDependsOn);
                    cleanupDependsOn.Complete();
                }

                createdEntities = m_WorldCreatedEntities.AsArray().AsReadOnly();
                destroyedEntities = m_WorldDestroyedEntities.AsArray().AsReadOnly();

                return dependsOn;
            }

            public void ReleaseCreatedAndDestroyedEntities(JobHandle dependsOn)
            {
                m_AccessController.ReleaseAsync(dependsOn);
            }
        }

        //*************************************************************************************************************
        // WORLD ENTITY STATE - JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct IncludeCleanupEntitiesJob : IJob
        {
            private NativeParallelHashSet<Entity> m_WorldCleanupEntities;
            private NativeList<Entity> m_WorldDestroyedEntities;
            private NativeReference<int> m_CleanupFrame;
            [ReadOnly] private readonly NativeList<Entity> m_PendingCleanupEntities;
            private readonly int m_CurrentFrame;

            public IncludeCleanupEntitiesJob(
                NativeParallelHashSet<Entity> worldCleanupEntities,
                NativeList<Entity> worldDestroyedEntities,
                NativeList<Entity> pendingCleanupEntities,
                NativeReference<int> cleanupFrame,
                int currentFrame)
            {
                m_WorldCleanupEntities = worldCleanupEntities;
                m_WorldDestroyedEntities = worldDestroyedEntities;
                m_PendingCleanupEntities = pendingCleanupEntities;
                m_CleanupFrame = cleanupFrame;
                m_CurrentFrame = currentFrame;
            }

            public void Execute()
            {
                //First we need to ensure that none of the destroyed entities were entities we already counted
                //as destroyed because they were clean up entities the previous time we called this
                for (int i = m_WorldDestroyedEntities.Length - 1; i >= 0; --i)
                {
                    if (m_WorldCleanupEntities.Contains(m_WorldDestroyedEntities[i]))
                    {
                        m_WorldDestroyedEntities.RemoveAtSwapBack(i);
                    }
                }

                //If we're on a new frame, we want to clear the cleanup entities.
                //We could have multiple Lifecycle systems and we want the cleanup entities to be valid for the
                //entirety of the frame
                if (m_CurrentFrame != m_CleanupFrame.Value)
                {
                    m_CleanupFrame.Value = m_CurrentFrame;
                    //Now that we've culled any previously handled entities, we can clear the lookup that we were using
                    m_WorldCleanupEntities.Clear();
                }

                //If we don't have any pending clean up entities, we're done.
                if (m_PendingCleanupEntities.Length == 0)
                {
                    return;
                }

                //Ensure we have enough space
                m_WorldDestroyedEntities.SetCapacity(m_WorldDestroyedEntities.Length + m_PendingCleanupEntities.Length);
                //We'll add all the pending clean up entities to the destroyed list
                m_WorldDestroyedEntities.AddRangeNoResize(m_PendingCleanupEntities);

                //Now we'll go through all the entities that are going to be cleaned up later in the frame.
                m_WorldCleanupEntities.UnionWith(m_PendingCleanupEntities);
            }
        }
    }
}