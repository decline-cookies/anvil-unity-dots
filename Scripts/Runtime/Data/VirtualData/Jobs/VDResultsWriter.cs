using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// Represents a write only reference to <see cref="VirtualData{TKey,TInstance}"/>
    /// <seealso cref="ITaskData{TEnum}"/>
    /// <seealso cref="VDResultsDestination{TResult}"/>
    /// </summary>
    /// <typeparam name="TResult">The type of result to write</typeparam>
    [BurstCompatible]
    internal readonly struct VDResultsWriter<TResult>
        where TResult : unmanaged, IKeyedData
    {
        [ReadOnly] private readonly UnsafeTypedStream<VDInstanceWrapper<TResult>>.Writer m_ResultWriter;
        [ReadOnly] private readonly uint m_Context;

        internal VDResultsWriter(UnsafeTypedStream<VDInstanceWrapper<TResult>>.Writer resultWriter, uint context)
        {
            m_ResultWriter = resultWriter;
            m_Context = context;
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
            m_ResultWriter.AsLaneWriter(laneIndex)
                          .Write(new VDInstanceWrapper<TResult>(result.Entity,
                                                                m_Context,
                                                                ref result));
        }
    }
}
