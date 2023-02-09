using Anvil.CSharp.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal interface IEntitySpawner : IAnvilDisposable
    {
        public void Init(
            EntityManager entityManager,
            EntityArchetype entityArchetype);

        public JobHandle Schedule(
            JobHandle dependsOn,
            ref EntityCommandBuffer ecb,
            NativeParallelHashMap<long, EntityArchetype> entityArchetypeLookup);
    }
}
