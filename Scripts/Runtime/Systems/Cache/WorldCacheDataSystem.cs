using Unity.Entities;

namespace Anvil.Unity.DOTS.Systems
{
    /// <summary>
    /// Data System (no update) for managing a cached view of a <see cref="World"/>
    /// </summary>
    public partial class WorldCacheDataSystem : AbstractDataSystem
    {
        public WorldCache WorldCache
        {
            get;
            private set;
        }

        protected override void Init()
        {
            WorldCache = new WorldCache(World);
        }

        protected override void OnDestroy()
        {
            WorldCache.Dispose();
            base.OnDestroy();
        }
    }
}
