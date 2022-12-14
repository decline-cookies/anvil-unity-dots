using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Job-Safe struct to allow for updating an instance of data
    /// </summary>
    /// <typeparam name="TInstance">The <see cref="IEntityProxyInstance"/> to update.</typeparam>
    [BurstCompatible]
    public struct DataStreamUpdater<TInstance>
        where TInstance : unmanaged, IEntityProxyInstance
    {
        private const int UNSET_LANE_INDEX = -1;

        [ReadOnly] private readonly UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.Writer m_PendingWriter;
        [ReadOnly] private readonly NativeArray<EntityProxyInstanceWrapper<TInstance>> m_Active;
        [ReadOnly] private DataStreamTargetResolver m_DataStreamTargetResolver;


        private UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.LaneWriter m_PendingLaneWriter;
        private int m_LaneIndex;
        private uint m_CurrentContext;

        internal DataStreamUpdater(UnsafeTypedStream<EntityProxyInstanceWrapper<TInstance>>.Writer pendingWriter,
                                   NativeArray<EntityProxyInstanceWrapper<TInstance>> active,
                                   DataStreamTargetResolver dataStreamTargetResolver) : this()
        {
            m_PendingWriter = pendingWriter;
            m_Active = active;
            m_DataStreamTargetResolver = dataStreamTargetResolver;

            m_PendingLaneWriter = default;
            m_LaneIndex = UNSET_LANE_INDEX;

            m_CurrentContext = default;

            Debug_InitializeUpdaterState();
        }

        /// <summary>
        /// Called once per thread to allow for initialization of state in the job
        /// </summary>
        /// <remarks>
        /// In most cases this will be called automatically by the Anvil Job type. If using this in a vanilla Unity
        /// Job type, this must be called manually before any other interaction with this struct.
        /// </remarks>
        /// <param name="nativeThreadIndex">The native thread index that the job is running on</param>
        public void InitForThread(int nativeThreadIndex)
        {
            Debug_EnsureInitThreadOnlyCalledOnce();

            m_LaneIndex = ParallelAccessUtil.CollectionIndexForThread(nativeThreadIndex);
            m_PendingLaneWriter = m_PendingWriter.AsLaneWriter(m_LaneIndex);
        }


        /// <summary>
        /// Signals that this instance should be processed to update again next frame.
        /// </summary>
        /// <param name="instance">The <see cref="IEntityProxyInstance"/></param>
        public void Continue(TInstance instance)
        {
            Continue(ref instance);
        }

        /// <inheritdoc cref="Continue(TInstance)"/>
        public void Continue(ref TInstance instance)
        {
            Debug_EnsureCanContinue(ref instance);
            m_PendingLaneWriter.Write(new EntityProxyInstanceWrapper<TInstance>(instance.Entity,
                                                                                 m_CurrentContext,
                                                                                 ref instance));
        }

        internal void Resolve()
        {
            Debug_EnsureCanResolve();
        }

        internal void Resolve<TResolveTargetType>(ref TResolveTargetType resolvedInstance)
            where TResolveTargetType : unmanaged, IEntityProxyInstance
        {
            Debug_EnsureCanResolve();
            //TODO: #69 - Profile this and see if it makes sense to not bother creating a DataStreamWriter and instead
            //TODO: manually create the lane writer and handle wrapping ourselves with ProxyInstanceWrapper
            // m_DataStreamTargetResolver.Resolve(m_CurrentContext,
            //                                    m_LaneIndex,
            //                                    ref resolvedInstance);
        }
        
        internal TInstance this[int index]
        {
            get
            {
                Debug_EnsureCanUpdate();
                EntityProxyInstanceWrapper<TInstance> instanceWrapper = m_Active[index];
                m_CurrentContext = instanceWrapper.InstanceID.Context;
                return instanceWrapper.Payload;
            }
        }


        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private enum UpdaterState
        {
            Uninitialized,
            Ready,
            Modifying
        }

        private UpdaterState m_State;
#endif

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_InitializeUpdaterState()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_State = UpdaterState.Uninitialized;
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureCanUpdate()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (m_State == UpdaterState.Uninitialized)
            {
                throw new InvalidOperationException($"{nameof(InitForThread)} must be called first before attempting to get an element.");
            }

            if (m_State == UpdaterState.Modifying)
            {
                throw new InvalidOperationException($"Trying to get an element but the previous element wasn't handled. Please ensure that {nameof(EntityProxyInstanceExtension.ContinueOn)} or {nameof(EntityProxyInstanceExtension.Resolve)} gets called before the next iteration.");
            }

            m_State = UpdaterState.Modifying;
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureCanContinue(ref TInstance instance)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (m_State == UpdaterState.Uninitialized)
            {
                throw new InvalidOperationException($"{nameof(InitForThread)} must be called first before attempting to continue an element.");
            }

            if (m_State == UpdaterState.Ready)
            {
                throw new InvalidOperationException($"Attempting to call {nameof(Continue)} on a {instance} but that element didn't come from this {nameof(DataStreamUpdater<TInstance>)}. Please ensure that the indexer was called first.");
            }

            m_State = UpdaterState.Ready;
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureCanResolve()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (m_State == UpdaterState.Uninitialized)
            {
                throw new InvalidOperationException($"{nameof(InitForThread)} must be called first before attempting to resolve an element.");
            }

            if (m_State == UpdaterState.Ready)
            {
                throw new InvalidOperationException($"Attempting to call {nameof(Resolve)} for an element that didn't come from this {nameof(DataStreamUpdater<TInstance>)}. Please ensure that the indexer was called first.");
            }

            if (m_State != UpdaterState.Modifying)
            {
                throw new InvalidOperationException($"Caught unhandled state {m_State}");
            }

            m_State = UpdaterState.Ready;
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureInitThreadOnlyCalledOnce()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_State != UpdaterState.Uninitialized)
            {
                throw new InvalidOperationException($"{nameof(InitForThread)} has already been called!");
            }

            m_State = UpdaterState.Ready;
#endif
        }
    }
}
