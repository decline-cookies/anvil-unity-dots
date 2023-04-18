using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using JetBrains.Annotations;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    [UsedImplicitly]
    public class EntitySpawner<TEntitySpawnDefinition> : AbstractAnvilBase,
                                                         ISharedWriteAccessControlledValue<EntitySpawnWriter<TEntitySpawnDefinition>>,
                                                         IEntitySpawner
        where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
    {
        private event Action<IEntitySpawner> OnPendingWorkAdded;

        event Action<IEntitySpawner> IEntitySpawner.OnPendingWorkAdded
        {
            add => this.OnPendingWorkAdded += value;
            remove => this.OnPendingWorkAdded -= value;
        }

        private readonly AccessControlledValue<UnsafeTypedStream<SpawnDefinitionWrapper<TEntitySpawnDefinition>>> m_DefinitionsToSpawn;
        private readonly AccessControlledValue<UnsafeTypedStream<Entity>> m_PrototypesToDestroy;
        private readonly int m_MainThreadIndex;
        private readonly bool m_MustDisableBurst;

        private readonly NativeParallelHashMap<long, EntityArchetype> m_EntityArchetypes;
        private readonly IReadAccessControlledValue<NativeParallelHashMap<long, Entity>> m_Prototypes;

        private EntityManager m_EntityManager;


        internal EntitySpawner(
            EntityManager entityManager,
            NativeParallelHashMap<long, EntityArchetype> entityArchetypes,
            IReadAccessControlledValue<NativeParallelHashMap<long, Entity>> prototypes,
            bool mustDisableBurst)
        {
            m_DefinitionsToSpawn = new AccessControlledValue<UnsafeTypedStream<SpawnDefinitionWrapper<TEntitySpawnDefinition>>>(new UnsafeTypedStream<SpawnDefinitionWrapper<TEntitySpawnDefinition>>(Allocator.Persistent));
            m_PrototypesToDestroy = new AccessControlledValue<UnsafeTypedStream<Entity>>(new UnsafeTypedStream<Entity>(Allocator.Persistent));
            m_MainThreadIndex = ParallelAccessUtil.CollectionIndexForMainThread();

            m_EntityManager = entityManager;
            m_EntityArchetypes = entityArchetypes;
            m_Prototypes = prototypes;

            //TODO: #86 - When upgrading to Entities 1.0 we can use an unmanaged shared component which will let us use the job in burst
            m_MustDisableBurst = mustDisableBurst;
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

        public void SpawnDeferred(TEntitySpawnDefinition spawnDefinition)
        {
            InternalSpawn(spawnDefinition, PrototypeSpawnBehaviour.None);
        }

        public void SpawnDeferred(NativeArray<TEntitySpawnDefinition> spawnDefinitions)
        {
            InternalSpawn(spawnDefinitions, PrototypeSpawnBehaviour.None);
        }

        public Entity SpawnImmediate(TEntitySpawnDefinition spawnDefinition)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            // We're using the EntityManager directly so that we have a valid Entity, but we use the ECB to set
            // the values so that we can conform to the IEntitySpawnDefinitionInterface and developers
            // don't have to implement twice.
            EntitySpawnHelper helper = AcquireEntitySpawnHelper();
            Entity entity = m_EntityManager.CreateEntity(helper.GetEntityArchetypeForDefinition<TEntitySpawnDefinition>());
            spawnDefinition.PopulateOnEntity(
                entity,
                ref ecb,
                helper);
            ecb.Playback(m_EntityManager);
            ecb.Dispose();
            ReleaseEntitySpawnHelper();
            return entity;
        }


        //*************************************************************************************************************
        // SPAWN API - PROTOTYPE
        //*************************************************************************************************************

        public void SpawnWithPrototypeDeferred(TEntitySpawnDefinition spawnDefinition, bool shouldDestroyPrototype)
        {
            InternalSpawn(spawnDefinition, shouldDestroyPrototype ? PrototypeSpawnBehaviour.Destroy : PrototypeSpawnBehaviour.Keep);
        }

        public void SpawnWithPrototypeDeferred(NativeArray<TEntitySpawnDefinition> spawnDefinitions, bool shouldDestroyPrototype)
        {
            InternalSpawn(spawnDefinitions, shouldDestroyPrototype ? PrototypeSpawnBehaviour.Destroy : PrototypeSpawnBehaviour.Keep);
        }

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


        //*************************************************************************************************************
        // SPAWN API - IN JOB
        //*************************************************************************************************************

        public AccessControlledValue<EntitySpawnWriter<TEntitySpawnDefinition>>.AccessHandle AcquireWithSharedWriteHandle()
        {
            DispatchPendingWorkAdded();

            var handle = m_DefinitionsToSpawn.AcquireWithHandle(AccessType.SharedWrite);
            EntitySpawnWriter<TEntitySpawnDefinition> writer = new EntitySpawnWriter<TEntitySpawnDefinition>(handle.Value.AsWriter());
            return AccessControlledValue<EntitySpawnWriter<TEntitySpawnDefinition>>.AccessHandle.CreateDerived(handle, writer);
        }

        public EntitySpawnWriter<TEntitySpawnDefinition> AcquireSharedWrite()
        {
            DispatchPendingWorkAdded();

            UnsafeTypedStream<SpawnDefinitionWrapper<TEntitySpawnDefinition>> definitionsToSpawn = m_DefinitionsToSpawn.Acquire(AccessType.SharedWrite);
            return new EntitySpawnWriter<TEntitySpawnDefinition>(definitionsToSpawn.AsWriter());
        }

        public JobHandle AcquireSharedWriteAsync(out EntitySpawnWriter<TEntitySpawnDefinition> value)
        {
            DispatchPendingWorkAdded();

            JobHandle dependsOn = m_DefinitionsToSpawn.AcquireAsync(AccessType.SharedWrite, out UnsafeTypedStream<SpawnDefinitionWrapper<TEntitySpawnDefinition>> definitionsToSpawn);
            value = new EntitySpawnWriter<TEntitySpawnDefinition>(definitionsToSpawn.AsWriter());
            return dependsOn;
        }

        public void Release()
        {
            m_DefinitionsToSpawn.Release();
        }

        public void ReleaseAsync(JobHandle releaseAccessDependency)
        {
            m_DefinitionsToSpawn.ReleaseAsync(releaseAccessDependency);
        }


        //*************************************************************************************************************
        // SPAWN API - IN JOB
        //*************************************************************************************************************

        public JobHandle Schedule(
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

        private JobHandle ScheduleSpawnJob(
            JobHandle dependsOn,
            UnsafeTypedStream<SpawnDefinitionWrapper<TEntitySpawnDefinition>> spawnDefinitions,
            EntitySpawnHelper entitySpawnHelper,
            ref EntityCommandBuffer ecb)
        {
            //TODO: #86 - Remove once we don't have to switch with BURST
            if (m_MustDisableBurst)
            {
                SpawnJobNoBurst job = new SpawnJobNoBurst(
                    spawnDefinitions,
                    ref ecb,
                    entitySpawnHelper);

                return job.Schedule(dependsOn);
            }
            else
            {
                SpawnJob job = new SpawnJob(
                    spawnDefinitions,
                    ref ecb,
                    entitySpawnHelper);

                return job.Schedule(dependsOn);
            }
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        //TODO: #86 - When upgrading to Entities 1.0 we can use an unmanaged shared component which will let us use the job in burst
        [BurstCompile]
        private struct SpawnJob : IJob
        {
            [ReadOnly] private UnsafeTypedStream<SpawnDefinitionWrapper<TEntitySpawnDefinition>> m_SpawnDefinitions;
            [ReadOnly] private readonly EntitySpawnHelper m_EntitySpawnHelper;

            private readonly long m_Hash;

            private EntityCommandBuffer m_ECB;

            public SpawnJob(
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