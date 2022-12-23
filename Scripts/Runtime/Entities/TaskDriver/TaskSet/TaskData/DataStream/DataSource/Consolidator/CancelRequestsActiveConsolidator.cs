using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [BurstCompatible]
    internal struct CancelRequestsActiveConsolidator
    {
        private UnsafeParallelHashMap<EntityProxyInstanceID, bool> m_Lookup;

        public CancelRequestsActiveConsolidator(UnsafeParallelHashMap<EntityProxyInstanceID, bool> lookup)
        {
            m_Lookup = lookup;
        }

        public void PrepareForConsolidation()
        {
            m_Lookup.Clear();
        }

        public void WritePending(EntityProxyInstanceID id)
        {
            //TODO: Handle the cases where we don't have cancellable data
            //TODO: Write to the Progress lookup at the same time
            m_Lookup.TryAdd(id, true);
        }
    }
}
