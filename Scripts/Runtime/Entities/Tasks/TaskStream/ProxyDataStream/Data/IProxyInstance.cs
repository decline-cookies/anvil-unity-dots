using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    //TODO: Docs
    public interface IProxyInstance
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
