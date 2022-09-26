using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class JobConfig : AbstractJobConfig,
                               IJobConfigScheduling,
                               IJobConfig
    {
        private readonly IJobConfig.ScheduleJobDelegate m_ScheduleJobFunction;

        public JobConfig(TaskFlowGraph taskFlowGraph, 
                         ITaskSystem taskSystem, 
                         ITaskDriver taskDriver, 
                         IJobConfig.ScheduleJobDelegate scheduleJobFunction) : base(taskFlowGraph, taskSystem, taskDriver)
        {
            m_ScheduleJobFunction = scheduleJobFunction;
        }

        protected sealed override string GetScheduleJobFunctionDebugInfo()
        {
            return $"{m_ScheduleJobFunction.Method.DeclaringType?.Name}.{m_ScheduleJobFunction.Method.Name}";
        }
        
        //*************************************************************************************************************
        // CONFIGURATION - SCHEDULING
        //*************************************************************************************************************
        public IJobConfigRequirements ScheduleOn<TInstance>(ITaskStream<TInstance> taskStream, BatchStrategy batchStrategy)
            where TInstance : unmanaged, IProxyInstance
        {
            ScheduleInfo = new ProxyDataStreamScheduleInfo<TInstance>(taskStream.DataStream, batchStrategy);
            return this;
        }

        public IJobConfigRequirements ScheduleOn<T>(NativeArray<T> nativeArray, BatchStrategy batchStrategy)
            where T : unmanaged
        {
            ScheduleInfo = new NativeArrayScheduleInfo<T>(nativeArray, batchStrategy);
            return this;
        }

        public IJobConfigRequirements ScheduleOn(EntityQuery entityQuery, BatchStrategy batchStrategy)
        {
            ScheduleInfo = new EntityQueryScheduleInfo(entityQuery, batchStrategy);
            return this;
        }

        //TODO: Add in ScheduleOn for Query Components
        
        //*************************************************************************************************************
        // EXECUTION
        //*************************************************************************************************************

        protected sealed override JobHandle CallScheduleFunction(JobHandle dependsOn, JobData jobData)
        {
            return m_ScheduleJobFunction(dependsOn, jobData, ScheduleInfo);
        }
        
        internal override DataStreamTargetResolver GetDataStreamChannelResolver()
        {
            throw new NotSupportedException($"Tried to get a {nameof(DataStreamTargetResolver)} but {this} doesn't support it!");
        }
    }
}
