using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Represents the completion of a Cancel Request from a generic perspective where only the <see cref="Entity"/>
    /// is returned that completed it's cancellation flow.
    /// </summary>
    public readonly struct CancelComplete : IEntityProxyInstance
    {
        public static implicit operator Entity(CancelComplete cancelComplete) => cancelComplete.Entity;
        public static implicit operator CancelComplete(Entity entity) => new CancelComplete(entity);

        /// <summary>
        /// The <see cref="Entity"/> that completed it's cancellation flow.
        /// </summary>
        public Entity Entity { get; }

        public CancelComplete(Entity entity)
        {
            Entity = entity;
        }
    }
}
