using System;
using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities
{
    public interface ITaskFlowGraph
    {
        public List<AbstractProxyDataStream> GetResolveChannelDataStreams<TResolveChannel>(TResolveChannel resolveChannel, ITaskSystem taskSystem)
            where TResolveChannel : Enum;
    }
}
