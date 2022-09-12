using Anvil.CSharp.Core;
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
        }

        public SystemTask(ProxyDataStream<TData> proxyDataStream, UpdateJobConfig<TData> updateJobConfig)
        {
            DataStream = proxyDataStream;
            UpdateJobConfig = updateJobConfig;
        }
        
        protected override void DisposeSelf()
        {
            DataStream.Dispose();
            base.DisposeSelf();
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
