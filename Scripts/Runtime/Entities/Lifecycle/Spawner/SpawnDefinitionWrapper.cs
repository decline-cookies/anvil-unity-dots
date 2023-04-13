namespace Anvil.Unity.DOTS.Entities
{
    internal readonly struct SpawnDefinitionWrapper<TEntitySpawnDefinition>
    {
        public readonly TEntitySpawnDefinition EntitySpawnDefinition;
        public readonly PrototypeSpawnBehaviour SpawnBehaviour;

        public SpawnDefinitionWrapper(TEntitySpawnDefinition entitySpawnDefinition, PrototypeSpawnBehaviour spawnBehaviour)
        {
            EntitySpawnDefinition = entitySpawnDefinition;
            SpawnBehaviour = spawnBehaviour;
        }
    }
}
