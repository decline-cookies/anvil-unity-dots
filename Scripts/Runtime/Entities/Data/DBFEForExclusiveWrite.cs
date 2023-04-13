using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Represents a <see cref="BufferFromEntity{T}"/> that can be read from and written to.
    /// Each <see cref="DynamicBuffer{T}"/> can be read/write in a separate thread in parallel
    /// but the contents of the <see cref="DynamicBuffer{T}"/> are constrained to that thread.
    /// To be used in jobs that allow for updating a specific instance in the DBFE
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IBufferElementData"/> to update.</typeparam>
    [BurstCompatible]
    public struct DBFEForExclusiveWrite<T> where T : struct, IBufferElementData
    {
        [NativeDisableParallelForRestriction] [WriteOnly]
        private BufferFromEntity<T> m_DBFE;

        public DBFEForExclusiveWrite(SystemBase system)
        {
            m_DBFE = system.GetBufferFromEntity<T>(false);
        }

        /// <summary>
        /// Gets the <see cref="DynamicBuffer{T}"/> that corresponds to the passed <see cref="Entity"/>
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> to lookup the <see cref="DynamicBuffer{T}"/></param>
        public DynamicBuffer<T> this[Entity entity]
        {
            get => m_DBFE[entity];
        }
    }
}