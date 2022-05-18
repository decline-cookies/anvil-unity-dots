using Unity.Entities;

namespace Anvil.Unity.DOTS.Systems
{
    /// <summary>
    /// Represents a cached state of a <see cref="ComponentSystemGroup"/>
    /// </summary>
    public class SystemGroupCache : SystemCache
    {
        /// <summary>
        /// The number of <see cref="ComponentSystemBase"/>s and/or <see cref="ComponentSystemGroup"/>s
        /// that are part of this <see cref="SystemGroup"/>
        /// </summary>
        public int CachedGroupSystemsCount
        {
            get;
            private set;
        }
        
        /// <summary>
        /// The <see cref="ComponentSystemGroup"/> this cache represents
        /// </summary>
        public ComponentSystemGroup SystemGroup
        {
            get;
        }

        internal SystemGroupCache(SystemGroupCache parentGroupCache, ComponentSystemGroup group) : base(parentGroupCache, group)
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
