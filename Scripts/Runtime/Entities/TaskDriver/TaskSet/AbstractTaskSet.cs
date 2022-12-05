using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractTaskSet : AbstractAnvilBase
    {
        public readonly List<AbstractDataStream> DataStreams;
        public readonly List<AbstractDataStream> CancellableDataStreams;
        public readonly List<AbstractDataStream> CancelResultDataStreams;
        public readonly CancelRequestDataStream CancelRequestDataStream;
        public readonly CancelCompleteDataStream CancelCompleteDataStream;
        public readonly AccessControlledValue<UnsafeParallelHashMap<EntityProxyInstanceID, bool>> CancelProgressLookup;

        public readonly List<AbstractDataStream> AllPublicDataStreams;

        private readonly List<AbstractJobConfig> m_JobConfigs;

        private bool m_IsHardened;


        public CommonTaskSet CommonTaskSet { get; }
        public AbstractTaskDriverSystem GoverningSystem { get; }
        public bool HasCancellableData { get; }
        public byte Context { get; }
        public World World { get; }

        protected AbstractTaskSet(World world, Type taskDriverType, AbstractTaskDriverSystem governingSystem)
        {
            World = world;
            Context = GenerateContext();
            GoverningSystem = governingSystem;
            CommonTaskSet = governingSystem.CommonTaskSet;

            m_JobConfigs = new List<AbstractJobConfig>();

            AllPublicDataStreams = new List<AbstractDataStream>();

            DataStreams = new List<AbstractDataStream>();
            CancellableDataStreams = new List<AbstractDataStream>();
            CancelResultDataStreams = new List<AbstractDataStream>();
            CancelProgressLookup = new AccessControlledValue<UnsafeParallelHashMap<EntityProxyInstanceID, bool>>(new UnsafeParallelHashMap<EntityProxyInstanceID, bool>(ChunkUtil.MaxElementsPerChunk<EntityProxyInstanceID>(),
                                                                                                                                                                        Allocator.Persistent));
            CancelCompleteDataStream = new CancelCompleteDataStream(this);
            CancelRequestDataStream = new CancelRequestDataStream(CancelProgressLookup,
                                                                  CancelCompleteDataStream,
                                                                  this);

            CreateDataStreams(taskDriverType);

            HasCancellableData = InitHasCancellableData();
        }

        protected abstract byte GenerateContext();

        protected virtual bool InitHasCancellableData()
        {
            return CancellableDataStreams.Count > 0;
        }

        protected override void DisposeSelf()
        {
            //Dispose all the data we own
            m_JobConfigs.DisposeAllAndTryClear();

            //TODO: #104 - Should this get baked into AccessControlledValue's Dispose method?
            CancelProgressLookup.Acquire(AccessType.Disposal);
            CancelProgressLookup.Dispose();

            CancelRequestDataStream.Dispose();
            CancelCompleteDataStream.Dispose();

            AllPublicDataStreams.DisposeAllAndTryClear();

            DataStreams.Clear();
            CancellableDataStreams.Clear();
            CancelResultDataStreams.Clear();

            //Clean up all the cached native arrays hidden in the schedulers
            // m_SystemJobConfigBulkJobSchedulerLookup?.DisposeAllValuesAndClear();
            // m_DriverJobConfigBulkJobSchedulerLookup?.DisposeAllValuesAndClear();

            base.DisposeSelf();
        }

        public AbstractDataStream CreateDataStream<TInstance>()
            where TInstance : unmanaged, IEntityProxyInstance
        {
            DataStream<TInstance> dataStream = new DataStream<TInstance>(CancelRequestDataStream, this);
            DataStreams.Add(dataStream);
            AllPublicDataStreams.Add(dataStream);
            
            return dataStream;
        }
        
        public AbstractDataStream CreateCancellableDataStream<TInstance>()
            where TInstance : unmanaged, IEntityProxyInstance
        {
            CancellableDataStream<TInstance> dataStream = new CancellableDataStream<TInstance>(CancelRequestDataStream, this);
            CancellableDataStreams.Add(dataStream);
            AllPublicDataStreams.Add(dataStream);
            
            return dataStream;
        }
        
        public AbstractDataStream CreateCancelResultDataStream<TInstance>()
            where TInstance : unmanaged, IEntityProxyInstance
        {
            CancelResultDataStream<TInstance> dataStream = new CancelResultDataStream<TInstance>(this);
            CancelResultDataStreams.Add(dataStream);
            AllPublicDataStreams.Add(dataStream);
            
            return dataStream;
        }
        

        protected void AssignDataStreamInstance(FieldInfo field, AbstractTaskDriver taskDriver, AbstractDataStream dataStream)
        {
            Debug_EnsureFieldNotSet(field, taskDriver);
            field.SetValue(taskDriver, dataStream);
        }

        

        
        
        public override string ToString()
        {
            //TODO: Implement based on TaskDriver and what data we are. 
            return base.ToString();
        }

        internal void Harden()
        {
            Debug_EnsureNotHardened();
            m_IsHardened = true;

            //TODO: Call abstract Harden
            //We want to harden any child taskDrivers
            //We want to create the bulk job schedulers
            // foreach (AbstractTaskDriver taskDriver in TaskDrivers)
            // {
            //     taskDriver.Harden();
            // }
            // m_SystemJobConfigBulkJobSchedulerLookup = m_TaskFlowGraph.CreateJobConfigBulkJobSchedulerLookupFor(this);
            // m_DriverJobConfigBulkJobSchedulerLookup = m_TaskFlowGraph.CreateJobConfigBulkJobSchedulerLookupFor(this, TaskDrivers);

            foreach (AbstractJobConfig jobConfig in m_JobConfigs)
            {
                jobConfig.Harden();
            }
        }

        //*************************************************************************************************************
        // JOB CONFIGURATION
        //*************************************************************************************************************

        public IJobConfigRequirements ConfigureJobTriggeredBy(EntityQuery entityQuery,
                                                              JobConfigScheduleDelegates.ScheduleEntityQueryJobDelegate scheduleJobFunction,
                                                              BatchStrategy batchStrategy)
        {
            EntityQueryJobConfig jobConfig = JobConfigFactory.CreateEntityQueryJobConfig(m_TaskFlowGraph,
                                                                                         this,
                                                                                         entityQuery,
                                                                                         scheduleJobFunction,
                                                                                         batchStrategy);
            RegisterJob(jobConfig, TaskFlowRoute.Populate);

            return jobConfig;
        }

        public IJobConfigRequirements ConfigureJobTriggeredBy<TInstance>(DataStream<TInstance> dataStream,
                                                                         JobConfigScheduleDelegates.ScheduleDataStreamJobDelegate<TInstance> scheduleJobFunction,
                                                                         BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            DataStreamJobConfig<TInstance> jobConfig = JobConfigFactory.CreateDataStreamJobConfig(m_TaskFlowGraph,
                                                                                                  this,
                                                                                                  dataStream,
                                                                                                  scheduleJobFunction,
                                                                                                  batchStrategy);
            RegisterJob(jobConfig, TaskFlowRoute.Populate);

            return jobConfig;
        }

        public IJobConfigRequirements ConfigureJobWhenCancelComplete(CancelCompleteDataStream cancelCompleteDataStream,
                                                                     JobConfigScheduleDelegates.ScheduleCancelCompleteJobDelegate scheduleJobFunction,
                                                                     BatchStrategy batchStrategy)
        {
            CancelCompleteJobConfig jobConfig = JobConfigFactory.CreateCancelCompleteJobConfig(m_TaskFlowGraph,
                                                                                               this,
                                                                                               cancelCompleteDataStream,
                                                                                               scheduleJobFunction,
                                                                                               batchStrategy);
            RegisterJob(jobConfig, TaskFlowRoute.Populate);

            return jobConfig;
        }

        public IResolvableJobConfigRequirements ConfigureCancelJobFor<TInstance>(CancellableDataStream<TInstance> dataStream,
                                                                                 JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction,
                                                                                 BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            CancelJobConfig<TInstance> jobConfig = JobConfigFactory.CreateCancelJobConfig(m_TaskFlowGraph,
                                                                                          this,
                                                                                          dataStream,
                                                                                          scheduleJobFunction,
                                                                                          batchStrategy);

            RegisterJob(jobConfig, TaskFlowRoute.Cancel);

            return jobConfig;
        }

        protected void RegisterJob(AbstractJobConfig jobConfig,
                                   TaskFlowRoute route)
        {
            Debug_EnsureNotHardened(route);
            m_TaskFlowGraph.RegisterJobConfig(jobConfig, route);
            m_JobConfigs.Add(jobConfig);
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNotHardened()
        {
            if (m_IsHardened)
            {
                throw new InvalidOperationException($"Trying to Harden {this} but we already are!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNotHardened(TaskFlowRoute route)
        {
            if (m_IsHardened)
            {
                throw new InvalidOperationException($"Trying to create a {route} job on {this} but we're already hardened! Please ensure that you configure your jobs in the constructor!");
            }
        }
    }
}
