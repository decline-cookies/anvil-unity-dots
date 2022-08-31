using Unity.Entities;

namespace Anvil.Unity.DOTS.Data
{
    //TODO: Docs
    public interface IEntityProxyData
    {
        /// <summary>
        /// The lookup key for this data
        /// </summary>
        public Entity Entity
        {
            get;
        }
    }
}
