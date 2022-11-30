using Anvil.CSharp.Data;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class CoreTaskDriverWork : AbstractTaskDriverWork
    {
        public static readonly HashSet<Type> VALID_DATA_STREAM_TYPES = new HashSet<Type>()
        {
            I_SYSTEM_DATA_STREAM, 
            I_SYSTEM_CANCELLABLE_DATA_STREAM, 
            I_SYSTEM_CANCEL_RESULT_DATA_STREAM
        };
        
        private readonly ByteIDProvider m_IDProvider;
        private readonly List<AbstractJobConfig> m_JobConfigs;
        private readonly List<ContextualTaskDriverWork> m_ContextualTaskDriverWork;
        private readonly Dictionary<Type, AbstractDataStream> m_DataStreamLookupByType;

        private Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>> m_SystemJobConfigBulkJobSchedulerLookup;
        private Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>> m_DriverJobConfigBulkJobSchedulerLookup;
        
        
        public byte Context { get; }

        protected sealed override HashSet<Type> ValidDataStreamTypes
        {
            get => VALID_DATA_STREAM_TYPES;
        }

        public CoreTaskDriverWork(Type taskDriverType) : base(taskDriverType)
        {
            m_IDProvider = new ByteIDProvider();
            m_ContextualTaskDriverWork = new List<ContextualTaskDriverWork>();
            m_DataStreamLookupByType = new Dictionary<Type, AbstractDataStream>();
            
            Context = m_IDProvider.GetNextID();
        }

        protected override void DisposeSelf()
        {
            m_IDProvider.Dispose();
            //We don't own these so we don't dispose them
            m_ContextualTaskDriverWork.Clear();
            
            //Disposed by the base
            m_DataStreamLookupByType.Clear();
            base.DisposeSelf();
        }

        public ContextualTaskDriverWork CreateContextualTaskDriverWork(AbstractTaskDriver taskDriver)
        {
            ContextualTaskDriverWork contextualTaskDriverWork = new ContextualTaskDriverWork(taskDriver, 
                                                                                             m_IDProvider.GetNextID(),
                                                                                             this);
            m_ContextualTaskDriverWork.Add(contextualTaskDriverWork);
            return contextualTaskDriverWork;
        }

        public AbstractDataStream GetDataStreamByType(Type instanceType)
        {
            return m_DataStreamLookupByType[instanceType];
        }

        protected override void InitDataStreamInstance(FieldInfo taskDriverField, Type instanceType, Type genericTypeDefinition)
        {
            //Since this is the core, we only create the DataStream and hold onto it, there's nothing to assign
            AbstractDataStream dataStream = CreateDataStreamInstance(instanceType, genericTypeDefinition);
            m_DataStreamLookupByType.Add(instanceType, dataStream);
        }
    }
}
