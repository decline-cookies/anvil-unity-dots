using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IMigrationObserver
    {
        public void MigrateTo(World destinationWorld, ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray);
    }
}
