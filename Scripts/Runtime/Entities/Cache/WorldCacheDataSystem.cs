using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
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

        protected override void OnCreate()
        {
            WorldCache = new WorldCache(World);

            base.OnCreate();
        }

        protected override void OnDestroy()
        {
            WorldCache.Dispose();
            base.OnDestroy();
        }
    }
}