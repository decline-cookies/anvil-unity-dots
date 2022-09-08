using Anvil.CSharp.Core;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public class SystemTask<TData> : AbstractAnvilBase,
                                     ISystemTask
        where TData : unmanaged, IProxyData
    {
        public ProxyDataStream<TData> DataStream
        {
            get;
        }

        public UpdateJobConfig<TData> UpdateJobConfig
        {
            get;
            private set;
        }

        public SystemTask()
        {
            DataStream = new ProxyDataStream<TData>();
        }
        
        protected override void DisposeSelf()
        {
            DataStream.Dispose();
            base.DisposeSelf();
        }
        
        public UpdateJobConfig<TData> ConfigureUpdateJob(World world,
                                                         byte context,
                                                         UpdateJobConfig<TData>.ScheduleJobDelegate scheduleJobDelegate, 
                                                         BatchStrategy batchStrategy)
        {
            UpdateJobConfig = new UpdateJobConfig<TData>(world,
                                                         context,
                                                         scheduleJobDelegate, 
                                                         DataStream, 
                                                         batchStrategy);
            return UpdateJobConfig;
        }

        public JobHandle ConsolidateForFrame(JobHandle dependsOn)
        {
            return DataStream.ConsolidateForFrame(dependsOn);
        }

        public JobHandle PrepareAndSchedule(JobHandle dependsOn)
        {
            return UpdateJobConfig.PrepareAndSchedule(dependsOn);
        }
    }
}
