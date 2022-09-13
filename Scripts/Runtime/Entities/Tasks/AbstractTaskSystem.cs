using Anvil.CSharp.Core;
using Anvil.CSharp.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Unity.Collections;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A type of System that runs <see cref="AbstractTaskDriver"/>s during its update phase.
    /// </summary>
    public abstract partial class AbstractTaskSystem<TTaskDriver, TTaskSystem> : AbstractTaskSystem
        where TTaskDriver : AbstractTaskDriver<TTaskDriver, TTaskSystem>
        where TTaskSystem : AbstractTaskSystem<TTaskDriver, TTaskSystem>
    {
        private static readonly Type I_PROXY_DATA_STREAM_TYPE = typeof(IProxyDataStream);

        private readonly List<TTaskDriver> m_TaskDrivers;
        private readonly ByteIDProvider m_TaskDriverIDProvider;

        private readonly Dictionary<IProxyDataStream, ITaskProcessor> m_UpdateTaskProcessorMapping;
        private readonly Dictionary<IProxyDataStream, ITaskProcessor> m_PopulateTaskProcessorMapping;
        private readonly List<ITaskProcessor> m_UpdateTaskProcessors;
        private readonly List<ITaskProcessor> m_PopulateTaskProcessors;
        private readonly List<ITaskProcessor> m_AllTaskProcessors;
        private readonly byte m_SystemLevelContext;

        private NativeArray<JobHandle> m_ProcessorDependenciesScratchPad;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private bool m_HasCheckedDataStreamJobIntegrity;
#endif

        protected AbstractTaskSystem()
        {
            m_TaskDrivers = new List<TTaskDriver>();

            m_TaskDriverIDProvider = new ByteIDProvider();
            m_SystemLevelContext = m_TaskDriverIDProvider.GetNextID();


            m_UpdateTaskProcessorMapping = new Dictionary<IProxyDataStream, ITaskProcessor>();
            m_PopulateTaskProcessorMapping = new Dictionary<IProxyDataStream, ITaskProcessor>();

            m_UpdateTaskProcessors = new List<ITaskProcessor>();
            m_PopulateTaskProcessors = new List<ITaskProcessor>();

            m_AllTaskProcessors = new List<ITaskProcessor>();

            CreateProxyDataStreams();

            //TODO: 3. Custom Update Job Types
            //TODO: Create the custom Update Job so we can parse to the different result channels.
        }

        protected override void OnDestroy()
        {
            foreach (ITaskProcessor taskProcessor in m_AllTaskProcessors)
            {
                taskProcessor.Dispose();
            }

            m_UpdateTaskProcessorMapping.Clear();
            m_PopulateTaskProcessorMapping.Clear();
            m_UpdateTaskProcessors.Clear();
            m_PopulateTaskProcessors.Clear();

            m_TaskDriverIDProvider.Dispose();


            if (m_ProcessorDependenciesScratchPad.IsCreated)
            {
                m_ProcessorDependenciesScratchPad.Dispose();
            }

            //Note: We don't dispose TaskDrivers here because their parent or direct reference will do so. 
            m_TaskDrivers.Clear();

            base.OnDestroy();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            Debug_CheckDataStreamJobsExist();
            BuildOptimizedCollections();
        }

        private void CreateProxyDataStreams()
        {
            Type systemType = GetType();
            //Get all the fields
            FieldInfo[] systemFields = systemType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo systemField in systemFields)
            {
                if (!I_PROXY_DATA_STREAM_TYPE.IsAssignableFrom(systemField.FieldType))
                {
                    continue;
                }

                IgnoreProxyDataStreamAttribute ignoreProxyDataStreamAttribute = systemField.GetCustomAttribute<IgnoreProxyDataStreamAttribute>();
                if (ignoreProxyDataStreamAttribute != null)
                {
                    continue;
                }

                Debug_CheckFieldIsReadOnly(systemField);
                Debug_CheckFieldTypeGenericTypeArguments(systemField.FieldType);

                //Get the data type 
                IProxyDataStream proxyDataStream = CreateProxyDataStream(systemField.FieldType.GenericTypeArguments[0]);

                //Ensure the System's field is set to the data stream
                systemField.SetValue(this, proxyDataStream);
            }
        }

        private IProxyDataStream CreateProxyDataStream(Type proxyDataType)
        {
            IProxyDataStream proxyDataStream = ProxyDataStreamFactory.Create(proxyDataType);
            //Initially null to keep record that this data doesn't have a corresponding Update Job
            m_UpdateTaskProcessorMapping.Add(proxyDataStream, null);
            m_PopulateTaskProcessorMapping.Add(proxyDataStream, null);

            return proxyDataStream;
        }

        //TODO: #39 - Some way to remove the update Job

        internal byte RegisterTaskDriver(TTaskDriver taskDriver)
        {
            Debug_EnsureTaskDriverSystemRelationship(taskDriver);
            m_TaskDrivers.Add(taskDriver);

            return m_TaskDriverIDProvider.GetNextID();
        }

        protected override void OnUpdate()
        {
            Dependency = UpdateTaskDriverSystem(Dependency);
        }

        //TODO: Determine if we need custom configs for job types
        protected IJobConfig ConfigureUpdateJobFor<TInstance>(ProxyDataStream<TInstance> dataStream,
                                                              JobConfig<TInstance>.ScheduleJobDelegate scheduleJobFunction,
                                                              BatchStrategy batchStrategy)
            where TInstance : unmanaged, IProxyInstance
        {
            return ConfigureJobFor(dataStream,
                                   scheduleJobFunction,
                                   batchStrategy,
                                   m_SystemLevelContext,
                                   m_UpdateTaskProcessorMapping);
        }

        //TODO: Determine if we need custom configs for job types
        internal IJobConfig ConfigurePopulateJobFor<TInstance>(ProxyDataStream<TInstance> dataStream,
                                                               JobConfig<TInstance>.ScheduleJobDelegate scheduleJobFunction,
                                                               BatchStrategy batchStrategy,
                                                               TTaskDriver taskDriver)
            where TInstance : unmanaged, IProxyInstance
        {
            return ConfigureJobFor(dataStream,
                                   scheduleJobFunction,
                                   batchStrategy,
                                   taskDriver.Context,
                                   m_PopulateTaskProcessorMapping);
        }


        private IJobConfig ConfigureJobFor<TInstance>(ProxyDataStream<TInstance> dataStream,
                                                      JobConfig<TInstance>.ScheduleJobDelegate scheduleJobFunction,
                                                      BatchStrategy batchStrategy,
                                                      byte context,
                                                      IDictionary<IProxyDataStream, ITaskProcessor> mapping)
            where TInstance : unmanaged, IProxyInstance
        {
            //TODO: Double check error messages make sense depending if this came from a TaskDriver or the System
            Debug_EnsureDataStreamIntegrity(dataStream);
            JobConfig<TInstance> jobConfig = new JobConfig<TInstance>(World,
                                                                      context,
                                                                      scheduleJobFunction,
                                                                      batchStrategy,
                                                                      dataStream);

            TaskProcessor<TInstance, JobConfig<TInstance>> taskProcessor = new TaskProcessor<TInstance, JobConfig<TInstance>>(dataStream, jobConfig);
            //Update the mapping, we now have an update processor for the data stream
            mapping[dataStream] = taskProcessor;

            return jobConfig;
        }


        private void BuildOptimizedCollections()
        {
            m_ProcessorDependenciesScratchPad = new NativeArray<JobHandle>(m_UpdateTaskProcessorMapping.Count, Allocator.Persistent);
            m_UpdateTaskProcessors.AddRange(m_UpdateTaskProcessorMapping.Values);
            m_PopulateTaskProcessors.AddRange(m_PopulateTaskProcessorMapping.Values);

            m_AllTaskProcessors.AddRange(m_UpdateTaskProcessors);
            m_AllTaskProcessors.AddRange(m_PopulateTaskProcessors);
        }

        private JobHandle UpdateTaskDriverSystem(JobHandle dependsOn)
        {
            //TODO: Renable once Task Drivers are enabled
            // //Have drivers be given the chance to add to the Instance Data
            // dependsOn = m_TaskDrivers.BulkScheduleParallel(dependsOn, AbstractTaskDriver.POPULATE_SCHEDULE_DELEGATE);

            //Consolidate our owned data streams to operate on them
            dependsOn = m_UpdateTaskProcessors.BulkScheduleParallel(dependsOn,
                                                                    ref m_ProcessorDependenciesScratchPad,
                                                                    ITaskProcessor.CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION);

            //TODO: #38 - Allow for cancels to occur

            //Allow the update jobs to occur on our owned data streams
            dependsOn = m_UpdateTaskProcessors.BulkScheduleParallel(dependsOn,
                                                                    ref m_ProcessorDependenciesScratchPad,
                                                                    ITaskProcessor.PREPARE_AND_SCHEDULE_FUNCTION);

            //TODO: Renable once Task Drivers are enabled
            // //Have drivers consolidate their result data
            // dependsOn = m_TaskDrivers.BulkScheduleParallel(dependsOn, AbstractTaskDriver.CONSOLIDATE_SCHEDULE_DELEGATE);

            //TODO: #38 - Allow for cancels on the drivers to occur

            //TODO: Renable once Task Drivers are enabled
            // //Have drivers to do their own generic work
            // dependsOn = m_TaskDrivers.BulkScheduleParallel(dependsOn, AbstractTaskDriver.UPDATE_SCHEDULE_DELEGATE);

            //Ensure this system's dependency is written back
            return dependsOn;
        }


        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureTaskDriverSystemRelationship(TTaskDriver taskDriver)
        {
            if (taskDriver.TaskSystem != this)
            {
                throw new InvalidOperationException($"{taskDriver} is part of system {taskDriver.TaskSystem} but it should be {this}!");
            }

            if (m_TaskDrivers.Contains(taskDriver))
            {
                throw new InvalidOperationException($"Trying to add {taskDriver} to {this}'s list of Task Drivers but it is already there!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_CheckFieldIsReadOnly(FieldInfo fieldInfo)
        {
            if (!fieldInfo.IsInitOnly)
            {
                throw new InvalidOperationException($"Field with name {fieldInfo.Name} on {fieldInfo.ReflectedType} is not marked as \"readonly\", please ensure that it is.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_CheckFieldTypeGenericTypeArguments(Type fieldType)
        {
            if (fieldType.GenericTypeArguments.Length != 1)
            {
                throw new InvalidOperationException($"Type {fieldType} is to be used to create a {typeof(ProxyDataStream<>)} but {fieldType} doesn't have the expected 1 generic type!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_CheckDataStreamJobsExist()
        {
            if (m_HasCheckedDataStreamJobIntegrity)
            {
                return;
            }

            m_HasCheckedDataStreamJobIntegrity = true;

            foreach (KeyValuePair<IProxyDataStream, ITaskProcessor> entry in m_UpdateTaskProcessorMapping)
            {
                if (entry.Value == null)
                {
                    throw new InvalidOperationException($"Data Stream of {entry.Key} exists but {nameof(ConfigureJobFor)} wasn't called in {nameof(OnCreate)}! Please ensure all {typeof(ProxyDataStream<>)} have a corresponding job configured via {nameof(ConfigureJobFor)} to ensure there is no data loss.");
                }
            }

            foreach (KeyValuePair<IProxyDataStream, ITaskProcessor> entry in m_PopulateTaskProcessorMapping)
            {
                if (entry.Value == null)
                {
                    throw new InvalidOperationException($"Data Stream of {entry.Key} exists but there are no populate jobs that exist. This data will never be written to.");
                }
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureDataStreamIntegrity(IProxyDataStream dataStream)
        {
            if (dataStream == null)
            {
                throw new InvalidOperationException($"Data Stream is null! Possible causes: "
                                                  + $"\n1. The incorrect reference to a {typeof(ProxyDataStream<>)} was passed in such as referencing a hidden variable or something not defined on this class. {typeof(ProxyDataStream<>)}'s are created via reflection in the constructor of this class."
                                                  + $"\n2. The {nameof(ConfigureJobFor)} function wasn't called from {nameof(OnCreate)}. The reflection to create {typeof(ProxyDataStream<>)}'s hasn't happened yet.");
            }

            //TODO: We're just using the update mapping, but maybe we should have a separate hashset or check all?
            if (!m_UpdateTaskProcessorMapping.ContainsKey(dataStream))
            {
                throw new InvalidOperationException($"DataStream of {dataStream.GetType()} was not registered to this class. Was it defined as a part of this class?");
            }
        }
    }
    
    //TODO: Might be able to get rid of this
    public abstract class AbstractTaskSystem : AbstractAnvilSystemBase
    {
        
    }
}
