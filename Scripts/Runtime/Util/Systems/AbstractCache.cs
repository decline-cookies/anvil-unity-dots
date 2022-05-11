using Anvil.CSharp.Core;

namespace Anvil.Unity.DOTS.Util
{
    public abstract class AbstractCache : AbstractAnvilBase
    {
        protected const int INITIAL_VERSION = -1;
        
        public int Version
        {
            get;
            internal set;
        } = INITIAL_VERSION;
    }
}
