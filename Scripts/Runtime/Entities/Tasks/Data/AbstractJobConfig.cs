using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public abstract class AbstractJobConfig : IJobConfig
    {
        public abstract JobHandle PrepareAndSchedule(JobHandle dependsOn);
    }
}
