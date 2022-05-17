using Anvil.Unity.DOTS.Util;
using System;
using Unity.Entities;
using UnityEngine;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// Helper class for managing access control to a <see cref="DynamicBuffer{T}"/> that you want to
    /// write to in parallel from multiple different types of jobs.
    /// </summary>
    /// <remarks>
    /// Unity by default treats a parallel job (JobTypeA run in parallel) as one job that can run over multiple
    /// threads at the same time. If you want to have more than one job such as JobTypeA and JobTypeB, Unity
    /// will not allow it unless you specify the <see cref="NativeDisableContainerSafetyRestriction"/> on
    /// your <see cref="DynamicBuffer{T}"/>.
    ///
    /// Doing so means that you can now write to the <see cref="DynamicBuffer{T}"/> from multiple job types
    /// but it assumes that you have taken the necessary precautions to ensure your jobs can't conflict with
    /// each other. Typically this means only writing to an index that matches your
    /// <see cref="NativeSetThreadIndex"/> or <see cref="ParallelCollectionUtil.CollectionIndexForThread"/>
    ///
    /// Unity's internal read and write handles for scheduling jobs assumes that all jobs are either a shared
    /// read or exclusive write. Since we want both job types to start at the same time, this
    /// <see cref="DynamicBufferSharedWriteHandle{T}"/> facilitates getting that start point while working
    /// nicely in tandem with Unity's built in system that know nothing about your shared writing.
    ///
    /// This can be accomplished with a <see cref="CollectionAccessController{TContext}"/> but it requires
    /// ALL systems in the project who touch the <see cref="DynamicBuffer{T}"/> in question to use the
    /// <see cref="CollectionAccessController{TContext}"/> which isn't always feasible.
    /// </remarks>
    public static class DynamicBufferSharedWriteManager
    {
        //*************************************************************************************************************
        // INTERNAL INTERFACES
        //*************************************************************************************************************

        internal interface IDynamicBufferSharedWriteHandle : IDisposable
        {
        }

        //*************************************************************************************************************
        // INTERNAL HELPER
        //*************************************************************************************************************

        /// <summary>
        /// Lookup based on World's.
        /// We don't want to have <see cref="DynamicBufferSharedWriteHandle{T}"/>'s operating across worlds.
        /// </summary>
        private class LookupByWorld : AbstractLookup<Type, World, LookupByComponentType>
        {
            private static LookupByComponentType CreationFunction(World world)
            {
                return new LookupByComponentType(world);
            }

            internal LookupByWorld() : base(typeof(LookupByWorld))
            {
            }

            internal LookupByComponentType GetOrCreate(World world)
            {
                return LookupGetOrCreate(world, CreationFunction);
            }
        }

        /// <summary>
        /// Lookup based on a specific <see cref="IBufferElementData"/>
        /// This is a child of <see cref="LookupByWorld"/>
        /// </summary>
        internal class LookupByComponentType : AbstractLookup<World, ComponentType, IDynamicBufferSharedWriteHandle>
        {
            internal LookupByComponentType(World context) : base(context)
            {
            }

            internal void Remove<T>()
                where T : IBufferElementData
            {
                ComponentType componentType = ComponentType.ReadWrite<T>();
                if (!ContainsKey(componentType))
                {
                    return;
                }

                LookupRemove(componentType);
            }

            // ReSharper disable once MemberHidesStaticFromOuterClass
            internal DynamicBufferSharedWriteHandle<T> GetOrCreate<T>()
                where T : IBufferElementData
            {
                ComponentType componentType = ComponentType.ReadWrite<T>();
                IDynamicBufferSharedWriteHandle handle = LookupGetOrCreate(componentType, CreationFunction<T>);

                return (DynamicBufferSharedWriteHandle<T>)handle;
            }

            private IDynamicBufferSharedWriteHandle CreationFunction<T>(ComponentType componentType)
                where T : IBufferElementData
            {
                return new DynamicBufferSharedWriteHandle<T>(componentType, Context, this);
            }
        }

        //*************************************************************************************************************
        // PUBLIC STATIC API
        //*************************************************************************************************************

        private static LookupByWorld s_LookupByWorld;

        private static LookupByWorld Lookup
        {
            get => s_LookupByWorld ?? (s_LookupByWorld = new LookupByWorld());
        }

        //Ensures the proper state with DomainReloading turned off in the Editor
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Dispose();
        }

        /// <summary>
        /// High level dispose to be used when exiting from an ECS/DOTS mode into another part of the application
        /// that doesn't use ECS/DOTS. All <see cref="DynamicBufferSharedWriteHandle{T}"/>'s will be removed
        /// from the lookup and disposed.
        /// </summary>
        public static void Dispose()
        {
            s_LookupByWorld?.Dispose();
            s_LookupByWorld = null;
        }

        /// <summary>
        /// Removes an instance of an <see cref="DynamicBufferSharedWriteHandle{T}"/> for a given
        /// <see cref="IBufferElementData"/> type.
        /// Will do nothing if it doesn't exist.
        ///
        /// NOTE: You are responsible for disposing the instance if necessary.
        /// <seealso cref="Dispose"/> for a full cleanup of all instances.
        /// </summary>
        /// <param name="world">The world that the instance is associated with.</param>
        /// <typeparam name="T">The <see cref="IBufferElementData"/> type the instance is associated with.</typeparam>
        public static void Remove<T>(World world)
            where T : IBufferElementData
        {
            if (!Lookup.TryGet(world, out LookupByComponentType lookupByComponentType))
            {
                return;
            }

            lookupByComponentType.Remove<T>();
        }

        /// <summary>
        /// Returns an instance of an <see cref="DynamicBufferSharedWriteHandle{T}"/> for a given
        /// <see cref="IBufferElementData"/> type.
        /// </summary>
        /// <param name="world">The world to associate this instance with.</param>
        /// <typeparam name="T">The type of<see cref="IBufferElementData"/> to associate this instance with.</typeparam>
        /// <returns>The <see cref="DynamicBufferSharedWriteHandle{T}"/> instance.</returns>
        public static DynamicBufferSharedWriteHandle<T> GetOrCreate<T>(World world)
            where T : IBufferElementData
        {
            LookupByComponentType lookupByComponentType = Lookup.GetOrCreate(world);
            return lookupByComponentType.GetOrCreate<T>();
        }
    }
}
