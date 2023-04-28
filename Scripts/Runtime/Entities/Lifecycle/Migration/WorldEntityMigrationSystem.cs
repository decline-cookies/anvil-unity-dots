using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// World specific system for handling Migration.
    /// Register <see cref="IMigrationObserver"/>s here to be notified when Migration occurs
    ///
    /// NOTE: Use <see cref="MigrateTo"/> on this System instead of directly interfacing with
    /// <see cref="EntityManager.MoveEntitiesFrom"/>
    /// </summary>
    public class WorldEntityMigrationSystem : AbstractDataSystem
    {
        private readonly HashSet<IMigrationObserver> m_MigrationObservers;
        // ReSharper disable once InconsistentNaming
        private NativeList<JobHandle> m_Dependencies_ScratchPad;

        public WorldEntityMigrationSystem()
        {
            m_MigrationObservers = new HashSet<IMigrationObserver>();
            m_Dependencies_ScratchPad = new NativeList<JobHandle>(8, Allocator.Persistent);
        }

        protected override void OnDestroy()
        {
            m_Dependencies_ScratchPad.Dispose();
            base.OnDestroy();
        }
        
        /// <summary>
        /// Adds a <see cref="IMigrationObserver"/> to be notified when Migration occurs and be given the chance to
        /// respond to it.
        /// </summary>
        /// <param name="migrationObserver">The <see cref="IMigrationObserver"/></param>
        public void AddMigrationObserver(IMigrationObserver migrationObserver)
        {
            m_MigrationObservers.Add(migrationObserver);
            m_Dependencies_ScratchPad.Resize(m_MigrationObservers.Count, NativeArrayOptions.UninitializedMemory);
        }
        
        /// <summary>
        /// Removes a <see cref="IMigrationObserver"/> if it no longer wishes to be notified of when a Migration occurs.
        /// </summary>
        /// <param name="migrationObserver">The <see cref="IMigrationObserver"/></param>
        public void RemoveMigrationObserver(IMigrationObserver migrationObserver)
        {
            m_MigrationObservers.Remove(migrationObserver);
            m_Dependencies_ScratchPad.Resize(m_MigrationObservers.Count, NativeArrayOptions.UninitializedMemory);
        }

        /// <summary>
        /// Migrates Entities from this <see cref="World"/> to the destination world with the provided query.
        /// This will then handle notifying all <see cref="IMigrationObserver"/>s to have the chance to respond with
        /// custom migration work.
        /// </summary>
        /// <param name="destinationWorld">The <see cref="World"/> to move Entities to.</param>
        /// <param name="entitiesToMigrateQuery">The <see cref="EntityQuery"/> to select the Entities to migrate.</param>
        public void MigrateTo(World destinationWorld, EntityQuery entitiesToMigrateQuery)
        {
            NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray = EntityManager.CreateEntityRemapArray(Allocator.TempJob);
            //Do the actual move and get back the remap info
            destinationWorld.EntityManager.MoveEntitiesFrom(EntityManager, entitiesToMigrateQuery, remapArray);
            
            //Let everyone have a chance to do any additional remapping
            JobHandle dependsOn = NotifyObserversToMigrateTo(destinationWorld, ref remapArray);
            //Dispose the array based on those remapping jobs being complete
            remapArray.Dispose(dependsOn);
            //Immediately complete the jobs so migration is complete and the world's state is correct
            dependsOn.Complete();
        }
        
        private JobHandle NotifyObserversToMigrateTo(World destinationWorld, ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray)
        {
            int index = 0;
            foreach (IMigrationObserver migrationObserver in m_MigrationObservers)
            {
                m_Dependencies_ScratchPad[index] = migrationObserver.MigrateTo(default, destinationWorld, ref remapArray);
                index++;
            }
            return JobHandle.CombineDependencies(m_Dependencies_ScratchPad.AsArray());
        }
    }
}
