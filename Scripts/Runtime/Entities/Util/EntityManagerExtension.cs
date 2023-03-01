using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Helper methods for a <see cref="EntityManager"/>
    /// </summary>
    public static class EntityManagerExtension
    {
        /// <summary>
        /// Similar to <see cref="EntityManager.Exists"/> but goes a bit further to detect if the <see cref="Entity"/>
        /// is in a clean up state or not.
        /// </summary>
        /// <remarks>
        /// It can be useful to keep a reference to an <see cref="Entity"/> in OO land and just check if it still
        /// exists before using it. However, some Entities will have clean up components on them if they use
        /// <see cref="ISystemStateComponentData"/>. The Entity will therefore still exist but not actually be able to
        /// be used.
        /// </remarks>
        /// <param name="entityManager">The <see cref="EntityManager"/> the Entity is a part of.</param>
        /// <param name="entity">The <see cref="Entity"/> to check</param>
        /// <returns>True if the Entity exists and is not in a cleanup state. False otherwise</returns>
        public static bool IsValid(this EntityManager entityManager, Entity entity)
        {
            return entityManager.Exists(entity) && !entityManager.HasComponent(entity, EntityCleanupHelper.CLEAN_UP_ENTITY_COMPONENT_TYPE);
        }
    }
}
