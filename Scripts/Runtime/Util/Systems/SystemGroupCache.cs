using Unity.Entities;

namespace Anvil.Unity.DOTS.Util
{
    public class SystemGroupCache : SystemCache
    {
        public int CachedGroupSystemsCount
        {
            get;
            private set;
        }

        public ComponentSystemGroup SystemGroup
        {
            get;
        }

        public SystemGroupCache(SystemGroupCache parentGroupCache, ComponentSystemGroup group) : base(parentGroupCache, group)
        {
            SystemGroup = group;
        }

        internal override void RebuildIfNeeded(SystemGroupCache parentGroupCache)
        {
            base.RebuildIfNeeded(parentGroupCache);
            
            //No need to check if we're different, just assign
            CachedGroupSystemsCount = SystemGroup.Systems.Count;
        }
    }
}
