using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Gives Arrival (Created or Imported) and Departure (Destroyed or Evicted) information about entities
    /// for a given world for a give frame. <seealso cref="AbstractEntityLifecycleStatusSystem"/>
    /// </summary>
    public interface IEntityLifecycleStatus
    {
        /// <summary>
        /// Gets access to any Arrivals this frame.
        /// </summary>
        /// <param name="arrivals">The entities that have arrived to this world this frame</param>
        /// <returns>The <see cref="JobHandle"/> to wait upon</returns>
        public JobHandle AcquireArrivalsAsync(out NativeList<Entity> arrivals);

        /// <summary>
        /// Gets access to any Departures this frame.
        /// </summary>
        /// <param name="departures">The entities that have departed from this world this frame</param>
        /// <returns>The <see cref="JobHandle"/> to wait upon</returns>
        public JobHandle AcquireDeparturesAsync(out NativeList<Entity> departures);

        /// <summary>
        /// Releases access to the Arrivals so other jobs can access them.
        /// </summary>
        /// <param name="dependsOn">The <see cref="JobHandle"/> to wait upon</param>
        public void ReleaseArrivalsAsync(JobHandle dependsOn);

        /// <summary>
        /// Releases access to the Departures so other jobs can access them.
        /// </summary>
        /// <param name="dependsOn">The <see cref="JobHandle"/> to wait upon</param>
        public void ReleaseDeparturesAsync(JobHandle dependsOn);

        /// <summary>
        /// Gets access to any Arrivals this frame.
        /// </summary>
        /// <returns>The entities that have arrived to this world this frame</returns>
        public AccessControlledValue<NativeList<Entity>>.AccessHandle AcquireArrivals();

        /// <summary>
        /// Gets access to any Departures this frame.
        /// </summary>
        /// <returns>The entities that have departed from this world this frame</returns>
        public AccessControlledValue<NativeList<Entity>>.AccessHandle AcquireDepartures();
    }
}
