using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal interface IMigratablePersistentData : IDisposable
    {
        public JobHandle MigrateTo(JobHandle dependsOn, IMigratablePersistentData destinationPersistentData, ref NativeArray<EntityRemapUtility.EntityRemapInfo> remapArray);
    }
}
