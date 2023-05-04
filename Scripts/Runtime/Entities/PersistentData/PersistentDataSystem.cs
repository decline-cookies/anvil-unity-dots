using Anvil.CSharp.Collections;
using Anvil.CSharp.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal partial class PersistentDataSystem : AbstractDataSystem,
                                                  IEntityWorldMigrationObserver,
                                                  IDataOwner
    {
        private const string WORLD_PATH = "World";
        private static readonly Dictionary<Type, AbstractPersistentData> s_ThreadPersistentData = new Dictionary<Type, AbstractPersistentData>();
        private static int s_InstanceCount;

        private readonly Dictionary<Type, AbstractPersistentData> m_EntityPersistentData;

        private readonly Dictionary<string, IMigratablePersistentData> m_MigrationPersistentDataLookup;
        // ReSharper disable once InconsistentNaming
        private NativeList<JobHandle> m_MigrationDependencies_ScratchPad;
        private EntityWorldMigrationSystem m_EntityWorldMigrationSystem;

        public DataOwnerID WorldUniqueID { get; }

        public PersistentDataSystem()
        {
            s_InstanceCount++;
            m_EntityPersistentData = new Dictionary<Type, AbstractPersistentData>();
            m_MigrationDependencies_ScratchPad = new NativeList<JobHandle>(8, Allocator.Persistent);
            m_MigrationPersistentDataLookup = new Dictionary<string, IMigratablePersistentData>();
            string idPath = $"{GetType().AssemblyQualifiedName}";
            WorldUniqueID = new DataOwnerID(idPath.GetBurstHashCode32());
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_EntityWorldMigrationSystem = World.GetOrCreateSystem<EntityWorldMigrationSystem>();
            m_EntityWorldMigrationSystem.RegisterMigrationObserver(this);
        }

        protected override void OnDestroy()
        {
            m_MigrationDependencies_ScratchPad.Dispose();
            m_EntityPersistentData.DisposeAllValuesAndClear();
            s_InstanceCount--;
            if (s_InstanceCount <= 0)
            {
                s_ThreadPersistentData.DisposeAllValuesAndClear();
            }
            
            m_EntityWorldMigrationSystem.UnregisterMigrationObserver(this);
            base.OnDestroy();
        }

        public ThreadPersistentData<T> GetOrCreateThreadPersistentData<T>(string uniqueContextIdentifier)
            where T : unmanaged, IThreadPersistentDataInstance
        {
            Type type = typeof(T);
            if (!s_ThreadPersistentData.TryGetValue(type, out AbstractPersistentData persistentData))
            {
                persistentData = new ThreadPersistentData<T>(this, uniqueContextIdentifier);
                s_ThreadPersistentData.Add(type, persistentData);
                persistentData.GenerateWorldUniqueID();
            }
            return (ThreadPersistentData<T>)persistentData;
        }
        
        public EntityPersistentData<T> GetOrCreateEntityPersistentData<T>(string uniqueContextIdentifier)
            where T : unmanaged, IEntityPersistentDataInstance
        {
            Type type = typeof(T);
            if (!m_EntityPersistentData.TryGetValue(type, out AbstractPersistentData persistentData))
            {
                persistentData = new EntityPersistentData<T>(this, uniqueContextIdentifier);
                m_EntityPersistentData.Add(type, persistentData);
                persistentData.GenerateWorldUniqueID();
                AddToMigrationLookup(WORLD_PATH, (EntityPersistentData<T>)persistentData);
            }

            return (EntityPersistentData<T>)persistentData;
        }
        
        //*************************************************************************************************************
        // MIGRATION
        //*************************************************************************************************************

        JobHandle IEntityWorldMigrationObserver.MigrateTo(JobHandle dependsOn, World destinationWorld, ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray)
        {
            PersistentDataSystem destinationPersistentDataSystem = destinationWorld.GetOrCreateSystem<PersistentDataSystem>();
            Debug_EnsureOtherWorldPersistentDataSystemExists(destinationWorld, destinationPersistentDataSystem);

            int index = 0;
            foreach (KeyValuePair<string, IMigratablePersistentData> entry in m_MigrationPersistentDataLookup)
            {
                if (!destinationPersistentDataSystem.m_MigrationPersistentDataLookup.TryGetValue(entry.Key, out IMigratablePersistentData destinationPersistentData))
                {
                    throw new InvalidOperationException($"Current World {World} has Entity Persistent Data of {entry.Key} but it doesn't exist in the destination world {destinationWorld}!");
                }
                m_MigrationDependencies_ScratchPad[index] = entry.Value.MigrateTo(dependsOn, destinationPersistentData, ref remapArray);
                index++;
            }

            return JobHandle.CombineDependencies(m_MigrationDependencies_ScratchPad.AsArray());
        }
        
        public void AddToMigrationLookup(string parentPath, IMigratablePersistentData entityPersistentData)
        {
            string path = $"{parentPath}-{entityPersistentData.GetType().GetReadableName()}";
            Debug_EnsureNoDuplicateMigrationData(path);
            m_MigrationPersistentDataLookup.Add(path, entityPersistentData);
            m_MigrationDependencies_ScratchPad.ResizeUninitialized(m_MigrationPersistentDataLookup.Count);
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************
        
        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void Debug_EnsureOtherWorldPersistentDataSystemExists(World destinationWorld, PersistentDataSystem persistentDataSystem)
        {
            if (persistentDataSystem == null)
            {
                throw new InvalidOperationException($"Expected World {destinationWorld} to have a {nameof(PersistentDataSystem)} but it does not!");
            }
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNoDuplicateMigrationData(string path)
        {
            if (m_MigrationPersistentDataLookup.ContainsKey(path))
            {
                throw new InvalidOperationException($"Trying to add Entity Persistent Data migration data for {this} but {path} is already in the lookup!");
            }
        }
    }
}