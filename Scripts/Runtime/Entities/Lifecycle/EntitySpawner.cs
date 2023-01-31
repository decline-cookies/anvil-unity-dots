using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System.Diagnostics.CodeAnalysis;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class EntitySpawner<TEntitySpawnDefinition> : AbstractEntitySpawner<TEntitySpawnDefinition>
        where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
    {
        public void Spawn(TEntitySpawnDefinition spawnDefinition)
        {
            InternalSpawn(spawnDefinition);
        }
        

        // public void Spawn(TEntitySpawnDefinition spawnDefinition, Entity prototype)
        // {
        //     // ReSharper disable once SuggestVarOrType_SimpleTypes
        //     using var handle = m_DefinitionsToSpawn.AcquireWithHandle(AccessType.SharedWrite);
        // }

        public Entity SpawnImmediate(TEntitySpawnDefinition spawnDefinition)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            // We're using the EntityManager directly so that we have a valid Entity, but we use the ECB to set
            // the values so that we can conform to the IEntitySpawnDefinitionInterface and developers
            // don't have to implement twice.
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            Entity entity = m_EntityManager.CreateEntity(m_EntityArchetype);
            spawnDefinition.PopulateOnEntity(entity, ref ecb);
            ecb.Playback(m_EntityManager);
            ecb.Dispose();
            return entity;
        }

        public JobHandle Schedule(JobHandle dependsOn, EntityCommandBuffer ecb, NativeParallelHashMap<long, EntityArchetype> entityArchetypeLookup)
        {
            JobHandle definitionsHandle = m_DefinitionsToSpawn.AcquireAsync(AccessType.SharedRead, out UnsafeTypedStream<TEntitySpawnDefinition> definitions);

            

            dependsOn = JobHandle.CombineDependencies(definitionsHandle, dependsOn);
            dependsOn = job.Schedule(dependsOn);

            m_DefinitionsToSpawn.ReleaseAsync(dependsOn);

            return dependsOn;
        }

        protected override JobHandle ScheduleSpawnJob(JobHandle dependsOn, ref UnsafeTypedStream<TEntitySpawnDefinition>.Reader reader)
        {
            SpawnJob job = new SpawnJob(reader,
                                        entityArchetypeLookup,
                                        ecb);
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        //TODO: #86 - When upgrading to Entities 1.0 we can use an unmanaged shared component which will let us use the job in burst
        private struct SpawnJob : IJob
        {
            [ReadOnly] private readonly UnsafeTypedStream<TEntitySpawnDefinition>.Reader m_SpawnDefinitionReader;
            [ReadOnly] private readonly NativeParallelHashMap<long, EntityArchetype> m_EntityArchetypeLookup;

            [NativeDisableContainerSafetyRestriction]
            private EntityCommandBuffer m_ECB;

            public SpawnJob(UnsafeTypedStream<TEntitySpawnDefinition>.Reader spawnDefinitionReader,
                            NativeParallelHashMap<long, EntityArchetype> entityArchetypeLookup,
                            EntityCommandBuffer ecb)
            {
                m_SpawnDefinitionReader = spawnDefinitionReader;
                m_EntityArchetypeLookup = entityArchetypeLookup;
                m_ECB = ecb;
            }

            public void Execute()
            {
                long typeHash = BurstRuntime.GetHashCode64<TEntitySpawnDefinition>();
                EntityArchetype entityArchetype = m_EntityArchetypeLookup[typeHash];
                foreach (TEntitySpawnDefinition spawnDefinition in m_SpawnDefinitionReader)
                {
                    Entity entity = m_ECB.CreateEntity(entityArchetype);
                    spawnDefinition.PopulateOnEntity(entity, ref m_ECB);
                }
            }
        }
    }
}
