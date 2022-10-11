using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Represents a <see cref="ComponentDataFromEntity{T}"/> that can be read from and written to in
    /// parallel. It sets the <see cref="NativeDisableParallelForRestrictionAttribute"/> on the CDFE.
    /// To be used in jobs that allow for updating a specific instance in the CDFE.
    /// </summary>
    /// <typeparam name="T">The type of <see cref="IComponentData"/> to update.</typeparam>
    [BurstCompatible]
    public struct CDFEUpdater<T>
        where T : struct, IComponentData
    {
        [NativeDisableParallelForRestriction] private ComponentDataFromEntity<T> m_CDFE;

        internal CDFEUpdater(SystemBase system)
        {
            m_CDFE = system.GetComponentDataFromEntity<T>(false);
        }
        
        /// <summary>
        /// Gets/Sets the <typeparamref name="T"/> that corresponds to the passed <see cref="Entity"/>
        /// </summary>
        /// <param name="entity">The <see cref="Entity"/> to lookup the data</param>
        public T this[Entity entity]
        {
            get => m_CDFE[entity];
            set => m_CDFE[entity] = value;
        }
    }
}
