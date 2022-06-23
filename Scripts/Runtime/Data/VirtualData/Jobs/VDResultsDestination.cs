using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// Represents a reference to <see cref="VirtualData{TKey,TInstance}"/> where results
    /// will be written to at a later time.
    /// No reading or writing can happen until that later time when the proper access will have
    /// been resolved.
    /// Use with <see cref="IVirtualDataInstance{TResult}"/>
    /// </summary>
    /// <typeparam name="TResult">The type of result that can be written</typeparam>
    [BurstCompatible]
    public readonly struct VDResultsDestination<TResult>
        where TResult : struct
    {
        [ReadOnly] private readonly UnsafeTypedStream<TResult>.Writer m_ResultWriter;

        internal VDResultsDestination(UnsafeTypedStream<TResult>.Writer resultWriter)
        {
            m_ResultWriter = resultWriter;
        }
        
        //Called internally when we're sure we have access to actually write
        internal VDResultsWriter<TResult> AsResultsWriter()
        {
            return new VDResultsWriter<TResult>(m_ResultWriter);
        }
    }
}
