using System;
using System.Collections.Generic;
using System.Reflection;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class TaskSet : AbstractTaskSet
    {
        private readonly AbstractTaskDriver m_TaskDriver;
        private readonly List<TaskSet> m_SubTaskSets;

        
        public TaskSet Parent { get; }
        
        public TaskDriverCancelFlow CancelFlow { get; }
        
        
        public TaskSet(AbstractTaskDriver taskDriver, CommonTaskSet commonTaskSet, TaskSet parent) : base(taskDriver.World, taskDriver.GetType(), commonTaskSet.GoverningSystem)
        {
            m_TaskDriver = taskDriver;

            Parent = parent;
            
            CancelFlow = new TaskDriverCancelFlow(this, Parent?.CancelFlow);
            
            m_SubTaskSets = new List<TaskSet>();
        }

        protected sealed override byte GenerateContext()
        {
            return CommonTaskSet.GenerateContextForContextWorkload();
        }
        
        protected sealed override bool InitHasCancellableData()
        {
            return base.InitHasCancellableData() || CommonTaskSet.HasCancellableData;
        }

        protected override void DisposeSelf()
        {
            CancelFlow.Dispose();
            base.DisposeSelf();
        }

        protected override void InitDataStreamInstance(FieldInfo taskDriverField, Type instanceType, Type genericTypeDefinition)
        {
            AbstractDataStream dataStream = null;
            //If this is a System Data Stream type, we just want to assign the one from the core
            dataStream = CommonTaskSet.VALID_DATA_STREAM_TYPES.Contains(genericTypeDefinition)
                ? CommonTaskSet.GetDataStreamByType(instanceType)
                : CreateDataStreamInstance(instanceType, genericTypeDefinition);
            
            AssignDataStreamInstance(taskDriverField, m_TaskDriver, dataStream);
        }
    }
}
