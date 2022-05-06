using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Anvil.Unity.DOTS.Jobs
{
    public static class DynamicBufferSharedWriteUtil
    {
        //*************************************************************************************************************
        // INTERNAL INTERFACES
        //*************************************************************************************************************

        internal interface IDynamicBufferSharedWriteHandle
        {
            void RegisterSystemForSharedWrite(ComponentSystemBase system);
        }
        
        
        //*************************************************************************************************************
        // INTERNAL HELPER
        //*************************************************************************************************************

        private class WorldLookup
        {
            private readonly Dictionary<ComponentType, IDynamicBufferSharedWriteHandle> m_Lookup = new Dictionary<ComponentType, IDynamicBufferSharedWriteHandle>();

            public World World
            {
                get;
            }

            public WorldLookup(World world)
            {
                World = world;
            }

            public DynamicBufferSharedWriteHandle<T> GetOrCreate<T>(ComponentSystemBase systemBase)
                where T : IBufferElementData
            {
                ComponentType componentType = ComponentType.ReadWrite<T>();
                if (!m_Lookup.TryGetValue(componentType, out IDynamicBufferSharedWriteHandle handle))
                {
                    handle = new DynamicBufferSharedWriteHandle<T>(World);
                    m_Lookup.Add(componentType, handle);
                }
          
                handle.RegisterSystemForSharedWrite(systemBase);

                return (DynamicBufferSharedWriteHandle<T>)handle;
            }

        }

        //*************************************************************************************************************
        // PUBLIC STATIC API
        //*************************************************************************************************************

        private static readonly Dictionary<World, WorldLookup> s_WorldLookups = new Dictionary<World, WorldLookup>();
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            //TODO: Disposal?
            s_WorldLookups.Clear();
        }


        public static DynamicBufferSharedWriteHandle<T> RegisterForSharedWrite<T>(SystemBase systemBase)
            where T : IBufferElementData
        {
            WorldLookup worldLookup = GetOrCreateWorldLookup(systemBase.World);
            return worldLookup.GetOrCreate<T>(systemBase);
        }

        private static WorldLookup GetOrCreateWorldLookup(World world)
        {
            if (!s_WorldLookups.TryGetValue(world, out WorldLookup worldLookup))
            {
                worldLookup = new WorldLookup(world);
                s_WorldLookups.Add(world, worldLookup);
            }

            return worldLookup;
        }

    }

    
}
