using System;
using Unity.Entities;
using UnityEngine;

namespace Anvil.Unity.DOTS.Util
{
    /// <summary>
    /// Helper class for managing a cached view of a <see cref="World"/>
    /// </summary>
    public static class WorldCacheUtil
    {
        //*************************************************************************************************************
        // INTERNAL HELPER
        //*************************************************************************************************************
        
        /// <summary>
        /// Lookup based on World's
        /// </summary>
        private class WorldLookup : AbstractLookup<Type, World, WorldCache>
        {
            private static WorldCache CreationFunction(World world)
            {
                return new WorldCache(world);
            }

            public WorldLookup() : base(typeof(WorldLookup))
            {
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public WorldCache GetOrCreate(World world)
            {
                return LookupGetOrCreate(world, CreationFunction);
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public void Remove(World world)
            {
                if (!ContainsKey(world))
                {
                    return;
                }

                LookupRemove(world);
            }
        }

        //*************************************************************************************************************
        // PUBLIC STATIC API
        //*************************************************************************************************************

        private static WorldLookup s_WorldLookup;

        private static WorldLookup Lookup
        {
            get => s_WorldLookup ?? (s_WorldLookup = new WorldLookup());
        }

        //Ensures the proper state with DomainReloading turned off in the Editor
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Dispose();
        }
        
        /// <summary>
        /// High level dispose to be used when exiting from an ECS/DOTS mode into another part of the application
        /// that doesn't use ECS/DOTS. All <see cref="WorldCache"/>s will be removed from the lookup and disposed.
        /// </summary>
        public static void Dispose()
        {
            s_WorldLookup?.Dispose();
            s_WorldLookup = null;
        }
        
        /// <summary>
        /// Removes an instance of a <see cref="WorldCache"/> for a given <see cref="World"/>
        /// Will gracefully do nothing if it doesn't exist.
        ///
        /// NOTE: You are responsible for disposing the instance if necessary.
        /// <seealso cref="Dispose"/> for a full cleanup of all instances.
        /// </summary>
        /// <param name="world">The world this instance is associated with.</param>
        public static void Remove(World world)
        {
            Lookup.Remove(world);
        }
        
        /// <summary>
        /// Returns an instance of a <see cref="WorldCache"/> for a given <see cref="World"/>
        /// Will create a new one if it doesn't already exist.
        /// </summary>
        /// <param name="world">The world to associate this instance with.</param>
        /// <returns>The <see cref="WorldCache"/> instance.</returns>
        public static WorldCache GetOrCreate(World world)
        {
            return Lookup.GetOrCreate(world);
        }
    }
}
