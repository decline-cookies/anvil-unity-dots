using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities
{
    public struct UpdateRequestJobData<TRequest, TResponse>
        where TRequest : struct, IRequestData<TResponse>
        where TResponse : struct
    {
        [NativeSetThreadIndex] [ReadOnly] private readonly int m_NativeThreadIndex;

        private readonly NativeArray<TRequest> m_Current;
        private UnsafeTypedStream<TRequest>.Writer m_ContinueWriter;

        private UnsafeTypedStream<TRequest>.LaneWriter m_ContinueLaneWriter;
        private int m_LaneIndex;

        public UpdateRequestJobData(NativeArray<TRequest> current,
                                    UnsafeTypedStream<TRequest>.Writer continueWriter)
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

        public TRequest this[int index]
        {
            get => m_Current[index];
        }

        public void Continue(ref TRequest value)
        {
            //TODO: Collection checks
            m_ContinueLaneWriter.Write(ref value);
        }
        
        public void Complete(ref TRequest request, ref TResponse response)
        {
            //TODO: Collection checks
            request.ResponseWriter.InitForThread();
            request.ResponseWriter.Add(ref response);
        }
    }
}
