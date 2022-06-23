using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// A struct to be used in jobs that writes new results.
    /// <seealso cref="IVirtualDataInstance{TResult}"/>
    /// <seealso cref="VDResultsDestination{TResult}"/>
    /// </summary>
    /// <typeparam name="TResult">The type of result to write</typeparam>
    [BurstCompatible]
    public readonly struct VDResultsWriter<TResult>
        where TResult : struct
    {
        [ReadOnly] private readonly UnsafeTypedStream<TResult>.Writer m_ResultWriter;

        internal VDResultsWriter(UnsafeTypedStream<TResult>.Writer resultWriter)
        {
            m_ResultWriter = resultWriter;
        }
        
        /// <summary>
        /// Adds a new result to the <see cref="VirtualData{TKey,TInstance}"/> of results.
        /// </summary>
        /// <param name="result">The result to write</param>
        /// <param name="laneIndex">The lane index to write to.</param>
        public void Add(TResult result, int laneIndex)
        {
            Add(ref result, laneIndex);
        }
        
        /// <inheritdoc cref="Add(TResult,int)"/>
        public void Add(ref TResult result, int laneIndex)
        {
            m_ResultWriter.AsLaneWriter(laneIndex).Write(ref result);
        }
    }
}
