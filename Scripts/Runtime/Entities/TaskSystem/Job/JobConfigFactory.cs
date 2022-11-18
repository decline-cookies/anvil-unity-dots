using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// <see cref="AbstractJobConfig"/>s, <see cref="AbstractJobData"/>s, and <see cref="AbstractScheduleInfo"/>
    /// all work together and are somewhat interdependent on each other.
    /// These functions serve to construct the matching pairs and stitch together relationships in a nice and easy
    /// manner.
    /// </summary>
    internal static class JobConfigFactory
    {
        public static CancelCompleteJobConfig CreateCancelCompleteJobConfig(TaskFlowGraph taskFlowGraph,
                                                                            AbstractTaskSystem taskSystem,
                                                                            AbstractTaskDriver taskDriver,
                                                                            CancelCompleteDataStream cancelCompleteDataStream,
                                                                            JobConfigScheduleDelegates.ScheduleCancelCompleteJobDelegate scheduleJobFunction,
                                                                            BatchStrategy batchStrategy)
        {
            CancelCompleteJobConfig jobConfig = new CancelCompleteJobConfig(taskFlowGraph,
                                                                            taskSystem,
                                                                            taskDriver,
                                                                            cancelCompleteDataStream);

            CancelCompleteJobData jobData = new CancelCompleteJobData(jobConfig,
                                                                      taskSystem.World);

            CancelCompleteScheduleInfo scheduleInfo = new CancelCompleteScheduleInfo(jobData,
                                                                                     cancelCompleteDataStream,
                                                                                     batchStrategy,
                                                                                     scheduleJobFunction);

            return FinalizeJobConfig(jobConfig, scheduleInfo);
        }

        public static UpdateJobConfig<TInstance> CreateUpdateJobConfig<TInstance>(TaskFlowGraph taskFlowGraph,
                                                                                  AbstractTaskSystem taskSystem,
                                                                                  AbstractTaskDriver taskDriver,
                                                                                  DataStream<TInstance> dataStream,
                                                                                  JobConfigScheduleDelegates.ScheduleUpdateJobDelegate<TInstance> scheduleJobFunction,
                                                                                  BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            UpdateJobConfig<TInstance> jobConfig = new UpdateJobConfig<TInstance>(taskFlowGraph,
                                                                                  taskSystem,
                                                                                  taskDriver,
                                                                                  dataStream);

            UpdateJobData<TInstance> jobData = new UpdateJobData<TInstance>(jobConfig,
                                                                            taskSystem.World);

            UpdateDataStreamScheduleInfo<TInstance> scheduleInfo = new UpdateDataStreamScheduleInfo<TInstance>(jobData,
                                                                                                               dataStream,
                                                                                                               batchStrategy,
                                                                                                               scheduleJobFunction);

            return FinalizeJobConfig(jobConfig, scheduleInfo);
        }

        public static CancelJobConfig<TInstance> CreateCancelJobConfig<TInstance>(TaskFlowGraph taskFlowGraph,
                                                                                  AbstractTaskSystem taskSystem,
                                                                                  AbstractTaskDriver taskDriver,
                                                                                  CancellableDataStream<TInstance> dataStream,
                                                                                  JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction,
                                                                                  BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            CancelJobConfig<TInstance> jobConfig = new CancelJobConfig<TInstance>(taskFlowGraph,
                                                                                  taskSystem,
                                                                                  taskDriver,
                                                                                  dataStream.PendingCancelDataStream);

            CancelJobData<TInstance> jobData = new CancelJobData<TInstance>(jobConfig,
                                                                            taskSystem.World);

            CancelDataStreamScheduleInfo<TInstance> scheduleInfo = new CancelDataStreamScheduleInfo<TInstance>(jobData,
                                                                                                               dataStream.PendingCancelDataStream,
                                                                                                               batchStrategy,
                                                                                                               scheduleJobFunction);
            return FinalizeJobConfig(jobConfig, scheduleInfo);
        }

        public static DataStreamJobConfig<TInstance> CreateDataStreamJobConfig<TInstance>(TaskFlowGraph taskFlowGraph,
                                                                                          AbstractTaskSystem taskSystem,
                                                                                          AbstractTaskDriver taskDriver,
                                                                                          DataStream<TInstance> dataStream,
                                                                                          JobConfigScheduleDelegates.ScheduleDataStreamJobDelegate<TInstance> scheduleJobFunction,
                                                                                          BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            DataStreamJobConfig<TInstance> jobConfig = new DataStreamJobConfig<TInstance>(taskFlowGraph,
                                                                                          taskSystem,
                                                                                          taskDriver,
                                                                                          dataStream);

            DataStreamJobData<TInstance> jobData = new DataStreamJobData<TInstance>(jobConfig,
                                                                                    taskSystem.World);

            DataStreamScheduleInfo<TInstance> scheduleInfo = new DataStreamScheduleInfo<TInstance>(jobData,
                                                                                                   dataStream,
                                                                                                   batchStrategy,
                                                                                                   scheduleJobFunction);

            return FinalizeJobConfig(jobConfig, scheduleInfo);
        }

        public static EntityQueryJobConfig CreateEntityQueryJobConfig(TaskFlowGraph taskFlowGraph,
                                                                      AbstractTaskSystem taskSystem,
                                                                      AbstractTaskDriver taskDriver,
                                                                      EntityQuery entityQuery,
                                                                      JobConfigScheduleDelegates.ScheduleEntityQueryJobDelegate scheduleJobFunction,
                                                                      BatchStrategy batchStrategy)
        {
            EntityQueryNativeArray entityQueryNativeArray = new EntityQueryNativeArray(entityQuery);

            EntityQueryJobConfig jobConfig = new EntityQueryJobConfig(taskFlowGraph,
                                                                      taskSystem,
                                                                      taskDriver,
                                                                      entityQueryNativeArray);

            EntityQueryJobData jobData = new EntityQueryJobData(jobConfig,
                                                                taskSystem.World);

            EntityQueryScheduleInfo scheduleInfo = new EntityQueryScheduleInfo(jobData,
                                                                               entityQueryNativeArray,
                                                                               batchStrategy,
                                                                               scheduleJobFunction);

            return FinalizeJobConfig(jobConfig, scheduleInfo);
        }

        public static EntityQueryComponentJobConfig<T> CreateEntityQueryComponentJobConfig<T>(TaskFlowGraph taskFlowGraph,
                                                                                              AbstractTaskSystem taskSystem,
                                                                                              AbstractTaskDriver taskDriver,
                                                                                              EntityQuery entityQuery,
                                                                                              JobConfigScheduleDelegates.ScheduleEntityQueryComponentJobDelegate<T> scheduleJobFunction,
                                                                                              BatchStrategy batchStrategy)
            where T : struct, IComponentData
        {
            EntityQueryComponentNativeArray<T> entityQueryComponentNativeArray = new EntityQueryComponentNativeArray<T>(entityQuery);

            EntityQueryComponentJobConfig<T> jobConfig = new EntityQueryComponentJobConfig<T>(taskFlowGraph,
                                                                                              taskSystem,
                                                                                              taskDriver,
                                                                                              entityQueryComponentNativeArray);

            EntityQueryComponentJobData<T> jobData = new EntityQueryComponentJobData<T>(jobConfig,
                                                                                        taskSystem.World);

            EntityQueryComponentScheduleInfo<T> scheduleInfo = new EntityQueryComponentScheduleInfo<T>(jobData,
                                                                                                       entityQueryComponentNativeArray,
                                                                                                       batchStrategy,
                                                                                                       scheduleJobFunction);

            return FinalizeJobConfig(jobConfig, scheduleInfo);
        }

        public static NativeArrayJobConfig<T> CreateNativeArrayJobConfig<T>(TaskFlowGraph taskFlowGraph,
                                                                            AbstractTaskSystem taskSystem,
                                                                            AbstractTaskDriver taskDriver,
                                                                            AccessControlledValue<NativeArray<T>> nativeArray,
                                                                            JobConfigScheduleDelegates.ScheduleNativeArrayJobDelegate<T> scheduleJobFunction,
                                                                            BatchStrategy batchStrategy)
            where T : struct
        {
            NativeArrayJobConfig<T> jobConfig = new NativeArrayJobConfig<T>(taskFlowGraph,
                                                                            taskSystem,
                                                                            taskDriver,
                                                                            nativeArray);

            NativeArrayJobData<T> jobData = new NativeArrayJobData<T>(jobConfig,
                                                                      taskSystem.World);

            NativeArrayScheduleInfo<T> scheduleInfo = new NativeArrayScheduleInfo<T>(jobData,
                                                                                     batchStrategy,
                                                                                     scheduleJobFunction);

            return FinalizeJobConfig(jobConfig, scheduleInfo);
        }

        private static TJobConfig FinalizeJobConfig<TJobConfig>(TJobConfig jobConfig, AbstractScheduleInfo scheduleInfo)
            where TJobConfig : AbstractJobConfig
        {
            jobConfig.AssignScheduleInfo(scheduleInfo);
            return jobConfig;
        }
    }
}
