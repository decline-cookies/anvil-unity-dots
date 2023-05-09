using Anvil.Unity.DOTS.Entities.TaskDriver;
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
        private static readonly WorldDataOwnerLookup<DataTargetID, AbstractPersistentData> s_ThreadPersistentData = new WorldDataOwnerLookup<DataTargetID, AbstractPersistentData>();
        private static int s_InstanceCount;

        private readonly WorldDataOwnerLookup<DataTargetID, AbstractPersistentData> m_EntityPersistentData;

        private bool m_IsHardened;

        // ReSharper disable once InconsistentNaming
        private NativeArray<JobHandle> m_MigrationDependencies_ScratchPad;
        private EntityWorldMigrationSystem m_EntityWorldMigrationSystem;

        public DataOwnerID WorldUniqueID { get; }

        public PersistentDataSystem()
        {
            s_InstanceCount++;
            m_EntityPersistentData = new WorldDataOwnerLookup<DataTargetID, AbstractPersistentData>();

            string idPath = $"{GetType().AssemblyQualifiedName}";
            WorldUniqueID = new DataOwnerID(idPath.GetBurstHashCode32());
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_EntityWorldMigrationSystem = World.GetOrCreateSystem<EntityWorldMigrationSystem>();
            m_EntityWorldMigrationSystem.RegisterMigrationObserver(this);
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            if (m_IsHardened)
            {
                return;
            }
            Harden();
        }

        protected override void OnDestroy()
        {
            m_EntityPersistentData.Dispose();
            m_MigrationDependencies_ScratchPad.Dispose();
            s_InstanceCount--;
            if (s_InstanceCount <= 0)
            {
                s_ThreadPersistentData.Dispose();
            }

            m_EntityWorldMigrationSystem.UnregisterMigrationObserver(this);
            base.OnDestroy();
        }
        
        //*************************************************************************************************************
        // INIT
        //*************************************************************************************************************

        public ThreadPersistentData<T> InitGetOrCreateThreadPersistentData<T>(string uniqueContextIdentifier)
            where T : unmanaged, IThreadPersistentDataInstance
        {
            Debug_EnsureNotHardened();
            return s_ThreadPersistentData.InitGetOrCreate(InitCreateThreadPersistentDataInstance<T>, this, uniqueContextIdentifier);
        }

        public EntityPersistentData<T> InitGetOrCreateEntityPersistentData<T>(string uniqueContextIdentifier)
            where T : unmanaged, IEntityPersistentDataInstance
        {
            Debug_EnsureNotHardened();
            return InitGetOrCreateEntityPersistentData<T>(this, uniqueContextIdentifier);
        }

        public EntityPersistentData<T> InitGetOrCreateEntityPersistentData<T>(IDataOwner dataOwner, string uniqueContextIdentifier)
            where T : unmanaged, IEntityPersistentDataInstance
        {
            Debug_EnsureNotHardened();
            return m_EntityPersistentData.InitGetOrCreate(InitCreateEntityPersistentDataInstance<T>, dataOwner, uniqueContextIdentifier);
        }

        public EntityPersistentData<T> InitCreateEntityPersistentData<T>(IDataOwner dataOwner, string uniqueContextIdentifier)
            where T : unmanaged, IEntityPersistentDataInstance
        {
            Debug_EnsureNotHardened();
            return m_EntityPersistentData.InitCreate(InitCreateEntityPersistentDataInstance<T>, dataOwner, uniqueContextIdentifier);
        }

        private EntityPersistentData<T> InitCreateEntityPersistentDataInstance<T>(IDataOwner dataOwner, string uniqueContextIdentifier)
            where T : unmanaged, IEntityPersistentDataInstance
        {
            return new EntityPersistentData<T>(dataOwner, uniqueContextIdentifier);
        }
        
        private ThreadPersistentData<T> InitCreateThreadPersistentDataInstance<T>(IDataOwner dataOwner, string uniqueContextIdentifier)
            where T : unmanaged, IThreadPersistentDataInstance
        {
            return new ThreadPersistentData<T>(dataOwner, uniqueContextIdentifier);
        }
        
        //*************************************************************************************************************
        // HARDENING
        //*************************************************************************************************************

        private void Harden()
        {
            Debug_EnsureNotHardened();
            m_IsHardened = true;
            if (!s_ThreadPersistentData.IsHardened)
            {
                s_ThreadPersistentData.Harden();
            }
            m_EntityPersistentData.Harden();
            m_MigrationDependencies_ScratchPad = new NativeArray<JobHandle>(m_EntityPersistentData.Count, Allocator.Persistent);
        }

        //*************************************************************************************************************
        // MIGRATION
        //*************************************************************************************************************

        JobHandle IEntityWorldMigrationObserver.MigrateTo(JobHandle dependsOn, World destinationWorld, ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray)
        {
            PersistentDataSystem destinationPersistentDataSystem = destinationWorld.GetOrCreateSystem<PersistentDataSystem>();
            Debug_EnsureOtherWorldPersistentDataSystemExists(destinationWorld, destinationPersistentDataSystem);

            int index = 0;
            //We only need to migrate EntityPersistentData.
            //ThreadPersistentData is global to the app and doesn't need to be migrated because no jobs or data should be in flight during migration.
            foreach (KeyValuePair<DataTargetID, AbstractPersistentData> entry in m_EntityPersistentData)
            {
                //We'll try and get the other world's corresponding EntityPersistentData, it might be null
                destinationPersistentDataSystem.m_EntityPersistentData.TryGetData(entry.Key, out AbstractPersistentData dstData);
                //Ours can't be do we direct cast to catch any code issues
                IMigratablePersistentData srcMigratablePersistentData = (IMigratablePersistentData)entry.Value;
                //Theirs might be so we as cast so it can be null
                IMigratablePersistentData dstMigratablePersistentData = dstData as IMigratablePersistentData;
                //Migrate
                m_MigrationDependencies_ScratchPad[index] = srcMigratablePersistentData.MigrateTo(dependsOn, dstMigratablePersistentData, ref remapArray);
                index++;
            }

            return JobHandle.CombineDependencies(m_MigrationDependencies_ScratchPad);
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

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void Debug_EnsureNotHardened()
        {
            if (m_IsHardened)
            {
                throw new InvalidOperationException($"{this} is already Hardened! It was not expected to be.");
            }
        }
    }
}
