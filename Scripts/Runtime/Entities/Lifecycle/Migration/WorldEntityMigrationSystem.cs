using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public class WorldEntityMigrationSystem : AbstractDataSystem
    {
        private readonly HashSet<IMigrationObserver> m_MigrationObservers;

        public WorldEntityMigrationSystem()
        {
            m_MigrationObservers = new HashSet<IMigrationObserver>();
        }

        public void AddMigrationObserver(IMigrationObserver migrationObserver)
        {
            m_MigrationObservers.Add(migrationObserver);
        }

        public void RemoveMigrationObserver(IMigrationObserver migrationObserver)
        {
            m_MigrationObservers.Remove(migrationObserver);
        }

        private void NotifyObserversToMigrateTo(World destinationWorld, ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray)
        {
            foreach (IMigrationObserver migrationObserver in m_MigrationObservers)
            {
                migrationObserver.MigrateTo(destinationWorld, ref remapArray);
            }
        }

        public void MigrateTo(World destinationWorld, EntityQuery entitiesToMigrateQuery)
        {
            // Do move
            NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray = EntityManager.CreateEntityRemapArray(Allocator.TempJob);
            destinationWorld.EntityManager.MoveEntitiesFrom(EntityManager, entitiesToMigrateQuery, remapArray);
            
            NotifyObserversToMigrateTo(destinationWorld, ref remapArray);
            remapArray.Dispose();
        }
    }
}
