using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IMigratable
    {
        public void Migrate(NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray);
    }
    
    public class WorldEntityMigrationSystem : AbstractDataSystem
    {
        private readonly List<IMigratable> m_Migratables;

        public WorldEntityMigrationSystem()
        {
            m_Migratables = new List<IMigratable>();
        }

        public void AddMigratable(IMigratable migratable)
        {
            m_Migratables.Add(migratable);
        }

        public void Remap(NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray)
        {
            foreach (IMigratable migratable in m_Migratables)
            {
                migratable.Migrate(remapArray);
            }
        }
    }
}
