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
            m_EntityWorldMigrationSystem = World.GetOrCreateSystemManaged<EntityWorldMigrationSystem>();
            m_EntityWorldMigrationSystem.RegisterMigrationObserver(this);
        }

        protected override void OnDestroy()
        {
            m_EntityPersistentData.Dispose();
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

        // TODO: #283 - Devise a different name to better reflect the intended use and nature of this data type
        public ThreadPersistentData<T> GetOrCreateThreadPersistentData<T>(string uniqueContextIdentifier)
            where T : unmanaged, IThreadPersistentDataInstance
        {
            DataTargetID worldUniqueID = AbstractPersistentData.GenerateWorldUniqueID(
                this,
                typeof(ThreadPersistentData<T>),
                uniqueContextIdentifier);

            return s_ThreadPersistentData.GetOrCreate(
                worldUniqueID,
                CreateThreadPersistentDataInstance<T>,
                this,
                uniqueContextIdentifier);
        }

        public EntityPersistentData<T> GetOrCreateEntityPersistentData<T>(string uniqueContextIdentifier)
            where T : unmanaged, IEntityPersistentDataInstance
        {
            return GetOrCreateEntityPersistentData<T>(this, uniqueContextIdentifier);
        }

        public EntityPersistentData<T> GetOrCreateEntityPersistentData<T>(IDataOwner dataOwner, string uniqueContextIdentifier)
            where T : unmanaged, IEntityPersistentDataInstance
        {
            DataTargetID worldUniqueID = AbstractPersistentData.GenerateWorldUniqueID(
                dataOwner,
                typeof(EntityPersistentData<T>),
                uniqueContextIdentifier);

            return m_EntityPersistentData.GetOrCreate(
                worldUniqueID,
                CreateEntityPersistentDataInstance<T>,
                dataOwner,
                uniqueContextIdentifier);
        }

        public EntityPersistentData<T> CreateEntityPersistentData<T>(IDataOwner dataOwner, string uniqueContextIdentifier)
            where T : unmanaged, IEntityPersistentDataInstance
        {
            return m_EntityPersistentData.Create(CreateEntityPersistentDataInstance<T>, dataOwner, uniqueContextIdentifier);
        }

        private EntityPersistentData<T> CreateEntityPersistentDataInstance<T>(IDataOwner dataOwner, string uniqueContextIdentifier)
            where T : unmanaged, IEntityPersistentDataInstance
        {
            return new EntityPersistentData<T>(dataOwner, uniqueContextIdentifier);
        }

        // TODO: #283 - Devise a different name to better reflect the intended use and nature of this data type
        private ThreadPersistentData<T> CreateThreadPersistentDataInstance<T>(IDataOwner dataOwner, string uniqueContextIdentifier)
            where T : unmanaged, IThreadPersistentDataInstance
        {
            return new ThreadPersistentData<T>(dataOwner, uniqueContextIdentifier);
        }

        //*************************************************************************************************************
        // MIGRATION
        //*************************************************************************************************************

        JobHandle IEntityWorldMigrationObserver.MigrateTo(JobHandle dependsOn, World destinationWorld, ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray)
        {
            PersistentDataSystem destinationPersistentDataSystem = destinationWorld.GetOrCreateSystemManaged<PersistentDataSystem>();
            Debug_EnsureOtherWorldPersistentDataSystemExists(destinationWorld, destinationPersistentDataSystem);

            NativeArray<JobHandle> migrationDependencies = new NativeArray<JobHandle>(m_EntityPersistentData.Count, Allocator.Temp);
            int index = 0;
            // TODO: #283 - Devise a different name to better reflect the intended use and nature of this data type
            //We only need to migrate EntityPersistentData.
            //ThreadPersistentData is global to the app and doesn't need to be migrated because no jobs or data should be in flight during migration.
            foreach (KeyValuePair<DataTargetID, AbstractPersistentData> entry in m_EntityPersistentData)
            {
                //We'll try and get the other world's corresponding EntityPersistentData, it might be null
                destinationPersistentDataSystem.m_EntityPersistentData.TryGetData(entry.Key, out AbstractPersistentData dstData);
                //Ours can't be null, so we direct cast to catch any code issues (will throw exception)
                IMigratablePersistentData srcMigratablePersistentData = (IMigratablePersistentData)entry.Value;
                //Theirs might be null, so we do an "as cast" so that it can remain null
                IMigratablePersistentData dstMigratablePersistentData = dstData as IMigratablePersistentData;
                //Migrate
                migrationDependencies[index] = srcMigratablePersistentData.MigrateTo(dependsOn, dstMigratablePersistentData, ref remapArray);
                index++;
            }

            return JobHandle.CombineDependencies(migrationDependencies);
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