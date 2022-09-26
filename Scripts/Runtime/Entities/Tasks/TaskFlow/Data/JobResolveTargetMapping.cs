using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities
{
    internal class JobResolveTargetMapping
    {
        //Channel > Context > Data
        internal readonly Dictionary<byte, Dictionary<byte, ResolveTargetData>> m_Mapping;

        public JobResolveTargetMapping()
        {
            m_Mapping = new Dictionary<byte, Dictionary<byte, ResolveTargetData>>();
        }

        public IEnumerable<ResolveTargetData> GetResolveTargetData<TResolveTarget>(TResolveTarget resolveTarget)
            where TResolveTarget : Enum
        {
            Dictionary<byte, ResolveTargetData> mapping = GetOrCreateContextMapping(resolveTarget);
            return mapping.Values;
        }


        public void RegisterDataStream<TResolveTarget>(TResolveTarget resolveTarget, AbstractProxyDataStream dataStream, byte context)
            where TResolveTarget : Enum
        {
            Dictionary<byte, ResolveTargetData> mapping = GetOrCreateContextMapping(resolveTarget);
            mapping.Add(context, new ResolveTargetData(dataStream, context));
        }

        private Dictionary<byte, ResolveTargetData> GetOrCreateContextMapping<TResolveTarget>(TResolveTarget resolveTarget)
            where TResolveTarget : Enum
        {
            ResolveTargetUtil.Debug_EnsureEnumValidity(resolveTarget);
            byte byteResolveTarget = UnsafeUtility.As<TResolveTarget, byte>(ref resolveTarget);

            if (!m_Mapping.TryGetValue(byteResolveTarget, out Dictionary<byte, ResolveTargetData> mappingByContext))
            {
                mappingByContext = new Dictionary<byte, ResolveTargetData>();
                m_Mapping.Add(byteResolveTarget, mappingByContext);
            }

            return mappingByContext;
        }
    }
}
