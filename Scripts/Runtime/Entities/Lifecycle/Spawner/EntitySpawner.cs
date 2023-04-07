using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using JetBrains.Annotations;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    [UsedImplicitly]
    internal class EntitySpawner<TEntitySpawnDefinition> : AbstractEntitySpawner<TEntitySpawnDefinition>
        where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
    {
        public void SpawnDeferred(TEntitySpawnDefinition spawnDefinition)
        {
            InternalSpawn(spawnDefinition);
        }

        public void SpawnDeferred(NativeArray<TEntitySpawnDefinition> spawnDefinitions)
        {
            InternalSpawn(spawnDefinitions);
        }

        public Entity SpawnImmediate(TEntitySpawnDefinition spawnDefinition)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            // We're using the EntityManager directly so that we have a valid Entity, but we use the ECB to set
            // the values so that we can conform to the IEntitySpawnDefinitionInterface and developers
            // don't have to implement twice.
            EntitySpawnHelper helper = AcquireEntitySpawnHelper();
            Entity entity = EntityManager.CreateEntity(helper.GetEntityArchetypeForDefinition<TEntitySpawnDefinition>());
            spawnDefinition.PopulateOnEntity(entity, ref ecb, helper);
            ecb.Playback(EntityManager);
            ecb.Dispose();
            ReleaseEntitySpawnHelper();
            return entity;
        }

        public JobHandle AcquireEntitySpawnWriterAsync(out EntitySpawnWriter<TEntitySpawnDefinition> entitySpawnWriter)
        {
            JobHandle dependsOn = AcquireAsync(AccessType.SharedWrite, out UnsafeTypedStream<TEntitySpawnDefinition> definitionsToSpawn);
            entitySpawnWriter = new EntitySpawnWriter<TEntitySpawnDefinition>(definitionsToSpawn.AsWriter());
            return dependsOn;
        }

        public void ReleaseEntitySpawnWriterAsync(JobHandle dependsOn)
        {
            ReleaseAsync(dependsOn);
        }

        protected override JobHandle ScheduleSpawnJob(
            JobHandle dependsOn,
            UnsafeTypedStream<TEntitySpawnDefinition> spawnDefinitions,
            EntitySpawnHelper entitySpawnHelper,
            ref EntityCommandBuffer ecb)
        {
            //TODO: #86 - Remove once we don't have to switch with BURST
            if (MustDisableBurst)
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
            [ReadOnly] private UnsafeTypedStream<TEntitySpawnDefinition> m_SpawnDefinitions;
            [ReadOnly] private readonly EntitySpawnHelper m_EntitySpawnHelper;

            private readonly long m_Hash;

            private EntityCommandBuffer m_ECB;

            public SpawnJob(
                UnsafeTypedStream<TEntitySpawnDefinition> spawnDefinitions,
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
                foreach (TEntitySpawnDefinition spawnDefinition in m_SpawnDefinitions)
                {
                    Entity entity = m_ECB.CreateEntity(m_EntitySpawnHelper.GetEntityArchetypeForDefinition(m_Hash));
                    spawnDefinition.PopulateOnEntity(entity, ref m_ECB, m_EntitySpawnHelper);
                }

                m_SpawnDefinitions.Clear();
            }
        }

        private struct SpawnJobNoBurst : IJob
        {
            [ReadOnly] private UnsafeTypedStream<TEntitySpawnDefinition> m_SpawnDefinitions;
            [ReadOnly] private readonly EntitySpawnHelper m_EntitySpawnHelper;

            private readonly long m_Hash;

            private EntityCommandBuffer m_ECB;

            public SpawnJobNoBurst(
                UnsafeTypedStream<TEntitySpawnDefinition> spawnDefinitions,
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
                foreach (TEntitySpawnDefinition spawnDefinition in m_SpawnDefinitions)
                {
                    Entity entity = m_ECB.CreateEntity(m_EntitySpawnHelper.GetEntityArchetypeForDefinition(m_Hash));
                    spawnDefinition.PopulateOnEntity(entity, ref m_ECB, m_EntitySpawnHelper);
                }

                m_SpawnDefinitions.Clear();
            }
        }
    }
}
