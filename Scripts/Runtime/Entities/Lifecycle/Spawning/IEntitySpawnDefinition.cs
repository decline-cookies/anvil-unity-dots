using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    public interface IEntitySpawnDefinition
    {
        public void PopulateOnEntity(Entity entity, ref EntityCommandBuffer ecb);
    }
}
