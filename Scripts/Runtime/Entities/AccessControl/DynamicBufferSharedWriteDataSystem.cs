using Unity.Entities;
using Anvil.CSharp.Data;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Data System (no update) for managing access control to a <see cref="DynamicBuffer{T}"/> that you want to
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
    /// <see cref="DynamicBufferSharedWriteController{T}"/> facilitates getting that start point while working
    /// nicely in tandem with Unity's built in system that know nothing about your shared writing.
    ///
    /// This can be accomplished with a <see cref="CollectionAccessController{TContext}"/> but it requires
    /// ALL systems in the project who touch the <see cref="DynamicBuffer{T}"/> in question to use the
    /// <see cref="CollectionAccessController{TContext}"/> which isn't always feasible.
    /// </remarks>
    public partial class DynamicBufferSharedWriteDataSystem : AbstractDataSystem
    {
        private LookupByComponentType m_LookupByComponentType;

        internal DynamicBufferSharedWriteController<T> GetOrCreate<T>()
            where T : IBufferElementData
        {
            return m_LookupByComponentType.GetOrCreate<T>();
        }

        protected override void Init()
        {
            m_LookupByComponentType = new LookupByComponentType(World);
        }

        protected override void OnDestroy()
        {
            m_LookupByComponentType.Dispose();
            base.OnDestroy();
        }


        //*************************************************************************************************************
        // INTERNAL HELPER
        //*************************************************************************************************************

        /// <summary>
        /// Lookup based on a specific <see cref="IBufferElementData"/>
        /// </summary>
        internal class LookupByComponentType : AbstractLookup<World, ComponentType, IDynamicBufferSharedWriteController>
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
            internal DynamicBufferSharedWriteController<T> GetOrCreate<T>()
                where T : IBufferElementData
            {
                ComponentType componentType = ComponentType.ReadWrite<T>();
                IDynamicBufferSharedWriteController controller = LookupGetOrCreate(componentType, CreationFunction<T>);

                return (DynamicBufferSharedWriteController<T>)controller;
            }

            private IDynamicBufferSharedWriteController CreationFunction<T>(ComponentType componentType)
                where T : IBufferElementData
            {
                return new DynamicBufferSharedWriteController<T>(componentType, Context, this);
            }
        }
    }
}
