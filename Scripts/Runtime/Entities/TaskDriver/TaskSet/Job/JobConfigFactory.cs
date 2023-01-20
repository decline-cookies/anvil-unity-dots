using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// <see cref="AbstractJobConfig"/>s, <see cref="AbstractJobData"/>s, and <see cref="AbstractScheduleInfo"/>
    /// all work together and are somewhat interdependent on each other.
    /// These functions serve to construct the matching pairs and stitch together relationships in a nice and easy
    /// manner.
    /// </summary>
    internal static class JobConfigFactory
    {
        public static CancelCompleteJobConfig CreateCancelCompleteJobConfig(ITaskSetOwner taskSetOwner,
                                                                            CancelCompleteDataStream cancelCompleteDataStream,
                                                                            JobConfigScheduleDelegates.ScheduleCancelCompleteJobDelegate scheduleJobFunction,
                                                                            BatchStrategy batchStrategy)
        {
            CancelCompleteJobConfig jobConfig = new CancelCompleteJobConfig(taskSetOwner,
                                                                            cancelCompleteDataStream);

            CancelCompleteJobData jobData = new CancelCompleteJobData(jobConfig);

            CancelCompleteScheduleInfo scheduleInfo = new CancelCompleteScheduleInfo(jobData,
                                                                                     cancelCompleteDataStream,
                                                                                     batchStrategy,
                                                                                     scheduleJobFunction);

            return FinalizeJobConfig(jobConfig, scheduleInfo);
        }

        public static UpdateJobConfig<TInstance> CreateUpdateJobConfig<TInstance>(ITaskSetOwner taskSetOwner,
                                                                                  EntityProxyDataStream<TInstance> dataStream,
                                                                                  JobConfigScheduleDelegates.ScheduleUpdateJobDelegate<TInstance> scheduleJobFunction,
                                                                                  BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            UpdateJobConfig<TInstance> jobConfig = new UpdateJobConfig<TInstance>(taskSetOwner,
                                                                                  dataStream);

            UpdateJobData<TInstance> jobData = new UpdateJobData<TInstance>(jobConfig);

            UpdateDataStreamScheduleInfo<TInstance> scheduleInfo = new UpdateDataStreamScheduleInfo<TInstance>(jobData,
                                                                                                               dataStream,
                                                                                                               batchStrategy,
                                                                                                               scheduleJobFunction);

            return FinalizeJobConfig(jobConfig, scheduleInfo);
        }

        public static CancelJobConfig<TInstance> CreateCancelJobConfig<TInstance>(ITaskSetOwner taskSetOwner,
                                                                                  EntityProxyDataStream<TInstance> pendingCancelDataStream,
                                                                                  JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction,
                                                                                  BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            CancelJobConfig<TInstance> jobConfig = new CancelJobConfig<TInstance>(taskSetOwner,
                                                                                  pendingCancelDataStream);

            CancelJobData<TInstance> jobData = new CancelJobData<TInstance>(jobConfig);

            CancelDataStreamScheduleInfo<TInstance> scheduleInfo = new CancelDataStreamScheduleInfo<TInstance>(jobData,
                                                                                                               pendingCancelDataStream,
                                                                                                               batchStrategy,
                                                                                                               scheduleJobFunction);

            return FinalizeJobConfig(jobConfig, scheduleInfo);
        }

        public static DataStreamJobConfig<TInstance> CreateDataStreamJobConfig<TInstance>(ITaskSetOwner taskSetOwner,
                                                                                          EntityProxyDataStream<TInstance> dataStream,
                                                                                          JobConfigScheduleDelegates.ScheduleDataStreamJobDelegate<TInstance> scheduleJobFunction,
                                                                                          BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            DataStreamJobConfig<TInstance> jobConfig = new DataStreamJobConfig<TInstance>(taskSetOwner,
                                                                                          dataStream);

            DataStreamJobData<TInstance> jobData = new DataStreamJobData<TInstance>(jobConfig);

            DataStreamScheduleInfo<TInstance> scheduleInfo = new DataStreamScheduleInfo<TInstance>(jobData,
                                                                                                   dataStream,
                                                                                                   batchStrategy,
                                                                                                   scheduleJobFunction);

            return FinalizeJobConfig(jobConfig, scheduleInfo);
        }

        public static EntityQueryJobConfig CreateEntityQueryJobConfig(ITaskSetOwner taskSetOwner,
                                                                      EntityQuery entityQuery,
                                                                      JobConfigScheduleDelegates.ScheduleEntityQueryJobDelegate scheduleJobFunction,
                                                                      BatchStrategy batchStrategy)
        {
            EntityQueryNativeArray entityQueryNativeArray = new EntityQueryNativeArray(entityQuery);

            EntityQueryJobConfig jobConfig = new EntityQueryJobConfig(taskSetOwner,
                                                                      entityQueryNativeArray);

            EntityQueryJobData jobData = new EntityQueryJobData(jobConfig);

            EntityQueryScheduleInfo scheduleInfo = new EntityQueryScheduleInfo(jobData,
                                                                               entityQueryNativeArray,
                                                                               batchStrategy,
                                                                               scheduleJobFunction);

            return FinalizeJobConfig(jobConfig, scheduleInfo);
        }

        public static EntityQueryComponentJobConfig<T> CreateEntityQueryComponentJobConfig<T>(ITaskSetOwner taskSetOwner,
                                                                                              EntityQuery entityQuery,
                                                                                              JobConfigScheduleDelegates.ScheduleEntityQueryComponentJobDelegate<T> scheduleJobFunction,
                                                                                              BatchStrategy batchStrategy)
            where T : struct, IComponentData
        {
            EntityQueryComponentNativeArray<T> entityQueryComponentNativeArray = new EntityQueryComponentNativeArray<T>(entityQuery);

            EntityQueryComponentJobConfig<T> jobConfig = new EntityQueryComponentJobConfig<T>(taskSetOwner,
                                                                                              entityQueryComponentNativeArray);

            EntityQueryComponentJobData<T> jobData = new EntityQueryComponentJobData<T>(jobConfig);

            EntityQueryComponentScheduleInfo<T> scheduleInfo = new EntityQueryComponentScheduleInfo<T>(jobData,
                                                                                                       entityQueryComponentNativeArray,
                                                                                                       batchStrategy,
                                                                                                       scheduleJobFunction);

            return FinalizeJobConfig(jobConfig, scheduleInfo);
        }

        public static NativeArrayJobConfig<T> CreateNativeArrayJobConfig<T>(ITaskSetOwner taskSetOwner,
                                                                            AccessControlledValue<NativeArray<T>> nativeArray,
                                                                            JobConfigScheduleDelegates.ScheduleNativeArrayJobDelegate<T> scheduleJobFunction,
                                                                            BatchStrategy batchStrategy)
            where T : struct
        {
            NativeArrayJobConfig<T> jobConfig = new NativeArrayJobConfig<T>(taskSetOwner,
                                                                            nativeArray);

            NativeArrayJobData<T> jobData = new NativeArrayJobData<T>(jobConfig);

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
