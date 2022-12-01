using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Represents a read only reference to a <see cref="IAbstractDataStream{TInstance}"/>
    /// To be used in jobs that only allows for reading of this data.
    /// </summary>
    /// <typeparam name="TInstance">They type of <see cref="IEntityProxyInstance"/> to read</typeparam>
    [BurstCompatible]
    public readonly struct DataStreamReader<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        [ReadOnly] private readonly NativeArray<EntityProxyInstanceWrapper<TInstance>> m_Iteration;

        internal DataStreamReader(NativeArray<EntityProxyInstanceWrapper<TInstance>> iteration)
        {
            m_Iteration = iteration;
        }

        /// <summary>
        /// Gets the <typeparamref name="TInstance"/> at the specified index.
        /// </summary>
        /// <param name="index">The index into the backing array</param>
        public TInstance this[int index]
        {
            get => m_Iteration[index].Payload;
        }
    }
}
