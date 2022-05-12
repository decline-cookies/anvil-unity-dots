using System;
using Anvil.CSharp.Core;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// A wrapper class for managing async access to a value.
    /// </summary>
    /// <remarks>
    /// Take care that value type implementations propagate state through copies (usually by wrapping a pointer).
    /// For Example: <see cref="NativeArray{T}" />
    /// </remarks>
    public abstract class AbstractValueAccessController<T> : AbstractAnvilBase
    {
        private enum AcquisitionState
        {
            Unacquired,
            ReadOnly,
            ReadWrite,
        }

        private readonly T m_Value;
        /// <summary>
        /// The handle to wait on before <see cref="m_Value"> can be read.
        /// </summary>
        private JobHandle m_ReadAccessDependency = default;
        /// <summary>
        /// The handle to wait on before <see cref="m_Value"> can be written to.
        /// </summary>
        private JobHandle m_WriteAccessDependency = default;
        private AcquisitionState m_State = AcquisitionState.Unacquired;


        /// <summary>
        /// Creates a new <see cref="AbstractValueAccessController{T}"/> with a given initial value.
        /// </summary>
        internal AbstractValueAccessController(T initialValue)
        {
            m_Value = initialValue;
        }

        protected override void DisposeSelf()
        {
            // NOTE: If these asserts trigger we should think about calling Complete() on these job handles.
            Debug.Assert(m_ReadAccessDependency.IsCompleted, "The read access dependency is not completed");
            Debug.Assert(m_WriteAccessDependency.IsCompleted, "The write access dependency is not completed");
            (m_Value as IDisposable)?.Dispose();

            base.DisposeSelf();
        }

        /// <summary>
        /// Get the value of the <see cref="AbstractValueAccessController{T}"/> returning a <see cref="JobHandle"/> to wait on before consuming.
        /// After the work with the value is scheduled <see cref="ReleaseAsync"/> must be called before any other calls to 
        /// <see cref="AcquireAsync" /> or <see cref="Acquire" /> are made for this value.
        /// </summary>
        /// <param name="isReadOnly">The access level required for the value. Accessing readonly will tend to require less waiting.</param>
        /// <param name="value">The value of the <see cref="AbstractValueAccessController{T}"/>.</param>
        /// <returns>A <see cref="JobHandle"/> to wait on before consuming the value.</returns>
        public JobHandle AcquireAsync(bool isReadOnly, out T value)
        {
            Debug.Assert(!IsDisposed);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            ValidateAndUpdateAcquireCaller();
#endif

            value = m_Value;
            return GetAcquisitionDependency(isReadOnly);
        }

        /// <summary>
        /// Schedule the release of the value's ownership after an asynchronous operation.
        /// </summary>
        /// <param name="releaseAccessDependency">The <see cref="JobHandle"/> that describes when work with the value is complete.</param>
        public void ReleaseAsync(JobHandle releaseAccessDependency)
        {
            Debug.Assert(!IsDisposed);
            Debug.Assert(m_State != AcquisitionState.Unacquired, "There is no outstanding acquisition to release.");

            switch (m_State)
            {
                case AcquisitionState.ReadOnly:
                    m_WriteAccessDependency = JobHandle.CombineDependencies(m_WriteAccessDependency, releaseAccessDependency);
                    break;

                case AcquisitionState.ReadWrite:
                    m_ReadAccessDependency =
                        m_WriteAccessDependency = JobHandle.CombineDependencies(m_WriteAccessDependency, releaseAccessDependency);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(Enum.GetName(typeof(AcquisitionState), m_State));
            }
            m_State = AcquisitionState.Unacquired;
        }

        /// <summary>
        /// Get the value of the <see cref="AbstractValueAccessController{T}"/> immediately.
        /// This blocks the calling thread until the value is available.
        /// <see cref="Release"/> must be called before any other calls to <see cref="AcquireAsync" /> 
        /// or <see cref="Acquire" /> are made for this value.
        /// </summary>
        /// <remarks>
        /// This method and its compliment <see cref="Release" /> are intended to be used for synchronous work 
        /// on the main thread.
        /// </remarks>
        public T Acquire(bool isReadOnly)
        {
            Debug.Assert(!IsDisposed);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            ValidateAndUpdateAcquireCaller();
#endif
            GetAcquisitionDependency(isReadOnly)
                .Complete();

            return m_Value;
        }

        /// <summary>
        /// Releases access to the value immediately.
        /// Paired with the use of <see cref="Acquire"/>.
        /// </summary>
        /// <remarks>
        /// This method can be called after <see cref="AcquireAsync"/> has been called but it will 
        // block the calling thread until the value's access dependency is resolved. <see cref="ReleaseAsync" /> 
        /// is typically the better option.
        /// </remarks>
        public void Release()
        {
            Debug.Assert(!IsDisposed);
            Debug.Assert(m_State != AcquisitionState.Unacquired, "There is no outstanding acquisition to release.");
            m_State = AcquisitionState.Unacquired;

            m_ReadAccessDependency.Complete();
            if (m_State == AcquisitionState.ReadWrite)
            {
                m_WriteAccessDependency.Complete();
            }
        }

        private JobHandle GetAcquisitionDependency(bool isReadOnly)
        {
            if (isReadOnly)
            {
                m_State = AcquisitionState.ReadOnly;
                return m_ReadAccessDependency;
            }

            m_State = AcquisitionState.ReadWrite;
            return m_WriteAccessDependency;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private string m_AcquireCallerInfo;

        private void ValidateAndUpdateAcquireCaller()
        {
            Debug.Assert(m_State == AcquisitionState.Unacquired, $"Release must be scheduled before scheduling acquisition again. Last ScheduleAcquire caller hasn't scheduled release yet. {m_AcquireCallerInfo}");

            System.Diagnostics.StackFrame frame = new System.Diagnostics.StackFrame(2);
            m_AcquireCallerInfo = $"{frame.GetMethod().Name} at {frame.GetFileName()}:{frame.GetFileLineNumber()}";
        }
#endif
    }
}
