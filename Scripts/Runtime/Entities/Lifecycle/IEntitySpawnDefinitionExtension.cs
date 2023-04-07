using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A collection of extension methods for <see cref="IEntitySpawnDefinition"/> instances.
    /// </summary>
    public static class IEntitySpawnDefinitionExtension
    {
        
        //TODO: UPDATE DOCS
        /// <summary>
        /// Create and populate an entity based on a <see cref="IEntitySpawnDefinition"/>.
        /// Generally <see cref="EntitySpawnSystem" />'s spawn methods should be preferred. They are more performant.
        /// This method is useful when the archetype or spawn system isn't available and you immediately need the entity
        /// configured on an <see cref="EntityCommandBuffer"/>. ("Ex: Proxy Entities)
        /// TODO: #192 - Replace when there is a way to have definitions spawn proxy entities as part of their populate.
        /// </summary>
        /// <param name="definition">The definition to create and populate an instance from.</param>
        /// <param name="ecb">The <see cref="EntityCommandBuffer"/> to write to.</param>
        /// <typeparam name="TDefinition">The type of the definition that implements <see cref="IEntitySpawnDefinition"/>.</typeparam>
        /// <returns>The created entity reference.</returns>
        public static Entity CreateAndPopulate<TDefinition>(ref this TDefinition definition, ref EntityCommandBuffer ecb, in EntitySpawnHelper entitySpawnHelper) 
            where TDefinition : unmanaged, IEntitySpawnDefinition
        {
            Entity entity = ecb.CreateEntity(entitySpawnHelper.GetEntityArchetypeForDefinition<TDefinition>());
            definition.PopulateOnEntity(entity, ref ecb, entitySpawnHelper);

            return entity;
        }
    }
}