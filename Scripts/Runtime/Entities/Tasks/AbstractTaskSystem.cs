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
    public abstract partial class AbstractTaskSystem : AbstractAnvilSystemBase,
                                                       ITaskSystem
    {
        private static readonly Type I_PROXY_DATA_STREAM_TYPE = typeof(IProxyDataStream);

        //TODO: Enable TaskDrivers again, a Task System can only have one type of TaskDriver since it governs them
        // private readonly List<TTaskDriver> m_TaskDrivers;
        private readonly ByteIDProvider m_TaskDriverIDProvider;
        private readonly HashSet<IProxyDataStream> m_PendingDataStreams;
        private readonly List<ISystemTaskProcessor> m_SystemTaskProcessors;
        private readonly byte m_SystemLevelContext;

        private NativeArray<JobHandle> m_ProcessorDependenciesScratchPad;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private bool m_HasCheckedDataStreamUpdateJobsIntegrity;
#endif

        protected AbstractTaskSystem()
        {
            // m_TaskDrivers = new List<TTaskDriver>();

            m_TaskDriverIDProvider = new ByteIDProvider();
            m_SystemLevelContext = m_TaskDriverIDProvider.GetNextID();

            m_PendingDataStreams = new HashSet<IProxyDataStream>();
            m_SystemTaskProcessors = new List<ISystemTaskProcessor>();

            CreateProxyDataStreams();

            //TODO: 2. Enable TaskDrivers
            //TODO: Task Drivers will hook into the Systems and run their own Tasks to populate.
            //TODO: 3. Custom Update Job Types
            //TODO: Create the custom Update Job so we can parse to the different result channels.
        }

        protected override void OnDestroy()
        {
            foreach (ISystemTaskProcessor systemTaskProcessor in m_SystemTaskProcessors)
            {
                systemTaskProcessor.Dispose();
            }
            m_SystemTaskProcessors.Clear();
            m_PendingDataStreams.Clear();

            m_TaskDriverIDProvider.Dispose();

            m_ProcessorDependenciesScratchPad.Dispose();

            //Note: We don't dispose TaskDrivers here because their parent or direct reference will do so. 
            // m_TaskDrivers.Clear();

            base.OnDestroy();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            Debug_CheckDataStreamUpdateJobsExist();
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

            m_ProcessorDependenciesScratchPad = new NativeArray<JobHandle>(m_PendingDataStreams.Count, Allocator.Persistent);
        }

        private IProxyDataStream CreateProxyDataStream(Type proxyDataType)
        {
            IProxyDataStream proxyDataStream = ProxyDataStreamFactory.Create(proxyDataType);
            m_PendingDataStreams.Add(proxyDataStream);

            return proxyDataStream;
        }

        //TODO: #39 - Some way to remove the update Job

        //TODO: RE-ENABLE IF NEEDED
        // internal byte RegisterTaskDriver(TTaskDriver taskDriver)
        // {
        //     Debug_EnsureTaskDriverSystemRelationship(taskDriver);
        //     m_TaskDrivers.Add(taskDriver);
        //     
        //     return m_TaskDriverIDProvider.GetNextID();
        // }

        protected override void OnUpdate()
        {
            Dependency = UpdateTaskDriverSystem(Dependency);
        }

        protected IUpdateJobConfig ConfigureUpdateJobFor<TInstance>(ProxyDataStream<TInstance> dataStream, UpdateJobConfig<TInstance>.ScheduleJobDelegate scheduleJobFunction, BatchStrategy batchStrategy)
            where TInstance : unmanaged, IProxyInstance
        {
            Debug_EnsureDataStreamIntegrity(dataStream);

            UpdateJobConfig<TInstance> updateJobConfig = new UpdateJobConfig<TInstance>(World, m_SystemLevelContext, scheduleJobFunction, batchStrategy, dataStream);
            
            SystemTaskProcessor<TInstance> systemTaskProcessor = new SystemTaskProcessor<TInstance>(dataStream, updateJobConfig);
            m_SystemTaskProcessors.Add(systemTaskProcessor);
            m_PendingDataStreams.Remove(dataStream);
            
            return updateJobConfig;
        }

        private JobHandle UpdateTaskDriverSystem(JobHandle dependsOn)
        {
            //TODO: Renable once Task Drivers are enabled
            // //Have drivers be given the chance to add to the Instance Data
            // dependsOn = m_TaskDrivers.BulkScheduleParallel(dependsOn, AbstractTaskDriver.POPULATE_SCHEDULE_DELEGATE);

            //Consolidate our owned data streams to operate on them
            dependsOn = m_SystemTaskProcessors.BulkScheduleParallel(dependsOn, 
                                                                    ref m_ProcessorDependenciesScratchPad, 
                                                                    ISystemTaskProcessor.CONSOLIDATE_FOR_FRAME_SCHEDULE_FUNCTION);

            //TODO: #38 - Allow for cancels to occur

            //Allow the update jobs to occur on our owned data streams
            dependsOn = m_SystemTaskProcessors.BulkScheduleParallel(dependsOn, 
                                                                    ref m_ProcessorDependenciesScratchPad, 
                                                                    ISystemTaskProcessor.PREPARE_AND_SCHEDULE_FUNCTION);

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

        //TODO: RENABLE IF NEEDED
        // [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        // private void Debug_EnsureTaskDriverSystemRelationship(TTaskDriver taskDriver)
        // {
        //     if (taskDriver.System != this)
        //     {
        //         throw new InvalidOperationException($"{taskDriver} is part of system {taskDriver.System} but it should be {this}!");
        //     }
        //
        //     if (m_TaskDrivers.Contains(taskDriver))
        //     {
        //         throw new InvalidOperationException($"Trying to add {taskDriver} to {this}'s list of Task Drivers but it is already there!");
        //     }
        // }

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
        private void Debug_CheckDataStreamUpdateJobsExist()
        {
            if (m_HasCheckedDataStreamUpdateJobsIntegrity)
            {
                return;
            }

            m_HasCheckedDataStreamUpdateJobsIntegrity = true;

            foreach (IProxyDataStream remainingDataStream in m_PendingDataStreams)
            {
                throw new InvalidOperationException($"Data Stream of {remainingDataStream} exists but {nameof(ConfigureUpdateJobFor)} wasn't called in {nameof(OnCreate)}! Please ensure all {typeof(ProxyDataStream<>)} have a corresponding job configured via {nameof(ConfigureUpdateJobFor)} to ensure there is no data loss.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureDataStreamIntegrity(IProxyDataStream dataStream)
        {
            if (dataStream == null)
            {
                throw new InvalidOperationException($"Data Stream is null! Possible causes: "
                                                  + $"\n1. The incorrect reference to a {typeof(ProxyDataStream<>)} was passed in such as referencing a hidden variable or something not defined on this class. {typeof(ProxyDataStream<>)}'s are created via reflection in the constructor of this class."
                                                  + $"\n2. The {nameof(ConfigureUpdateJobFor)} function wasn't called from {nameof(OnCreate)}. The reflection to create {typeof(ProxyDataStream<>)}'s hasn't happened yet.");
            }

            if (!m_PendingDataStreams.Contains(dataStream))
            {
                throw new InvalidOperationException($"DataStream of {dataStream.GetType()} was not registered to this class. Was it defined as a part of this class?");
            }
        }
    }
}
