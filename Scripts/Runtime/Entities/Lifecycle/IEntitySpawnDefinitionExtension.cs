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
        /// Generally <see cref="EntitySpawnSystem" />'s spawn methods should be preferred. They are more performant.
        /// This method is useful when the archetype or spawn system isn't available and you immediately need the entity
        /// configured on an <see cref="EntityCommandBuffer"/>. ("Ex: Proxy Entities)
        /// TODO: #192 - Replace when there is a way to have definitions spawn proxy entities as part of their populate.
        /// </summary>
        /// <param name="definition">The definition to create and populate an instance from.</param>
        /// <param name="ecb">The <see cref="EntityCommandBuffer"/> to write to.</param>
        /// <typeparam name="T">The type of the definition that implements <see cref="IEntitySpawnDefinition"/>.</typeparam>
        /// <returns>The created entity reference.</returns>
        public static Entity CreateAndPopulate<T>(ref this T definition, ref EntityCommandBuffer ecb) where T : struct, IEntitySpawnDefinition
        {
            Entity entity = ecb.CreateEntity();
            ecb.AddComponent(entity, new ComponentTypes(definition.RequiredComponents));
            definition.PopulateOnEntity(entity, ref ecb);

            return entity;
        }
    }
}