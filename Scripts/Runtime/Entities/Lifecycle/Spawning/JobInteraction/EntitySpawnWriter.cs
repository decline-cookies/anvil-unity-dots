using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities
{
    [BurstCompatible]
    public struct EntitySpawnWriter<TEntitySpawnDefinition>
        where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
    {
        private const int UNSET_LANE_INDEX = -1;

        [ReadOnly] private readonly UnsafeTypedStream<TEntitySpawnDefinition>.Writer m_Writer;

        private UnsafeTypedStream<TEntitySpawnDefinition>.LaneWriter m_LaneWriter;
        private int m_LaneIndex;

        internal EntitySpawnWriter(UnsafeTypedStream<TEntitySpawnDefinition>.Writer writer)
        {
            m_Writer = writer;

            m_LaneWriter = default;
            m_LaneIndex = UNSET_LANE_INDEX;
        }

        public void InitForThread(int nativeThreadIndex)
        {
            m_LaneIndex = ParallelAccessUtil.CollectionIndexForThread(nativeThreadIndex);
            m_LaneWriter = m_Writer.AsLaneWriter(m_LaneIndex);
        }

        public void InitForMainThread()
        {
            m_LaneIndex = ParallelAccessUtil.CollectionIndexForMainThread();
            m_LaneWriter = m_Writer.AsLaneWriter(m_LaneIndex);
        }

        public void Add(TEntitySpawnDefinition definition)
        {
            Add(ref definition);
        }

        public void Add(ref TEntitySpawnDefinition definition)
        {
            m_LaneWriter.Write(ref definition);
        }

        public void Add(TEntitySpawnDefinition definition, int laneIndex)
        {
            Add(ref definition, laneIndex);
        }

        public void Add(ref TEntitySpawnDefinition definition, int laneIndex)
        {
            m_Writer.AsLaneWriter(laneIndex)
                    .Write(ref definition);
        }
    }
}
