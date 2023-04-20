using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    // TODO: #86 - When upgrading to Entities 1.0 we can use an unmanaged shared component which will let us use burst
    // TODO:       for all spawners. Remove this type and replace NoBurstSpawnJob with SpawnJob in EntitySpawner.
    public class BurstEntitySpawner<TEntitySpawnDefinition> : EntitySpawner<TEntitySpawnDefinition>
        where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
    {
        internal BurstEntitySpawner(
            EntityManager entityManager,
            NativeParallelHashMap<long, EntityArchetype> entityArchetypes,
            IReadAccessControlledValue<NativeParallelHashMap<long, Entity>> prototypes)
            : base(
                entityManager,
                entityArchetypes,
                prototypes) { }

        private protected override JobHandle ScheduleSpawnJob(
            JobHandle dependsOn,
            UnsafeTypedStream<SpawnDefinitionWrapper<TEntitySpawnDefinition>> spawnDefinitions,
            EntitySpawnHelper entitySpawnHelper,
            ref EntityCommandBuffer ecb)
        {
            SpawnJob job = new SpawnJob(spawnDefinitions, ref ecb, entitySpawnHelper);
            return job.Schedule(dependsOn);
        }

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
    }
}