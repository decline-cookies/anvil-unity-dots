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

            internal CollectionAccessController<TContext> GetOrCreate<TContext>(TContext context)
            {
                Type contextType = typeof(TContext);
                ContextLookup<TContext> contextLookup = (ContextLookup<TContext>)LookupGetOrCreate(contextType, CreationFunction);
                return contextLookup.GetOrCreate(context);
            }

            internal void Remove<TContext>(TContext context)
            {
                Type contextType = typeof(TContext);
                if (!TryGet(contextType, out IContextLookup contextLookup))
                {
                    return;
                }

                ((ContextLookup<TContext>)contextLookup).Remove(context);
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

            internal CollectionAccessController<TContext> GetOrCreate(TContext context)
            {
                return (CollectionAccessController<TContext>)LookupGetOrCreate(context, CreationFunction);
            }
            
            internal void Remove(TContext context)
            {
                if (!ContainsKey(context))
                {
                    return;
                }

                LookupRemove(context);
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
        
        /// <summary>
        /// High level dispose to be used when exiting from an ECS/DOTS mode into another part of the application
        /// that doesn't use ECS/DOTS. All <see cref="CollectionAccessController{TContext}"/> will be removed
        /// from the lookup and disposed.
        /// </summary>
        public static void Dispose()
        {
            s_WorldLookup.Dispose();
            s_WorldLookup = null;
        }
        
        /// <summary>
        /// Removes an instance of an <see cref="CollectionAccessController{TContext}"/> for a given key.
        /// Will gracefully do nothing if it doesn't exist.
        ///
        /// NOTE: You are responsible for disposing the instance if necessary.
        /// </summary>
        /// <param name="world">The world to associate this instance with.</param>
        /// <param name="context">The key to use to lookup the instance.</param>
        /// <typeparam name="TContext">The type of key to use to get an instance of an <see cref="CollectionAccessController{TContext}"/></typeparam>
        public static void Remove<TContext>(World world, TContext context)
        {
            if (!Lookup.TryGet(world, out TypeLookup typeLookup))
            {
                return;
            }
            typeLookup.Remove(context);
        }
        
        /// <inheritdoc cref="Remove{TContext}"/>
        public static void RemoveCollectionAccessController<TContext>(this World world, TContext context)
        {
            Remove(world, context);
        }

        /// <inheritdoc cref="Remove{TContext}"/>
        /// <param name="systemBase">A <see cref="SystemBase"/> to get the <paramref name="world"/> for association</param>
        public static void RemoveCollectionAccessController<TContext>(this SystemBase systemBase, TContext context)
        {
            Remove(systemBase.World, context);
        }
        
        /// <summary>
        /// Returns an instance of an <see cref="CollectionAccessController{TContext}"/> for a given key.
        /// Will create a new one if it doesn't already exist.
        /// </summary>
        /// <param name="world">The world to associate this instance with.</param>
        /// <param name="context">The key to use to lookup the instance.</param>
        /// <typeparam name="TContext">The type of key to use to get an instance of an <see cref="CollectionAccessController{TContext}"/></typeparam>
        /// <returns>The <see cref="CollectionAccessController{TKey}"/> instance.</returns>
        public static CollectionAccessController<TContext> GetOrCreate<TContext>(World world, TContext context)
        {
            TypeLookup typeLookup = Lookup.GetOrCreate(world);
            return typeLookup.GetOrCreate(context);
        }
        
        /// <inheritdoc cref="GetOrCreate{TContext}"/>
        public static CollectionAccessController<TContext> GetOrCreateCollectionAccessController<TContext>(this World world, TContext context)
        {
            return GetOrCreate(world, context);
        }
        
        /// <inheritdoc cref="GetOrCreate{TContext}"/>
        /// <param name="systemBase">A <see cref="SystemBase"/> to get the <paramref name="world"/> for association</param>
        public static CollectionAccessController<TContext> GetOrCreateCollectionAccessController<TContext>(this SystemBase systemBase, TContext context)
        {
            return GetOrCreate(systemBase.World, context);
        }


    }
}
