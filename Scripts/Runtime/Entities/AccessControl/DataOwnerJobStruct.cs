
using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities
{
    public struct DataOwnerJobStruct<T>
        where T : struct, ICompleteData<T>
    {
        [NativeSetThreadIndex] [ReadOnly] private readonly int m_NativeThreadIndex;

        private readonly NativeArray<T> m_Current;
        private UnsafeTypedStream<T>.Writer m_ContinueWriter;

        private UnsafeTypedStream<T>.LaneWriter m_ContinueLaneWriter;
        private int m_LaneIndex;

        public DataOwnerJobStruct(NativeArray<T> current,
                                  UnsafeTypedStream<T>.Writer continueWriter)
        {
            m_Current = current;
            m_ContinueWriter = continueWriter;

            m_ContinueLaneWriter = default;
            m_NativeThreadIndex = -1;
            m_LaneIndex = -1;
        }

        public void InitForThread()
        {
            //TODO: Collection checks - Ensure this is called before anything else is called
            if (m_ContinueLaneWriter.IsCreated)
            {
                return;
            }
            m_LaneIndex = ParallelAccessUtil.CollectionIndexForThread(m_NativeThreadIndex);
            m_ContinueLaneWriter = m_ContinueWriter.AsLaneWriter(m_LaneIndex);
        }
        
        public T this[int index]
        {
            get => m_Current[index];
        }

        public void Continue(ref T value)
        {
            //TODO: Collection checks
            m_ContinueLaneWriter.Write(ref value);
        }

        //TODO: This may not be T, it may be a different result
        public void Complete(ref T value)
        {
            //TODO: Collection checks
            value.CompletedWriter.AsLaneWriter(m_LaneIndex).Write(ref value);
        }
    }
}
