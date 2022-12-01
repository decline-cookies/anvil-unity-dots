using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class UpdateJobConfig<TInstance> : AbstractResolvableJobConfig
        where TInstance : unmanaged, IEntityProxyInstance
    {
        public UpdateJobConfig(TaskFlowGraph taskFlowGraph,
                               AbstractTaskDriverSystem taskSystem,
                               AbstractTaskDriver taskDriver,
                               DataStream<TInstance> dataStream) 
            : base(taskFlowGraph, 
                   taskSystem, 
                   taskDriver)
        {
            RequireDataStreamForUpdate(dataStream);
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - DATA STREAM
        //*************************************************************************************************************

        private void RequireDataStreamForUpdate(DataStream<TInstance> dataStream)
        {
            AddAccessWrapper(new DataStreamAccessWrapper<TInstance>(dataStream, AccessType.ExclusiveWrite, Usage.Update));
        }
    }
}
