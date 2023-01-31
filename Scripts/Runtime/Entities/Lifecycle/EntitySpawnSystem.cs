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

        public void Spawn<TEntitySpawnDefinition>(TEntitySpawnDefinition spawnDefinition)
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            EntitySpawner<TEntitySpawnDefinition> entitySpawner = GetOrCreateEntitySpawner<TEntitySpawnDefinition>();
            entitySpawner.Spawn(spawnDefinition);

            Enabled = true;
            m_ActiveEntitySpawners.Add(entitySpawner);
        }

        public void Spawn<TEntitySpawnDefinition>(TEntitySpawnDefinition spawnDefinition, Entity prototype)
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            EntitySpawner<TEntitySpawnDefinition> entitySpawner = GetOrCreateEntitySpawner<TEntitySpawnDefinition>();
            entitySpawner.Spawn(spawnDefinition, prototype);

            Enabled = true;
            m_ActiveEntitySpawners.Add(entitySpawner);
        }

        public Entity SpawnImmediate<TEntitySpawnDefinition>(TEntitySpawnDefinition spawnDefinition)
            where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
        {
            EntitySpawner<TEntitySpawnDefinition> entitySpawner = GetOrCreateEntitySpawner<TEntitySpawnDefinition>();
            return entitySpawner.SpawnImmediate(spawnDefinition);
        }

        //TODO: Implement Off thread spawning

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
                dependencies[index] = entitySpawner.Schedule(dependsOn, ecb, entityArchetypesLookup);
                index++;
            }

            dependsOn = JobHandle.CombineDependencies(dependencies);
            m_EntityArchetypes.ReleaseAsync(dependsOn);
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
                // ReSharper disable once SuggestVarOrType_SimpleTypes
                using var handle = m_EntityArchetypes.AcquireWithHandle(AccessType.ExclusiveWrite);
                CreateEntityArchetypeForDefinition(definitionType, handle.Value, out EntityArchetype entityArchetype, out long entityArchetypeHash);
                entitySpawner = new EntitySpawner<TEntitySpawnDefinition>(EntityManager, entityArchetype, entityArchetypeHash);
                m_EntitySpawners.Add(definitionType, entitySpawner);
            }

            return (EntitySpawner<TEntitySpawnDefinition>)entitySpawner;
        }

        private void CreateEntityArchetypeForDefinition(Type definitionType, 
                                                        NativeParallelHashMap<long, EntityArchetype> entityArchetypesLookup,
                                                        out EntityArchetype entityArchetype,
                                                        out long entityArchetypeHash)
        {
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
            entityArchetypeHash = BurstRuntime.GetHashCode64(definitionType);
            entityArchetypesLookup.Add(entityArchetypeHash, entityArchetype);
        }
    }
}
