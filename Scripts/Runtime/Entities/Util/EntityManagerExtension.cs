using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public static class EntityManagerExtension
    {
        public static bool IsValid(this EntityManager entityManager, Entity entity)
        {
            return entityManager.Exists(entity) && !entityManager.HasComponent(entity, EntityCleanupHelper.CLEAN_UP_ENTITY_COMPONENT_TYPE);
        }
    }
}
