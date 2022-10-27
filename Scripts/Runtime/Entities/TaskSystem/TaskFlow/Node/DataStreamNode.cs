using System;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class DataStreamNode : AbstractNode
    {
        private readonly NodeLookup m_Lookup;

        public AbstractEntityProxyDataStream DataStream { get; }

        public Type EntityProxyInstanceType { get; }

        public DataStreamNode(NodeLookup lookup,
                              AbstractEntityProxyDataStream dataStream,
                              TaskFlowGraph taskFlowGraph,
                              AbstractTaskSystem taskSystem,
                              AbstractTaskDriver taskDriver) : base(taskFlowGraph, taskSystem, taskDriver)
        {
            m_Lookup = lookup;
            DataStream = dataStream;

            //TODO: As part of #66 and/or #68 - This is a bit stinky. We know it's always going to work but the refactor in those Issues will make this a bit more robust.
            EntityProxyInstanceType = dataStream.Type.GenericTypeArguments[0];
        }

        public override string ToString()
        {
            return $"{DataStream} located in {TaskDebugUtil.GetLocationName(TaskSystem, TaskDriver)}";
        }
    }
}
