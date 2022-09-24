using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities
{
    internal class DataStreamNode : AbstractNode
    {
        private readonly Dictionary<Type, byte> m_ResolveChannelLookup;
        private readonly DataStreamNodeLookup m_Lookup;

        public AbstractProxyDataStream DataStream
        {
            get;
        }
        
        public AbstractTaskStream TaskStream
        {
            get;
        }

        public DataStreamNode(DataStreamNodeLookup lookup,
                              AbstractProxyDataStream dataStream,
                              TaskFlowGraph taskFlowGraph,
                              ITaskSystem taskSystem,
                              ITaskDriver taskDriver,
                              AbstractTaskStream taskStream) : base(taskFlowGraph, taskSystem, taskDriver)
        {
            m_Lookup = lookup;
            DataStream = dataStream;
            TaskStream = taskStream;

            m_ResolveChannelLookup = new Dictionary<Type, byte>();
        }

        protected override void DisposeSelf()
        {
            DataStream.Dispose();
            m_ResolveChannelLookup.Clear();

            base.DisposeSelf();
        }

        public override string ToString()
        {
            //TODO: Update with Task Stream info
            return $"{DataStream} located in {TaskDebugUtil.GetLocation(TaskSystem, TaskDriver)}";
        }

        public void RegisterAsResolveChannel(ResolveChannelAttribute resolveChannelAttribute)
        {
            Type type = resolveChannelAttribute.ResolveChannel.GetType();
            byte value = (byte)resolveChannelAttribute.ResolveChannel;

            m_ResolveChannelLookup.Add(type, value);
        }

        public bool IsResolveChannel<TResolveChannel>(TResolveChannel resolveChannel)
            where TResolveChannel : Enum
        {
            Type type = typeof(TResolveChannel);
            if (!m_ResolveChannelLookup.TryGetValue(type, out byte value))
            {
                return false;
            }
            
            ResolveChannelUtil.Debug_EnsureEnumValidity(resolveChannel);
            byte storedResolveChannel = UnsafeUtility.As<TResolveChannel, byte>(ref resolveChannel);
            return value == storedResolveChannel;
        }
    }
}
