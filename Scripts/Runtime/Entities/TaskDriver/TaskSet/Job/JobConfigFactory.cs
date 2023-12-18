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
        public static UpdateJobConfig<TInstance> CreateUpdateJobConfig<TInstance>(
            ITaskSetOwner taskSetOwner,
            EntityProxyDataStream<TInstance> dataStream,
            JobConfigScheduleDelegates.ScheduleUpdateJobDelegate<TInstance> scheduleJobFunction,
            BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityKeyedTask
        {
            UpdateJobConfig<TInstance> jobConfig = new UpdateJobConfig<TInstance>(taskSetOwner, dataStream);

            UpdateJobData<TInstance> jobData = new UpdateJobData<TInstance>(jobConfig);

            UpdateScheduleInfo<TInstance> scheduleInfo = new UpdateScheduleInfo<TInstance>(
                jobData,
                dataStream,
                batchStrategy,
                scheduleJobFunction);

            return FinalizeJobConfig(jobConfig, scheduleInfo);
        }

        public static CancelJobConfig<TInstance> CreateCancelJobConfig<TInstance>(
            ITaskSetOwner taskSetOwner,
            EntityProxyDataStream<TInstance> activeCancelDataStream,
            JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction,
            BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityKeyedTask
        {
            CancelJobConfig<TInstance> jobConfig = new CancelJobConfig<TInstance>(taskSetOwner, activeCancelDataStream);

            CancelJobData<TInstance> jobData = new CancelJobData<TInstance>(jobConfig);

            CancelScheduleInfo<TInstance> scheduleInfo = new CancelScheduleInfo<TInstance>(
                jobData,
                activeCancelDataStream,
                batchStrategy,
                scheduleJobFunction);

            return FinalizeJobConfig(jobConfig, scheduleInfo);
        }

        public static DataStreamJobConfig<TInstance> CreateDataStreamJobConfig<TInstance>(
            ITaskSetOwner taskSetOwner,
            EntityProxyDataStream<TInstance> dataStream,
            JobConfigScheduleDelegates.ScheduleDataStreamJobDelegate<TInstance> scheduleJobFunction,
            BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityKeyedTask
        {
            DataStreamJobConfig<TInstance> jobConfig = new DataStreamJobConfig<TInstance>(taskSetOwner, dataStream);

            DataStreamJobData<TInstance> jobData = new DataStreamJobData<TInstance>(jobConfig);

            DataStreamScheduleInfo<TInstance> scheduleInfo = new DataStreamScheduleInfo<TInstance>(
                jobData,
                dataStream,
                batchStrategy,
                scheduleJobFunction);

            return FinalizeJobConfig(jobConfig, scheduleInfo);
        }

        public static EntityQueryJobConfig CreateEntityQueryJobConfig(
            ITaskSetOwner taskSetOwner,
            EntityQuery entityQuery,
            JobConfigScheduleDelegates.ScheduleEntityQueryJobDelegate scheduleJobFunction,
            BatchStrategy batchStrategy)
        {
            EntityQueryNativeList entityQueryNativeList = new EntityQueryNativeList(entityQuery);

            EntityQueryJobConfig jobConfig = new EntityQueryJobConfig(taskSetOwner, entityQueryNativeList);

            EntityQueryJobData jobData = new EntityQueryJobData(jobConfig);

            EntityQueryScheduleInfo scheduleInfo = new EntityQueryScheduleInfo(
                jobData,
                entityQueryNativeList,
                batchStrategy,
                scheduleJobFunction);

            return FinalizeJobConfig(jobConfig, scheduleInfo);
        }

        public static EntityQueryComponentJobConfig<T> CreateEntityQueryComponentJobConfig<T>(
            ITaskSetOwner taskSetOwner,
            EntityQuery entityQuery,
            JobConfigScheduleDelegates.ScheduleEntityQueryComponentJobDelegate<T> scheduleJobFunction,
            BatchStrategy batchStrategy)
            where T : unmanaged, IComponentData
        {
            EntityQueryComponentNativeList<T> entityQueryComponentNativeList = new EntityQueryComponentNativeList<T>(entityQuery);

            EntityQueryComponentJobConfig<T> jobConfig = new EntityQueryComponentJobConfig<T>(
                taskSetOwner,
                entityQueryComponentNativeList);

            EntityQueryComponentJobData<T> jobData = new EntityQueryComponentJobData<T>(jobConfig);

            EntityQueryComponentScheduleInfo<T> scheduleInfo = new EntityQueryComponentScheduleInfo<T>(
                jobData,
                entityQueryComponentNativeList,
                batchStrategy,
                scheduleJobFunction);

            return FinalizeJobConfig(jobConfig, scheduleInfo);
        }

        public static NativeArrayJobConfig<T> CreateNativeArrayJobConfig<T>(
            ITaskSetOwner taskSetOwner,
            AccessControlledValue<NativeArray<T>> nativeArray,
            JobConfigScheduleDelegates.ScheduleNativeArrayJobDelegate<T> scheduleJobFunction,
            BatchStrategy batchStrategy)
            where T : struct
        {
            NativeArrayJobConfig<T> jobConfig = new NativeArrayJobConfig<T>(taskSetOwner, nativeArray);

            NativeArrayJobData<T> jobData = new NativeArrayJobData<T>(jobConfig);

            NativeArrayScheduleInfo<T> scheduleInfo = new NativeArrayScheduleInfo<T>(jobData, batchStrategy, scheduleJobFunction);

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