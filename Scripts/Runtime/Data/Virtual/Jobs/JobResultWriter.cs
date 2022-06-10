using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    [BurstCompatible]
    public readonly struct JobResultWriter<TResult>
        where TResult : struct
    {
        [ReadOnly] private readonly UnsafeTypedStream<TResult>.Writer m_ResultWriter;

        public JobResultWriter(UnsafeTypedStream<TResult>.Writer resultWriter)
        {
            m_ResultWriter = resultWriter;
        }

        public void Add(TResult value, int laneIndex)
        {
            Add(ref value, laneIndex);
        }

        public void Add(ref TResult value, int laneIndex)
        {
            m_ResultWriter.AsLaneWriter(laneIndex).Write(ref value);
        }
    }
}
