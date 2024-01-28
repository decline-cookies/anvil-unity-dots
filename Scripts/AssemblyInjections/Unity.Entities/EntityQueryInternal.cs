using Unity.Burst;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    [BurstCompile]
    public static class EntityQueryInternal
    {
        [BurstCompile]
        public static unsafe int GetNumWriters(ref this EntityQuery entityQuery)
        {
            EntityQueryImpl* entityQueryImpl = entityQuery.__impl;
            return entityQueryImpl->_QueryData->WriterTypesCount;
        }
    }
}