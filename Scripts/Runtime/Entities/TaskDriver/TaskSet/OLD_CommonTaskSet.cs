// using Anvil.CSharp.Data;
// using System;
// using System.Collections.Generic;
// using System.Reflection;
// using Unity.Entities;
// using Unity.Jobs;
//
// namespace Anvil.Unity.DOTS.Entities.Tasks
// {
//     internal class CommonTaskSet : AbstractTaskSet
//     {
//         private readonly ByteIDProvider m_IDProvider;
//         private readonly List<TaskSet> m_ContextTaskSets;
//         private readonly Dictionary<Type, AbstractDataStream> m_DataStreamLookupByType;
//
//         private Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>> m_SystemJobConfigBulkJobSchedulerLookup;
//         private Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>> m_DriverJobConfigBulkJobSchedulerLookup;
//         
//
//         public CommonTaskSet(World world, Type taskDriverType, AbstractTaskDriverSystem governingSystem) : base(world, taskDriverType, governingSystem)
//         {
//             m_IDProvider = new ByteIDProvider();
//             m_ContextTaskSets = new List<TaskSet>();
//             m_DataStreamLookupByType = new Dictionary<Type, AbstractDataStream>();
//         }
//
//         protected sealed override byte GenerateContext()
//         {
//             return m_IDProvider.GetNextID();
//         }
//
//         protected override void DisposeSelf()
//         {
//             m_IDProvider.Dispose();
//             //We don't own these so we don't dispose them
//             m_ContextTaskSets.Clear();
//
//             //Disposed by the base
//             m_DataStreamLookupByType.Clear();
//             base.DisposeSelf();
//         }
//
//         public TaskSet CreateTaskSet(AbstractTaskDriver taskDriver, TaskSet parent)
//         {
//             TaskSet taskSet = new TaskSet(taskDriver,
//                                           this,
//                                           parent);
//             m_ContextTaskSets.Add(taskSet);
//             return taskSet;
//         }
//
//         public byte GenerateContextForContextWorkload()
//         {
//             return GenerateContext();
//         }
//
//         public AbstractDataStream GetDataStreamByType(Type instanceType)
//         {
//             return m_DataStreamLookupByType[instanceType];
//         }
//
//         protected override void InitDataStreamInstance(FieldInfo taskDriverField, Type instanceType, Type genericTypeDefinition)
//         {
//             //Since this is the core, we only create the DataStream and hold onto it, there's nothing to assign
//             AbstractDataStream dataStream = CreateDataStreamInstance(instanceType, genericTypeDefinition);
//             m_DataStreamLookupByType.Add(instanceType, dataStream);
//         }
//
//         //*************************************************************************************************************
//         // EXECUTION
//         //*************************************************************************************************************
//
//         public JobHandle Update(JobHandle dependsOn)
//         {
//             //TODO: #102 - I think all jobs call all be run in parallel because they don't conflict...
//             //Run all TaskDriver populate jobs to allow them to write to data streams (TaskDrivers -> generic TaskSystem data)
//             dependsOn = ScheduleJobs(dependsOn,
//                                      TaskFlowRoute.Populate,
//                                      m_DriverJobConfigBulkJobSchedulerLookup);
//
//             //Schedule the Update Jobs to run on System Data, we are guaranteed to have up to date Cancel Requests
//             dependsOn = ScheduleJobs(dependsOn,
//                                      TaskFlowRoute.Update,
//                                      m_SystemJobConfigBulkJobSchedulerLookup);
//
//             //Schedule the Cancel Jobs to run on System Data, we are guaranteed to have Cancelled instances now if they were requested
//             dependsOn = ScheduleJobs(dependsOn,
//                                      TaskFlowRoute.Cancel,
//                                      m_SystemJobConfigBulkJobSchedulerLookup);
//
//             //TODO: #72 - Allow for other phases as needed, try to make as parallel as possible
//
//             // Have drivers to do their own generic work if necessary
//             dependsOn = ScheduleJobs(dependsOn,
//                                      TaskFlowRoute.Update,
//                                      m_DriverJobConfigBulkJobSchedulerLookup);
//
//             // Have drivers do their own cancel work if necessary
//             dependsOn = ScheduleJobs(dependsOn,
//                                      TaskFlowRoute.Cancel,
//                                      m_DriverJobConfigBulkJobSchedulerLookup);
//
//             //Ensure this system's dependency is written back
//             return dependsOn;
//         }
//         
//         private JobHandle ScheduleJobs(JobHandle dependsOn,
//                                        TaskFlowRoute route,
//                                        Dictionary<TaskFlowRoute, BulkJobScheduler<AbstractJobConfig>> lookup)
//         {
//             BulkJobScheduler<AbstractJobConfig> scheduler = lookup[route];
//             return scheduler.Schedule(dependsOn,
//                                       AbstractJobConfig.PREPARE_AND_SCHEDULE_FUNCTION);
//         }
//
//         //*************************************************************************************************************
//         // JOB CONFIGURATION
//         //*************************************************************************************************************
//
//         public IResolvableJobConfigRequirements ConfigureJobToUpdate<TInstance>(ICommonDataStream<TInstance> dataStream,
//                                                                                 JobConfigScheduleDelegates.ScheduleUpdateJobDelegate<TInstance> scheduleJobFunction,
//                                                                                 BatchStrategy batchStrategy)
//             where TInstance : unmanaged, IEntityProxyInstance
//         {
//             //TODO: Ensure we own this dataStream
//             UpdateJobConfig<TInstance> jobConfig = JobConfigFactory.CreateUpdateJobConfig(m_TaskFlowGraph,
//                                                                                           this,
//                                                                                           (DataStream<TInstance>)dataStream,
//                                                                                           scheduleJobFunction,
//                                                                                           batchStrategy);
//             RegisterJob(jobConfig, TaskFlowRoute.Update);
//
//             return jobConfig;
//         }
//         
//         public IResolvableJobConfigRequirements ConfigureJobToCancel<TInstance>(ICommonCancellableDataStream<TInstance> dataStream,
//                                                                                    JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction,
//                                                                                    BatchStrategy batchStrategy)
//             where TInstance : unmanaged, IEntityProxyInstance
//         {
//             //TODO: Ensure we own this dataStream
//             CancelJobConfig<TInstance> jobConfig = JobConfigFactory.CreateCancelJobConfig(m_TaskFlowGraph,
//                                                                                           this,
//                                                                                           (CancellableDataStream<TInstance>)dataStream,
//                                                                                           scheduleJobFunction,
//                                                                                           batchStrategy);
//
//             RegisterJob(jobConfig, TaskFlowRoute.Cancel);
//
//             return jobConfig;
//         }
//
//         
//     }
// }
