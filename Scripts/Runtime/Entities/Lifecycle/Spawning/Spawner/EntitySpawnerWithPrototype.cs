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
        private readonly AccessControlledValue<UnsafeTypedStream<Entity>> m_PrototypesToDestroy;
        private readonly UnsafeTypedStream<Entity>.LaneWriter m_MainThreadPrototypesWriter;

        public EntitySpawnerWithPrototype()
        {
            m_PrototypesToDestroy = new AccessControlledValue<UnsafeTypedStream<Entity>>(new UnsafeTypedStream<Entity>(Allocator.Persistent));
            // ReSharper disable once SuggestVarOrType_SimpleTypes
            using var handle = m_PrototypesToDestroy.AcquireWithHandle(AccessType.ExclusiveWrite);
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            m_MainThreadPrototypesWriter = handle.Value.AsLaneWriter(ParallelAccessUtil.CollectionIndexForMainThread());
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
            m_MainThreadPrototypesWriter.Write(prototype);
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
                                                      UnsafeTypedStream<EntityPrototypeDefinitionWrapper<TEntitySpawnDefinition>> spawnDefinitions, 
                                                      ref EntityCommandBuffer ecb)
        {
            JobHandle prototypesHandle = m_PrototypesToDestroy.AcquireAsync(AccessType.ExclusiveWrite, out UnsafeTypedStream<Entity> prototypes);
            dependsOn = JobHandle.CombineDependencies(prototypesHandle, dependsOn);
            SpawnJob job = new SpawnJob(spawnDefinitions,
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
            [ReadOnly] private UnsafeTypedStream<EntityPrototypeDefinitionWrapper<TEntitySpawnDefinition>> m_SpawnDefinitions;
            private UnsafeTypedStream<Entity> m_PrototypesToDestroy;

            private EntityCommandBuffer m_ECB;

            public SpawnJob(UnsafeTypedStream<EntityPrototypeDefinitionWrapper<TEntitySpawnDefinition>> spawnDefinitions, 
                            ref EntityCommandBuffer ecb,
                            UnsafeTypedStream<Entity> prototypesToDestroy)
            {
                m_SpawnDefinitions = spawnDefinitions;
                m_ECB = ecb;
                m_PrototypesToDestroy = prototypesToDestroy;
            }

            public void Execute()
            {
                foreach (EntityPrototypeDefinitionWrapper<TEntitySpawnDefinition> wrapper in m_SpawnDefinitions)
                {
                    Entity entity = m_ECB.Instantiate(wrapper.Prototype);
                    // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
                    wrapper.EntitySpawnDefinition.PopulateOnEntity(entity, ref m_ECB);
                }

                m_SpawnDefinitions.Clear();

                foreach (Entity entity in m_PrototypesToDestroy)
                {
                    m_ECB.DestroyEntity(entity);
                }
                
                m_PrototypesToDestroy.Clear();
            }
        }
    }
    
    //*************************************************************************************************************
    // WRAPPER
    //*************************************************************************************************************
    internal struct EntityPrototypeDefinitionWrapper<TEntitySpawnDefinition>
        where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
    {
        public readonly Entity Prototype;
        public readonly TEntitySpawnDefinition EntitySpawnDefinition;
        public EntityPrototypeDefinitionWrapper(Entity prototype, 
                                                TEntitySpawnDefinition entitySpawnDefinition)
        {
            Prototype = prototype;
            EntitySpawnDefinition = entitySpawnDefinition;
        }
    }
}
