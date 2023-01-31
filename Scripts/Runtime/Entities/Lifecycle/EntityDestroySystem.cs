using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System.Diagnostics.CodeAnalysis;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(EntitySpawnSystem))]
    public class EntityDestroySystem : AbstractAnvilSystemBase
    {
        private readonly AccessControlledValue<UnsafeTypedStream<Entity>> m_EntitiesToDestroy;
        private readonly UnsafeTypedStream<Entity>.LaneWriter m_MainThreadLaneWriter;
        private readonly UnsafeTypedStream<Entity>.Reader m_Reader;
        
        private EndSimulationEntityCommandBufferSystem m_CommandBufferSystem;

        [SuppressMessage("ReSharper", "PossiblyImpureMethodCallOnReadonlyVariable")]
        public EntityDestroySystem()
        {
            m_EntitiesToDestroy = new AccessControlledValue<UnsafeTypedStream<Entity>>(new UnsafeTypedStream<Entity>(Allocator.Persistent));
            // ReSharper disable once SuggestVarOrType_SimpleTypes
            using var handle = m_EntitiesToDestroy.AcquireWithHandle(AccessType.ExclusiveWrite);
            m_MainThreadLaneWriter = handle.Value.AsLaneWriter(ParallelAccessUtil.CollectionIndexForMainThread());
            m_Reader = handle.Value.AsReader();
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            
            m_CommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            //Default to being off, a call to Destroy will enable it
            Enabled = false;
        }

        protected override void OnDestroy()
        {
            m_EntitiesToDestroy.Dispose();
            base.OnDestroy();
        }

        public void Destroy(Entity entity)
        {
            // ReSharper disable once SuggestVarOrType_SimpleTypes
            using var handle = m_EntitiesToDestroy.AcquireWithHandle(AccessType.SharedWrite);
            m_MainThreadLaneWriter.Write(entity);
            
            Enabled = true;
        }

        protected override void OnUpdate()
        {
            Dependency = Schedule(Dependency);

            Enabled = false;
        }

        private JobHandle Schedule(JobHandle dependsOn)
        {
            EntityCommandBuffer ecb = m_CommandBufferSystem.CreateCommandBuffer();
            JobHandle readerHandle = m_EntitiesToDestroy.AcquireAsync(AccessType.SharedRead, out UnsafeTypedStream<Entity> entitiesToDestroy);

            DestroyJob job = new DestroyJob(m_Reader,
                                            ecb);
            
            dependsOn = JobHandle.CombineDependencies(readerHandle, dependsOn);
            dependsOn = job.Schedule(dependsOn);
            
            m_EntitiesToDestroy.ReleaseAsync(dependsOn);
            m_CommandBufferSystem.AddJobHandleForProducer(dependsOn);
            
            return dependsOn;
        }
        
        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        private struct DestroyJob : IJob
        {
            [ReadOnly] private readonly UnsafeTypedStream<Entity>.Reader m_EntitiesToDestroyReader;

            [NativeDisableContainerSafetyRestriction]
            private EntityCommandBuffer m_ECB;

            public DestroyJob(UnsafeTypedStream<Entity>.Reader entitiesToDestroyReader, EntityCommandBuffer ecb)
            {
                m_EntitiesToDestroyReader = entitiesToDestroyReader;
                m_ECB = ecb;
            }

            public void Execute()
            {
                NativeArray<Entity> entitiesToDestroy = m_EntitiesToDestroyReader.ToNativeArray(Allocator.Temp);
                m_ECB.DestroyEntity(entitiesToDestroy);
            }
        }
    }
}
