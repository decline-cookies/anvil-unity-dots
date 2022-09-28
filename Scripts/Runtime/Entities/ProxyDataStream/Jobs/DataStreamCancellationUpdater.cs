using Anvil.Unity.DOTS.Data;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities
{
    [BurstCompatible]
    public struct DataStreamCancellationUpdater<TInstance>
        where TInstance : unmanaged, IProxyInstance
    {
        private const int UNSET_LANE_INDEX = -1;
        [ReadOnly] private readonly UnsafeTypedStream<ProxyInstanceWrapper<TInstance>>.Writer m_ContinueWriter;
        [ReadOnly] private readonly NativeArray<ProxyInstanceWrapper<TInstance>> m_Iteration;
        [ReadOnly] private DataStreamTargetResolver m_DataStreamTargetResolver;
        
        private UnsafeTypedStream<ProxyInstanceWrapper<TInstance>>.LaneWriter m_ContinueLaneWriter;
        private int m_LaneIndex;
        private byte m_CurrentContext;

        internal DataStreamCancellationUpdater(UnsafeTypedStream<ProxyInstanceWrapper<TInstance>>.Writer continueWriter,
                                               NativeArray<ProxyInstanceWrapper<TInstance>> iteration,
                                               DataStreamTargetResolver dataStreamTargetResolver) : this()
        {
            m_ContinueWriter = continueWriter;
            m_Iteration = iteration;
            m_DataStreamTargetResolver = dataStreamTargetResolver;
            
            m_ContinueLaneWriter = default;
            m_LaneIndex = UNSET_LANE_INDEX;

            m_CurrentContext = default;
            
            Debug_InitializeUpdaterState();
        }
        
        public void InitForThread(int nativeThreadIndex)
        {
            Debug_EnsureInitThreadOnlyCalledOnce();

            m_LaneIndex = ParallelAccessUtil.CollectionIndexForThread(nativeThreadIndex);
            m_ContinueLaneWriter = m_ContinueWriter.AsLaneWriter(m_LaneIndex);
        }
        
        public void Continue(TInstance instance)
        {
            Continue(ref instance);
        }
        
        public void Continue(ref TInstance instance)
        {
            Debug_EnsureCanContinue(ref instance);
            m_ContinueLaneWriter.Write(new ProxyInstanceWrapper<TInstance>(instance.Entity,
                                                                           m_CurrentContext,
                                                                           ref instance));
        }
        
        internal void Resolve()
        {
            Debug_EnsureCanResolve();
        }
        
        internal void Resolve<TResolveTarget, TResolvedInstance>(TResolveTarget resolveTarget,
                                                                 ref TResolvedInstance resolvedInstance)
            where TResolveTarget : Enum
            where TResolvedInstance : unmanaged, IProxyInstance
        {
            Debug_EnsureCanResolve();
            //TODO: Profile this and see if it makes sense to not bother creating a DataStreamWriter and instead
            //TODO: manually create the lane writer and handle wrapping ourselves with ProxyInstanceWrapper
            m_DataStreamTargetResolver.Resolve(resolveTarget,
                                               m_CurrentContext,
                                               m_LaneIndex,
                                               ref resolvedInstance);
        }
        
        internal TInstance this[int index]
        {
            get
            {
                Debug_EnsureCanUpdate();
                ProxyInstanceWrapper<TInstance> instanceWrapper = m_Iteration[index];
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
                throw new InvalidOperationException($"Trying to get an element but the previous element wasn't handled. Please ensure that {nameof(ProxyDataStreamExtension.ContinueOn)} or {nameof(ProxyDataStreamExtension.Resolve)} gets called before the next iteration.");
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

            Debug.Assert(m_State == UpdaterState.Modifying);
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
