using System;
using System.Collections.Generic;
using System.Reflection;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class ContextWorkload : AbstractWorkload,
                                     IContextWorkload
    {
        private static readonly HashSet<Type> VALID_DATA_STREAM_TYPES = new HashSet<Type>()
        {
            I_DRIVER_DATA_STREAM, 
            I_DRIVER_CANCELLABLE_DATA_STREAM, 
            I_DRIVER_CANCEL_RESULT_DATA_STREAM,
            I_SYSTEM_DATA_STREAM,
            I_SYSTEM_CANCELLABLE_DATA_STREAM,
            I_SYSTEM_CANCEL_RESULT_DATA_STREAM
        };

        
        private readonly AbstractTaskDriver m_TaskDriver;
        private readonly List<ContextWorkload> m_SubContextWorkloads;

        public RootWorkload RootWorkload { get; }
        public ContextWorkload Parent { get; }
        
        public TaskDriverCancelFlow CancelFlow { get; }
        
        
        
        protected sealed override HashSet<Type> ValidDataStreamTypes
        {
            get => VALID_DATA_STREAM_TYPES;
        }
        
        public ContextWorkload(AbstractTaskDriver taskDriver, RootWorkload rootWorkload, ContextWorkload parent) : base(taskDriver.World, taskDriver.GetType(), rootWorkload.GoverningSystem)
        {
            m_TaskDriver = taskDriver;
            RootWorkload = rootWorkload;

            Parent = parent;
            
            CancelFlow = new TaskDriverCancelFlow(this, Parent?.CancelFlow);
            
            m_SubContextWorkloads = new List<ContextWorkload>();
        }

        protected sealed override byte GenerateContext()
        {
            return RootWorkload.GenerateContextForContextWorkload();
        }
        
        protected sealed override bool InitHasCancellableData()
        {
            return base.InitHasCancellableData() || RootWorkload.HasCancellableData;
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
            dataStream = RootWorkload.VALID_DATA_STREAM_TYPES.Contains(genericTypeDefinition)
                ? RootWorkload.GetDataStreamByType(instanceType)
                : CreateDataStreamInstance(instanceType, genericTypeDefinition);
            
            AssignDataStreamInstance(taskDriverField, m_TaskDriver, dataStream);
        }
    }
}
