using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A collection of extension methods for <see cref="IEntitySpawnDefinition"/> instances.
    /// </summary>
    public static class IEntitySpawnDefinitionExtension
    {
        /// <summary>
        /// Create and populate an entity based on a <see cref="IEntitySpawnDefinition"/>.
        /// </summary>
        /// <param name="definition">The definition to create and populate an instance from.</param>
        /// <param name="ecb">The <see cref="EntityCommandBuffer"/> to write to.</param>
        /// <param name="entitySpawnHelper">The <see cref="EntitySpawnHelper"/></param>
        /// <typeparam name="TDefinition">The type of the definition that implements <see cref="IEntitySpawnDefinition"/>.</typeparam>
        public static void CreateAndPopulate<TDefinition>(ref this TDefinition definition, ref EntityCommandBuffer ecb, in EntitySpawnHelper entitySpawnHelper)
            where TDefinition : unmanaged, IEntitySpawnDefinition
        {
            Entity entity = ecb.CreateEntity(entitySpawnHelper.GetEntityArchetypeForDefinition<TDefinition>());
            definition.PopulateOnEntity(entity, ref ecb, entitySpawnHelper);
        }

        /// <summary>
        /// Create and populate an entity based on a <see cref="IEntitySpawnDefinition"/> using a prototype <see cref="Entity"/>
        /// </summary>
        /// <param name="definition">The definition to create and populate an instance from.</param>
        /// <param name="ecb">The <see cref="EntityCommandBuffer"/> to write to.</param>
        /// <param name="entitySpawnHelper">The <see cref="EntitySpawnHelper"/></param>
        /// <typeparam name="TDefinition">The type of the definition that implements <see cref="IEntitySpawnDefinition"/>.</typeparam>
        public static void CreateAndPopulateWithPrototype<TDefinition>(ref this TDefinition definition, ref EntityCommandBuffer ecb, in EntitySpawnHelper entitySpawnHelper)
            where TDefinition : unmanaged, IEntitySpawnDefinition
        {
            Entity entity = ecb.Instantiate(entitySpawnHelper.GetPrototypeEntityForDefinition<TDefinition>());
            definition.PopulateOnEntity(entity, ref ecb, entitySpawnHelper);
        }
    }
}