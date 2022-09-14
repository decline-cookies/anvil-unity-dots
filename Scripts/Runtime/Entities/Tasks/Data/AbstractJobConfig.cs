using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public abstract class AbstractJobConfig<TInstance> : IJobConfig
        where TInstance : unmanaged, IProxyInstance
    {
        public abstract JobHandle PrepareAndSchedule(JobHandle dependsOn);
    }
}
