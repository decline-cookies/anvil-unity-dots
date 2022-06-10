using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    [BurstCompatible]
    public readonly struct JobSourceMainThreadWriter<T>
        where T : struct
    {
        private readonly UnsafeTypedStream<T>.LaneWriter m_SourceLaneWriter;

        public JobSourceMainThreadWriter(UnsafeTypedStream<T>.LaneWriter sourceLaneWriter)
        {
            m_SourceLaneWriter = sourceLaneWriter;
        }
        
        public void Add(T value)
        {
            Add(ref value);
        }

        public void Add(ref T value)
        {
            m_SourceLaneWriter.Write(ref value);
        }
    }
}
