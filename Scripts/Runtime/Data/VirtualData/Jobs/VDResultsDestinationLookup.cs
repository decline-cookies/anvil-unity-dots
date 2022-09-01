using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Data
{
    //TODO: DOCS
    [BurstCompatible]
    public struct VDResultsDestinationLookup : IDisposable
    {
        private UnsafeParallelHashMap<byte, long> m_Lookup;

        public bool IsCreated
        {
            get => m_Lookup.IsCreated;
        }
        
        internal unsafe VDResultsDestinationLookup(Dictionary<byte, AbstractVirtualData> destinations)
        {
            m_Lookup = new UnsafeParallelHashMap<byte, long>(destinations.Count, Allocator.Persistent);
            foreach (KeyValuePair<byte, AbstractVirtualData> entry in destinations)
            {
                void* ptr = entry.Value.GetWriterPointer();
                long address = (long)ptr;
                m_Lookup.Add(entry.Key, address);
            }
        }

        public void Dispose()
        {
            if (!m_Lookup.IsCreated)
            {
                return;
            }
            m_Lookup.Dispose();
        }

        internal unsafe VDResultsDestination<TTaskResultData> GetVDResultsDestination<TTaskResultEnum, TTaskResultData>(TTaskResultEnum resultsDestinationType)
            where TTaskResultEnum : unmanaged, Enum
            where TTaskResultData : unmanaged, IEntityProxyData
        {
            //TODO: Throw error if lookup doesn't contain the key or it's bigger than a byte via UnsafeUtility.SizeOf
            byte key = UnsafeUtility.As<TTaskResultEnum, byte>(ref resultsDestinationType);
            long address = m_Lookup[key];
            void* ptr = (void*)address;
            VDResultsDestination<TTaskResultData> resultsDestination = VDResultsDestination<TTaskResultData>.ReinterpretFromPointer(ptr);
            return resultsDestination;
        }
    }
}
