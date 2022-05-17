using Anvil.CSharp.Core;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Helper class to aid with caching
    /// </summary>
    public abstract class AbstractCache : AbstractAnvilBase
    {
        protected const int INITIAL_VERSION = -1;

        /// <summary>
        /// The current version of this cache
        /// </summary>
        public int Version
        {
            get;
            internal set;
        } = INITIAL_VERSION;
    }
}
