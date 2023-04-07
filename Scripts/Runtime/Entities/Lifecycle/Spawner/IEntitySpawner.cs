using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal interface IEntitySpawner : IAnvilDisposable
    {
        public void Init(
            EntityManager entityManager,
            NativeParallelHashMap<long, EntityArchetype> entityArchetypes,
            IReadOnlyAccessControlledValue<NativeParallelHashMap<long, Entity>> entityPrototypes,
            bool mustDisableBurst);

        public JobHandle Schedule(
            JobHandle dependsOn,
            ref EntityCommandBuffer ecb);
    }
}
