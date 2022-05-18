using Unity.Entities;

namespace Anvil.Unity.DOTS.Systems
{
    /// <summary>
    /// Data System (no update) for managing a cached view of a <see cref="World"/>
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class WorldCacheDataSystem : SystemBase
    {
        public WorldCache WorldCache
        {
            get;
            private set;
        }
        
        protected override void OnCreate()
        {
            base.OnCreate();
            WorldCache = new WorldCache(World);
        }

        protected override void OnDestroy()
        {
            WorldCache.Dispose();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
        }
    }
}
