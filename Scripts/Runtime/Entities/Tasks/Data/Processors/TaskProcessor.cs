using Anvil.CSharp.Core;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal class TaskProcessor<TInstance, TJobConfig> : AbstractAnvilBase,
                                                          ITaskProcessor
        where TInstance : unmanaged, IProxyInstance
        where TJobConfig : AbstractJobConfig<TInstance>
    {
        public ProxyDataStream<TInstance> DataStream
        {
            get;
        }

        public TJobConfig JobConfig
        {
            get;
        }

        internal TaskProcessor(ProxyDataStream<TInstance> proxyDataStream, TJobConfig jobConfig)
        {
            DataStream = proxyDataStream;
            JobConfig = jobConfig;
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
            return JobConfig.PrepareAndSchedule(dependsOn);
        }
    }
}
