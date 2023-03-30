using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IEntityLifecycleStatus
    {
        public JobHandle AcquireArrivalsAsync(out NativeList<Entity> arrivals);
        public JobHandle AcquireDeparturesAsync(out NativeList<Entity> departures);

        public void ReleaseArrivalsAsync(JobHandle dependsOn);
        public void ReleaseDeparturesAsync(JobHandle dependsOn);

        public AccessControlledValue<NativeList<Entity>>.AccessHandle AcquireArrivals();
        public AccessControlledValue<NativeList<Entity>>.AccessHandle AcquireDepartures();
    }
}
