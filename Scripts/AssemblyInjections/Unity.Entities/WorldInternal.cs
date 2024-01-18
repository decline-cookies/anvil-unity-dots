using System;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// A collection of methods to to gain access to static <see cref="World"/> members that are marked internal.
    /// </summary>
    public static class WorldInternal
    {
        /// <summary>
        /// Dispatched after a system is created.
        /// </summary>
        /// <remarks>
        /// This is a proxy for the internal <see cref="World.SystemCreated"/> static event that Unity has made internal.
        /// </remarks>
        public static event Action<World, ComponentSystemBase> OnSystemCreated
        {
            add => World.SystemCreated += value;
            remove => World.SystemCreated -= value;
        }

        /// <summary>
        /// Dispatched before a system is destroyed.
        /// </summary>
        /// <remarks>
        /// This is a proxy for the internal <see cref="World.SystemDestroyed"/> static event that Unity has made internal.
        /// </remarks>
        public static event Action<World, ComponentSystemBase> OnSystemDestroyed
        {
            add => World.SystemDestroyed += value;
            remove => World.SystemDestroyed -= value;
        }
    }
}
