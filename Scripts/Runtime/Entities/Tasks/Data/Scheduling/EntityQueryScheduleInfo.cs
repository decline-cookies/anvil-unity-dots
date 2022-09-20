using Anvil.Unity.DOTS.Data;
using System;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    internal class EntityQueryScheduleInfo : IScheduleInfo
    {
        public int BatchSize
        {
            get;
        }

        public int Length
        {
            get => m_Wrapper.NativeArray.Length;
        }

        public DeferredNativeArrayScheduleInfo DeferredNativeArrayScheduleInfo
        {
            get => throw new NotSupportedException($"This scheduling info is based on a {nameof(EntityQuery)}, there is no {nameof(DeferredNativeArrayScheduleInfo)} to get.");
        }

        public EntityQuery Query
        {
            get;
        }

        private EntityQueryAccessWrapper m_Wrapper;

        public EntityQueryScheduleInfo(EntityQuery entityQuery, BatchStrategy batchStrategy)
        {
            Query = entityQuery;

            BatchSize = batchStrategy == BatchStrategy.MaximizeChunk
                ? ChunkUtil.MaxElementsPerChunk<Entity>()
                : 1;
        }

        public void LinkWithWrapper(EntityQueryAccessWrapper wrapper)
        {
            m_Wrapper = wrapper;
        }
    }
}
