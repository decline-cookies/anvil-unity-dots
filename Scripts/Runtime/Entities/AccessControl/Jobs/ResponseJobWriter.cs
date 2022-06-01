using Anvil.Unity.DOTS.Data;

namespace Anvil.Unity.DOTS.Entities
{
    public readonly struct ResponseJobWriter<TResponse>
        where TResponse : struct
    {
        private const int DEFAULT_THREAD_INDEX = -1;
        
        private readonly UnsafeTypedStream<TResponse>.Writer m_CompleteWriter;
        
        public ResponseJobWriter(UnsafeTypedStream<TResponse>.Writer completeWriter)
        {
            m_CompleteWriter = completeWriter;
        }
        
        
        public void Add(TResponse value, int laneIndex)
        {
            Add(ref value, laneIndex);
        }
        
        public void Add(ref TResponse value, int laneIndex)
        {
            m_CompleteWriter.AsLaneWriter(laneIndex).Write(ref value);
        }
    }
}
