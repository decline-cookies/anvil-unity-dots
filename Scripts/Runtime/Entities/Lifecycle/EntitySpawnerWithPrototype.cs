using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using JetBrains.Annotations;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    [UsedImplicitly]
    internal class EntitySpawnerWithPrototype<TEntitySpawnDefinition> : AbstractEntitySpawner<EntityPrototypeDefinitionWrapper<TEntitySpawnDefinition>>
        where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
    {
        private readonly AccessControlledValue<NativeParallelHashSet<Entity>> m_PrototypesToDestroy;

        public EntitySpawnerWithPrototype()
        {
            m_PrototypesToDestroy = new AccessControlledValue<NativeParallelHashSet<Entity>>(new NativeParallelHashSet<Entity>(8, Allocator.Persistent));
        }

        protected override void DisposeSelf()
        {
            m_PrototypesToDestroy.Dispose();
            base.DisposeSelf();
        }

        private void MarkPrototypeToBeDestroyed(Entity prototype)
        {
            // ReSharper disable once SuggestVarOrType_SimpleTypes
            using var prototypes = m_PrototypesToDestroy.AcquireWithHandle(AccessType.ExclusiveWrite);
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            prototypes.Value.Add(prototype);
        }

        public void Spawn(Entity prototype, TEntitySpawnDefinition spawnDefinition, bool shouldDestroyPrototype)
        {
            InternalSpawn(new EntityPrototypeDefinitionWrapper<TEntitySpawnDefinition>(prototype, spawnDefinition));
            if (shouldDestroyPrototype)
            {
                MarkPrototypeToBeDestroyed(prototype);
            }
        }

        public void Spawn(Entity prototype, ICollection<TEntitySpawnDefinition> spawnDefinitions, bool shouldDestroyPrototype)
        {
            NativeArray<EntityPrototypeDefinitionWrapper<TEntitySpawnDefinition>> nativeArraySpawnDefinitions = new NativeArray<EntityPrototypeDefinitionWrapper<TEntitySpawnDefinition>>(spawnDefinitions.Count, Allocator.Temp);
            int index = 0;
            foreach (TEntitySpawnDefinition spawnDefinition in spawnDefinitions)
            {
                nativeArraySpawnDefinitions[index] = new EntityPrototypeDefinitionWrapper<TEntitySpawnDefinition>(prototype, spawnDefinition);
                index++;
            }
            InternalSpawn(nativeArraySpawnDefinitions);
            
            if (shouldDestroyPrototype)
            {
                MarkPrototypeToBeDestroyed(prototype);
            }
        }

        public Entity SpawnImmediate(Entity prototype, TEntitySpawnDefinition spawnDefinition, bool shouldDestroyPrototype)
        {
            EntityCommandBuffer ecb = new EntityCommandBuffer(Allocator.Temp);
            // We're using the EntityManager directly so that we have a valid Entity, but we use the ECB to set
            // the values so that we can conform to the IEntitySpawnDefinitionInterface and developers
            // don't have to implement twice.
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            Entity entity = EntityManager.Instantiate(prototype);
            spawnDefinition.PopulateOnEntity(entity, ref ecb);

            if (shouldDestroyPrototype)
            {
                ecb.DestroyEntity(prototype);
            }
            
            ecb.Playback(EntityManager);
            ecb.Dispose();
            return entity;
        }

        protected override JobHandle ScheduleSpawnJob(JobHandle dependsOn, 
                                                      in UnsafeTypedStream<EntityPrototypeDefinitionWrapper<TEntitySpawnDefinition>>.Reader reader, 
                                                      ref EntityCommandBuffer ecb)
        {
            JobHandle prototypesHandle = m_PrototypesToDestroy.AcquireAsync(AccessType.ExclusiveWrite, out NativeParallelHashSet<Entity> prototypes);
            dependsOn = JobHandle.CombineDependencies(prototypesHandle, dependsOn);
            SpawnJob job = new SpawnJob(reader,
                                        ref ecb,
                                        prototypes);

            dependsOn = job.Schedule(dependsOn);
            m_PrototypesToDestroy.ReleaseAsync(dependsOn);
            return dependsOn;
        }
        
        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        //TODO: #86 - When upgrading to Entities 1.0 we can use an unmanaged shared component which will let us use the job in burst
        [BurstCompile]
        private struct SpawnJob : IJob
        {
            [ReadOnly] private readonly UnsafeTypedStream<EntityPrototypeDefinitionWrapper<TEntitySpawnDefinition>>.Reader m_SpawnDefinitionReader;
            private NativeParallelHashSet<Entity> m_PrototypesToDestroy;

            private EntityCommandBuffer m_ECB;

            public SpawnJob(UnsafeTypedStream<EntityPrototypeDefinitionWrapper<TEntitySpawnDefinition>>.Reader spawnDefinitionReader, 
                            ref EntityCommandBuffer ecb,
                            NativeParallelHashSet<Entity> prototypesToDestroy)
            {
                m_SpawnDefinitionReader = spawnDefinitionReader;
                m_ECB = ecb;
                m_PrototypesToDestroy = prototypesToDestroy;
            }

            public void Execute()
            {
                foreach (EntityPrototypeDefinitionWrapper<TEntitySpawnDefinition> wrapper in m_SpawnDefinitionReader)
                {
                    Entity entity = m_ECB.Instantiate(wrapper.Prototype);
                    // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
                    wrapper.EntitySpawnDefinition.PopulateOnEntity(entity, ref m_ECB);
                }

                foreach (Entity entity in m_PrototypesToDestroy)
                {
                    m_ECB.DestroyEntity(entity);
                }
            }
        }
    }
}
