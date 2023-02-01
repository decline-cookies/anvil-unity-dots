using Anvil.CSharp.Collections;
using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial class EntitySpawnSystem : AbstractAnvilSystemBase
    {
        private const string COMPONENTS_FIELD_NAME = "COMPONENTS";
        private static readonly Type COMPONENT_TYPE_ARRAY = typeof(ComponentType[]);

        private EndSimulationEntityCommandBufferSystem m_CommandBufferSystem;
        private readonly AccessControlledValue<NativeParallelHashMap<long, EntityArchetype>> m_EntityArchetypes;

        private readonly Dictionary<Type, IEntitySpawner> m_EntitySpawners;
        private readonly HashSet<IEntitySpawner> m_ActiveEntitySpawners;

        public EntitySpawnSystem()
        {
            m_EntitySpawners = new Dictionary<Type, IEntitySpawner>();
            m_ActiveEntitySpawners = new HashSet<IEntitySpawner>();
            m_EntityArchetypes = new AccessControlledValue<NativeParallelHashMap<long, EntityArchetype>>(new NativeParallelHashMap<long, EntityArchetype>(ChunkUtil.MaxElementsPerChunk<EntityArchetype>(), Allocator.Persistent));
        }

        protected override void OnCreate()
        {
            base.OnCreate();

            m_CommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            //Default to being off, a call to Spawn will enable it
            Enabled = false;
        }

        protected override void OnDestroy()
        {
            m_ActiveEntitySpawners.Clear();
            m_EntityArchetypes.Dispose();
            m_EntitySpawners.DisposeAllValuesAndClear();
            base.OnDestroy();
        }
        
        //*************************************************************************************************************
        // SPAWN DEFERRED
        //*************************************************************************************************************
        public void SpawnDeferred<TEntitySpawnDefinition>(TEntitySpawnDefinition spawnDefinition)
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            EntitySpawner<TEntitySpawnDefinition> entitySpawner = GetOrCreateEntitySpawner<EntitySpawner<TEntitySpawnDefinition>, TEntitySpawnDefinition>();
            entitySpawner.SpawnDeferred(spawnDefinition);

            Enabled = true;
            m_ActiveEntitySpawners.Add(entitySpawner);
        }

        public void SpawnDeferred<TEntitySpawnDefinition>(NativeArray<TEntitySpawnDefinition> spawnDefinitions)
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            EntitySpawner<TEntitySpawnDefinition> entitySpawner = GetOrCreateEntitySpawner<EntitySpawner<TEntitySpawnDefinition>, TEntitySpawnDefinition>();
            entitySpawner.SpawnDeferred(spawnDefinitions);
            
            Enabled = true;
            m_ActiveEntitySpawners.Add(entitySpawner);
        }

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
        // SPAWN DEFERRED WITH PROTOTYPE
        //*************************************************************************************************************
        public void SpawnDeferred<TEntitySpawnDefinition>(Entity prototype, TEntitySpawnDefinition spawnDefinition, bool shouldDestroyPrototype)
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            EntitySpawnerWithPrototype<TEntitySpawnDefinition> entitySpawner = GetOrCreateEntitySpawner<EntitySpawnerWithPrototype<TEntitySpawnDefinition>, TEntitySpawnDefinition>();
            entitySpawner.Spawn(prototype, spawnDefinition, shouldDestroyPrototype);
        
            Enabled = true;
            m_ActiveEntitySpawners.Add(entitySpawner);
        }

        public void SpawnDeferred<TEntitySpawnDefinition>(Entity prototype, ICollection<TEntitySpawnDefinition> spawnDefinitions, bool shouldDestroyPrototype)
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            EntitySpawnerWithPrototype<TEntitySpawnDefinition> entitySpawner = GetOrCreateEntitySpawner<EntitySpawnerWithPrototype<TEntitySpawnDefinition>, TEntitySpawnDefinition>();
            entitySpawner.Spawn(prototype, spawnDefinitions, shouldDestroyPrototype);
            
            Enabled = true;
            m_ActiveEntitySpawners.Add(entitySpawner);
        }
        
        //*************************************************************************************************************
        // SPAWN IMMEDIATE
        //*************************************************************************************************************

        public Entity SpawnImmediate<TEntitySpawnDefinition>(TEntitySpawnDefinition spawnDefinition)
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            EntitySpawner<TEntitySpawnDefinition> entitySpawner = GetOrCreateEntitySpawner<EntitySpawner<TEntitySpawnDefinition>, TEntitySpawnDefinition>();
            return entitySpawner.SpawnImmediate(spawnDefinition);
        }
        
        //TODO: Implement a SpawnImmediate that takes in a NativeArray or ICollection if needed.

        //*************************************************************************************************************
        // SPAWN IMMEDIATE WITH PROTOTYPE
        //*************************************************************************************************************
        public Entity SpawnImmediate<TEntitySpawnDefinition>(Entity prototype, TEntitySpawnDefinition spawnDefinition, bool shouldDestroyPrototype = false)
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            EntitySpawnerWithPrototype<TEntitySpawnDefinition> entitySpawner = GetOrCreateEntitySpawner<EntitySpawnerWithPrototype<TEntitySpawnDefinition>, TEntitySpawnDefinition>();
            return entitySpawner.SpawnImmediate(prototype, spawnDefinition, shouldDestroyPrototype);
        }

        //TODO: Implement a SpawnImmediate that takes in a NativeArray or ICollection if needed.

        protected override void OnUpdate()
        {
            Dependency = ScheduleActiveEntitySpawners(Dependency);

            m_ActiveEntitySpawners.Clear();
            Enabled = false;
        }

        private JobHandle ScheduleActiveEntitySpawners(JobHandle dependsOn)
        {
            NativeArray<JobHandle> dependencies = new NativeArray<JobHandle>(m_ActiveEntitySpawners.Count + 1, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            dependencies[^1] = m_EntityArchetypes.AcquireAsync(AccessType.SharedRead, out NativeParallelHashMap<long, EntityArchetype> entityArchetypesLookup);

            int index = 0;
            foreach (IEntitySpawner entitySpawner in m_ActiveEntitySpawners)
            {
                EntityCommandBuffer ecb = m_CommandBufferSystem.CreateCommandBuffer();
                dependencies[index] = entitySpawner.Schedule(dependsOn, ref ecb, entityArchetypesLookup);
                index++;
            }

            dependsOn = JobHandle.CombineDependencies(dependencies);
            m_EntityArchetypes.ReleaseAsync(dependsOn);
            m_CommandBufferSystem.AddJobHandleForProducer(dependsOn);
            return dependsOn;
        }
        
        private TEntitySpawner GetOrCreateEntitySpawner<TEntitySpawner, TEntitySpawnDefinition>()
            where TEntitySpawner : IEntitySpawner, new()
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            Type spawnerType = typeof(TEntitySpawner);
            Type definitionType = typeof(TEntitySpawnDefinition);
            // ReSharper disable once InvertIf
            if (!m_EntitySpawners.TryGetValue(spawnerType, out IEntitySpawner entitySpawner))
            {
                // ReSharper disable once SuggestVarOrType_SimpleTypes
                using var handle = m_EntityArchetypes.AcquireWithHandle(AccessType.ExclusiveWrite);
                CreateEntityArchetypeForDefinition(definitionType, handle.Value, out EntityArchetype entityArchetype, out long entityArchetypeHash);
                entitySpawner = new TEntitySpawner();
                entitySpawner.Init(EntityManager,
                                   entityArchetype);
                m_EntitySpawners.Add(spawnerType, entitySpawner);
            }

            return (TEntitySpawner)entitySpawner;
        }

        private void CreateEntityArchetypeForDefinition(Type definitionType, 
                                                        NativeParallelHashMap<long, EntityArchetype> entityArchetypesLookup,
                                                        out EntityArchetype entityArchetype,
                                                        out long entityArchetypeHash)
        {
            entityArchetypeHash = BurstRuntime.GetHashCode64(definitionType);
            if (entityArchetypesLookup.TryGetValue(entityArchetypeHash, out entityArchetype))
            {
                return;
            }

            if (!definitionType.IsValueType)
            {
                throw new InvalidOperationException($"Definition Type of {definitionType.GetReadableName()} should be a readonly struct but it is not.");
            }

            if (definitionType.GetCustomAttribute<BurstCompatibleAttribute>() == null)
            {
                throw new InvalidOperationException($"Definition Type of {definitionType.GetReadableName()} should have the {nameof(BurstCompatibleAttribute)} set but it does not.");
            }

            if (definitionType.GetCustomAttribute<IsReadOnlyAttribute>() == null)
            {
                throw new InvalidOperationException($"Definition Type of {definitionType.GetReadableName()} should be readonly but it is not.");
            }

            FieldInfo componentsField = definitionType.GetField(COMPONENTS_FIELD_NAME, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (componentsField == null)
            {
                throw new InvalidOperationException($"Definition Type of {definitionType.GetReadableName()} should have a `public static readonly ComponentType[] {COMPONENTS_FIELD_NAME}` field to define the Component Types on the Entity. Please add this.");
            }

            if (componentsField.FieldType != COMPONENT_TYPE_ARRAY)
            {
                throw new InvalidOperationException($"Definition Type of {definitionType.GetReadableName()} has a static field called {COMPONENTS_FIELD_NAME} but it is of type {componentsField.FieldType.GetReadableName()} and it must be {COMPONENT_TYPE_ARRAY.GetReadableName()}");
            }

            entityArchetype = EntityManager.CreateArchetype((ComponentType[])componentsField.GetValue(null));
            entityArchetypesLookup.Add(entityArchetypeHash, entityArchetype);
        }
    }
}
