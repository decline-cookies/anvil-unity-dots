using Anvil.CSharp.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    internal partial class PersistentDataSystem : AbstractDataSystem,
                                                  IMigrationObserver
    {
        private static readonly Dictionary<Type, AbstractPersistentData> s_ThreadPersistentData = new Dictionary<Type, AbstractPersistentData>();
        private static int s_InstanceCount;

        private readonly Dictionary<Type, AbstractPersistentData> m_EntityPersistentData;
        
        public PersistentDataSystem()
        {
            s_InstanceCount++;
            m_EntityPersistentData = new Dictionary<Type, AbstractPersistentData>();
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            WorldEntityMigrationSystem worldEntityMigrationSystem = World.GetOrCreateSystem<WorldEntityMigrationSystem>();
            worldEntityMigrationSystem.AddMigrationObserver(this);
        }

        protected override void OnDestroy()
        {
            m_EntityPersistentData.DisposeAllValuesAndClear();
            s_InstanceCount--;
            if (s_InstanceCount <= 0)
            {
                s_ThreadPersistentData.DisposeAllValuesAndClear();
            }
            base.OnDestroy();
        }

        public ThreadPersistentData<T> GetOrCreateThreadPersistentData<T>()
            where T : unmanaged, IThreadPersistentDataInstance
        {
            Type type = typeof(T);
            if (!s_ThreadPersistentData.TryGetValue(type, out AbstractPersistentData persistentData))
            {
                persistentData = new ThreadPersistentData<T>();
                s_ThreadPersistentData.Add(type, persistentData);
            }
            return (ThreadPersistentData<T>)persistentData;
        }
        
        public EntityPersistentData<T> GetOrCreateEntityPersistentData<T>()
            where T : unmanaged, IEntityPersistentDataInstance
        {
            Type type = typeof(T);
            if (!m_EntityPersistentData.TryGetValue(type, out AbstractPersistentData persistentData))
            {
                persistentData = new EntityPersistentData<T>();
                m_EntityPersistentData.Add(type, persistentData);
            }

            return (EntityPersistentData<T>)persistentData;
        }
        
        //*************************************************************************************************************
        // MIGRATION
        //*************************************************************************************************************

        public void MigrateTo(World destinationWorld, ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray)
        {
            PersistentDataSystem destinationPersistentDataSystem = destinationWorld.GetOrCreateSystem<PersistentDataSystem>();
            Debug_EnsureOtherWorldPersistentDataSystemExists(destinationWorld, destinationPersistentDataSystem);

            foreach (KeyValuePair<Type, AbstractPersistentData> entry in m_EntityPersistentData)
            {
                if (!destinationPersistentDataSystem.m_EntityPersistentData.TryGetValue(entry.Key, out AbstractPersistentData destinationPersistentData))
                {
                    continue;
                }
                
                entry.Value.MigrateTo(destinationPersistentData, ref remapArray);
            }
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
    }
}