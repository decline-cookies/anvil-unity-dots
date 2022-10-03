using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities
{
    //TODO: Move to Tasks folder/namespace?
    [BurstCompatible]
    internal struct CancelRequestsReader : IDisposable
    {
        [ReadOnly] private UnsafeParallelHashMap<ProxyInstanceID, byte> m_CancelRequestsForID;

        public CancelRequestsReader(UnsafeParallelHashMap<ProxyInstanceID, byte> cancelRequestsForID)
        {
            m_CancelRequestsForID = cancelRequestsForID;
        }

        public bool ShouldCancel(ProxyInstanceID id)
        {
            return m_CancelRequestsForID.ContainsKey(id);
        }

        public void Dispose()
        {
            if (!m_CancelRequestsForID.IsCreated)
            {
                return;
            }
            m_CancelRequestsForID.Dispose();
        }
    }
}
