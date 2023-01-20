using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Represents an instance of data that is linked to an <see cref="Entity"/>.
    /// </summary>
    /// <remarks>
    /// While this data could be an <see cref="IComponentData"/> and attached to the <see cref="Entity"/> this leads
    /// to larger chunks, possible fragmentation and difficulties in being able to schedule jobs to shared-write to
    /// the data. Instead, this data proxies the Entity and allows for it to be used in the Task system while
    /// maintaining the link to the original <see cref="Entity"/>
    /// </remarks>
    public interface IEntityProxyInstance
    {
        /// <summary>
        /// The Entity this data belongs to
        /// </summary>
        public Entity Entity
        {
            get;
        }
    }
}
