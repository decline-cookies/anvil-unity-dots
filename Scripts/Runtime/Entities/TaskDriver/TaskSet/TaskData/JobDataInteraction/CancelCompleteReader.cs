using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Represents a read only reference to an <see cref="CancelCompleted"/> instance that has completed
    /// it's cancellation work.
    /// To be used in jobs that only allow reading of this data.
    /// </summary>
    [BurstCompatible]
    public readonly struct CancelCompleteReader
    {
        [ReadOnly] private readonly NativeArray<EntityProxyInstanceWrapper<CancelCompleted>> m_Iteration;

        internal CancelCompleteReader(NativeArray<EntityProxyInstanceWrapper<CancelCompleted>> iteration)
        {
            m_Iteration = iteration;
        }

        /// <summary>
        /// Gets the <see cref="CancelCompleted"/> at the specified index.
        /// </summary>
        /// <param name="index">The index into the backing array</param>
        public CancelCompleted this[int index]
        {
            get => m_Iteration[index].Payload;
        }
    }
}
