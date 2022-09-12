using Anvil.CSharp.Core;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public class SystemTaskProcessor<TInstance> : AbstractAnvilBase,
                                                  ISystemTaskProcessor
        where TInstance : unmanaged, IProxyInstance
    {
        public ProxyDataStream<TInstance> DataStream
        {
            get;
        }

        public UpdateJobConfig<TInstance> UpdateJobConfig
        {
            get;
        }

        public SystemTaskProcessor(ProxyDataStream<TInstance> proxyDataStream, UpdateJobConfig<TInstance> updateJobConfig)
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
