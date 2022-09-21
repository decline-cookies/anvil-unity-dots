using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities
{
    [BurstCompatible]
    public struct DataStreamResolver : IDisposable
    {
        private static unsafe long GetDataStreamPointerAddress(AbstractProxyDataStream dataStream)
        {
            void* writerPtr = dataStream.GetWriterPointer();
            long address = (long)writerPtr;
            return address;
        }

        [ReadOnly] private UnsafeParallelHashMap<byte, long> m_DataStreamByContext;

        internal DataStreamResolver(Dictionary<byte, ResolveChannelData> mapping)
        {
            int numContexts = mapping.Count;
            m_DataStreamByContext = new UnsafeParallelHashMap<byte, long>(numContexts, Allocator.Persistent);
            foreach (ResolveChannelData data in mapping.Values)
            {
                m_DataStreamByContext.Add(data.Context, GetDataStreamPointerAddress(data.DataStream));
            }
        }

        public void Dispose()
        {
            if (m_DataStreamByContext.IsCreated)
            {
                m_DataStreamByContext.Dispose();
            }
        }

        internal unsafe void Resolve<TResolvedInstance>(byte context, 
                                                        int laneIndex,
                                                        ref TResolvedInstance resolvedInstance)
            where TResolvedInstance : unmanaged, IProxyInstance
        {
            Debug_EnsureContainsContext(context);
            long address = m_DataStreamByContext[context];
            void* writerPtr = (void*)address;
            DataStreamWriter<TResolvedInstance> writer = new DataStreamWriter<TResolvedInstance>(writerPtr, context, laneIndex);
            
            writer.Add(ref resolvedInstance);
        }
        
        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureContainsContext(byte context)
        {
            if (!m_DataStreamByContext.ContainsKey(context))
            {
                throw new InvalidOperationException($"Trying to get Resolve Channel Data Stream with context of {context} but no data stream exists! Does the data you are expecting to write to have the {nameof(ResolveChannelAttribute)} attribute?");
            }
        }
    }
}