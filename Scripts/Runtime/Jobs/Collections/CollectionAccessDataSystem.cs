using Anvil.Unity.DOTS.Util;
using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// Data System (no update) for managing access control to collections across multiple jobs.
    /// </summary>
    /// <remarks>
    /// Unity will handle this for you if you're dealing with <see cref="EntityQuery"/>
    /// but if it's just a native collection, you will have to manage your read/write access.
    ///
    /// The safety system
    /// - <see cref="SystemDependencySafetyUtility"/>
    /// - <see cref="AtomicSafetyHandle"/>
    /// - <see cref="ComponentDependencyManager"/>)
    /// will throw errors if you handle it incorrectly but manually managing dependencies is messy and error prone.
    /// </remarks>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class CollectionAccessDataSystem : SystemBase
    {
        private LookupByType m_LookupByType;

        /// <summary>
        /// Returns an instance of an <see cref="CollectionAccessController{TContext}"/> for a given key.
        /// Will create a new one if it doesn't already exist.
        /// </summary>
        /// <param name="context">The key to use to lookup the instance.</param>
        /// <typeparam name="TContext">The type of key to use to get an instance of an <see cref="CollectionAccessController{TContext}"/></typeparam>
        /// <returns>The <see cref="CollectionAccessController{TKey}"/> instance.</returns>
        public CollectionAccessController<TContext> GetOrCreate<TContext>(TContext context)
        {
            return m_LookupByType.GetOrCreate(context);
        }

        /// <summary>
        /// Removes an instance of a <see cref="CollectionAccessController{TContext}"/> for a given key.
        /// Will do nothing if it doesn't exist.
        ///
        /// NOTE: You are responsible for disposing the instance if necessary.
        /// </summary>
        /// <param name="context">The key to use to lookup the instance.</param>
        /// <typeparam name="TContext">The type of key to use to get an instance of an <see cref="CollectionAccessController{TContext}"/></typeparam>
        public void Remove<TContext>(TContext context)
        {
            m_LookupByType.Remove(context);
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            m_LookupByType = new LookupByType(World);
        }

        protected override void OnDestroy()
        {
            m_LookupByType.Dispose();
            base.OnDestroy();
        }

        protected override void OnUpdate()
        {
        }

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
        /// Lookup based on a Type
        /// Allows an <see cref="CollectionAccessController{TContext}"/> to be created using different app specific
        /// enums for example.
        /// </summary>
        private class LookupByType : AbstractLookup<World, Type, IContextLookup>
        {
            private static IContextLookup CreationFunction<TContext>(Type context)
            {
                return new LookupByContext<TContext>(context);
            }

            internal LookupByType(World context) : base(context)
            {
            }

            internal CollectionAccessController<TContext> GetOrCreate<TContext>(TContext context)
            {
                Type contextType = typeof(TContext);
                LookupByContext<TContext> lookupByContext = (LookupByContext<TContext>)LookupGetOrCreate(contextType, CreationFunction<TContext>);
                return lookupByContext.GetOrCreate(context);
            }

            internal void Remove<TContext>(TContext context)
            {
                Type contextType = typeof(TContext);
                if (!TryGet(contextType, out IContextLookup contextLookup))
                {
                    return;
                }

                ((LookupByContext<TContext>)contextLookup).Remove(context);
            }
        }

        /// <summary>
        /// Lookup based on a specific value of a <see cref="Type"/> from the parent <see cref="LookupByType"/>
        /// Allows for a <see cref="CollectionAccessController{TContext}"/> to be specific to a value of an enum for
        /// example.
        /// </summary>
        internal class LookupByContext<TContext> : AbstractLookup<Type, TContext, ICollectionAccessController>,
                                                   IContextLookup
        {
            internal LookupByContext(Type context) : base(context)
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
    }
}
