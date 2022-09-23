using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities
{
    [BurstCompatible]
    public struct DataStreamChannelResolver : IDisposable
    {
        [ReadOnly] private UnsafeParallelHashMap<byte, DataStreamResolver> m_ResolversByChannel;

        internal DataStreamChannelResolver(JobResolveChannelMapping jobResolveChannelMapping)
        {
            int numChannels = jobResolveChannelMapping.m_Mapping.Count;
            m_ResolversByChannel = new UnsafeParallelHashMap<byte, DataStreamResolver>(numChannels, Allocator.Persistent);
            foreach (KeyValuePair<byte, Dictionary<byte, ResolveChannelData>> entry in jobResolveChannelMapping.m_Mapping)
            {
                m_ResolversByChannel.Add(entry.Key, new DataStreamResolver(entry.Value));
            }
        }

        public void Dispose()
        {
            if (!m_ResolversByChannel.IsCreated)
            {
                return;
            }
            
            foreach (KeyValue<byte, DataStreamResolver> entry in m_ResolversByChannel)
            {
                entry.Value.Dispose();
            }

            m_ResolversByChannel.Dispose();
        }

        internal void Resolve<TResolveChannel, TResolvedInstance>(TResolveChannel resolveChannel,
                                                                  byte context,
                                                                  int laneIndex,
                                                                  ref TResolvedInstance resolvedInstance)
            where TResolveChannel : Enum
            where TResolvedInstance : unmanaged, IProxyInstance
        {
            byte key = UnsafeUtility.As<TResolveChannel, byte>(ref resolveChannel);
            Debug_EnsureContainsResolveChannel(key);
            DataStreamResolver resolver = m_ResolversByChannel[key];
            resolver.Resolve(context, 
                             laneIndex,
                             ref resolvedInstance);
        }
        
        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureContainsResolveChannel(byte resolveChannelKey)
        {
            if (!m_ResolversByChannel.ContainsKey(resolveChannelKey))
            {
                throw new InvalidOperationException($"Trying to get Resolve Channel with key of {resolveChannelKey} but no channel exists! Did this job require the right Resolve Channel?");
            }
        }
    }
}
