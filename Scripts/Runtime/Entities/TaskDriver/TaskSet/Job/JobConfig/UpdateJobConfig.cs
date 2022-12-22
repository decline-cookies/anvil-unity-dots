using Anvil.Unity.DOTS.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class UpdateJobConfig<TInstance> : AbstractResolvableJobConfig
        where TInstance : unmanaged, IEntityProxyInstance
    {
        public UpdateJobConfig(ITaskSetOwner taskSetOwner,
                               EntityProxyDataStream<TInstance> dataStream) 
            : base(taskSetOwner)
        {
            RequireDataStreamForUpdate(dataStream);
        }

        //*************************************************************************************************************
        // CONFIGURATION - REQUIRED DATA - DATA STREAM
        //*************************************************************************************************************

        private void RequireDataStreamForUpdate(EntityProxyDataStream<TInstance> dataStream)
        {
            //When updating we want to read from the Active and write to the Pending
            AddAccessWrapper(new DataStreamActiveAccessWrapper<TInstance>(dataStream, AccessType.SharedRead, Usage.Update));
            AddAccessWrapper(new DataStreamPendingAccessWrapper<TInstance>(dataStream, AccessType.SharedWrite, Usage.Update));
        }
    }
}
