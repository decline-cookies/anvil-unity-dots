using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities
{
    internal class DataStreamAsResolveChannelAccessWrapper : DataStreamAccessWrapper
    {
        public static DataStreamAsResolveChannelAccessWrapper Create<TResolveChannel>(TResolveChannel resolveChannel, ResolveChannelData wrapper)
            where TResolveChannel : Enum
        {
            ResolveChannelUtil.Debug_EnsureEnumValidity(resolveChannel);
            return new DataStreamAsResolveChannelAccessWrapper(UnsafeUtility.As<TResolveChannel, byte>(ref resolveChannel), wrapper);
        }
        
        public byte ResolveChannel
        {
            get;
        }

        public byte Context
        {
            get;
        }
        
        private DataStreamAsResolveChannelAccessWrapper(byte resolveChannel, ResolveChannelData wrapper) : base(wrapper.DataStream, AccessType.SharedWrite)
        {
            ResolveChannel = resolveChannel;
            Context = wrapper.Context;
        }
    }
}
