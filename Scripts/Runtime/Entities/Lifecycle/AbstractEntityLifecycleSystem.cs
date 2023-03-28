using Anvil.CSharp.Collections;
using Anvil.Unity.DOTS.Jobs;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Provides the ability to hook into <see cref="EntityManager.GetCreatedAndDestroyedEntitiesAsync"/>
    /// to get a <see cref="IReadOnlyEntityLifecycleGroup"/> view into entities that were created/imported or
    /// destroyed/evicted this frame.
    /// </summary>
    /// <remarks>
    /// Update the <see cref="UpdateInGroupAttribute"/> to place the extended concrete
    /// class right after structural changes in the app. Multiple instances of this class are allowed as
    /// <see cref="EntityManager.GetCreatedAndDestroyedEntitiesAsync"/> will work off the same state list.
    /// Ex. Creating simulation entities and then running an EntityLifecycleSystem will return the newly created
    /// simulation entities. In the same frame, creating rendering entities and then running another
    /// EntityLifecycleSystem will return only the newly created rendering entities as we've already been notified
    /// of the simulation entities this frame.
    /// </remarks>
    //By default, we'll assume that all spawning/destroying happens in the Initialization Phase
    [UpdateInGroup(typeof(PostInitialization_Anvil), OrderLast = true)]
    public abstract partial class AbstractEntityLifecycleSystem : AbstractAnvilSystemBase
    {
        private static NativeList<int> s_State;
        private static int s_InstanceCount;
        
        //Ensure our static state is correct
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            if (s_State.IsCreated)
            {
                s_State.Dispose();
            }
        }
        
        
        private readonly List<EntityLifecycleGroup> m_EntityLifecycleGroups;
        
        private NativeList<Entity> m_CreatedEntities;
        private NativeList<Entity> m_DestroyedEntities;

        private NativeArray<EntityLifecycleFilteredGroup> m_CreationFilteredGroups;
        private NativeArray<EntityLifecycleFilteredGroup> m_DestructionFilteredGroups;
        private NativeArray<JobHandle> m_Dependencies;

        private bool m_IsInitialized;

        protected AbstractEntityLifecycleSystem()
        {
            m_EntityLifecycleGroups = new List<EntityLifecycleGroup>();
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            s_InstanceCount++;
            if (!s_State.IsCreated)
            {
                s_State = new NativeList<int>(ChunkUtil.MaxElementsPerChunk<Entity>(), Allocator.Persistent);
            }
            m_CreatedEntities = new NativeList<Entity>(ChunkUtil.MaxElementsPerChunk<Entity>(), Allocator.Persistent);
            m_DestroyedEntities = new NativeList<Entity>(ChunkUtil.MaxElementsPerChunk<Entity>(), Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            s_InstanceCount--;
            if (s_InstanceCount <= 0)
            {
                s_State.Dispose();
            }
            m_CreatedEntities.Dispose();
            m_DestroyedEntities.Dispose();

            if (m_CreationFilteredGroups.IsCreated)
            {
                m_CreationFilteredGroups.Dispose();
            }
            if (m_DestructionFilteredGroups.IsCreated)
            {
                m_DestructionFilteredGroups.Dispose();
            }
            if (m_Dependencies.IsCreated)
            {
                m_Dependencies.Dispose();
            }

            m_EntityLifecycleGroups.DisposeAllAndTryClear();

            base.OnDestroy();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            if (m_IsInitialized)
            {
                return;
            }
            m_IsInitialized = true;
            Harden();
        }

        /// <summary>
        /// Creates a new <see cref="IReadOnlyEntityLifecycleGroup"/> based on the passed in <see cref="ComponentType"/>
        /// </summary>
        /// <remarks>
        /// Note that the limits on <see cref="EntityQueryMask"/> are relevant since each group creates its own
        /// EntityQueryMask under the hood. Each entity is compared via the mask for however many groups there are
        /// </remarks>
        /// <param name="queryComponentTypes">
        /// The types of components to construct an <see cref="EntityQueryMask"/>
        /// </param>
        /// <returns>
        /// An instance of <see cref="IReadOnlyEntityLifecycleGroup"/> to use in future jobs/processing.
        /// </returns>
        public IReadOnlyEntityLifecycleGroup CreateEntityLifecycleGroup(params ComponentType[] queryComponentTypes)
        {
            EntityLifecycleGroup entityLifecycleGroup = new EntityLifecycleGroup(this, queryComponentTypes);
            m_EntityLifecycleGroups.Add(entityLifecycleGroup);
            return entityLifecycleGroup;
        }

        private void Harden()
        {
            int groupCount = m_EntityLifecycleGroups.Count;
            m_CreationFilteredGroups = new NativeArray<EntityLifecycleFilteredGroup>(groupCount, Allocator.Persistent);
            m_DestructionFilteredGroups = new NativeArray<EntityLifecycleFilteredGroup>(groupCount, Allocator.Persistent);
            m_Dependencies = new NativeArray<JobHandle>(groupCount + 1, Allocator.Persistent);

            for (int i = 0; i < groupCount; ++i)
            {
                EntityLifecycleGroup entityLifecycleGroup = m_EntityLifecycleGroups[i];
                entityLifecycleGroup.Harden(
                    out EntityLifecycleFilteredGroup creationFilteredGroup,
                    out EntityLifecycleFilteredGroup destructionFilteredGroup);
                m_CreationFilteredGroups[i] = creationFilteredGroup;
                m_DestructionFilteredGroups[i] = destructionFilteredGroup;
            }
        }

        protected override void OnUpdate()
        {
            Dependency = UpdateAsync(Dependency);
        }


        private JobHandle UpdateAsync(JobHandle dependsOn)
        {
            //First we need to get the list of created and destroyed entities
            EntityManager.GetCreatedAndDestroyedEntities(
                s_State,
                m_CreatedEntities,
                m_DestroyedEntities);

            //Then we can run the filter passes on created and destroyed in parallel
            dependsOn = JobHandle.CombineDependencies(
                UpdateCreationAsync(dependsOn),
                UpdateDestructionAsync(dependsOn));

            return dependsOn;
        }

        private JobHandle UpdateCreationAsync(JobHandle dependsOn)
        {
            //Get access to all the filtered collections
            for (int i = 0; i < m_EntityLifecycleGroups.Count; ++i)
            {
                m_Dependencies[i] = m_EntityLifecycleGroups[i].AcquireCreationForUpdate();
            }
            m_Dependencies[^1] = dependsOn;

            //Run the creation filter job
            FilterJob creationFilterJob = new FilterJob(
                m_CreatedEntities,
                m_CreationFilteredGroups);
            dependsOn = creationFilterJob.Schedule(dependsOn);

            //Release access so downstream readers can react
            for (int i = 0; i < m_EntityLifecycleGroups.Count; ++i)
            {
                m_EntityLifecycleGroups[i].ReleaseCreationAsync(dependsOn);
            }
            return dependsOn;
        }

        private JobHandle UpdateDestructionAsync(JobHandle dependsOn)
        {
            //Get access to all the filtered collections
            for (int i = 0; i < m_EntityLifecycleGroups.Count; ++i)
            {
                m_Dependencies[i] = m_EntityLifecycleGroups[i].AcquireDestructionForUpdate();
            }
            m_Dependencies[^1] = dependsOn;

            //Run the destruction job
            FilterJob destructionFilterJob = new FilterJob(
                m_DestroyedEntities,
                m_DestructionFilteredGroups);
            dependsOn = destructionFilterJob.Schedule(dependsOn);

            //Release access so downstream readers can react
            for (int i = 0; i < m_EntityLifecycleGroups.Count; ++i)
            {
                m_EntityLifecycleGroups[i].ReleaseDestructionAsync(dependsOn);
            }
            return dependsOn;
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct FilterJob : IJob
        {
            [ReadOnly] private readonly NativeList<Entity> m_CandidateEntities;
            private NativeArray<EntityLifecycleFilteredGroup> m_FilteredGroups;

            public FilterJob(
                NativeList<Entity> candidateEntities,
                NativeArray<EntityLifecycleFilteredGroup> filteredGroups)
            {
                m_CandidateEntities = candidateEntities;
                m_FilteredGroups = filteredGroups;
            }

            public void Execute()
            {
                //Clear the groups from whatever was written the last frame so we have a blank slate
                for (int i = 0; i < m_FilteredGroups.Length; ++i)
                {
                    m_FilteredGroups[i].FilteredEntities.Clear();
                }

                //For each candidate, see if it matches any of our filters
                for (int i = 0; i < m_CandidateEntities.Length; ++i)
                {
                    FilterEntity(m_CandidateEntities[i]);
                }
            }

            private void FilterEntity(Entity entity)
            {
                for (int i = 0; i < m_FilteredGroups.Length; ++i)
                {
                    EntityLifecycleFilteredGroup filteredGroup = m_FilteredGroups[i];
                    if (!filteredGroup.Mask.Matches(entity))
                    {
                        continue;
                    }
                    filteredGroup.FilteredEntities.Add(entity);
                }
            }
        }
    }
}
