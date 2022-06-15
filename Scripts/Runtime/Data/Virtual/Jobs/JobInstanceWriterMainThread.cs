using Unity.Collections;

namespace Anvil.Unity.DOTS.Data
{
    [BurstCompatible]
    public readonly struct JobInstanceWriterMainThread<TInstance>
        where TInstance : struct
    {
        private readonly UnsafeTypedStream<TInstance>.LaneWriter m_InstanceLaneWriter;

        public JobInstanceWriterMainThread(UnsafeTypedStream<TInstance>.LaneWriter instanceLaneWriter)
        {
            m_InstanceLaneWriter = instanceLaneWriter;
        }

        public void Add(TInstance value)
        {
            Add(ref value);
        }

        public void Add(ref TInstance value)
        {
            m_InstanceLaneWriter.Write(ref value);
        }
    }
}
