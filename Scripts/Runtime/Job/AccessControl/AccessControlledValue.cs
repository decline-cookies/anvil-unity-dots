using Anvil.CSharp.Core;
using System;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// A class that wraps a data object of type <typeparamref name="T"/> and a <see cref="AccessController"/>
    /// to allow for safe access to the data object.
    /// </summary>
    /// <typeparam name="T">The type of data to wrap access control to</typeparam>
    public class AccessControlledValue<T> : AbstractAnvilBase,
                                            IAccessControlledValue<T>
    {
        private readonly AccessController m_AccessController;

        private readonly T m_Value;
        
        /// <summary>
        /// Creates a new instance of <see cref="AccessControlledValue{T}"/> for the passed in
        /// data.
        /// </summary>
        /// <param name="value">The data instance to wrap access control around.</param>
        public AccessControlledValue(T value)
        {
            m_Value = value;
            m_AccessController = new AccessController();
        }

        protected override void DisposeSelf()
        {
            if (m_Value is IDisposable disposable)
            {
                m_AccessController.Acquire(AccessType.Disposal);
                disposable.Dispose();
            }
            m_AccessController.Dispose();
            base.DisposeSelf();
        }

        /// <inheritdoc cref="IBaseAccessControlledValue{T}.GetDependencyFor"/>
        public JobHandle GetDependencyFor(AccessType accessType)
        {
            return m_AccessController.GetDependencyFor(accessType);
        }

        /// <inheritdoc cref="IAccessControlledValue{T}.AcquireWithHandle"/>
        public AccessHandle AcquireWithHandle(AccessType accessType)
        {
            return new AccessHandle(this, accessType);
        }

        /// <inheritdoc cref="IAccessControlledValue{T}.Acquire"/>
        public T Acquire(AccessType accessType)
        {
            m_AccessController.Acquire(accessType);
            return m_Value;
        }

        /// <inheritdoc cref="IAccessControlledValue{T}.AcquireAsync"/>
        public JobHandle AcquireAsync(AccessType accessType, out T value)
        {
            value = m_Value;
            return m_AccessController.AcquireAsync(accessType);
        }
        
        /// <inheritdoc cref="IReadOnlyAccessControlledValue{T}.AcquireWithReadOnlyHandle"/>
        public AccessHandle AcquireWithReadOnlyHandle()
        {
            return AcquireWithHandle(AccessType.SharedRead);
        }

        /// <inheritdoc cref="IReadOnlyAccessControlledValue{T}.AcquireReadOnly"/>
        public T AcquireReadOnly()
        {
            return Acquire(AccessType.SharedRead);
        }

        /// <inheritdoc cref="IReadOnlyAccessControlledValue{T}.AcquireReadOnlyAsync"/>
        public JobHandle AcquireReadOnlyAsync(out T value)
        {
            return AcquireAsync(AccessType.SharedRead, out value);
        }

        /// <inheritdoc cref="IBaseAccessControlledValue{T}.Release"/>
        public void Release()
        {
            m_AccessController.Release();
        }
        
        /// <inheritdoc cref="IBaseAccessControlledValue{T}.ReleaseAsync"/>
        public void ReleaseAsync(JobHandle releaseAccessDependency)
        {
            m_AccessController.ReleaseAsync(releaseAccessDependency);
        }

        // ----- Inner Types ----- //
        /// <summary>
        /// A convenience type that provides a synchronous handle to the controlled value that is released when
        /// disposed.
        /// </summary>
        /// <remarks>
        /// This type is the equivalent of calling <see cref="AccessControlledValue{T}.Acquire"/> and
        /// <see cref="AccessControlledValue{T}.Release"/> yourself but is intended to be used with a using statement so
        /// that the handle is always released.
        /// </remarks>
        public readonly struct AccessHandle : IDisposable
        {
            /// <summary>
            /// The controlled value
            /// </summary>
            public readonly T Value;

            private readonly AccessControlledValue<T> m_Controller;


            /// <summary>
            /// Creates a new instance that gains synchronous access from the provided
            /// <see cref="AccessControlledValue{T}"/>.
            /// </summary>
            /// <param name="controller">The <see cref="AccessControlledValue{T}"/> to acquire from.</param>
            /// <param name="accessType">The type of <see cref="AccessType"/> needed.</param>
            public AccessHandle(AccessControlledValue<T> controller, AccessType accessType)
            {
                Value = controller.Acquire(accessType);
                m_Controller = controller;
            }

            /// <inheritdoc cref="IDisposable"/>
            public void Dispose()
            {
                m_Controller.Release();
            }
        }
    }
}
