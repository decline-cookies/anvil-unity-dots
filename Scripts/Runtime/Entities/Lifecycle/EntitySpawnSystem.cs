using Anvil.CSharp.Collections;
using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
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
        //TODO: Build this into it's own type
        private static readonly Type I_ENTITY_SPAWN_DEFINITION_TYPE = typeof(IEntitySpawnDefinition);
        private static readonly Dictionary<Type, IEntitySpawnDefinition> s_SpawnDefinitionTypes = new Dictionary<Type, IEntitySpawnDefinition>();
        private static readonly Dictionary<Type, bool> s_SpawnDefinitionShouldDisableBurstLookup = new Dictionary<Type, bool>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            s_SpawnDefinitionTypes.Clear();
            s_SpawnDefinitionShouldDisableBurstLookup.Clear();
            //We'll reflect through the whole app to find all the possible IEntitySpawnDefinitions that exist. 
            //We do this once here so we don't have to reflect for each World that exists.
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                {
                    continue;
                }
                foreach (Type type in assembly.GetTypes())
                {
                    if (!type.IsValueType || !I_ENTITY_SPAWN_DEFINITION_TYPE.IsAssignableFrom(type))
                    {
                        continue;
                    }

                    s_SpawnDefinitionTypes.Add(type, (IEntitySpawnDefinition)Activator.CreateInstance(type));

                    if (s_SpawnDefinitionShouldDisableBurstLookup.ContainsKey(type))
                    {
                        continue;
                    }
                    s_SpawnDefinitionShouldDisableBurstLookup.Add(type, ShouldDisableBurst(type));
                }
            }
        }

        private static bool ShouldDisableBurst(Type type)
        {
            //We've already processed this type and it exists in the lookup, we can just return
            if (s_SpawnDefinitionShouldDisableBurstLookup.TryGetValue(type, out bool shouldDisableBurst))
            {
                return shouldDisableBurst;
            }

            //If any of our components require disabling burst we can early exit.
            if (ShouldRequiredComponentsDisableBurst(type))
            {
                return true;
            }

            //Otherwise crawl our fields and properties to see if there are any proxy definitions that would require us
            //to also disable burst
            Type[] definitionTypes = type
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(fieldInfo => fieldInfo.FieldType)
                .Union(
                    type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        .Select(propertyInfo => propertyInfo.PropertyType))
                .ToArray();

            foreach (Type definitionType in definitionTypes)
            {
                if (!I_ENTITY_SPAWN_DEFINITION_TYPE.IsAssignableFrom(definitionType))
                {
                    continue;
                }

                //We have at least one field that needs us to disable burst, early out.
                if (ShouldRequiredComponentsDisableBurst(definitionType))
                {
                    return true;
                }

                //Let's dive in deeper
                if (ShouldDisableBurst(definitionType))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ShouldRequiredComponentsDisableBurst(Type type)
        {
            ComponentType[] requiredComponents = s_SpawnDefinitionTypes[type].RequiredComponents;
            return requiredComponents.Any(componentType => componentType.IsSharedComponent);
        }

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
            m_EntityArchetypes.Dispose();
            m_EntityPrototypes.Dispose();
            m_EntitySpawners.DisposeAllValuesAndClear();
            base.OnDestroy();
        }

        private void CreateArchetypeLookup()
        {
            foreach ((Type definitionType, IEntitySpawnDefinition entitySpawnDefinition) in s_SpawnDefinitionTypes)
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
        // SPAWN DEFERRED
        //*************************************************************************************************************

        /// <summary>
        /// Spawns an <see cref="Entity"/> with the given definition later on when the associated
        /// <see cref="EntityCommandBufferSystem"/> runs.
        /// </summary>
        /// <remarks>
        /// Will enable the system to be run for at least one frame. If no more spawn requests come in, the system
        /// will disable itself until more requests come in.
        /// </remarks>
        /// <param name="spawnDefinition">
        /// The <see cref="IEntitySpawnDefinition"/> to populate the created <see cref="Entity"/> with.
        /// </param>
        /// <typeparam name="TEntitySpawnDefinition">The type of <see cref="IEntitySpawnDefinition"/></typeparam>
        public void SpawnDeferred<TEntitySpawnDefinition>(TEntitySpawnDefinition spawnDefinition)
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            EntitySpawner<TEntitySpawnDefinition> entitySpawner = GetOrCreateEntitySpawner<TEntitySpawnDefinition>();
            entitySpawner.SpawnDeferred(spawnDefinition);

            EnableSystem(entitySpawner);
        }

        /// <summary>
        /// Spawns multiple <see cref="Entity"/>s with the given definitions later on when the associated
        /// <see cref="EntityCommandBufferSystem"/> runs.
        /// </summary>
        /// <remarks>
        /// Will enable the system to be run for at least one frame. If no more spawn requests come in, the system
        /// will disable itself until more requests come in.
        /// </remarks>
        /// <param name="spawnDefinitions">
        /// The <see cref="IEntitySpawnDefinition"/>s to populate the created <see cref="Entity"/>s with.
        /// </param>
        /// <typeparam name="TEntitySpawnDefinition">The type of <see cref="IEntitySpawnDefinition"/></typeparam>
        public void SpawnDeferred<TEntitySpawnDefinition>(NativeArray<TEntitySpawnDefinition> spawnDefinitions)
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            EntitySpawner<TEntitySpawnDefinition> entitySpawner = GetOrCreateEntitySpawner<TEntitySpawnDefinition>();
            entitySpawner.SpawnDeferred(spawnDefinitions);

            EnableSystem(entitySpawner);
        }

        /// <inheritdoc cref="SpawnDeferred{TEntitySpawnDefinition}(NativeArray{TEntitySpawnDefinition})"/>
        public void SpawnDeferred<TEntitySpawnDefinition>(ICollection<TEntitySpawnDefinition> spawnDefinitions)
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            NativeArray<TEntitySpawnDefinition> nativeArraySpawnDefinitions = new NativeArray<TEntitySpawnDefinition>(spawnDefinitions.Count, Allocator.Temp);
            int index = 0;
            foreach (TEntitySpawnDefinition spawnDefinition in spawnDefinitions)
            {
                nativeArraySpawnDefinitions[index] = spawnDefinition;
                index++;
            }

            SpawnDeferred(nativeArraySpawnDefinitions);
        }


        //*************************************************************************************************************
        // SPAWN IN A JOB
        //*************************************************************************************************************

        /// <summary>
        /// Returns a <see cref="EntitySpawnWriter{TEntitySpawnDefinition}"/> to enable queueing
        /// up <see cref="IEntitySpawnDefinition"/>s to spawn during the system's update phase while in a job.
        /// </summary>
        /// <param name="entitySpawnWriter">The <see cref="EntitySpawnWriter{TEntitySpawnDefinition}"/> to use</param>
        /// <typeparam name="TEntitySpawnDefinition">The type of <see cref="IEntitySpawnDefinition"/></typeparam>
        /// <returns>
        /// A <see cref="JobHandle"/> representing when the <see cref="EntitySpawnWriter{TEntitySpawnDefinition}"/>
        /// can be used.
        /// </returns>
        public JobHandle AcquireEntitySpawnWriterAsync<TEntitySpawnDefinition>(out EntitySpawnWriter<TEntitySpawnDefinition> entitySpawnWriter)
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            EntitySpawner<TEntitySpawnDefinition> entitySpawner = GetOrCreateEntitySpawner<TEntitySpawnDefinition>();

            EnableSystem(entitySpawner);

            return entitySpawner.AcquireEntitySpawnWriterAsync(out entitySpawnWriter);
        }

        /// <summary>
        /// Allows the system to know when other jobs have finished trying to queue
        /// up <see cref="IEntitySpawnDefinition"/>s to be spawned.
        /// </summary>
        /// <param name="dependsOn">The <see cref="JobHandle"/> to wait on</param>
        /// <typeparam name="TEntitySpawnDefinition">The type of <see cref="IEntitySpawnDefinition"/></typeparam>
        public void ReleaseEntitySpawnWriterAsync<TEntitySpawnDefinition>(JobHandle dependsOn)
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            EntitySpawner<TEntitySpawnDefinition> entitySpawner = GetOrCreateEntitySpawner<TEntitySpawnDefinition>();
            entitySpawner.ReleaseEntitySpawnWriterAsync(dependsOn);
        }


        //*************************************************************************************************************
        // SPAWN DEFERRED WITH PROTOTYPE
        //*************************************************************************************************************

        /// <summary>
        /// Spawns an <see cref="Entity"/> with the given definition by cloning the passed in prototype
        /// <see cref="Entity"/> when the associated <see cref="EntityCommandBufferSystem"/> runs later on.
        /// </summary>
        /// <remarks>
        /// Will enable the system to be run for at least one frame. If no more spawn requests come in, the system
        /// will disable itself until more requests come in.
        /// </remarks>
        /// <param name="spawnDefinition">
        /// The <see cref="IEntitySpawnDefinition"/> to populate the created <see cref="Entity"/> with.
        /// </param>
        /// <param name="shouldDestroyPrototype">
        /// If true, will destroy the prototype <see cref="Entity"/> after creation.
        /// </param>
        /// <typeparam name="TEntitySpawnDefinition">The type of <see cref="IEntitySpawnDefinition"/></typeparam>
        public void SpawnWithPrototypeDeferred<TEntitySpawnDefinition>(TEntitySpawnDefinition spawnDefinition, bool shouldDestroyPrototype)
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            EntitySpawner<TEntitySpawnDefinition> entitySpawner = GetOrCreateEntitySpawner<TEntitySpawnDefinition>();
            entitySpawner.SpawnWithPrototypeDeferred(spawnDefinition, shouldDestroyPrototype);

            EnableSystem(entitySpawner);
        }

        /// <summary>
        /// Spawns multiple <see cref="Entity"/>s with the given definitions later on when the associated
        /// <see cref="EntityCommandBufferSystem"/> runs.
        /// </summary>
        /// <remarks>
        /// Will enable the system to be run for at least one frame. If no more spawn requests come in, the system
        /// will disable itself until more requests come in.
        /// </remarks>
        /// <param name="spawnDefinitions">
        /// The <see cref="IEntitySpawnDefinition"/>s to populate the created <see cref="Entity"/>s with.
        /// </param>
        /// <typeparam name="TEntitySpawnDefinition">The type of <see cref="IEntitySpawnDefinition"/></typeparam>
        public void SpawnWithPrototypeDeferred<TEntitySpawnDefinition>(NativeArray<TEntitySpawnDefinition> spawnDefinitions, bool shouldDestroyPrototype)
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            EntitySpawner<TEntitySpawnDefinition> entitySpawner = GetOrCreateEntitySpawner<TEntitySpawnDefinition>();
            entitySpawner.SpawnWithPrototypeDeferred(spawnDefinitions, shouldDestroyPrototype);

            EnableSystem(entitySpawner);
        }

        /// <summary>
        /// Spawns <see cref="Entity"/>s with the given definitions by cloning the passed in prototype
        /// <see cref="Entity"/> when the associated <see cref="EntityCommandBufferSystem"/> runs later on.
        /// </summary>
        /// <remarks>
        /// Will enable the system to be run for at least one frame. If no more spawn requests come in, the system
        /// will disable itself until more requests come in.
        /// </remarks>
        /// <param name="spawnDefinitions">
        /// The <see cref="IEntitySpawnDefinition"/>s to populate the created <see cref="Entity"/>s with.
        /// </param>
        /// <param name="shouldDestroyPrototype">
        /// If true, will destroy the prototype <see cref="Entity"/> after creation.
        /// </param>
        /// <typeparam name="TEntitySpawnDefinition">The type of <see cref="IEntitySpawnDefinition"/></typeparam>
        public void SpawnWithPrototypeDeferred<TEntitySpawnDefinition>(ICollection<TEntitySpawnDefinition> spawnDefinitions, bool shouldDestroyPrototype)
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            NativeArray<TEntitySpawnDefinition> nativeArraySpawnDefinitions = new NativeArray<TEntitySpawnDefinition>(spawnDefinitions.Count, Allocator.Temp);
            int index = 0;
            foreach (TEntitySpawnDefinition spawnDefinition in spawnDefinitions)
            {
                nativeArraySpawnDefinitions[index] = spawnDefinition;
                index++;
            }

            SpawnWithPrototypeDeferred(nativeArraySpawnDefinitions, shouldDestroyPrototype);
        }

        //*************************************************************************************************************
        // SPAWN IMMEDIATE
        //*************************************************************************************************************

        /// <summary>
        /// Spawns an <see cref="Entity"/> with the given definition immediately and returns it.
        /// </summary>
        /// <remarks>
        /// This will not enable this system.
        /// </remarks>
        /// <param name="spawnDefinition">
        /// The <see cref="IEntitySpawnDefinition"/> to populate the created <see cref="Entity"/> with.
        /// </param>
        /// <typeparam name="TEntitySpawnDefinition">The type of <see cref="IEntitySpawnDefinition"/></typeparam>
        /// <returns>The created <see cref="Entity"/></returns>
        public Entity SpawnImmediate<TEntitySpawnDefinition>(TEntitySpawnDefinition spawnDefinition)
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            EntitySpawner<TEntitySpawnDefinition> entitySpawner = GetOrCreateEntitySpawner<TEntitySpawnDefinition>();
            return entitySpawner.SpawnImmediate(spawnDefinition);
        }

        //TODO: Implement a SpawnImmediate that takes in a NativeArray or ICollection if needed.

        //*************************************************************************************************************
        // SPAWN IMMEDIATE WITH PROTOTYPE
        //*************************************************************************************************************

        /// <summary>
        /// Spawns an <see cref="Entity"/> with the given definition immediately by cloning the passed in prototype
        /// <see cref="Entity"/> and returns it immediately.
        /// </summary>
        /// <remarks>
        /// This will not enable this system.
        /// </remarks>
        /// <param name="spawnDefinition">
        /// The <see cref="IEntitySpawnDefinition"/> to populate the created <see cref="Entity"/> with.
        /// </param>
        /// <param name="shouldDestroyPrototype">
        /// If true, will destroy the prototype <see cref="Entity"/> after creation.
        /// </param>
        /// <typeparam name="TEntitySpawnDefinition">The type of <see cref="IEntitySpawnDefinition"/></typeparam>
        /// <returns>The created <see cref="Entity"/></returns>
        public Entity SpawnWithPrototypeImmediate<TEntitySpawnDefinition>(TEntitySpawnDefinition spawnDefinition, bool shouldDestroyPrototype = false)
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            EntitySpawner<TEntitySpawnDefinition> entitySpawner = GetOrCreateEntitySpawner<TEntitySpawnDefinition>();
            return entitySpawner.SpawnWithPrototypeImmediate(spawnDefinition, shouldDestroyPrototype);
        }

        //TODO: Implement a SpawnImmediate that takes in a NativeArray or ICollection if needed.

        public void RegisterEntityPrototypeForDefinition<TEntitySpawnDefinition>(Entity prototype)
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            using var handle = m_EntityPrototypes.AcquireWithHandle(AccessType.ExclusiveWrite);
            long hash = BurstRuntime.GetHashCode64<TEntitySpawnDefinition>();
            //TODO: DEBUG Ensure
            handle.Value.Add(hash, prototype);
        }

        public void UnregisterEntityPrototypeForDefinition<TEntitySpawnDefinition>(bool shouldDestroy)
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            using var handle = m_EntityPrototypes.AcquireWithHandle(AccessType.ExclusiveWrite);
            long hash = BurstRuntime.GetHashCode64<TEntitySpawnDefinition>();
            //TODO: DEBUG Ensure
            if (handle.Value.Remove(hash, out Entity prototype) && shouldDestroy)
            {
                EntityManager.DestroyEntity(prototype);
            }
        }

        private void EnableSystem(IEntitySpawner entitySpawner)
        {
            //By using this, we're writing immediately, but will need to execute later on when the system runs
            Enabled = true;
            m_ActiveEntitySpawners.Add(entitySpawner);
        }

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

        private EntitySpawner<TEntitySpawnDefinition> GetOrCreateEntitySpawner<TEntitySpawnDefinition>()
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            Type definitionType = typeof(TEntitySpawnDefinition);
            // ReSharper disable once InvertIf
            if (!m_EntitySpawners.TryGetValue(definitionType, out IEntitySpawner entitySpawner))
            {
                entitySpawner = new EntitySpawner<TEntitySpawnDefinition>(
                    EntityManager,
                    m_EntityArchetypes,
                    m_EntityPrototypes,
                    s_SpawnDefinitionShouldDisableBurstLookup[definitionType]);
                m_EntitySpawners.Add(definitionType, entitySpawner);
            }

            return (EntitySpawner<TEntitySpawnDefinition>)entitySpawner;
        }
    }
}
