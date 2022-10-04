using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class JobResolveTargetMapping
    {
        //Target > Context > Data
        //For a given Target, we get a lookup keyed by Context's, which contain the actual data to map to.
        internal readonly Dictionary<byte, Dictionary<byte, ResolveTargetData>> Mapping;

        public Type DataStreamType { get; private set; }
        
        public JobResolveTargetMapping()
        {
            Mapping = new Dictionary<byte, Dictionary<byte, ResolveTargetData>>();
        }

        public List<ResolveTargetData> GetResolveTargetData<TResolveTarget>(TResolveTarget resolveTarget)
            where TResolveTarget : Enum
        {
            Dictionary<byte, ResolveTargetData> mapping = GetOrCreateContextMapping(resolveTarget);
            return mapping.Values.ToList();
        }


        public void RegisterDataStream<TResolveTarget>(TResolveTarget resolveTarget, AbstractProxyDataStream dataStream, byte context)
            where TResolveTarget : Enum
        {
            Debug_EnsureDataStreamTypeMatches(dataStream.Type);
            DataStreamType = dataStream.Type;
            Dictionary<byte, ResolveTargetData> mapping = GetOrCreateContextMapping(resolveTarget);
            mapping.Add(context, new ResolveTargetData(dataStream, context));
        }

        private Dictionary<byte, ResolveTargetData> GetOrCreateContextMapping<TResolveTarget>(TResolveTarget resolveTarget)
            where TResolveTarget : Enum
        {
            ResolveTargetUtil.Debug_EnsureEnumValidity(resolveTarget);
            byte byteResolveTarget = UnsafeUtility.As<TResolveTarget, byte>(ref resolveTarget);

            if (!Mapping.TryGetValue(byteResolveTarget, out Dictionary<byte, ResolveTargetData> mappingByContext))
            {
                mappingByContext = new Dictionary<byte, ResolveTargetData>();
                Mapping.Add(byteResolveTarget, mappingByContext);
            }

            return mappingByContext;
        }
        
        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureDataStreamTypeMatches(Type dataStreamType)
        {
            if (DataStreamType != null && DataStreamType != dataStreamType)
            {
                throw new InvalidOperationException($"Tried to registers a DataStream of type {dataStreamType} as a Resolve Target but there is already another stream of type {DataStreamType}. These need to all be the same type.");
            }
        }
    }
}
