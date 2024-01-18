using Anvil.Unity.DOTS.Entities.TaskDriver;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Represents a <see cref="BufferLookup{T}"/> that can be read from and written to.
    /// Each <see cref="DynamicBuffer{T}"/> can be read/write in a separate thread in parallel
    /// but the contents of the <see cref="DynamicBuffer{T}"/> are constrained to that thread.
    /// To be used in jobs that allow for updating a specific instance in the DBFE
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IBufferElementData"/> to update.</typeparam>
    /// <remarks>
    /// NOTE: The <see cref="BufferLookup{T}"/> has the
    /// <see cref="NativeDisableContainerSafetyRestrictionAttribute"/> applied meaning that Unity will not issue
    /// safety warnings when using it in jobs. This is because there might be many jobs of the same type but
    /// representing different <see cref="AbstractTaskDriver"/>s and Unity's safety system gets upset if you straddle
    /// across the jobs.
    /// </remarks>
    public struct DBFEForExclusiveWrite<T> where T : unmanaged, IBufferElementData
    {
        // TODO: #197 - Improve Safety. Currently unable to detect parallel writing from multiple jobs.
        // Required to allow JobPart patterns
        [NativeDisableContainerSafetyRestriction] [NativeDisableParallelForRestriction] [WriteOnly]
        private BufferLookup<T> m_DBFE;

        public DBFEForExclusiveWrite(SystemBase system)
        {
            m_DBFE = system.GetBufferLookup<T>(false);
        }

        /// <summary>
        /// Gets the <see cref="DynamicBuffer{T}"/> that corresponds to the passed <see cref="Entity"/>
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> to lookup the <see cref="DynamicBuffer{T}"/></param>
        public DynamicBuffer<T> this[Entity entity]
        {
            get => m_DBFE[entity];
        }

        /// <inheritdoc cref="BufferLookup{T}.HasComponent"/>
        public bool HasBuffer(Entity entity) => m_DBFE.HasBuffer(entity);

        /// <inheritdoc cref="BufferLookup{T}.TryGetBuffer"/>
        public bool TryGetBuffer(Entity entity, out DynamicBuffer<T> component) => m_DBFE.TryGetBuffer(entity, out component);
    }
}