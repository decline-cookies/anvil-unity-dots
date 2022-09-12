using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Represents a read only reference to <see cref="ProxyDataStream{TData}"/>
    /// To be used in jobs that only allows for reading of this data.
    /// </summary>
    /// <remarks>
    /// Commonly used to read the data and trigger something else.
    /// </remarks>
    /// <typeparam name="TData">They type of data to read</typeparam>
    [BurstCompatible]
    public readonly struct DataStreamReader<TData>
        where TData : unmanaged, IProxyData
    {
        [ReadOnly] private readonly NativeArray<ProxyDataWrapper<TData>> m_Iteration;

        internal DataStreamReader(NativeArray<ProxyDataWrapper<TData>> iteration)
        {
            m_Iteration = iteration;
        }

        /// <summary>
        /// Gets the <typeparamref name="TData"/> at the specified index.
        /// </summary>
        /// <param name="index">The index into the backing array</param>
        public TData this[int index]
        {
            get => m_Iteration[index].Payload;
        }
    }
}
