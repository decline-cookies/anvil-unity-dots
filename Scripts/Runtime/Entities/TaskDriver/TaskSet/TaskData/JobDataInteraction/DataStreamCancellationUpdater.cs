using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Job-Safe struct to allow for cancelling an instance of data
    /// </summary>
    /// <typeparam name="TInstance">The <see cref="IEntityKeyedTask"/> to cancel</typeparam>
    [BurstCompatible]
    public struct DataStreamCancellationUpdater<TInstance> where TInstance : unmanaged, IEntityKeyedTask
    {
        private const int UNSET_LANE_INDEX = -1;
        [ReadOnly] private readonly UnsafeTypedStream<EntityKeyedTaskWrapper<TInstance>>.Writer m_PendingWriter;
        [ReadOnly] private readonly NativeArray<EntityKeyedTaskWrapper<TInstance>> m_Active;
        [ReadOnly] private ResolveTargetTypeLookup m_ResolveTargetTypeLookup;

        private UnsafeParallelHashMap<EntityKeyedTaskID, bool> m_CancelProgressLookup;
        private UnsafeTypedStream<EntityKeyedTaskWrapper<TInstance>>.LaneWriter m_PendingLaneWriter;
        private int m_LaneIndex;
        private DataOwnerID m_CurrentDataOwnerID;
        private DataTargetID m_CurrentDataTargetID;

        internal DataStreamCancellationUpdater(
            UnsafeTypedStream<EntityKeyedTaskWrapper<TInstance>>.Writer pendingWriter,
            NativeArray<EntityKeyedTaskWrapper<TInstance>> active,
            ResolveTargetTypeLookup resolveTargetTypeLookup,
            UnsafeParallelHashMap<EntityKeyedTaskID, bool> cancelProgressLookup) : this()
        {
            m_PendingWriter = pendingWriter;
            m_Active = active;
            m_ResolveTargetTypeLookup = resolveTargetTypeLookup;
            m_CancelProgressLookup = cancelProgressLookup;

            m_PendingLaneWriter = default;
            m_LaneIndex = UNSET_LANE_INDEX;

            m_CurrentDataOwnerID = default;
            m_CurrentDataTargetID = default;

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
        /// Signals that this instance should be processed to cancel again next frame.
        /// </summary>
        /// <param name="instance">The <see cref="IEntityKeyedTask"/></param>
        public void Continue(TInstance instance)
        {
            Continue(ref instance);
        }

        /// <inheritdoc cref="Continue(TInstance)"/>
        public void Continue(ref TInstance instance)
        {
            Debug_EnsureCanContinue(ref instance);
            m_PendingLaneWriter.Write(
                new EntityKeyedTaskWrapper<TInstance>(
                    instance.Key,
                    m_CurrentDataOwnerID,
                    m_CurrentDataTargetID,
                    ref instance));


            //Hold open the progress so we can keep processing
            EntityKeyedTaskID id = new EntityKeyedTaskID(instance.Key, m_CurrentDataOwnerID, m_CurrentDataTargetID);
            Debug_EnsureIDIsPresent(id);
            m_CancelProgressLookup[id] = true;
        }

        internal void Resolve()
        {
            Debug_EnsureCanResolve();
        }

        internal void Resolve<TResolveTargetType>(ref TResolveTargetType resolvedInstance)
            where TResolveTargetType : unmanaged, IEntityKeyedTask
        {
            Debug_EnsureCanResolve();
            //TODO: #69 - Profile this and see if it makes sense to not bother creating a DataStreamWriter and instead
            //TODO: manually create the lane writer and handle wrapping ourselves with ProxyInstanceWrapper
            m_ResolveTargetTypeLookup.Resolve(
                m_CurrentDataOwnerID,
                m_LaneIndex,
                ref resolvedInstance);
        }

        internal TInstance this[int index]
        {
            get
            {
                Debug_EnsureCanUpdate();
                EntityKeyedTaskWrapper<TInstance> instanceWrapper = m_Active[index];
                m_CurrentDataOwnerID = instanceWrapper.InstanceID.DataOwnerID;
                m_CurrentDataTargetID = instanceWrapper.InstanceID.DataTargetID;
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
        private void Debug_EnsureIDIsPresent(EntityKeyedTaskID id)
        {
            if (!m_CancelProgressLookup.ContainsKey(id))
            {
                throw new InvalidOperationException($"Tried to hold open {id} so that cancelling can continue but the entry doesn't exist in the lookup!");
            }
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
                throw new InvalidOperationException($"Trying to get an element but the previous element wasn't handled. Please ensure that {nameof(EntityKeyedTaskExtension.ContinueOn)} or {nameof(EntityKeyedTaskExtension.Resolve)} gets called before the next iteration.");
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
                throw new InvalidOperationException($"Attempting to call {nameof(Continue)} on a {instance} but our state is {m_State}. Most likely {nameof(Continue)} was called twice or {nameof(Resolve)} was called already for this instance. Alternatively, this instance may not belong to this {nameof(DataStreamCancellationUpdater<TInstance>)}");
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
                throw new InvalidOperationException($"Attempting to call {nameof(Resolve)} on an instance but our state is {m_State}. Most likely {nameof(Resolve)} was called twice or {nameof(Continue)} was called already for this instance. Alternatively, this instance may not belong to this {nameof(DataStreamCancellationUpdater<TInstance>)}");
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