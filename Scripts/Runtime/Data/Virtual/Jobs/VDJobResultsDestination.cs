using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    [BurstCompatible]
    public readonly struct VDJobResultsDestination<TResult>
        where TResult : struct
    {
        public static implicit operator VDJobResultsWriter<TResult>(VDJobResultsDestination<TResult> destination) => new VDJobResultsWriter<TResult>(destination.m_ResultWriter);

        [ReadOnly] private readonly UnsafeTypedStream<TResult>.Writer m_ResultWriter;

        public VDJobResultsDestination(UnsafeTypedStream<TResult>.Writer resultWriter)
        {
            m_ResultWriter = resultWriter;
        }
    }
}
