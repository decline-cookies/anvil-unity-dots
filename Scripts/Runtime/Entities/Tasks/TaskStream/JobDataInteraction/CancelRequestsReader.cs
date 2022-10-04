using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [BurstCompatible]
    internal struct CancelRequestsReader
    {
        [ReadOnly] private UnsafeParallelHashMap<EntityProxyInstanceID, byte> m_CancelRequestsForID;

        public CancelRequestsReader(UnsafeParallelHashMap<EntityProxyInstanceID, byte> cancelRequestsForID)
        {
            m_CancelRequestsForID = cancelRequestsForID;
        }

        public bool ShouldCancel(EntityProxyInstanceID id)
        {
            return m_CancelRequestsForID.ContainsKey(id);
        }
    }
}
