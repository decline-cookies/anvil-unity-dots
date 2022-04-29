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
    /// - They schedule their writing jobs using the <see cref="ExclusiveWriteAccessDependency"/> handle.
    /// - This means that all those jobs can start at the same time.
    /// - All of those jobs use <see cref="AddJobHandleForParallelWriting"/> to let the <see cref="CollectionAccessController"/> know that there is writing going on. We cannot read until this is done.
    ///
    /// ----- INTERNAL WRITE PHASE -----
    /// - A managing system now needs to do some work where it reads from and writes to the collection.
    /// - It schedules it's job to do that using the <see cref="ReadWriteAccessDependency"/> handle.
    /// - This means that it can do it's work once all the previous external writers have completed.
    /// - This job then uses <see cref="AddJobHandleForReadWriting"/> to the let the <see cref="CollectionAccessController"/> know that there is reading and writing going on that cannot be interrupted by external parallel writes.
    ///
    /// ----- EXTERNAL READ PHASE -----
    /// - Multiple different systems want to read from the collection.
    /// - They schedule their reading jobs using the <see cref="ReadAccessDependency"/> handle.
    /// - This means that all those jobs can start at the same time.
    /// - All of those jobs use <see cref="AddJobHandleForParallelReading"/> to let the <see cref="CollectionAccessController"/> know that there is reading going on. We cannot write until this is done.
    ///
    /// ----- CLEAN UP PHASE -----
    /// - The collection used above needs to be disposed but we need to ensure all reading and writing are complete.
    /// - The collection disposes using the <see cref="ParallelReadAccessDependency"/> handle.
    /// - This means that all reading from the collection has been completed. It is safe to dispose as no one is using it anymore.
    /// </remarks>
    public class CollectionAccessController : AbstractAnvilBase
    {
        private enum AcquisitionState
        {
            Unacquired,
            ExclusiveWrite,
            SharedWrite,
            SharedRead
        }

        private JobHandle m_ExclusiveWriteDependency;
        private JobHandle m_SharedWriteDependency;
        private JobHandle m_SharedReadDependency;

        private AcquisitionState m_State;
        
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private string m_AcquireCallerInfo;
        private string m_ReleaseCallerInfo;
        private JobHandle m_LastHandleAcquired;
#endif

        public bool IsWritingComplete
        {
            get => m_ExclusiveWriteDependency.IsCompleted && m_SharedWriteDependency.IsCompleted;
        }

        public bool IsReadingComplete
        {
            get => m_SharedReadDependency.IsCompleted;
        }
        
        public JobHandle Acquire(AccessType accessType, JobHandle additionalDependencies)
        {
            return JobHandle.CombineDependencies(Acquire(accessType), additionalDependencies);
        }
        
        public JobHandle Acquire(AccessType accessType)
        {
            Debug.Assert(!IsDisposed);
            ValidateAcquireState();

            JobHandle acquiredHandle = default;
            switch (accessType)
            {
                case AccessType.ExclusiveWrite:
                case AccessType.ForDisposal:
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

        public void Release()
        {
            JobHandle releaseHandle = default;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            releaseHandle = m_LastHandleAcquired;
#endif
            Release(releaseHandle);
        }

        public void Release(JobHandle releaseAccessDependency)
        {
            Debug.Assert(!IsDisposed);
            ValidateReleaseState(releaseAccessDependency);

            switch (m_State)
            {
                case AcquisitionState.ExclusiveWrite:
                    //If you were exclusively writing, then no one else can do any writing or reading until you're done
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

        protected override void DisposeSelf()
        {
            base.DisposeSelf();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void ValidateAcquireState()
        {
            Debug.Assert(m_State == AcquisitionState.Unacquired, $"{nameof(Release)} must be called before {nameof(Acquire)} is called again. Last {nameof(Acquire)} was called from: {m_AcquireCallerInfo}");
            StackFrame frame = new StackFrame(2, true);
            m_AcquireCallerInfo = $"{frame.GetMethod().Name} at {frame.GetFileName()}:{frame.GetFileLineNumber()}";
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void ValidateReleaseState(JobHandle releaseAccessDependency)
        {
            Debug.Assert(m_State != AcquisitionState.Unacquired, $"{nameof(Release)} was called multiple times. Last {nameof(Release)} was called from: {m_ReleaseCallerInfo}");
            StackFrame frame = new StackFrame(2, true);
            m_ReleaseCallerInfo = $"{frame.GetMethod().Name} at {frame.GetFileName()}:{frame.GetFileLineNumber()}";

            // Note: The arguments to JobHandle.CheckFenceIsDependencyOrDidSyncFence seem backward.
            // This checks whether the releaseAccessDependency is part of the chain from the last handle given out when acquired.
            Debug.Assert(JobHandle.CheckFenceIsDependencyOrDidSyncFence(m_LastHandleAcquired, releaseAccessDependency), $"Dependency Chain Broken: The {nameof(JobHandle)} passed into {nameof(Release)} is not part of the chain from the {nameof(JobHandle)} that was given in the last call to {nameof(Acquire)}. Check to ensure your ordering of {nameof(Acquire)} and {nameof(Release)} match.");
        }
    }
}
