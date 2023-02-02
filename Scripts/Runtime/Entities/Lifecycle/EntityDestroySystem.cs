using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// System that helps in destroying <see cref="Entity"/>'s
    /// </summary>
    /// /// <remarks>
    /// By default, this system updates in <see cref="SimulationSystemGroup"/> but can be configured by subclassing
    /// and using the <see cref="UpdateInGroupAttribute"/> to target a different group.
    /// 
    /// By default, this system updates before <see cref="EntitySpawnSystem"/> so that Entity reuse in a chunk can
    /// occur. This can be configured by subclassing and using the <see cref="UpdateBeforeAttribute"/> or
    /// <see cref="UpdateAfterAttribute"/>.
    /// 
    /// By default, this system uses the <see cref="EndSimulationEntityCommandBufferSystem"/> to playback the
    /// generated <see cref="EntityCommandBuffer"/>s. This can be configured by subclassing and using the
    /// <see cref="UseCommandBufferSystemAttribute"/> to target a different <see cref="EntityCommandBufferSystem"/>
    /// </remarks>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(EntitySpawnSystem))]
    [UseCommandBufferSystem(typeof(EndSimulationEntityCommandBufferSystem))]
    public class EntityDestroySystem : AbstractAnvilSystemBase
    {
        private readonly AccessControlledValue<UnsafeTypedStream<Entity>> m_EntitiesToDestroy;
        private readonly UnsafeTypedStream<Entity>.LaneWriter m_MainThreadLaneWriter;
        private readonly Type m_CommandBufferSystemType;
        
        private EntityCommandBufferSystem m_CommandBufferSystem;
        
        
        public EntityDestroySystem()
        {
            m_EntitiesToDestroy = new AccessControlledValue<UnsafeTypedStream<Entity>>(new UnsafeTypedStream<Entity>(Allocator.Persistent));
            // ReSharper disable once SuggestVarOrType_SimpleTypes
            using var handle = m_EntitiesToDestroy.AcquireWithHandle(AccessType.ExclusiveWrite);
            // ReSharper disable once PossiblyImpureMethodCallOnReadonlyVariable
            m_MainThreadLaneWriter = handle.Value.AsLaneWriter(ParallelAccessUtil.CollectionIndexForMainThread());

            m_CommandBufferSystemType = GetType().GetCustomAttribute<UseCommandBufferSystemAttribute>().CommandBufferSystemType;
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            
            m_CommandBufferSystem = (EntityCommandBufferSystem)World.GetOrCreateSystem(m_CommandBufferSystemType);

            //Default to being off, a call to DestroyDeferred function will enable it
            Enabled = false;
        }

        protected override void OnDestroy()
        {
            m_EntitiesToDestroy.Dispose();
            base.OnDestroy();
        }
        
        //*************************************************************************************************************
        // DESTROY DEFERRED
        //*************************************************************************************************************
        
        /// <summary>
        /// Destroys an <see cref="Entity"/> later on when the associated <see cref="EntityCommandBufferSystem"/> runs.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> to destroy</param>
        public void DestroyDeferred(Entity entity)
        {
            //By using this, we're writing immediately, but will need to execute later on when the system runs.
            Enabled = true;
            
            // ReSharper disable once SuggestVarOrType_SimpleTypes
            using var handle = m_EntitiesToDestroy.AcquireWithHandle(AccessType.SharedWrite);
            m_MainThreadLaneWriter.Write(entity);
        }
        
        //TODO: Implement a DestroyDeferred that takes in a NativeArray or ICollection if needed.
        
        
        //*************************************************************************************************************
        // DESTROY IMMEDIATE
        //*************************************************************************************************************

        /// <summary>
        /// Destroys an <see cref="Entity"/> immediately.
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> to destroy</param>
        public void DestroyImmediate(Entity entity)
        {
            //No enabling of the system since we're executing immediately
            EntityManager.DestroyEntity(entity);
        }
        
        //TODO: Implement a DestroyImmediate that takes in a NativeArray or ICollection if needed.
        
        
        //*************************************************************************************************************
        // DESTROY IN A JOB
        //*************************************************************************************************************
        
        /// <summary>
        /// Returns a <see cref="EntityDestroyWriter"/> to enable queueing up <see cref="Entity"/>s
        /// to destroy during the system's update phase while in a job.
        /// </summary>
        /// <param name="entityDestroyWriter">The <see cref="EntityDestroyWriter"/> to use</param>
        /// <returns>
        /// A <see cref="JobHandle"/> representing when the <see cref="EntityDestroyWriter"/> can be used.
        /// </returns>
        public JobHandle AcquireEntityDestroyWriterAsync(out EntityDestroyWriter entityDestroyWriter)
        {
            //By using this, we're going to want to process later, so we'll enable.
            Enabled = true;
            
            JobHandle dependsOn = m_EntitiesToDestroy.AcquireAsync(AccessType.SharedWrite, out UnsafeTypedStream<Entity> entitiesToDestroy);
            entityDestroyWriter = new EntityDestroyWriter(entitiesToDestroy.AsWriter());
            return dependsOn;
        }
        
        /// <summary>
        /// Allows the system to know when other jobs have finished trying to queue up <see cref="Entity"/>s
        /// to be destroyed.
        /// </summary>
        /// <param name="dependsOn">The <see cref="JobHandle"/> to wait on.</param>
        public void ReleaseEntityDestroyWriterAsync(JobHandle dependsOn)
        {
            m_EntitiesToDestroy.ReleaseAsync(dependsOn);
        }
        
        //*************************************************************************************************************
        // UPDATE
        //*************************************************************************************************************

        protected override void OnUpdate()
        {
            Dependency = Schedule(Dependency);

            Enabled = false;
        }

        private JobHandle Schedule(JobHandle dependsOn)
        {
            EntityCommandBuffer ecb = m_CommandBufferSystem.CreateCommandBuffer();
            JobHandle entitiesToDestroyHandle = m_EntitiesToDestroy.AcquireAsync(AccessType.SharedRead, out UnsafeTypedStream<Entity> entitiesToDestroy);

            DestroyJob job = new DestroyJob(entitiesToDestroy,
                                            ecb);
            
            dependsOn = JobHandle.CombineDependencies(entitiesToDestroyHandle, dependsOn);
            dependsOn = job.Schedule(dependsOn);
            
            m_EntitiesToDestroy.ReleaseAsync(dependsOn);
            m_CommandBufferSystem.AddJobHandleForProducer(dependsOn);
            
            return dependsOn;
        }
        
        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct DestroyJob : IJob
        {
            [ReadOnly] private UnsafeTypedStream<Entity> m_EntitiesToDestroy;
            
            private EntityCommandBuffer m_ECB;

            public DestroyJob(UnsafeTypedStream<Entity> entitiesToDestroy, EntityCommandBuffer ecb)
            {
                m_EntitiesToDestroy = entitiesToDestroy;
                m_ECB = ecb;
            }

            public void Execute()
            {
                NativeArray<Entity> entitiesToDestroy = m_EntitiesToDestroy.ToNativeArray(Allocator.Temp);
                m_ECB.DestroyEntity(entitiesToDestroy);
                m_EntitiesToDestroy.Clear();
            }
        }
    }
}
