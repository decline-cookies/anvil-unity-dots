using System;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class DataStreamNode : AbstractNode
    {
        private readonly NodeLookup m_Lookup;

        public AbstractEntityProxyDataStream DataStream { get; }
        public AbstractTaskStream TaskStream { get; }

        public bool IsResolveTarget { get; }
        
        public Type EntityProxyInstanceType { get; }

        public DataStreamNode(NodeLookup lookup,
                              AbstractEntityProxyDataStream dataStream,
                              TaskFlowGraph taskFlowGraph,
                              AbstractTaskSystem taskSystem,
                              AbstractTaskDriver taskDriver,
                              AbstractTaskStream taskStream) : base(taskFlowGraph, taskSystem, taskDriver)
        {
            m_Lookup = lookup;
            DataStream = dataStream;
            TaskStream = taskStream;
            //TODO: As part of #66, #67 and/or #68 this is not ideal.
            IsResolveTarget = taskStream.IsDataStreamAResolveTarget && taskStream.GetDataStream() == dataStream;
            
            //TODO: As part of #66 and/or #68 - This is a bit stinky. We know it's always going to work but the refactor in those Issues will make this a bit more robust.
            EntityProxyInstanceType = dataStream.Type.GenericTypeArguments[0];
        }

        public override string ToString()
        {
            return $"{DataStream} as part of {TaskStream} located in {TaskDebugUtil.GetLocationName(TaskSystem, TaskDriver)}";
        }
    }
}
