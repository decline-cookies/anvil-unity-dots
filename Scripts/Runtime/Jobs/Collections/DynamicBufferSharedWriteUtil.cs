using Anvil.Unity.DOTS.Util;
using System;
using Unity.Entities;
using UnityEngine;

namespace Anvil.Unity.DOTS.Jobs
{
    public static class DynamicBufferSharedWriteUtil
    {
        //*************************************************************************************************************
        // INTERNAL INTERFACES
        //*************************************************************************************************************

        internal interface IDynamicBufferSharedWriteHandle : IDisposable
        {
            void RegisterSystemForSharedWrite(ComponentSystemBase system);
        }


        //*************************************************************************************************************
        // INTERNAL HELPER
        //*************************************************************************************************************

        private class WorldLookup : AbstractLookup<Type, World, ComponentTypeLookup>
        {
            private static ComponentTypeLookup CreationFunction(World world)
            {
                return new ComponentTypeLookup(world);
            }

            public WorldLookup() : base(typeof(WorldLookup))
            {
            }

            internal ComponentTypeLookup GetOrCreate(World world)
            {
                return LookupGetOrCreate(world, CreationFunction);
            }
        }

        private class ComponentTypeLookup : AbstractLookup<World, ComponentType, IDynamicBufferSharedWriteHandle>
        {
            public ComponentTypeLookup(World context) : base(context)
            {
            }

            public DynamicBufferSharedWriteHandle<T> GetOrCreate<T>(ComponentSystemBase systemBase)
                where T : IBufferElementData
            {
                ComponentType componentType = ComponentType.ReadWrite<T>();

                IDynamicBufferSharedWriteHandle handle = LookupGetOrCreate(componentType, CreationFunction<T>);
                handle.RegisterSystemForSharedWrite(systemBase);

                return (DynamicBufferSharedWriteHandle<T>)handle;
            }

            private IDynamicBufferSharedWriteHandle CreationFunction<T>(ComponentType componentType)
                where T : IBufferElementData
            {
                return new DynamicBufferSharedWriteHandle<T>(componentType, Context);
            }
        }

        //*************************************************************************************************************
        // PUBLIC STATIC API
        //*************************************************************************************************************

        private static WorldLookup s_WorldLookup = new WorldLookup();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Dispose();
            s_WorldLookup = new WorldLookup();
        }

        public static void Dispose()
        {
            s_WorldLookup.Dispose();
        }

        public static DynamicBufferSharedWriteHandle<T> RegisterForSharedWrite<T>(SystemBase systemBase)
            where T : IBufferElementData
        {
            ComponentTypeLookup componentTypeLookup = s_WorldLookup.GetOrCreate(systemBase.World);
            return componentTypeLookup.GetOrCreate<T>(systemBase);
        }
    }
}
