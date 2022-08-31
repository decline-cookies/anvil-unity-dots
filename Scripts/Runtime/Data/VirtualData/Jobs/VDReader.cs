using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// Represents a read only reference to <see cref="VirtualData{TKey,TInstance}"/>
    /// To be used in jobs that only allows for reading of this data.
    /// </summary>
    /// <remarks>
    /// Commonly used to read the data and trigger something else.
    /// </remarks>
    /// <typeparam name="TInstance">They type of data to read</typeparam>
    [BurstCompatible]
    public readonly struct VDReader<TInstance>
        where TInstance : unmanaged, IKeyedData
    {
        [ReadOnly] private readonly NativeArray<VDInstanceWrapper<TInstance>> m_Iteration;

        internal VDReader(NativeArray<VDInstanceWrapper<TInstance>> iteration)
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
