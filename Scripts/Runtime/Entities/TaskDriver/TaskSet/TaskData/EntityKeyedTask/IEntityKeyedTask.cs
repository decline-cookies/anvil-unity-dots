using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Represents an instance of data that is linked to an <see cref="Key"/>.
    /// </summary>
    /// <remarks>
    /// While this data could be an <see cref="IComponentData"/> and attached to the <see cref="Key"/> this leads
    /// to larger chunks, possible fragmentation and difficulties in being able to schedule jobs to shared-write to
    /// the data. Instead, this data proxies the Entity and allows for it to be used in the Task system while
    /// maintaining the link to the original <see cref="Key"/>
    /// </remarks>
    public interface IEntityKeyedTask
    {
        /// <summary>
        /// The Entity this data is keyed on
        /// </summary>
        public Entity Key
        {
            get;
        }
    }
}
