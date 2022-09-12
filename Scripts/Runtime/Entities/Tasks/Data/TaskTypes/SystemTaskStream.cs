using Anvil.CSharp.Core;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public class SystemTaskStream<TInstance> : AbstractAnvilBase,
                                               ISystemTaskStream
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

        public SystemTaskStream(ProxyDataStream<TInstance> proxyDataStream, UpdateJobConfig<TInstance> updateJobConfig)
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
