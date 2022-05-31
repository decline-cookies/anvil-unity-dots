using Anvil.Unity.DOTS.Data;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities
{
    public struct ResponseJobData<T>
        where T : struct
    {
        [NativeSetThreadIndex] [ReadOnly] private readonly int m_NativeThreadIndex;
        private UnsafeTypedStream<T>.Writer m_CompleteWriter;
        
        
        public ResponseJobData(UnsafeTypedStream<T>.Writer completeWriter)
        {
            m_CompleteWriter = completeWriter;
            
            m_NativeThreadIndex = -1;
        }
        
        
        public void Add(T value, int laneIndex)
        {
            Add(ref value, laneIndex);
        }
        
        public void Add(ref T value, int laneIndex)
        {
            m_CompleteWriter.AsLaneWriter(laneIndex).Write(ref value);
        }
    }
}
