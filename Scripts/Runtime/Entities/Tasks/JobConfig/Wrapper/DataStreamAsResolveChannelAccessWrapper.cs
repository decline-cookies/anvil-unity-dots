using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities
{
    internal class DataStreamAsResolveChannelAccessWrapper : DataStreamAccessWrapper
    {
        public static DataStreamAsResolveChannelAccessWrapper Create<TResolveChannel>(TResolveChannel resolveChannel, AbstractProxyDataStream dataStream)
            where TResolveChannel : Enum
        {
            ResolveChannelUtil.Debug_EnsureEnumValidity(resolveChannel);
            return new DataStreamAsResolveChannelAccessWrapper(UnsafeUtility.As<TResolveChannel, byte>(ref resolveChannel), dataStream);
        }
        
        public byte ResolveChannel
        {
            get;
        }
        
        private DataStreamAsResolveChannelAccessWrapper(byte resolveChannel, AbstractProxyDataStream dataStream) : base(dataStream, AccessType.SharedWrite)
        {
            ResolveChannel = resolveChannel;
        }
    }
}
