using Anvil.Unity.DOTS.Util;
using System;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// Helper class for managing access control to collections across multiple jobs.
    /// </summary>
    /// <remarks>
    /// Unity will handle this for you if you're dealing with <see cref="EntityQuery"/> or <see cref="IComponentData"/>
    /// but if it's just a native collection, you will have to manage your read/write access.
    ///
    /// The <see cref="IJob"/> safety system will throw errors if you handle it incorrectly but that's not a very
    /// efficient way to develop and correct.
    /// </remarks>
    public static class CollectionAccessControlUtil
    {
        //*************************************************************************************************************
        // INTERNAL INTERFACES
        //*************************************************************************************************************

        internal interface ICollectionAccessController : IDisposable
        {
        }

        private interface IContextLookup : IDisposable
        {
        }


        //*************************************************************************************************************
        // INTERNAL HELPER
        //*************************************************************************************************************

        private class WorldLookup : AbstractLookup<Type, World, TypeLookup>
        {
            private static TypeLookup CreationFunction(World world)
            {
                return new TypeLookup(world);
            }

            public WorldLookup() : base(typeof(WorldLookup))
            {
            }

            internal TypeLookup GetOrCreate(World world)
            {
                return LookupGetOrCreate(world, CreationFunction);
            }
        }

        private class TypeLookup : AbstractLookup<World, Type, IContextLookup>
        {
            private static IContextLookup CreationFunction(Type type)
            {
                return new ContextLookup<Type>(type);
            }

            internal TypeLookup(World world) : base(world)
            {
            }

            internal CollectionAccessController<TContext> GetOrCreateCollectionAccessController<TContext>(TContext context)
            {
                Type contextType = typeof(TContext);
                ContextLookup<TContext> contextLookup = (ContextLookup<TContext>)LookupGetOrCreate(contextType, CreationFunction);
                return contextLookup.GetOrCreateCollectionAccessController(context);
            }
        }

        private class ContextLookup<TContext> : AbstractLookup<Type, TContext, ICollectionAccessController>,
                                                IContextLookup
        {
            private static ICollectionAccessController CreationFunction(TContext context)
            {
                return new CollectionAccessController<TContext>(context);
            }

            internal ContextLookup(Type context) : base(context)
            {
            }

            internal CollectionAccessController<TContext> GetOrCreateCollectionAccessController(TContext context)
            {
                return (CollectionAccessController<TContext>)LookupGetOrCreate(context, CreationFunction);
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
        
        /// <summary>
        /// High level dispose to be used when exiting from an ECS/DOTS mode into another part of the application
        /// that doesn't used ECS/DOTS. All controllers will be removed from the lookup and disposed.
        /// </summary>
        public static void Dispose()
        {
            s_WorldLookup.Dispose();
        }

        //TODO: SystemBase extension method
        //TODO: World extension method
        //TODO: Remove method
        /// <summary>
        /// Returns an instance of an <see cref="CollectionAccessController{TKey}"/> for a given key.
        /// Will create a new one if it doesn't already exist.
        /// </summary>
        /// <typeparam name="TContext">A type to give context to allow for multiple keys of the same type (int, string etc)
        /// to be used in a project.</typeparam>
        /// <typeparam name="TKey">The type of key to use to get an instance of an <see cref="CollectionAccessController{TKey}"/></typeparam>
        /// <param name="key">The actual key to use</param>
        /// <returns>The <see cref="CollectionAccessController{TKey}"/> instance.</returns>
        public static CollectionAccessController<TContext> GetOrCreate<TContext>(World world, TContext context)
        {
            TypeLookup typeLookup = s_WorldLookup.GetOrCreate(world);
            return typeLookup.GetOrCreateCollectionAccessController(context);
        }

        
    }
}
