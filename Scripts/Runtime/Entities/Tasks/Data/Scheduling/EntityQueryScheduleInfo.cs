using Anvil.Unity.DOTS.Data;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public class EntityQueryScheduleInfo : IScheduleInfo
    {
        public int BatchSize
        {
            get;
        }

        public int Length
        {
            get;
        }

        public DeferredNativeArrayScheduleInfo DeferredNativeArrayScheduleInfo
        {
            get;
        }

        public EntityQuery Query
        {
            get;
        }
        public EntityQueryScheduleInfo(EntityQuery entityQuery, BatchStrategy batchStrategy)
        {
            Query = entityQuery;

            BatchSize = batchStrategy == BatchStrategy.MaximizeChunk
                ? ChunkUtil.MaxElementsPerChunk<Entity>()
                : 1;
        }
    }
}
