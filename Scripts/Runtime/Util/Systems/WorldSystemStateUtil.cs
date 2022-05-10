using System;
using Unity.Entities;
using UnityEngine;

namespace Anvil.Unity.DOTS.Util
{
    public static class WorldSystemStateUtil
    {
        //*************************************************************************************************************
        // INTERNAL HELPER
        //*************************************************************************************************************

        private class WorldLookup : AbstractLookup<Type, World, WorldSystemState>
        {
            private static WorldSystemState CreationFunction(World world)
            {
                return new WorldSystemState(world);
            }

            public WorldLookup() : base(typeof(WorldLookup))
            {
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public WorldSystemState GetOrCreate(World world)
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

        public static WorldSystemState GetOrCreate(World world)
        {
            return Lookup.GetOrCreate(world);
        }
        //TODO: Extensions
    }
}
