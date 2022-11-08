using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Represents a read only reference to a <see cref="Entity"/> that has completed
    /// it's cancellation work.
    /// To be used in jobs that only allows for reading of this data.
    /// </summary>
    [BurstCompatible]
    public readonly struct CancelCompleteReader
    {
        [ReadOnly] private readonly NativeArray<EntityProxyInstanceID> m_Iteration;

        internal CancelCompleteReader(NativeArray<EntityProxyInstanceID> iteration)
        {
            m_Iteration = iteration;
        }

        /// <summary>
        /// Gets the <see cref="Entity"/> at the specified index.
        /// </summary>
        /// <param name="index">The index into the backing array</param>
        public Entity this[int index]
        {
            get => m_Iteration[index].Entity;
        }
    }
}
