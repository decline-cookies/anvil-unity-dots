using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Represents the completion of a Cancel Request from a generic perspective where only the <see cref="Entity"/>
    /// is returned that completed it's cancellation flow.
    /// </summary>
    public readonly struct CancelCompleted : IEntityProxyInstance
    {
        public static implicit operator Entity(CancelCompleted cancelCompleted) => cancelCompleted.Entity;
        public static implicit operator CancelCompleted(Entity entity) => new CancelCompleted(entity);
        
        /// <summary>
        /// The <see cref="Entity"/> that completed it's cancellation flow.
        /// </summary>
        public Entity Entity { get; }

        public CancelCompleted(Entity entity)
        {
            Entity = entity;
        }
    }
}
