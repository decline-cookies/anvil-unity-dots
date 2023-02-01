using Anvil.Unity.DOTS.Data;
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

        public void Spawn(TEntitySpawnDefinition spawnDefinition)
        {
            InternalSpawn(spawnDefinition);
        }

        public void Spawn(NativeArray<TEntitySpawnDefinition> spawnDefinitions)
        {
            InternalSpawn(spawnDefinitions);
        }
        
        public Entity SpawnImmediate(TEntitySpawnDefinition spawnDefinition)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            // We're using the EntityManager directly so that we have a valid Entity, but we use the ECB to set
            // the values so that we can conform to the IEntitySpawnDefinitionInterface and developers
            // don't have to implement twice.
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            Entity entity = EntityManager.CreateEntity(EntityArchetype);
            spawnDefinition.PopulateOnEntity(entity, ref ecb);
            ecb.Playback(EntityManager);
            ecb.Dispose();
            return entity;
        }

        protected override JobHandle ScheduleSpawnJob(JobHandle dependsOn,
                                                      in UnsafeTypedStream<TEntitySpawnDefinition>.Reader reader,
                                                      ref EntityCommandBuffer ecb)
        {
            SpawnJob job = new SpawnJob(reader,
                                        EntityArchetype,
                                        ref ecb);

            return job.Schedule(dependsOn);
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        //TODO: #86 - When upgrading to Entities 1.0 we can use an unmanaged shared component which will let us use the job in burst
        [BurstCompile]
        private struct SpawnJob : IJob
        {
            [ReadOnly] private readonly UnsafeTypedStream<TEntitySpawnDefinition>.Reader m_SpawnDefinitionReader;
            [ReadOnly] private readonly EntityArchetype m_Archetype;
            
            private EntityCommandBuffer m_ECB;

            public SpawnJob(UnsafeTypedStream<TEntitySpawnDefinition>.Reader spawnDefinitionReader,
                            EntityArchetype archetype,
                            ref EntityCommandBuffer ecb)
            {
                m_SpawnDefinitionReader = spawnDefinitionReader;
                m_Archetype = archetype;
                m_ECB = ecb;
            }

            public void Execute()
            {
                foreach (TEntitySpawnDefinition spawnDefinition in m_SpawnDefinitionReader)
                {
                    Entity entity = m_ECB.CreateEntity(m_Archetype);
                    spawnDefinition.PopulateOnEntity(entity, ref m_ECB);
                }
            }
        }
    }
}
