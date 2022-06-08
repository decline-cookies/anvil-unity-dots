using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    public readonly struct JobDataForCompletion<T>
        where T : struct
    {
        [ReadOnly] private readonly UnsafeTypedStream<T>.Writer m_CompletionWriter;

        public JobDataForCompletion(UnsafeTypedStream<T>.Writer completionWriter)
        {
            m_CompletionWriter = completionWriter;
        }

        public void Add(T value, int laneIndex)
        {
            Add(ref value, laneIndex);
        }

        public void Add(ref T value, int laneIndex)
        {
            m_CompletionWriter.AsLaneWriter(laneIndex).Write(ref value);
        }
    }
}
