using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities
{
    [BurstCompatible]
    public struct DataStreamChannelFilter : IDisposable
    {
        [ReadOnly] private UnsafeParallelHashMap<byte, RequestCancelReader> m_FiltersByChannel;

        public void Dispose()
        {
            if (!m_FiltersByChannel.IsCreated)
            {
                return;
            }

            foreach (KeyValue<byte, RequestCancelReader> entry in m_FiltersByChannel)
            {
                entry.Value.Dispose();
            }

            m_FiltersByChannel.Dispose();
        }
    }
}
