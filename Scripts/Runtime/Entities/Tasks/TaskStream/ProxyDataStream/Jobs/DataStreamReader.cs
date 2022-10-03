using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Represents a read only reference to <see cref="ProxyDataStream{TInstance}"/>
    /// To be used in jobs that only allows for reading of this data.
    /// </summary>
    /// <remarks>
    /// Commonly used to read the data and trigger something else.
    /// </remarks>
    /// <typeparam name="TInstance">They type of data to read</typeparam>
    [BurstCompatible]
    public readonly struct DataStreamReader<TInstance>
        where TInstance : unmanaged, IProxyInstance
    {
        [ReadOnly] private readonly NativeArray<ProxyInstanceWrapper<TInstance>> m_Iteration;

        internal DataStreamReader(NativeArray<ProxyInstanceWrapper<TInstance>> iteration)
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
