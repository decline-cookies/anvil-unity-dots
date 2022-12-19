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
    /// - Multiple different systems want to write to the data.
    /// - They call <see cref="AcquireAsync"/> with access type of <see cref="AccessType.SharedWrite"/>.
    /// - This means that all those jobs can start at the same time.
    /// - NOTE: You as the developer must ensure that your jobs write safely during a <see cref="AccessType.SharedWrite"/>. Typically this is done by using the <see cref="NativeSetThreadIndex"/> attribute to guarantee unique writing.
    /// - All of those jobs use <see cref="ReleaseAsync"/> to let the <see cref="AccessController"/> know that there is parallel writing going on. We cannot read or exclusive write until this done.
    ///
    /// ----- INTERNAL WRITE PHASE -----
    /// - A managing system now needs to do some work where it reads from and writes to the data.
    /// - It schedules it's job to do that using <see cref="AcquireAsync"/> with access type of <see cref="AccessType.ExclusiveWrite"/>
    /// - This means that it can do it's work once all the previous external writers and/or readers have completed.
    /// - NOTE: Typically this means that one thread is reading/writing to more than one (up to all) possible buckets in the data.
    /// - This job then uses <see cref="ReleaseAsync"/> to the let the <see cref="AccessController"/> know that there an exclusive write going on that cannot be interrupted by parallel writes or reads.
    ///
    /// ----- EXTERNAL READ PHASE -----
    /// - Multiple different systems want to read from the data.
    /// - They schedule their reading jobs using <see cref="AcquireAsync"/> with access type of <see cref="AccessType.SharedRead"/>
    /// - This means that all those reading jobs can start at the same time.
    /// - All of those jobs use <see cref="ReleaseAsync"/> to let the <see cref="AccessController"/> know that there is reading going on. We cannot write again until this is done.
    ///
    /// ----- CLEAN UP PHASE -----
    /// - The data used above needs to be disposed but we need to ensure all reading and writing are complete.
    /// - The data disposes using <see cref="AcquireAsync"/> with access type of <see cref="AccessType.Disposal"/>
    /// - This means that all reading and writing from the data has been completed. It is safe to dispose as no one is using it anymore and further calls to <see cref="AcquireAsync"/> will fail unless <see cref="Reset"/> is called.
    /// - Calling Reset indicates to the controller that the underlying instance has changed and all previous JobHandles no longer apply.
    /// </remarks>
    public class AccessController : AbstractAnvilBase
    {
        private enum AcquisitionState
        {
            Unacquired,
            ExclusiveWrite,
            SharedWrite,
            SharedRead,
            Disposing
        }

        private JobHandle m_ExclusiveWriteDependency;
        private JobHandle m_SharedWriteDependency;
        private JobHandle m_SharedReadDependency;
        private JobHandle m_LastHandleAcquired;

        private AcquisitionState m_State;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private StackTrace m_LastAccessOperationStack;
#endif


        public AccessController()
        {
        }

        protected override void DisposeSelf()
        {
            // NOTE: If these asserts trigger we should think about calling Complete() on these job handles.
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!m_ExclusiveWriteDependency.IsCompleted)
            {
                throw new InvalidOperationException("The exclusive write access dependency is not completed");
            }

            if (!m_SharedWriteDependency.IsCompleted)
            {
                throw new InvalidOperationException("The shared write access dependency is not completed");
            }

            if (!m_SharedReadDependency.IsCompleted)
            {
                throw new InvalidOperationException("The shared read access dependency is not completed");
            }
#endif

            base.DisposeSelf();
        }

        /// <summary>
        /// Resets the internal state of this <see cref="AccessController"/> so that it can
        /// be used again.
        /// </summary>
        /// <remarks>
        /// Typically this is used in cases where the underlying data that you are using the
        /// <see cref="AccessController"/> to gate access to is created/destroyed each frame
        /// or is being double buffered. The data itself needs to be disposed and recreated but from the higher
        /// level point of view of reading and writing to the concept of the data we want to use the same
        /// access controller.
        ///
        /// You would <see cref="AcquireAsync"/> with <see cref="AccessType.Disposal"/> and then dispose the old
        /// data and then call <see cref="Reset"/> on the <see cref="AccessController"/>
        /// to reflect that there is a new data to read from/write to.
        /// </remarks>
        /// <param name="initialDependency">Optional initial dependency to set access to.</param>
        public void Reset(JobHandle initialDependency = default)
        {
            m_State = AcquisitionState.Unacquired;
            m_ExclusiveWriteDependency = m_SharedWriteDependency = m_SharedReadDependency = initialDependency;
            m_LastHandleAcquired = default;
        }

        /// <summary>
        /// Gets the current <see cref="JobHandle"/> that must be completed before the provided <see cref="AccessType"/>
        /// may be performed without modifying the state of the controller.
        /// This is the same <see cref="JobHandle"/> that would be returned by <see cref="AcquireAsync"/> when provided
        /// the same parameter.
        /// </summary>
        /// <remarks>
        /// Generally <see cref="AcquireAsync"/> should be used. This method is an advanced feature for specialized
        /// situations like detecting if a value has been acquired for writing between calls.
        /// </remarks>
        /// <param name="accessType">The type of <see cref="AccessType"/> needed.</param>
        /// <returns>
        /// A <see cref="JobHandle"/> that needs to be completed before the requested access type would be valid.
        /// </returns>
        public JobHandle GetDependencyFor(AccessType accessType)
        {
            Debug.Assert(!IsDisposed);

            return accessType switch
            {
                AccessType.Disposal => m_ExclusiveWriteDependency,
                AccessType.ExclusiveWrite => m_ExclusiveWriteDependency,
                AccessType.SharedWrite => m_SharedWriteDependency,
                AccessType.SharedRead => m_SharedReadDependency,
                _ => throw new ArgumentOutOfRangeException(nameof(accessType), accessType,
                    $"Tried to get dependency with {nameof(AccessType)} of {accessType} but no code path satisfies!")
            };
        }

        /// <summary>
        /// Acquires access synchronously for a given <see cref="AccessType"/> and returns an <see cref="AccessHandle"/>.
        /// This is the preferred method of synchronous access vs <see cref="Acquire"/>/<see cref="Release"/>.
        /// </summary>
        /// <remarks>
        /// The <see cref="AccessHandle"/> is a safer way to synchronously maintain access to an
        /// <see cref="AccessController{T}"/>. Paired with a using statement access to the controller will be released
        /// when the handle falls out of scope.
        /// </remarks>
        /// <example>using var valueHandle = myAccessController.AcquireWithHandle(AccessType.SharedRead);</example>
        /// <param name="accessType">The type of <see cref="AccessType"/> needed.</param>
        /// <returns>
        /// The <see cref="AccessHandle"/> that maintains access to the controller until disposed.
        /// </returns>
        public AccessHandle AcquireWithHandle(AccessType accessType)
        {
            return new AccessHandle(this, accessType);
        }

        /// <summary>
        /// Acquires access synchronously for a given <see cref="AccessType"/>
        /// Will block on the calling thread if there are any jobs that need to complete before this
        /// can be used.
        ///
        /// Typically this is used when wanting to perform main thread work on data.
        /// </summary>
        /// <param name="accessType">The type of <see cref="AccessType"/> needed.</param>
        public void Acquire(AccessType accessType)
        {
            JobHandle acquireDependency = AcquireAsync(accessType);
            acquireDependency.Complete();
        }

        /// <summary>
        /// Acquires access asynchronously for a given <see cref="AccessType"/>
        /// A <see cref="JobHandle"/> will be returned to schedule jobs that require this access.
        ///
        /// Not respecting the <see cref="JobHandle"/> could lead to dependency errors.
        ///
        /// Typically this is used when wanting to perform work on data in a job to be scheduled.
        /// </summary>
        /// <param name="accessType">The type of <see cref="AccessType"/> needed.</param>
        /// <returns>A <see cref="JobHandle"/> to wait on before accessing</returns>
        public JobHandle AcquireAsync(AccessType accessType)
        {
            Debug.Assert(!IsDisposed);
            ValidateAcquireState();

            JobHandle acquiredHandle = default;
            switch (accessType)
            {
                case AccessType.Disposal:
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

            //TODO: #129 - Remove once we have unit tests.
            Debug.Assert(acquiredHandle.Equals(GetDependencyFor(accessType)));

            m_LastHandleAcquired = acquiredHandle;
            CaptureAccessOperationStack();

            return acquiredHandle;
        }

        /// <summary>
        /// Releases access so other callers can use it.
        /// Could potentially block on the calling thread if <see cref="AcquireAsync"/> was called first and the
        /// dependency returned has not yet been completed.
        ///
        /// Typically this is used when main thread work on data is complete.
        /// </summary>
        public void Release()
        {
            ReleaseAsync(m_LastHandleAcquired);
            m_LastHandleAcquired.Complete();
        }

        /// <summary>
        /// Releases access so other callers can use it once the <paramref name="releaseAccessDependency"/>
        /// is complete.
        ///
        /// Typically this used when job work on data needs to be completed before other callers can use that data
        /// again.
        /// </summary>
        /// <param name="releaseAccessDependency">The <see cref="JobHandle"/> to wait upon</param>
        public void ReleaseAsync(JobHandle releaseAccessDependency)
        {
            Debug.Assert(!IsDisposed);
            ValidateReleaseState(releaseAccessDependency);

            switch (m_State)
            {
                case AcquisitionState.ExclusiveWrite:
                    //If you were exclusively writing, then no one else can do any writing or reading until you're done
                    m_ExclusiveWriteDependency
                        = m_SharedWriteDependency
                            = m_SharedReadDependency
                                = releaseAccessDependency;
                    break;
                case AcquisitionState.SharedWrite:
                    //If you were shared writing, then no one else can exclusive write or read until you're done
                    m_ExclusiveWriteDependency
                        = m_SharedReadDependency
                            = JobHandle.CombineDependencies(m_ExclusiveWriteDependency, releaseAccessDependency);
                    break;
                case AcquisitionState.SharedRead:
                    //If you were reading, then no one else can do any writing until your reading is done
                    m_ExclusiveWriteDependency
                        = m_SharedWriteDependency
                            = JobHandle.CombineDependencies(m_ExclusiveWriteDependency, releaseAccessDependency);
                    break;
                case AcquisitionState.Disposing:
                    throw new InvalidOperationException($"Current state was {m_State}, no need to call {nameof(ReleaseAsync)}. Enable ENABLE_UNITY_COLLECTIONS_CHECKS for more info.");
                case AcquisitionState.Unacquired:
                    throw new InvalidOperationException($"Current state was {m_State}, {nameof(ReleaseAsync)} was called multiple times. Enable ENABLE_UNITY_COLLECTIONS_CHECKS for more info.");
                default:
                    throw new ArgumentOutOfRangeException(nameof(m_State), m_State, $"Tried to release but {nameof(m_State)} was {m_State} and no code path satisfies!");
            }

            CaptureAccessOperationStack();
            m_State = AcquisitionState.Unacquired;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void ValidateAcquireState()
        {
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (m_State == AcquisitionState.Disposing)
            {
                throw new InvalidOperationException($"{nameof(AccessController)} is already in the {AcquisitionState.Disposing} state. No longer allowed to acquire until {nameof(Reset)} is called.");
            }

            if (m_State != AcquisitionState.Unacquired)
            {
                throw new InvalidOperationException($"{nameof(ReleaseAsync)} must be called before {nameof(AcquireAsync)} is called again." +
                                                    $"\n ----- Last Acquisition Stack -----" +
                                                    $"\n {GetLastAccessOperationStack()}" +
                                                    $"\n ----- END Last Acquisition Stack -----\n");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void ValidateReleaseState(JobHandle releaseAccessDependency)
        {
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (m_State == AcquisitionState.Unacquired)
            {
                throw new InvalidOperationException($"{nameof(ReleaseAsync)} was called multiple times." +
                                                    $"\n ----- Last Release Stack -----" +
                                                    $"\n {GetLastAccessOperationStack()}" +
                                                    $"\n ----- END Last Release Stack -----\n");
            }

            if (m_State == AcquisitionState.Disposing)
            {
                throw new InvalidOperationException($"{nameof(ReleaseAsync)} was called but the {nameof(AccessController)} is already in the {AcquisitionState.Disposing} state. No need to call release since no one else can write or read. Call {nameof(Reset)} if you want to reuse the controller.");
            }

            if (!releaseAccessDependency.DependsOn(m_LastHandleAcquired))
            {
                throw new InvalidOperationException($"Dependency Chain Broken: The {nameof(JobHandle)} passed into {nameof(ReleaseAsync)} is not part of the chain from the {nameof(JobHandle)} that was given in the last call to {nameof(AcquireAsync)}. Check to ensure your ordering of {nameof(AcquireAsync)} and {nameof(ReleaseAsync)} match.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CaptureAccessOperationStack()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_LastAccessOperationStack = new StackTrace(1, true);
#endif
        }

        private string GetLastAccessOperationStack()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return m_LastAccessOperationStack.ToString();
#else
            throw new NotSupportedException();
#endif
        }


        // ----- Inner Types ----- //
        /// <summary>
        /// A convenience type that provides a synchronous handle to the <see cref="AccessController"/> that is released
        /// when disposed.
        /// </summary>
        /// <remarks>
        /// This type is the equivalent of calling <see cref="AccessController.Acquire"/> and
        /// <see cref="AccessController.Release"/> yourself but is intended to be used with a using statement so
        /// that the handle is always released.
        /// </remarks>
        public readonly struct AccessHandle : IDisposable
        {
            private readonly AccessController m_Controller;


            /// <summary>
            /// Creates a new instance that gains synchronous access from the provided controller.
            /// </summary>
            /// <param name="access">The <see cref="AccessController"/> to acquire from.</param>
            /// <param name="accessType">The type of <see cref="AccessType"/> needed.</param>
            public AccessHandle(AccessController controller, AccessType accessType)
            {
                m_Controller = controller;
                m_Controller.Acquire(accessType);
            }

            /// <inheritdoc cref="IDisposable"/>
            public void Dispose()
            {
                m_Controller.Release();
            }
        }
    }
}