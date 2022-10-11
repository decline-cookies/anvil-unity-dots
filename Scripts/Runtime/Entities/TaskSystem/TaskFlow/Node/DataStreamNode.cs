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
            
            //TODO: As part of #66, #68 and/or #71 - This is a bit stinky. We know it's always going to work but the refactor in those Issues will make this a bit more robust.
            EntityProxyInstanceType = dataStream.Type.GenericTypeArguments[0];
        }

        protected override void DisposeSelf()
        {
            //TODO: #71 - This is wonky. We should be disposing the TaskStream but we only own the DataStream here.
            //TODO: The ownership should be with the TaskSystem/TaskDriver. 
            DataStream.Dispose();

            base.DisposeSelf();
        }

        public override string ToString()
        {
            return $"{DataStream} as part of {TaskStream} located in {TaskDebugUtil.GetLocationName(TaskSystem, TaskDriver)}";
        }
    }
}
