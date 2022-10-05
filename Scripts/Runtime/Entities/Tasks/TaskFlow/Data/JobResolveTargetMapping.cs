using System.Collections.Generic;
using System.Linq;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class JobResolveTargetMapping
    {
        //Target > Context > Data
        //For a given Target, we get a lookup keyed by Context's, which contain the actual data to map to.
        internal readonly Dictionary<uint, Dictionary<byte, ResolveTargetData>> Mapping;

        public JobResolveTargetMapping()
        {
            Mapping = new Dictionary<uint, Dictionary<byte, ResolveTargetData>>();
        }

        public List<ResolveTargetData> GetResolveTargetData<TResolveTargetType>()
            where TResolveTargetType : unmanaged, IEntityProxyInstance
        {
            Dictionary<byte, ResolveTargetData> mapping = GetOrCreateContextMapping<TResolveTargetType>();
            return mapping.Values.ToList();
        }


        public void RegisterDataStream<TResolveTargetType>(EntityProxyDataStream<TResolveTargetType> dataStream, byte context)
            where TResolveTargetType : unmanaged, IEntityProxyInstance
        {
            Dictionary<byte, ResolveTargetData> mapping = GetOrCreateContextMapping<TResolveTargetType>();
            mapping.Add(context, new ResolveTargetData(dataStream, 
                                                       context));
        }

        private Dictionary<byte, ResolveTargetData> GetOrCreateContextMapping<TResolveTargetType>()
            where TResolveTargetType : unmanaged, IEntityProxyInstance
        {
            uint id = ResolveTargetUtil.RegisterResolveTarget<TResolveTargetType>();

            if (!Mapping.TryGetValue(id, out Dictionary<byte, ResolveTargetData> mappingByContext))
            {
                mappingByContext = new Dictionary<byte, ResolveTargetData>();
                Mapping.Add(id, mappingByContext);
            }

            return mappingByContext;
        }
    }
}
