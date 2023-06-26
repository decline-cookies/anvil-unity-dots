using Anvil.Unity.DOTS.Core;
using Unity.Collections;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Represents the completion of a Cancel Request from a generic perspective where only the <see cref="Key"/>
    /// is returned that completed it's cancellation flow.
    /// </summary>
    public readonly struct CancelComplete : IEntityProxyInstance, IToFixedString<FixedString128Bytes>
    {
        public static implicit operator Entity(CancelComplete cancelComplete) => cancelComplete.Key;
        public static implicit operator CancelComplete(Entity entity) => new CancelComplete(entity);

        /// <summary>
        /// The <see cref="Key"/> that completed it's cancellation flow.
        /// </summary>
        public Entity Key { get; }

        public CancelComplete(Entity entity)
        {
            Key = entity;
        }

        public FixedString128Bytes ToFixedString()
        {
            return $"Key:{Key.ToFixedString()}";
        }
    }
}