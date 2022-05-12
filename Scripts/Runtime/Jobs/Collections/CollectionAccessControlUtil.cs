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

        /// <summary>
        /// Lookup based on World's.
        /// We don't want to have <see cref="CollectionAccessController{TContext}"/>'s operating across worlds.
        /// </summary>
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

        /// <summary>
        /// Lookup based on a Type
        /// Allows an <see cref="CollectionAccessController{TContext}"/> to be created using different app specific
        /// enums for example.
        /// This is a child of <see cref="WorldLookup"/>
        /// </summary>
        private class TypeLookup : AbstractLookup<World, Type, IContextLookup>
        {
            private static IContextLookup CreationFunction<TContext>(Type context)
            {
                return new ContextLookup<TContext>(context);
            }

            internal TypeLookup(World context) : base(context)
            {
            }

            internal CollectionAccessController<TContext> GetOrCreate<TContext>(TContext context)
            {
                Type contextType = typeof(TContext);
                ContextLookup<TContext> contextLookup = (ContextLookup<TContext>)LookupGetOrCreate(contextType, CreationFunction<TContext>);
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

        /// <summary>
        /// Lookup based on a specific value of a <see cref="Type"/> from the parent <see cref="TypeLookup"/>
        /// Allows for a <see cref="CollectionAccessController{TContext}"/> to be specific to a value of an enum for
        /// example.
        /// </summary>
        internal class ContextLookup<TContext> : AbstractLookup<Type, TContext, ICollectionAccessController>,
                                                 IContextLookup
        {
            internal ContextLookup(Type context) : base(context)
            {
            }

            internal CollectionAccessController<TContext> GetOrCreate(TContext context)
            {
                return (CollectionAccessController<TContext>)LookupGetOrCreate(context, CreationFunction);
            }

            private ICollectionAccessController CreationFunction(TContext context)
            {
                return new CollectionAccessController<TContext>(context, this);
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

        //Ensures the proper state with DomainReloading turned off in the Editor
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
            s_WorldLookup?.Dispose();
            s_WorldLookup = null;
        }

        /// <summary>
        /// Removes an instance of an <see cref="CollectionAccessController{TContext}"/> for a given key.
        /// Will gracefully do nothing if it doesn't exist.
        ///
        /// NOTE: You are responsible for disposing the instance if necessary.
        /// <seealso cref="Dispose"/> for a full cleanup of all instances.
        /// </summary>
        /// <param name="world">The world this instance is associated with.</param>
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
    }
}
