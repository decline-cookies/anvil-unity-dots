using Anvil.CSharp.Data;
using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class RootWorkload : AbstractWorkload,
                                  IRootWorkload
    {
        public static readonly HashSet<Type> VALID_DATA_STREAM_TYPES = new HashSet<Type>()
        {
            I_SYSTEM_DATA_STREAM, I_SYSTEM_CANCELLABLE_DATA_STREAM, I_SYSTEM_CANCEL_RESULT_DATA_STREAM
        };

        private readonly ByteIDProvider m_IDProvider;
        private readonly List<AbstractJobConfig> m_JobConfigs;
        private readonly List<ContextWorkload> m_ContextWorkloads;
        private readonly Dictionary<Type, AbstractDataStream> m_DataStreamLookupByType;

        private Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>> m_SystemJobConfigBulkJobSchedulerLookup;
        private Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>> m_DriverJobConfigBulkJobSchedulerLookup;
        

        protected sealed override HashSet<Type> ValidDataStreamTypes
        {
            get => VALID_DATA_STREAM_TYPES;
        }

        public RootWorkload(Type taskDriverType, AbstractTaskDriverSystem governingSystem) : base(taskDriverType, governingSystem)
        {
            m_IDProvider = new ByteIDProvider();
            m_ContextWorkloads = new List<ContextWorkload>();
            m_DataStreamLookupByType = new Dictionary<Type, AbstractDataStream>();
        }

        protected sealed override byte GenerateContext()
        {
            return m_IDProvider.GetNextID();
        }

        protected override void DisposeSelf()
        {
            m_IDProvider.Dispose();
            //We don't own these so we don't dispose them
            m_ContextWorkloads.Clear();

            //Disposed by the base
            m_DataStreamLookupByType.Clear();
            base.DisposeSelf();
        }

        public ContextWorkload CreateContextWorkload(AbstractTaskDriver taskDriver)
        {
            ContextWorkload contextWorkload = new ContextWorkload(taskDriver,
                                                                  this);
            m_ContextWorkloads.Add(contextWorkload);
            return contextWorkload;
        }

        public byte GenerateContextForContextWorkload()
        {
            return GenerateContext();
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
        
        //*************************************************************************************************************
        // EXECUTION
        //*************************************************************************************************************

        public JobHandle Update(JobHandle dependsOn)
        {
            //TODO: IMPLEMENT
            return dependsOn;
        }

        //*************************************************************************************************************
        // JOB CONFIGURATION
        //*************************************************************************************************************

        public IResolvableJobConfigRequirements ConfigureJobToUpdate<TInstance>(ISystemDataStream<TInstance> dataStream,
                                                                                JobConfigScheduleDelegates.ScheduleUpdateJobDelegate<TInstance> scheduleJobFunction,
                                                                                BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            UpdateJobConfig<TInstance> jobConfig = JobConfigFactory.CreateUpdateJobConfig(m_TaskFlowGraph,
                                                                                          this,
                                                                                          null,
                                                                                          (DataStream<TInstance>)dataStream,
                                                                                          scheduleJobFunction,
                                                                                          batchStrategy);
            RegisterJob(null, jobConfig, TaskFlowRoute.Update);

            return jobConfig;
        }

        private void RegisterJob(AbstractTaskDriver taskDriver,
                                 AbstractJobConfig jobConfig,
                                 TaskFlowRoute route)
        {
            Debug_EnsureNotHardened(route, taskDriver);
            m_TaskFlowGraph.RegisterJobConfig(jobConfig, route);
            if (taskDriver != null)
            {
                taskDriver.AddToJobConfigs(jobConfig);
            }
            else
            {
                m_JobConfigs.Add(jobConfig);
            }
        }
    }
}
