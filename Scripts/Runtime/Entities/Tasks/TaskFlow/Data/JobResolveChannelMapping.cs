using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities
{
    internal class JobResolveChannelMapping
    {
        //Channel > Context > Data
        internal readonly Dictionary<byte, Dictionary<byte, ResolveChannelData>> m_Mapping;

        public JobResolveChannelMapping()
        {
            m_Mapping = new Dictionary<byte, Dictionary<byte, ResolveChannelData>>();
        }

        public IEnumerable<ResolveChannelData> GetResolveChannelData<TResolveChannel>(TResolveChannel resolveChannel)
            where TResolveChannel : Enum
        {
            Dictionary<byte, ResolveChannelData> mapping = GetOrCreateContextMapping(resolveChannel);
            return mapping.Values;
        }


        public void RegisterDataStream<TResolveChannel>(TResolveChannel resolveChannel, AbstractProxyDataStream dataStream, byte context)
            where TResolveChannel : Enum
        {
            Dictionary<byte, ResolveChannelData> mapping = GetOrCreateContextMapping(resolveChannel);
            mapping.Add(context, new ResolveChannelData(dataStream, context));
        }

        private Dictionary<byte, ResolveChannelData> GetOrCreateContextMapping<TResolveChannel>(TResolveChannel resolveChannel)
            where TResolveChannel : Enum
        {
            ResolveChannelUtil.Debug_EnsureEnumValidity(resolveChannel);
            byte byteResolveChannel = UnsafeUtility.As<TResolveChannel, byte>(ref resolveChannel);
            
            if (!m_Mapping.TryGetValue(byteResolveChannel, out Dictionary<byte, ResolveChannelData> mappingByContext))
            {
                mappingByContext = new Dictionary<byte, ResolveChannelData>();
                m_Mapping.Add(byteResolveChannel, mappingByContext);
            }

            return mappingByContext;
        }
    }
}
