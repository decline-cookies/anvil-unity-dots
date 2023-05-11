using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// An access controlled instance responsible for spawning entities of a specific definition.
    /// Will produce <see cref="EntitySpawnWriter{TEntitySpawnDefinition}"/> instances for scheduling entity spawning
    /// from jobs.
    /// </summary>
    /// <typeparam name="TEntitySpawnDefinition">
    /// The <see cref="IEntitySpawnDefinition"/> type that this spawner spawns.
    /// </typeparam>
    public class EntitySpawner<TEntitySpawnDefinition> : AbstractAnvilBase,
                                                         ISharedWriteAccessControlledValue<EntitySpawnWriter<TEntitySpawnDefinition>>,
                                                         IEntitySpawner
        where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
    {
        private event Action<IEntitySpawner> OnPendingWorkAdded;

        event Action<IEntitySpawner> IEntitySpawner.OnPendingWorkAdded
        {
            add => OnPendingWorkAdded += value;
            remove => OnPendingWorkAdded -= value;
        }

        private readonly AccessControlledValue<UnsafeTypedStream<SpawnDefinitionWrapper<TEntitySpawnDefinition>>> m_DefinitionsToSpawn;
        private readonly AccessControlledValue<UnsafeTypedStream<Entity>> m_PrototypesToDestroy;
        private readonly int m_MainThreadIndex;

        private readonly NativeParallelHashMap<long, EntityArchetype> m_EntityArchetypes;
        private readonly IReadAccessControlledValue<NativeParallelHashMap<long, Entity>> m_Prototypes;

        private EntityManager m_EntityManager;


        internal EntitySpawner(
            EntityManager entityManager,
            NativeParallelHashMap<long, EntityArchetype> entityArchetypes,
            IReadAccessControlledValue<NativeParallelHashMap<long, Entity>> prototypes)
        {
            m_DefinitionsToSpawn = new AccessControlledValue<UnsafeTypedStream<SpawnDefinitionWrapper<TEntitySpawnDefinition>>>(new UnsafeTypedStream<SpawnDefinitionWrapper<TEntitySpawnDefinition>>(Allocator.Persistent));
            m_PrototypesToDestroy = new AccessControlledValue<UnsafeTypedStream<Entity>>(new UnsafeTypedStream<Entity>(Allocator.Persistent));
            m_MainThreadIndex = ParallelAccessUtil.CollectionIndexForMainThread();

            m_EntityManager = entityManager;
            m_EntityArchetypes = entityArchetypes;
            m_Prototypes = prototypes;
        }

        protected override void DisposeSelf()
        {
            OnPendingWorkAdded = null;

            m_DefinitionsToSpawn.Dispose();
            m_PrototypesToDestroy.Dispose();
            base.DisposeSelf();
        }

        private void DispatchPendingWorkAdded()
        {
            OnPendingWorkAdded?.Invoke(this);
        }

        private EntitySpawnHelper AcquireEntitySpawnHelper()
        {
            return new EntitySpawnHelper(m_EntityArchetypes, m_Prototypes.AcquireRead());
        }

        private void ReleaseEntitySpawnHelper()
        {
            m_Prototypes.Release();
        }

        private void InternalSpawn(TEntitySpawnDefinition element, PrototypeSpawnBehaviour prototypeSpawnBehaviour)
        {
            DispatchPendingWorkAdded();

            // ReSharper disable once SuggestVarOrType_SimpleTypes
            using var handle = m_DefinitionsToSpawn.AcquireWithHandle(AccessType.SharedWrite);
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            handle.Value.AsLaneWriter(m_MainThreadIndex).Write(new SpawnDefinitionWrapper<TEntitySpawnDefinition>(element, prototypeSpawnBehaviour));
        }

        private void InternalSpawn(NativeArray<TEntitySpawnDefinition> elements, PrototypeSpawnBehaviour prototypeSpawnBehaviour)
        {
            DispatchPendingWorkAdded();

            // ReSharper disable once SuggestVarOrType_SimpleTypes
            using var handle = m_DefinitionsToSpawn.AcquireWithHandle(AccessType.SharedWrite);
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            UnsafeTypedStream<SpawnDefinitionWrapper<TEntitySpawnDefinition>>.LaneWriter laneWriter = handle.Value.AsLaneWriter(m_MainThreadIndex);
            foreach (TEntitySpawnDefinition element in elements)
            {
                laneWriter.Write(new SpawnDefinitionWrapper<TEntitySpawnDefinition>(element, prototypeSpawnBehaviour));
            }
        }


        //*************************************************************************************************************
        // SPAWN API - REGULAR
        //*************************************************************************************************************

        /// <summary>
        /// Spawns an <see cref="Entity"/> with the given definition later on when the associated
        /// <see cref="EntityCommandBufferSystem"/> runs.
        /// </summary>
        /// <param name="spawnDefinition">
        /// The <see cref="IEntitySpawnDefinition"/> to populate the created <see cref="Entity"/> with.
        /// </param>
        public void SpawnDeferred(TEntitySpawnDefinition spawnDefinition)
        {
            InternalSpawn(spawnDefinition, PrototypeSpawnBehaviour.None);
        }

        /// <summary>
        /// Spawns multiple <see cref="Entity"/>s with the given definitions later on when the associated
        /// <see cref="EntityCommandBufferSystem"/> runs.
        /// </summary>
        /// <remarks>
        /// Will enable the system to be run for at least one frame. If no more spawn requests come in, the system
        /// will disable itself until more requests come in.
        /// </remarks>
        /// <param name="spawnDefinitions">
        /// The <see cref="IEntitySpawnDefinition"/>s to populate the created <see cref="Entity"/>s with.
        /// </param>
        public void SpawnDeferred(NativeArray<TEntitySpawnDefinition> spawnDefinitions)
        {
            InternalSpawn(spawnDefinitions, PrototypeSpawnBehaviour.None);
        }

        /// <summary>
        /// Spawns an <see cref="Entity"/> with the given definition immediately and returns it.
        /// </summary>
        /// <remarks>
        /// This will not enable the owning system.
        /// </remarks
        /// <param name="spawnDefinition">
        /// The <see cref="IEntitySpawnDefinition"/> to populate the created <see cref="Entity"/> with.
        /// </param>
        /// <returns>The created <see cref="Entity"/></returns>
        public Entity SpawnImmediate(TEntitySpawnDefinition spawnDefinition)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            // We're using the EntityManager directly so that we have a valid Entity, but we use the ECB to set
            // the values so that we can conform to the IEntitySpawnDefinitionInterface and developers
            // don't have to implement twice.
            EntitySpawnHelper helper = AcquireEntitySpawnHelper();
            Entity entity = m_EntityManager.CreateEntity(helper.GetEntityArchetypeForDefinition<TEntitySpawnDefinition>());
            spawnDefinition.PopulateOnEntity(entity, ref ecb, helper);
            ecb.Playback(m_EntityManager);
            ecb.Dispose();
            ReleaseEntitySpawnHelper();
            return entity;
        }

        /// <summary>
        /// Spawns multiple <see cref="Entity"/>s with the given definitions immediately.
        /// </summary>
        /// <param name="spawnDefinitions">
        /// The <see cref="IEntitySpawnDefinition"/>s to populate the created <see cref="Entity"/>s with.
        /// </param>
        public void SpawnImmediate(NativeArray<TEntitySpawnDefinition> spawnDefinitions)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            // We're using the EntityManager directly so that we have a valid Entity, but we use the ECB to set
            // the values so that we can conform to the IEntitySpawnDefinitionInterface and developers
            // don't have to implement twice.
            EntitySpawnHelper helper = AcquireEntitySpawnHelper();

            for (int i = 0; i < spawnDefinitions.Length; i++)
            {
                Entity entity = m_EntityManager.CreateEntity(helper.GetEntityArchetypeForDefinition<TEntitySpawnDefinition>());
                spawnDefinitions[i].PopulateOnEntity(entity, ref ecb, helper);
            }

            ecb.Playback(m_EntityManager);
            ecb.Dispose();
            ReleaseEntitySpawnHelper();
        }

        /// <summary>
        /// Spawns multiple <see cref="Entity"/>s with the given definitions immediately and returns the entity references
        /// </summary>
        /// <param name="spawnDefinitions">
        /// The <see cref="IEntitySpawnDefinition"/>s to populate the created <see cref="Entity"/>s with.
        /// </param>
        /// <param name="entitiesAllocator">The allocator to use for the returned entities collection</param>
        /// <returns>A collection of all the entities spawned.</returns>
        public NativeArray<Entity> SpawnImmediate(NativeArray<TEntitySpawnDefinition> spawnDefinitions, Allocator entitiesAllocator)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            // We're using the EntityManager directly so that we have a valid Entity, but we use the ECB to set
            // the values so that we can conform to the IEntitySpawnDefinitionInterface and developers
            // don't have to implement twice.
            EntitySpawnHelper helper = AcquireEntitySpawnHelper();

            NativeArray<Entity> entities = m_EntityManager.CreateEntity(
                helper.GetEntityArchetypeForDefinition<TEntitySpawnDefinition>(),
                spawnDefinitions.Length,
                entitiesAllocator);

            for (int i = 0; i < spawnDefinitions.Length; i++)
            {
                spawnDefinitions[i].PopulateOnEntity(entities[i], ref ecb, helper);
            }

            ecb.Playback(m_EntityManager);
            ecb.Dispose();
            ReleaseEntitySpawnHelper();

            return entities;
        }

        //*************************************************************************************************************
        // SPAWN API - PROTOTYPE
        //*************************************************************************************************************

        /// <summary>
        /// Spawns an <see cref="Entity"/> with the given definition by cloning the registered prototype
        /// <see cref="Entity"/> when the associated <see cref="EntityCommandBufferSystem"/> runs later on.
        /// </summary>
        /// <remarks>
        /// Will enable the owning system to be run for at least one frame. If no more spawn requests come in, the system
        /// will disable itself until more requests come in.
        /// </remarks>
        /// <param name="spawnDefinition">
        /// The <see cref="IEntitySpawnDefinition"/> to populate the created <see cref="Entity"/> with.
        /// </param>
        /// <param name="shouldDestroyPrototype">
        /// If true, will destroy the prototype <see cref="Entity"/> after creation.
        /// </param>
        public void SpawnWithPrototypeDeferred(TEntitySpawnDefinition spawnDefinition, bool shouldDestroyPrototype)
        {
            InternalSpawn(spawnDefinition, shouldDestroyPrototype ? PrototypeSpawnBehaviour.Destroy : PrototypeSpawnBehaviour.Keep);
        }

        /// <summary>
        /// Spawns multiple <see cref="Entity"/>s with the given definitions later on when the associated
        /// <see cref="EntityCommandBufferSystem"/> runs.
        /// </summary>
        /// <remarks>
        /// Will enable the owning system to be run for at least one frame. If no more spawn requests come in, the system
        /// will disable itself until more requests come in.
        /// </remarks>
        /// <param name="spawnDefinitions">
        /// The <see cref="IEntitySpawnDefinition"/>s to populate the created <see cref="Entity"/>s with.
        /// </param>
        /// <param name="shouldDestroyPrototype">
        /// If true, will destroy the prototype <see cref="Entity"/> after creation.
        /// </param>
        public void SpawnWithPrototypeDeferred(NativeArray<TEntitySpawnDefinition> spawnDefinitions, bool shouldDestroyPrototype)
        {
            InternalSpawn(spawnDefinitions, shouldDestroyPrototype ? PrototypeSpawnBehaviour.Destroy : PrototypeSpawnBehaviour.Keep);
        }

        /// <summary>
        /// Spawns an <see cref="Entity"/> with the given definition immediately by cloning the passed in prototype
        /// <see cref="Entity"/> and returns it immediately.
        /// </summary>
        /// <remarks>
        /// This will not enable the owning system.
        /// </remarks>
        /// <param name="spawnDefinition">
        /// The <see cref="IEntitySpawnDefinition"/> to populate the created <see cref="Entity"/> with.
        /// </param>
        /// <param name="shouldDestroyPrototype">
        /// If true, will destroy the prototype <see cref="Entity"/> after creation.
        /// </param>
        /// <returns>The created <see cref="Entity"/></returns>
        public Entity SpawnWithPrototypeImmediate(TEntitySpawnDefinition spawnDefinition, bool shouldDestroyPrototype)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            // We're using the EntityManager directly so that we have a valid Entity, but we use the ECB to set
            // the values so that we can conform to the IEntitySpawnDefinitionInterface and developers
            // don't have to implement twice.
            EntitySpawnHelper helper = AcquireEntitySpawnHelper();
            Entity prototype = helper.GetPrototypeEntityForDefinition<TEntitySpawnDefinition>();
            Entity entity = m_EntityManager.Instantiate(prototype);
            spawnDefinition.PopulateOnEntity(entity, ref ecb, helper);

            if (shouldDestroyPrototype)
            {
                ecb.DestroyEntity(prototype);
            }

            ecb.Playback(m_EntityManager);
            ecb.Dispose();

            ReleaseEntitySpawnHelper();
            return entity;
        }

        //TODO: Implement a SpawnImmediate that takes in a NativeArray or ICollection if needed.


        //*************************************************************************************************************
        // SPAWN API - IN JOB
        //*************************************************************************************************************

        /// <inheritdoc cref="ISharedWriteAccessControlledValue{T}.AcquireWithSharedWriteHandle"/>
        public AccessControlledValue<EntitySpawnWriter<TEntitySpawnDefinition>>.AccessHandle AcquireWithSharedWriteHandle()
        {
            DispatchPendingWorkAdded();

            var handle = m_DefinitionsToSpawn.AcquireWithHandle(AccessType.SharedWrite);
            EntitySpawnWriter<TEntitySpawnDefinition> writer = new EntitySpawnWriter<TEntitySpawnDefinition>(handle.Value.AsWriter());
            return AccessControlledValue<EntitySpawnWriter<TEntitySpawnDefinition>>.AccessHandle.CreateDerived(handle, writer);
        }

        /// <inheritdoc cref="ISharedWriteAccessControlledValue{T}.AcquireSharedWrite"/>
        public EntitySpawnWriter<TEntitySpawnDefinition> AcquireSharedWrite()
        {
            DispatchPendingWorkAdded();

            UnsafeTypedStream<SpawnDefinitionWrapper<TEntitySpawnDefinition>> definitionsToSpawn = m_DefinitionsToSpawn.Acquire(AccessType.SharedWrite);
            return new EntitySpawnWriter<TEntitySpawnDefinition>(definitionsToSpawn.AsWriter());
        }

        /// <inheritdoc cref="ISharedWriteAccessControlledValue{T}.AcquireSharedWriteAsync"/>
        public JobHandle AcquireSharedWriteAsync(out EntitySpawnWriter<TEntitySpawnDefinition> value)
        {
            DispatchPendingWorkAdded();

            JobHandle dependsOn = m_DefinitionsToSpawn.AcquireAsync(AccessType.SharedWrite, out UnsafeTypedStream<SpawnDefinitionWrapper<TEntitySpawnDefinition>> definitionsToSpawn);
            value = new EntitySpawnWriter<TEntitySpawnDefinition>(definitionsToSpawn.AsWriter());
            return dependsOn;
        }

        /// <inheritdoc cref="ISharedWriteAccessControlledValue{T}.Release"/>
        public void Release()
        {
            m_DefinitionsToSpawn.Release();
        }

        /// <inheritdoc cref="ISharedWriteAccessControlledValue{T}.ReleaseAsync"/>
        public void ReleaseAsync(JobHandle releaseAccessDependency)
        {
            m_DefinitionsToSpawn.ReleaseAsync(releaseAccessDependency);
        }


        //*************************************************************************************************************
        // SPAWN API - IN JOB
        //*************************************************************************************************************

        JobHandle IEntitySpawner.Schedule(
            JobHandle dependsOn,
            ref EntityCommandBuffer ecb)
        {
            dependsOn = JobHandle.CombineDependencies(
                dependsOn,
                m_DefinitionsToSpawn.AcquireAsync(AccessType.ExclusiveWrite, out UnsafeTypedStream<SpawnDefinitionWrapper<TEntitySpawnDefinition>> definitions),
                m_Prototypes.AcquireReadAsync(out var prototypes));

            EntitySpawnHelper entitySpawnHelper = new EntitySpawnHelper(m_EntityArchetypes, prototypes);

            dependsOn = ScheduleSpawnJob(dependsOn, definitions, entitySpawnHelper, ref ecb);

            m_DefinitionsToSpawn.ReleaseAsync(dependsOn);
            m_Prototypes.ReleaseAsync(dependsOn);
            return dependsOn;
        }

        private protected virtual JobHandle ScheduleSpawnJob(
            JobHandle dependsOn,
            UnsafeTypedStream<SpawnDefinitionWrapper<TEntitySpawnDefinition>> spawnDefinitions,
            EntitySpawnHelper entitySpawnHelper,
            ref EntityCommandBuffer ecb)
        {
            SpawnJobNoBurst job = new SpawnJobNoBurst(spawnDefinitions, ref ecb, entitySpawnHelper);
            return job.Schedule(dependsOn);
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************
        private struct SpawnJobNoBurst : IJob
        {
            [ReadOnly] private UnsafeTypedStream<SpawnDefinitionWrapper<TEntitySpawnDefinition>> m_SpawnDefinitions;
            [ReadOnly] private readonly EntitySpawnHelper m_EntitySpawnHelper;

            private readonly long m_Hash;

            private EntityCommandBuffer m_ECB;

            public SpawnJobNoBurst(
                UnsafeTypedStream<SpawnDefinitionWrapper<TEntitySpawnDefinition>> spawnDefinitions,
                ref EntityCommandBuffer ecb,
                in EntitySpawnHelper entitySpawnHelper)
            {
                m_SpawnDefinitions = spawnDefinitions;
                m_ECB = ecb;
                m_EntitySpawnHelper = entitySpawnHelper;

                m_Hash = BurstRuntime.GetHashCode64<TEntitySpawnDefinition>();
            }

            public void Execute()
            {
                NativeParallelHashSet<Entity> prototypesToDestroy = new NativeParallelHashSet<Entity>(32, Allocator.Temp);
                foreach (SpawnDefinitionWrapper<TEntitySpawnDefinition> spawnDefinition in m_SpawnDefinitions)
                {
                    switch (spawnDefinition.SpawnBehaviour)
                    {
                        case PrototypeSpawnBehaviour.None:
                            CreateEntity(spawnDefinition.EntitySpawnDefinition);
                            break;

                        case PrototypeSpawnBehaviour.Keep:
                            InstantiateEntity(spawnDefinition.EntitySpawnDefinition, m_EntitySpawnHelper.GetPrototypeEntityForDefinition(m_Hash));
                            break;

                        case PrototypeSpawnBehaviour.Destroy:
                            Entity prototype = m_EntitySpawnHelper.GetPrototypeEntityForDefinition(m_Hash);
                            prototypesToDestroy.Add(prototype);
                            InstantiateEntity(spawnDefinition.EntitySpawnDefinition, prototype);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                m_SpawnDefinitions.Clear();

                foreach (Entity entity in prototypesToDestroy)
                {
                    m_ECB.DestroyEntity(entity);
                }
            }

            private void CreateEntity(TEntitySpawnDefinition spawnDefinition)
            {
                Entity entity = m_ECB.CreateEntity(m_EntitySpawnHelper.GetEntityArchetypeForDefinition(m_Hash));
                spawnDefinition.PopulateOnEntity(entity, ref m_ECB, m_EntitySpawnHelper);
            }

            private void InstantiateEntity(TEntitySpawnDefinition spawnDefinition, Entity prototype)
            {
                Entity entity = m_ECB.Instantiate(prototype);
                spawnDefinition.PopulateOnEntity(entity, ref m_ECB, m_EntitySpawnHelper);
            }
        }
    }
}