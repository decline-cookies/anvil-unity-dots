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
                              AbstractTaskStream taskStream,
                              bool isResolveTarget) : base(taskFlowGraph, taskSystem, taskDriver)
        {
            m_Lookup = lookup;
            DataStream = dataStream;
            TaskStream = taskStream;
            IsResolveTarget = isResolveTarget;

            EntityProxyInstanceType = dataStream.Type.GenericTypeArguments[0];
        }

        protected override void DisposeSelf()
        {
            DataStream.Dispose();

            base.DisposeSelf();
        }

        public override string ToString()
        {
            return $"{DataStream} as part of {TaskStream} located in {TaskDebugUtil.GetLocationName(TaskSystem, TaskDriver)}";
        }
    }
}
