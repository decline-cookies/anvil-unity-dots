using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Util;
using System;
using System.Collections.Generic;
using System.Net;
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

        internal interface IContextLookup : IDisposable
        {
            
        }
        
        
        //*************************************************************************************************************
        // INTERNAL HELPER
        //*************************************************************************************************************

        private class WorldLookup : AbstractLookup<World, Type, IContextLookup>
        {
            private static IContextLookup CreationFunction(Type type)
            {
                return new ContextLookup<Type>(type);
            }
            
            internal WorldLookup(World world) : base(world)
            {
            }

            internal CollectionAccessController<TContext> GetOrCreateCollectionAccessController<TContext>(TContext context)
            {
                Type contextType = typeof(TContext);
                ContextLookup<TContext> contextLookup = (ContextLookup<TContext>)GetOrCreate(contextType, CreationFunction);
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
                return (CollectionAccessController<TContext>)GetOrCreate(context, CreationFunction);
            }
            
        }


        //*************************************************************************************************************
        // PUBLIC STATIC API
        //*************************************************************************************************************

        private static readonly Dictionary<World, WorldLookup> s_WorldLookups = new Dictionary<World, WorldLookup>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Dispose();
        }
        
        //TODO: SystemBase extension method
        //TODO: World extension method
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
            WorldLookup worldLookup = GetOrCreateWorldLookup(world);
            return worldLookup.GetOrCreateCollectionAccessController(context);
        }

        /// <summary>
        /// High level dispose to be used when exiting from an ECS/DOTS mode into another part of the application
        /// that doesn't used ECS/DOTS. All controllers will be removed from the lookup and disposed.
        /// </summary>
        public static void Dispose()
        {
            foreach (WorldLookup worldLookup in s_WorldLookups.Values)
            {
                worldLookup.Dispose();
            }

            s_WorldLookups.Clear();
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
