using Anvil.CSharp.Core;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    public interface ISystemTaskProcessor : IAnvilDisposable
    {
        public JobHandle ConsolidateForFrame(JobHandle dependsOn);
        public JobHandle PrepareAndSchedule(JobHandle dependsOn);
    }
}
