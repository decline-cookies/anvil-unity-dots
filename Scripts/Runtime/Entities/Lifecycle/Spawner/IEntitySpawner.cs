using Anvil.CSharp.Core;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal interface IEntitySpawner : IAnvilDisposable
    {
        public JobHandle Schedule(
            JobHandle dependsOn,
            ref EntityCommandBuffer ecb);
    }
}
