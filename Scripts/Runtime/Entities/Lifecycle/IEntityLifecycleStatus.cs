using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Gives Arrival (Created or Imported) and Departure (Destroyed or Evicted) information about entities
    /// for a given world and frame. <seealso cref="AbstractEntityLifecycleStatusSystem"/>
    /// </summary>
    public interface IEntityLifecycleStatus
    {
        /// <summary>
        /// Gets access to any Arrivals this frame.
        /// </summary>
        public IReadAccessControlledValue<NativeList<Entity>> ArrivedEntities { get; }

        /// <summary>
        /// Gets access to any Departures this frame.
        /// </summary>
        public IReadAccessControlledValue<NativeList<Entity>> DepartedEntities { get; }
    }
}