using Anvil.CSharp.Collections;
using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// System that helps in spawning new <see cref="Entity"/>s and uses <see cref="IEntitySpawnDefinition"/>s
    /// to do so.
    /// </summary>
    /// <remarks>
    /// By default, this system updates in <see cref="SimulationSystemGroup"/> but can be configured by subclassing
    /// and using the <see cref="UpdateInGroupAttribute"/> to target a different group.
    ///
    /// By default, this system uses the <see cref="EndSimulationEntityCommandBufferSystem"/> to playback the
    /// generated <see cref="EntityCommandBuffer"/>s. This can be configured by subclassing and using the
    /// <see cref="UseCommandBufferSystemAttribute"/> to target a different <see cref="EntityCommandBufferSystem"/>
    /// </remarks>
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    [UseCommandBufferSystem(typeof(EndSimulationEntityCommandBufferSystem))]
    public partial class EntitySpawnSystem : AbstractAnvilSystemBase
    {
        private EntityCommandBufferSystem m_CommandBufferSystem;
        private NativeParallelHashMap<long, EntityArchetype> m_EntityArchetypes;

        private readonly Dictionary<Type, IEntitySpawner> m_EntitySpawners;
        private readonly HashSet<IEntitySpawner> m_ActiveEntitySpawners;
        private readonly AccessControlledValue<NativeParallelHashMap<long, Entity>> m_EntityPrototypes;

        public EntitySpawnSystem()
        {
            m_EntitySpawners = new Dictionary<Type, IEntitySpawner>();
            m_ActiveEntitySpawners = new HashSet<IEntitySpawner>();
            m_EntityArchetypes = new NativeParallelHashMap<long, EntityArchetype>(ChunkUtil.MaxElementsPerChunk<EntityArchetype>(), Allocator.Persistent);
            m_EntityPrototypes = new AccessControlledValue<NativeParallelHashMap<long, Entity>>(
                new NativeParallelHashMap<long, Entity>(
                    ChunkUtil.MaxElementsPerChunk<Entity>(),
                    Allocator.Persistent));
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            Type type = GetType();
            Type commandBufferSystemType = type.GetCustomAttribute<UseCommandBufferSystemAttribute>().CommandBufferSystemType;
            m_CommandBufferSystem = (EntityCommandBufferSystem)World.GetOrCreateSystem(commandBufferSystemType);

            CreateArchetypeLookup();

            //Default to being off, a call to a SpawnDeferred function will enable it
            Enabled = false;
        }

        protected override void OnDestroy()
        {
            m_ActiveEntitySpawners.Clear();
            m_EntitySpawners.DisposeAllValuesAndClear();
            m_EntityArchetypes.Dispose();
            m_EntityPrototypes.Dispose();
            base.OnDestroy();
        }

        private void CreateArchetypeLookup()
        {
            foreach ((Type definitionType, IEntitySpawnDefinition entitySpawnDefinition) in EntitySpawnSystemReflectionHelper.SPAWN_DEFINITION_TYPES)
            {
                long entityArchetypeHash = BurstRuntime.GetHashCode64(definitionType);
                if (m_EntityArchetypes.TryGetValue(entityArchetypeHash, out EntityArchetype entityArchetype))
                {
                    continue;
                }

                if (definitionType.GetCustomAttribute<BurstCompatibleAttribute>() == null)
                {
                    throw new InvalidOperationException($"Definition Type of {definitionType.GetReadableName()} should have the {nameof(BurstCompatibleAttribute)} set but it does not.");
                }

                if (definitionType.GetCustomAttribute<IsReadOnlyAttribute>() == null)
                {
                    throw new InvalidOperationException($"Definition Type of {definitionType.GetReadableName()} should be readonly but it is not.");
                }

                // ReSharper disable once PossibleNullReferenceException
                entityArchetype = EntityManager.CreateArchetype(entitySpawnDefinition.RequiredComponents);
                m_EntityArchetypes.Add(entityArchetypeHash, entityArchetype);
            }
        }

        private void EntitySpawner_OnWriterAcquired(IEntitySpawner entitySpawner)
        {
            //By using this, we're writing immediately, but will need to execute later on when the system runs
            Enabled = true;
            m_ActiveEntitySpawners.Add(entitySpawner);
        }

        private EntitySpawner<TEntitySpawnDefinition> GetOrCreateEntitySpawner<TEntitySpawnDefinition>()
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            Type definitionType = typeof(TEntitySpawnDefinition);
            // ReSharper disable once InvertIf
            if (!m_EntitySpawners.TryGetValue(definitionType, out IEntitySpawner entitySpawner))
            {
                // TODO: #86 - When upgrading to Entities 1.0 we can use an unmanaged shared component which will let us
                // TODO:       use burst for all spawners.
                entitySpawner = EntitySpawnSystemReflectionHelper.SHOULD_DISABLE_BURST_LOOKUP[definitionType]
                    ? new EntitySpawner<TEntitySpawnDefinition>(
                        EntityManager,
                        m_EntityArchetypes,
                        m_EntityPrototypes)
                    : new BurstEntitySpawner<TEntitySpawnDefinition>(
                        EntityManager,
                        m_EntityArchetypes,
                        m_EntityPrototypes);
                entitySpawner.OnPendingWorkAdded += EntitySpawner_OnWriterAcquired;
                m_EntitySpawners.Add(definitionType, entitySpawner);
            }

            return (EntitySpawner<TEntitySpawnDefinition>)entitySpawner;
        }

        /// <summary>
        /// Returns an <see cref="EntitySpawner{TEntitySpawnDefinition}"/> to enable acquiring a writer to spawn
        /// <see cref="IEntitySpawnDefinition"/>s during the system's update phase while in a job.
        /// </summary>
        /// <typeparam name="TEntitySpawnDefinition">The type of <see cref="IEntitySpawnDefinition"/></typeparam>
        /// <returns>
        /// An <see cref="EntitySpawner{TEntitySpawnDefinition}"/> to acquire a writer against.
        /// </returns>
        public EntitySpawner<TEntitySpawnDefinition> GetSpawner<TEntitySpawnDefinition>()
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            return GetOrCreateEntitySpawner<TEntitySpawnDefinition>();
        }

        //*************************************************************************************************************
        // PROTOTYPE REGISTRATION
        //*************************************************************************************************************

        public void RegisterEntityPrototypeForDefinition<TEntitySpawnDefinition>(Entity prototype)
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            using var handle = m_EntityPrototypes.AcquireWithHandle(AccessType.ExclusiveWrite);
            long hash = BurstRuntime.GetHashCode64<TEntitySpawnDefinition>();
            DEBUG_EnsurePrototypeIsNotRegistered(typeof(TEntitySpawnDefinition), hash, handle.Value);
            handle.Value.Add(hash, prototype);
        }

        public void UnregisterEntityPrototypeForDefinition<TEntitySpawnDefinition>(bool shouldDestroy)
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            //If we're tearing down and this system was destroyed before whoever was trying to unregister, we
            //can't do anything. It's ok, because our Dispose would have handled freeing everything up.
            if (m_EntityPrototypes.IsDisposed)
            {
                return;
            }

            using var handle = m_EntityPrototypes.AcquireWithHandle(AccessType.ExclusiveWrite);
            long hash = BurstRuntime.GetHashCode64<TEntitySpawnDefinition>();
            DEBUG_EnsurePrototypeIsRegistered(typeof(TEntitySpawnDefinition), hash, handle.Value);
            if (handle.Value.Remove(hash, out Entity prototype) && shouldDestroy)
            {
                EntityManager.DestroyEntity(prototype);
            }
        }

        //*************************************************************************************************************
        // UPDATE
        //*************************************************************************************************************

        protected override void OnUpdate()
        {
            Dependency = ScheduleActiveEntitySpawners(Dependency);

            //Ensure we're turned back off
            m_ActiveEntitySpawners.Clear();
            Enabled = false;
        }

        private JobHandle ScheduleActiveEntitySpawners(JobHandle dependsOn)
        {
            NativeArray<JobHandle> dependencies = new NativeArray<JobHandle>(m_ActiveEntitySpawners.Count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            //All the active spawners that need to go this frame are given a chance to go ahead and run their spawn jobs
            int index = 0;

            foreach (IEntitySpawner entitySpawner in m_ActiveEntitySpawners)
            {
                //Normally for each ECB created, you want to add the job handle for producer to the command buffer
                //system. However, we know that all these handles will be combined, so we can just do one call at the
                //end of the function. Creating here and passing into the Schedule function allows us to see the
                //creation and AddJobHandleForProducer calls close by so we know we're adhering to the "pattern".
                EntityCommandBuffer ecb = m_CommandBufferSystem.CreateCommandBuffer();
                dependencies[index] = entitySpawner.Schedule(dependsOn, ref ecb);
                index++;
            }

            dependsOn = JobHandle.CombineDependencies(dependencies);
            m_CommandBufferSystem.AddJobHandleForProducer(dependsOn);
            return dependsOn;
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void DEBUG_EnsurePrototypeIsRegistered(Type type, long hash, NativeParallelHashMap<long, Entity> prototypes)
        {
            if (!prototypes.ContainsKey(hash))
            {
                throw new InvalidOperationException($"Expected prototype to be registered for {type.GetReadableName()} but it wasn't! Did you call {nameof(RegisterEntityPrototypeForDefinition)}?");
            }
        }

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void DEBUG_EnsurePrototypeIsNotRegistered(Type type, long hash, NativeParallelHashMap<long, Entity> prototypes)
        {
            if (prototypes.ContainsKey(hash))
            {
                throw new InvalidOperationException($"Trying to register {type.GetReadableName()} but it already is!");
            }
        }
    }
}