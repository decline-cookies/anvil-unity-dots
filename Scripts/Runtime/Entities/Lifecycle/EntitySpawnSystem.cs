using Anvil.CSharp.Data;
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
using UnityEngine;

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
        private readonly AccessControlledValue<NativeParallelHashMap<long, Entity>> m_EntityPrototypes;
        private readonly HashSet<EntitySpawner> m_ActiveSpawners;
        private readonly List<EntitySpawner> m_PendingReleaseSpawners;
        private readonly uint m_SystemID;

        private readonly Queue<int> m_InstanceIDQueue;
        private int m_LargestInstanceIDIssued;

        private EntityCommandBufferSystem m_CommandBufferSystem;
        private NativeParallelHashMap<long, EntityArchetype> m_EntityArchetypes;


        public EntitySpawnSystem()
        {
            m_ActiveSpawners = new HashSet<EntitySpawner>();
            m_PendingReleaseSpawners = new List<EntitySpawner>();
            m_InstanceIDQueue = new Queue<int>();
            m_LargestInstanceIDIssued = -1;

            m_EntityArchetypes = new NativeParallelHashMap<long, EntityArchetype>(ChunkUtil.MaxElementsPerChunk<EntityArchetype>(), Allocator.Persistent);
            m_EntityPrototypes = new AccessControlledValue<NativeParallelHashMap<long, Entity>>(
                new NativeParallelHashMap<long, Entity>(
                    ChunkUtil.MaxElementsPerChunk<Entity>(),
                    Allocator.Persistent));
            m_SystemID = s_EntitySpawnSystemIDProvider.GetNextID();
        }

        private int GetNextInstanceID()
        {
            //If we have an ID free to use in the queue, just get it
            if (m_InstanceIDQueue.Count > 0)
            {
                return m_InstanceIDQueue.Dequeue();
            }
            //If we don't, then increment our pool of IDs and return that instead.
            m_LargestInstanceIDIssued++;
            return m_LargestInstanceIDIssued;
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            Type type = GetType();
            Type commandBufferSystemType = type.GetCustomAttribute<UseCommandBufferSystemAttribute>().CommandBufferSystemType;
            m_CommandBufferSystem = (EntityCommandBufferSystem)World.GetOrCreateSystem(commandBufferSystemType);

            CreateArchetypeLookup();
        }

        protected override void OnDestroy()
        {
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
        // SPAWN API - IN JOB
        //*************************************************************************************************************

        public JobHandle AcquireAsync(out EntitySpawner value)
        {
            JobHandle prototypesDependency = m_EntityPrototypes.AcquireAsync(AccessType.SharedRead, out var prototypes);
            value = AcquireSpawner(true, prototypes);
            return prototypesDependency;
        }

        public void ReleaseAsync(JobHandle releaseAccessDependency)
        {
            m_EntityPrototypes.ReleaseAsync(releaseAccessDependency);
            m_CommandBufferSystem.AddJobHandleForProducer(releaseAccessDependency);
        }

        private EntitySpawner AcquireDeferred()
        {
            return AcquireSpawner(true, m_EntityPrototypes.Acquire(AccessType.SharedRead));
        }

        private void ReleaseDeferred(in EntitySpawner entitySpawner)
        {
            m_EntityPrototypes.Release();
            ReleaseSpawner(entitySpawner);
        }

        private EntitySpawner AcquireImmediate()
        {
            return AcquireSpawner(false, m_EntityPrototypes.Acquire(AccessType.SharedRead));
        }

        private void ReleaseImmediate(in EntitySpawner entitySpawner)
        {
            m_EntityPrototypes.Release();
            entitySpawner.Playback(EntityManager);
            entitySpawner.DisposeECB();
            ReleaseSpawner(entitySpawner);
        }

        private EntitySpawner AcquireSpawner(bool isDeferred, NativeParallelHashMap<long, Entity> prototypeLookup)
        {
            EntityCommandBuffer entityCommandBuffer = isDeferred ? m_CommandBufferSystem.CreateCommandBuffer() : new EntityCommandBuffer(Allocator.TempJob);

            EntitySpawner.SpawnerID spawnerID = new EntitySpawner.SpawnerID(m_SystemID, GetNextInstanceID());
            EntitySpawner entitySpawner = new EntitySpawner(
                spawnerID,
                entityCommandBuffer,
                m_EntityArchetypes,
                prototypeLookup);

            m_ActiveSpawners.Add(entitySpawner);
            s_AllSpawners.Add(entitySpawner.ID, entitySpawner);

            return entitySpawner;
        }

        private void ReleaseSpawner(in EntitySpawner entitySpawner)
        {
            m_InstanceIDQueue.Enqueue(entitySpawner.ID.InstanceID);
            m_ActiveSpawners.Remove(entitySpawner);
            s_AllSpawners.Remove(entitySpawner.ID);
        }

        //*************************************************************************************************************
        // SPAWN API - REGULAR
        //*************************************************************************************************************

        /// <summary>
        /// Spawns an <see cref="Entity"/> with the given definition later on when the associated
        /// <see cref="EntityCommandBufferSystem"/> runs.
        /// </summary>
        /// <param name="spawnDefinition">
        /// The <see cref="IEntitySpawnDefinition"/> to populate the created <see cref="Entity"/> with.
        /// </param>
        public Entity SpawnDeferred<TDefinition>(TDefinition spawnDefinition)
            where TDefinition : unmanaged, IEntitySpawnDefinition
        {
            EntitySpawner entitySpawner = AcquireDeferred();
            Entity entity = entitySpawner.SpawnDeferredEntity(spawnDefinition);
            ReleaseDeferred(entitySpawner);
            return entity;
        }

        public NativeArray<Entity> SpawnDeferred<TDefinition>(NativeArray<TDefinition> spawnDefinitions, Allocator entitiesAllocator)
            where TDefinition : unmanaged, IEntitySpawnDefinition
        {
            NativeArray<Entity> entities = new NativeArray<Entity>(spawnDefinitions.Length, entitiesAllocator);
            EntitySpawner entitySpawner = AcquireDeferred();
            for (int i = 0; i < spawnDefinitions.Length; ++i)
            {
                entities[i] = entitySpawner.SpawnDeferredEntity(spawnDefinitions[i]);
            }
            ReleaseDeferred(entitySpawner);
            return entities;
        }

        public Entity SpawnImmediate<TDefinition>(TDefinition spawnDefinition)
            where TDefinition : unmanaged, IEntitySpawnDefinition
        {
            EntitySpawner entitySpawner = AcquireImmediate();

            EntityArchetype entityArchetype = entitySpawner.GetEntityArchetypeForDefinition<TDefinition>();
            Entity entity = EntityManager.CreateEntity(entityArchetype);
            spawnDefinition.PopulateOnEntity(entity, entitySpawner);

            ReleaseImmediate(entitySpawner);

            return entity;
        }

        public void SpawnImmediate<TDefinition>(NativeArray<TDefinition> spawnDefinitions)
            where TDefinition : unmanaged, IEntitySpawnDefinition
        {
            EntitySpawner entitySpawner = AcquireImmediate();

            EntityArchetype entityArchetype = entitySpawner.GetEntityArchetypeForDefinition<TDefinition>();
            NativeArray<Entity> entities = EntityManager.CreateEntity(entityArchetype, spawnDefinitions.Length, Allocator.Temp);
            for (int i = 0; i < spawnDefinitions.Length; i++)
            {
                spawnDefinitions[i].PopulateOnEntity(entities[i], entitySpawner);
            }

            ReleaseImmediate(entitySpawner);
        }

        public NativeArray<Entity> SpawnImmediate<TDefinition>(NativeArray<TDefinition> spawnDefinitions, Allocator entitiesAllocator)
            where TDefinition : unmanaged, IEntitySpawnDefinition
        {
            EntitySpawner entitySpawner = AcquireImmediate();

            EntityArchetype entityArchetype = entitySpawner.GetEntityArchetypeForDefinition<TDefinition>();
            NativeArray<Entity> entities = EntityManager.CreateEntity(entityArchetype, spawnDefinitions.Length, entitiesAllocator);

            for (int i = 0; i < spawnDefinitions.Length; i++)
            {
                spawnDefinitions[i].PopulateOnEntity(entities[i], entitySpawner);
            }

            ReleaseImmediate(entitySpawner);

            return entities;
        }

        //*************************************************************************************************************
        // UPDATE
        //*************************************************************************************************************


        protected override void OnUpdate()
        {
            m_PendingReleaseSpawners.Clear();
            foreach (EntitySpawner entitySpawner in m_ActiveSpawners)
            {
                if (!entitySpawner.DidPlayback())
                {
                    continue;
                }
                m_PendingReleaseSpawners.Add(entitySpawner);
            }
            foreach (EntitySpawner entitySpawner in m_PendingReleaseSpawners)
            {
                ReleaseSpawner(entitySpawner);
            }
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


        //*************************************************************************************************************
        // STATIC REGISTRATION
        //*************************************************************************************************************

        private static readonly Dictionary<EntitySpawner.SpawnerID, EntitySpawner> s_AllSpawners = new Dictionary<EntitySpawner.SpawnerID, EntitySpawner>();
        private static IDProvider s_EntitySpawnSystemIDProvider;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            s_EntitySpawnSystemIDProvider?.Dispose();
            s_EntitySpawnSystemIDProvider = new IDProvider();
            s_AllSpawners.Clear();
        }

        public static EntitySpawner GetEntitySpawnerByID(EntitySpawner.SpawnerID spawnerID)
        {
            if (!s_AllSpawners.TryGetValue(spawnerID, out EntitySpawner entitySpawner))
            {
                throw new InvalidOperationException($"Tried to get {nameof(EntitySpawner)} by ID of {spawnerID} but it wasn't in the lookup!");
            }
            return entitySpawner;
        }
    }
}
