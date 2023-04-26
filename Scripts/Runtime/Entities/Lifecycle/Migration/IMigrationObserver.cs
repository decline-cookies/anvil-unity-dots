using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IMigrationObserver
    {
        public void Migrate(World destinationWorld, ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray);
    }
}
