using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public struct EntityPrototypeDefinitionWrapper<TEntitySpawnDefinition>
        where TEntitySpawnDefinition : unmanaged, IEntitySpawnDefinition
    {
        public readonly Entity Prototype;
        public readonly TEntitySpawnDefinition EntitySpawnDefinition;
        public EntityPrototypeDefinitionWrapper(Entity prototype, 
                                                TEntitySpawnDefinition entitySpawnDefinition)
        {
            Prototype = prototype;
            EntitySpawnDefinition = entitySpawnDefinition;
        }
    }
}
