using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities
{
    //TODO: Move to Tasks folder/namespace?
    [BurstCompatible]
    internal struct RequestCancelReader : IDisposable
    {
        [ReadOnly] private UnsafeParallelHashMap<ProxyInstanceID, byte> m_RequestCancelInstances;

        public RequestCancelReader(UnsafeParallelHashMap<ProxyInstanceID, byte> requestCancelInstances)
        {
            m_RequestCancelInstances = requestCancelInstances;
        }

        public bool ShouldCancel(ProxyInstanceID id)
        {
            return m_RequestCancelInstances.ContainsKey(id);
        }

        public void Dispose()
        {
            if (!m_RequestCancelInstances.IsCreated)
            {
                return;
            }
            m_RequestCancelInstances.Dispose();
        }
    }
}
