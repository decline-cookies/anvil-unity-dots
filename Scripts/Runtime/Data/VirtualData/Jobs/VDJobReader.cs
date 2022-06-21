using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// A struct to be used in jobs that only allows for reading.
    /// Commonly used to read and trigger something else.
    /// </summary>
    /// <typeparam name="TInstance">They type of data to read</typeparam>
    [BurstCompatible]
    public readonly struct VDJobReader<TInstance>
        where TInstance : struct
    {
        [ReadOnly] private readonly NativeArray<TInstance> m_Iteration;

        internal VDJobReader(NativeArray<TInstance> iteration)
        {
            m_Iteration = iteration;
        }
        
        /// <summary>
        /// Gets the <typeparamref name="TInstance"/> at the specified index.
        /// </summary>
        /// <param name="index">The index into the backing array</param>
        public TInstance this[int index]
        {
            get => m_Iteration[index];
        }
    }
}
