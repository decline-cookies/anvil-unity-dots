using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Profiling;

namespace Anvil.Unity.DOTS.Entities
{
    public class WorldEntityMigrationSystem : AbstractDataSystem
    {
        private readonly HashSet<IMigrationObserver> m_MigrationObservers;
        // ReSharper disable once InconsistentNaming
        private NativeList<JobHandle> m_Dependencies_ScratchPad;

        private ProfilerMarker m_PrepareProfileMarker = new ProfilerMarker("PREPARE MIGRATION");

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

        public void AddMigrationObserver(IMigrationObserver migrationObserver)
        {
            m_MigrationObservers.Add(migrationObserver);
            m_Dependencies_ScratchPad.Resize(m_MigrationObservers.Count, NativeArrayOptions.UninitializedMemory);
        }

        public void RemoveMigrationObserver(IMigrationObserver migrationObserver)
        {
            m_MigrationObservers.Remove(migrationObserver);
            m_Dependencies_ScratchPad.Resize(m_MigrationObservers.Count, NativeArrayOptions.UninitializedMemory);
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

        public void MigrateTo(World destinationWorld, EntityQuery entitiesToMigrateQuery)
        {
            Logger.Debug($"MIGRATING ON FRAME {UnityEngine.Time.frameCount}");
            m_PrepareProfileMarker.Begin();
            
            NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray = EntityManager.CreateEntityRemapArray(Allocator.TempJob);
            //Do the actual move and get back the remap info
            destinationWorld.EntityManager.MoveEntitiesFrom(EntityManager, entitiesToMigrateQuery, remapArray);
            
            //Let everyone have a chance to do any additional remapping
            JobHandle dependsOn = NotifyObserversToMigrateTo(destinationWorld, ref remapArray);
            //Dispose the array based on those remapping jobs being complete
            remapArray.Dispose(dependsOn);
            //Immediately complete the jobs so migration is complete and the world's state is correct
            dependsOn.Complete();
            
            m_PrepareProfileMarker.End();
        }
    }
}
