using Anvil.CSharp.Core;
using System;
using System.Diagnostics;
using Unity.Jobs;
using Debug = UnityEngine.Debug;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// A utility class that handles managing access for reading and writing so that jobs can be scheduled easily.
    /// </summary>
    /// <remarks>
    /// This can be a little bit complicated to wrap your head around so here is an example
    ///
    /// ----- EXTERNAL WRITE PHASE -----
    /// - Multiple different systems want to write to the collection.
    /// - They call <see cref="Acquire"/> with access type of <see cref="AccessType.SharedWrite"/>.
    /// - This means that all those jobs can start at the same time.
    /// - All of those jobs use <see cref="Release"/> to let the <see cref="CollectionAccessController{TContext, TKey}"/> know that there is parallel writing going on. We cannot read or exclusive write until this done.
    ///
    /// ----- INTERNAL WRITE PHASE -----
    /// - A managing system now needs to do some work where it reads from and writes to the collection.
    /// - It schedules it's job to do that using <see cref="Acquire"/> with access type of <see cref="AccessType.ExclusiveWrite"/>
    /// - This means that it can do it's work once all the previous external writers and/or readers have completed.
    /// - This job then uses <see cref="Release"/> to the let the <see cref="CollectionAccessController{TContext, TKey}"/> know that there an exclusive write going on that cannot be interrupted by parallel writes or reads.
    ///
    /// ----- EXTERNAL READ PHASE -----
    /// - Multiple different systems want to read from the collection.
    /// - They schedule their reading jobs using <see cref="Acquire"/> with access type of <see cref="AccessType.SharedRead"/>
    /// - This means that all those reading jobs can start at the same time.
    /// - All of those jobs use <see cref="Release"/> to let the <see cref="CollectionAccessController{TContext, TKey}"/> know that there is reading going on. We cannot write again until this is done.
    ///
    /// ----- CLEAN UP PHASE -----
    /// - The collection used above needs to be disposed but we need to ensure all reading and writing are complete.
    /// - The collection disposes using <see cref="Acquire"/> with access type of <see cref="AccessType.ForDisposal"/>
    /// - This means that all reading and writing from the collection has been completed. It is safe to dispose as no one is using it anymore and further calls to <see cref="Acquire"/> will fail unless <see cref="Reset"/> is called.
    /// </remarks>
    public class CollectionAccessController<TContext, TKey> : AbstractAnvilBase
    {
        private enum AcquisitionState
        {
            Unacquired,
            ExclusiveWrite,
            SharedWrite,
            SharedRead,
            Disposing
        }

        /// <summary>
        /// Dispatched when this <see cref="CollectionAccessController{TContext,TKey}"/> is disposed.
        /// </summary>
        public Action<CollectionAccessController<TContext, TKey>> OnDisposed;

        private JobHandle m_ExclusiveWriteDependency;
        private JobHandle m_SharedWriteDependency;
        private JobHandle m_SharedReadDependency;

        private AcquisitionState m_State;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private string m_AcquireCallerInfo;
        private string m_ReleaseCallerInfo;
        private JobHandle m_LastHandleAcquired;
#endif

        /// <summary>
        /// The key used for lookup in the <see cref="CollectionAccessControl{TContext,TKey}"/> lookup.
        /// </summary>
        public TKey Key
        {
            get;
        }

        /// <summary>
        /// Creates a new instance of a <see cref="CollectionAccessController{TContext,TKey}"/>
        /// </summary>
        /// <param name="key">The key to use for looking up this <see cref="CollectionAccessController{TContext,TKey}"/></param>
        internal CollectionAccessController(TKey key)
        {
            Key = key;
        }

        protected override void DisposeSelf()
        {
            // NOTE: If these asserts trigger we should think about calling Complete() on these job handles.
            Debug.Assert(m_ExclusiveWriteDependency.IsCompleted, "The exclusive write access dependency is not completed");
            Debug.Assert(m_SharedWriteDependency.IsCompleted, "The shared write access dependency is not completed");
            Debug.Assert(m_SharedReadDependency.IsCompleted, "The shared read access dependency is not completed");

            OnDisposed?.Invoke(this);
            OnDisposed = null;

            base.DisposeSelf();
        }

        /// <summary>
        /// Resets the internal state of this <see cref="CollectionAccessController{TContext,TKey}"/> so that it can
        /// be used again. 
        /// </summary>
        /// <remarks>
        /// Typically this is used in cases where the underlying collection that you are using the
        /// <see cref="CollectionAccessController{TContext,TKey}"/> to gate access to is created/destroyed each frame
        /// or is being double buffered. The collection itself needs to be disposed and recreated but from the higher
        /// level point of view of reading and writing to "data" we want to use the same access controller.
        ///
        /// You would <see cref="Acquire"/> with <see cref="AccessType.ForDisposal"/> and then dispose the old
        /// collection and then call <see cref="Reset"/> on the <see cref="CollectionAccessController{TContext,TKey}"/>
        /// to reflect that there is a new collection to read from/write to.
        /// </remarks>
        /// <param name="initialDependency">Optional initial dependency to set access to.</param>
        public void Reset(JobHandle initialDependency = default)
        {
            m_State = AcquisitionState.Unacquired;
            m_ExclusiveWriteDependency = m_SharedWriteDependency = m_SharedReadDependency = initialDependency;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_AcquireCallerInfo = string.Empty;
            m_ReleaseCallerInfo = string.Empty;
            m_LastHandleAcquired = default;
#endif
        }

        /// <summary>
        /// Returns a <see cref="JobHandle"/> to schedule a job off of based on the desired <see cref="AccessType"/>.
        /// </summary>
        /// <param name="accessType">The type of access to schedule the job off of.</param>
        /// <returns>The <see cref="JobHandle"/> to wait upon.</returns>
        public JobHandle Acquire(AccessType accessType)
        {
            Debug.Assert(!IsDisposed);
            ValidateAcquireState();

            JobHandle acquiredHandle = default;
            switch (accessType)
            {
                case AccessType.ForDisposal:
                    m_State = AcquisitionState.Disposing;
                    acquiredHandle = m_ExclusiveWriteDependency;
                    break;
                case AccessType.ExclusiveWrite:
                    m_State = AcquisitionState.ExclusiveWrite;
                    acquiredHandle = m_ExclusiveWriteDependency;
                    break;
                case AccessType.SharedWrite:
                    m_State = AcquisitionState.SharedWrite;
                    acquiredHandle = m_SharedWriteDependency;
                    break;
                case AccessType.SharedRead:
                    m_State = AcquisitionState.SharedRead;
                    acquiredHandle = m_SharedReadDependency;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(accessType), accessType, $"Tried to acquire with {nameof(AccessType)} of {accessType} but no code path satisfies!");
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_LastHandleAcquired = acquiredHandle;
#endif

            return acquiredHandle;
        }

        /// <summary>
        /// Allows the <see cref="CollectionAccessController{TContext,TKey}"/> to be aware of the work that you are doing
        /// for a specific <see cref="AccessType"/>. You must call <see cref="Release"/> after any call to <see cref="Acquire"/>
        /// before you call <see cref="Acquire"/> again.
        /// </summary>
        /// <param name="releaseAccessDependency">The <see cref="JobHandle"/> to the job that is doing the reading or
        /// writing from/to the underlying collection this <see cref="CollectionAccessController{TContext,TKey}"/>
        /// is gating access to.</param>
        public void Release(JobHandle releaseAccessDependency)
        {
            Debug.Assert(!IsDisposed);
            ValidateReleaseState(releaseAccessDependency);

            switch (m_State)
            {
                case AcquisitionState.Disposing:
                case AcquisitionState.ExclusiveWrite:
                    //If you were exclusively writing, then no one else can do any writing or reading until you're done
                    //In the disposing case, we set to the same dependencies but gate in the checks
                    m_ExclusiveWriteDependency = JobHandle.CombineDependencies(m_ExclusiveWriteDependency, releaseAccessDependency);
                    m_SharedWriteDependency = m_ExclusiveWriteDependency;
                    m_SharedReadDependency = m_ExclusiveWriteDependency;
                    break;
                case AcquisitionState.SharedWrite:
                    //If you were shared writing, then no one else can exclusive write or read until you're done
                    m_ExclusiveWriteDependency = JobHandle.CombineDependencies(m_ExclusiveWriteDependency, releaseAccessDependency);
                    m_SharedReadDependency = m_ExclusiveWriteDependency;
                    break;
                case AcquisitionState.SharedRead:
                    //If you were reading, then no one else can do any writing until your reading is done
                    m_ExclusiveWriteDependency = JobHandle.CombineDependencies(m_ExclusiveWriteDependency, releaseAccessDependency);
                    m_SharedWriteDependency = m_ExclusiveWriteDependency;
                    break;
                case AcquisitionState.Unacquired:
                default:
                    throw new ArgumentOutOfRangeException(nameof(m_State), m_State, $"Tried to release but {nameof(m_State)} was {m_State} and no code path satisfies!");
            }

            m_State = AcquisitionState.Unacquired;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void ValidateAcquireState()
        {
            Debug.Assert(m_State != AcquisitionState.Disposing, $"{nameof(CollectionAccessController<TContext, TKey>)} is already in the {AcquisitionState.Disposing} state. No longer allowed to acquire until {nameof(Reset)} is called. Last {nameof(Acquire)} was called from: {m_AcquireCallerInfo}");
            Debug.Assert(m_State == AcquisitionState.Unacquired, $"{nameof(Release)} must be called before {nameof(Acquire)} is called again. Last {nameof(Acquire)} was called from: {m_AcquireCallerInfo}");
            StackFrame frame = new StackFrame(2, true);
            m_AcquireCallerInfo = $"{frame.GetMethod().Name} at {frame.GetFileName()}:{frame.GetFileLineNumber()}";
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void ValidateReleaseState(JobHandle releaseAccessDependency)
        {
            Debug.Assert(m_State != AcquisitionState.Unacquired, $"{nameof(Release)} was called multiple times. Last {nameof(Release)} was called from: {m_ReleaseCallerInfo}");
            Debug.Assert(m_State != AcquisitionState.Disposing, $"{nameof(Release)} was called but the {nameof(CollectionAccessController<TContext, TKey>)} is already in the {AcquisitionState.Disposing} state. No need to call release since no one else can write or read. Call {nameof(Reset)} if you want to reuse the controller.");
            StackFrame frame = new StackFrame(2, true);
            m_ReleaseCallerInfo = $"{frame.GetMethod().Name} at {frame.GetFileName()}:{frame.GetFileLineNumber()}";

            // Note: The arguments to JobHandle.CheckFenceIsDependencyOrDidSyncFence seem backward.
            // This checks whether the releaseAccessDependency is part of the chain from the last handle given out when acquired.
            Debug.Assert(JobHandle.CheckFenceIsDependencyOrDidSyncFence(m_LastHandleAcquired, releaseAccessDependency), $"Dependency Chain Broken: The {nameof(JobHandle)} passed into {nameof(Release)} is not part of the chain from the {nameof(JobHandle)} that was given in the last call to {nameof(Acquire)}. Check to ensure your ordering of {nameof(Acquire)} and {nameof(Release)} match.");
        }
    }
}