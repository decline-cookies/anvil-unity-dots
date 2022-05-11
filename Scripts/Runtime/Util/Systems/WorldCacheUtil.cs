using System;
using Unity.Entities;
using UnityEngine;

namespace Anvil.Unity.DOTS.Util
{
    public static class WorldCacheUtil
    {
        //*************************************************************************************************************
        // INTERNAL HELPER
        //*************************************************************************************************************

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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Dispose();
        }

        public static void Dispose()
        {
            s_WorldLookup?.Dispose();
            s_WorldLookup = null;
        }

        public static void Remove(World world)
        {
            Lookup.Remove(world);
        }
        //TODO: Extensions

        public static WorldCache GetOrCreate(World world)
        {
            return Lookup.GetOrCreate(world);
        }
        //TODO: Extensions
    }
}
