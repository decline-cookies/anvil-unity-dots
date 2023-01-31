using Anvil.CSharp.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal interface IEntitySpawner : IAnvilDisposable
    {
        public JobHandle Schedule(JobHandle dependsOn, EntityCommandBuffer ecb, NativeParallelHashMap<long, EntityArchetype> entityArchetypeLookup);
    }
}
