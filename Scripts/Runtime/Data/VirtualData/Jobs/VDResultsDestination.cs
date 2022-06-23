using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// A struct to be used in jobs that acts as a placeholder for results to be written to
    /// later on. Use with <see cref="IVirtualDataInstance{TResult}"/>
    /// </summary>
    /// <typeparam name="TResult">The type of result that can be written</typeparam>
    [BurstCompatible]
    public readonly struct VDResultsDestination<TResult>
        where TResult : struct
    {
        //Implicit cast to a writer when we need to actually write.
        //Otherwise this struct doesn't let you do anything with it until we can guarantee you
        //have access.
        public static implicit operator VDResultsWriter<TResult>(VDResultsDestination<TResult> destination) => new VDResultsWriter<TResult>(destination.m_ResultWriter);

        [ReadOnly] private readonly UnsafeTypedStream<TResult>.Writer m_ResultWriter;

        internal VDResultsDestination(UnsafeTypedStream<TResult>.Writer resultWriter)
        {
            m_ResultWriter = resultWriter;
        }
    }
}
