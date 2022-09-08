using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    //TODO: Docs
    public interface IProxyData
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
