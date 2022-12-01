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

        private readonly RootWorkload m_RootWorkload;
        private readonly AbstractTaskDriver m_TaskDriver;
        
        

        protected sealed override HashSet<Type> ValidDataStreamTypes
        {
            get => VALID_DATA_STREAM_TYPES;
        }


        public ContextWorkload(AbstractTaskDriver taskDriver, RootWorkload rootWorkload) : base(taskDriver.GetType(), rootWorkload.GoverningSystem)
        {
            m_TaskDriver = taskDriver;
            m_RootWorkload = rootWorkload;
        }

        protected override byte GenerateContext()
        {
            return m_RootWorkload.GenerateContextForContextWorkload();
        }

        protected override void DisposeSelf()
        {
            base.DisposeSelf();
        }

        protected override void InitDataStreamInstance(FieldInfo taskDriverField, Type instanceType, Type genericTypeDefinition)
        {
            AbstractDataStream dataStream = null;
            //If this is a System Data Stream type, we just want to assign the one from the core
            if (RootWorkload.VALID_DATA_STREAM_TYPES.Contains(genericTypeDefinition))
            {
                dataStream = m_RootWorkload.GetDataStreamByType(instanceType);
            }
            else
            {
                dataStream = CreateDataStreamInstance(instanceType, genericTypeDefinition);
            }
            
            AssignDataStreamInstance(taskDriverField, m_TaskDriver, dataStream);
        }
    }
}
