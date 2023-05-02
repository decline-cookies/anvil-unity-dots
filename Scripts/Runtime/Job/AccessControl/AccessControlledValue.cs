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
                                            IAccessControlledValue<T>,
                                            IReadAccessControlledValue<T>,
                                            ISharedWriteAccessControlledValue<T>,
                                            IExclusiveWriteAccessControlledValue<T>
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

        /// <summary>
        /// Gets the current <see cref="JobHandle"/> that must be completed before the provided <see cref="AccessType"/>
        /// may be performed without modifying the state of the controller.
        /// This is the same <see cref="JobHandle"/> that would be returned by
        /// <see cref="AccessControlledValue{T}.AcquireAsync"/> or
        /// <see cref="AcquireReadAsync"/> when provided the same parameter.
        /// </summary>
        /// <remarks>
        /// Generally <see cref="AccessControlledValue{T}.AcquireAsync"/> or
        /// <see cref="AcquireReadAsync"/>should be used.
        /// This method is an advanced feature for specialized
        /// situations like detecting if a value has been acquired for writing between calls.
        /// </remarks>
        /// <param name="accessType">The type of <see cref="AccessType"/> needed.</param>
        /// <returns>
        /// A <see cref="JobHandle"/> that needs to be completed before the requested access type would be valid.
        /// </returns>
        public JobHandle GetDependencyFor(AccessType accessType)
        {
            return m_AccessController.GetDependencyFor(accessType);
        }

        /// <inheritdoc cref="IAccessControlledValue{T}.AcquireWithHandle"/>
        public AccessHandle AcquireWithHandle(AccessType accessType)
        {
            return new AccessHandle(m_AccessController.AcquireWithHandle(accessType), m_Value);
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

        /// <inheritdoc cref="IReadAccessControlledValue{T}.AcquireWithReadHandle"/>
        public AccessHandle AcquireWithReadHandle()
        {
            return AcquireWithHandle(AccessType.SharedRead);
        }

        /// <inheritdoc cref="IReadAccessControlledValue{T}.AcquireRead"/>
        public T AcquireRead()
        {
            return Acquire(AccessType.SharedRead);
        }

        /// <inheritdoc cref="IReadAccessControlledValue{T}.AcquireReadAsync"/>
        public JobHandle AcquireReadAsync(out T value)
        {
            return AcquireAsync(AccessType.SharedRead, out value);
        }

        /// <inheritdoc cref="ISharedWriteAccessControlledValue{T}.AcquireWithSharedWriteHandle"/>
        public AccessHandle AcquireWithSharedWriteHandle()
        {
            return AcquireWithHandle(AccessType.SharedWrite);
        }

        /// <inheritdoc cref="ISharedWriteAccessControlledValue{T}.AcquireSharedWrite"/>
        public T AcquireSharedWrite()
        {
            return Acquire(AccessType.SharedWrite);
        }

        /// <inheritdoc cref="ISharedWriteAccessControlledValue{T}.AcquireSharedWriteAsync"/>
        public JobHandle AcquireSharedWriteAsync(out T value)
        {
            return AcquireAsync(AccessType.SharedWrite, out value);
        }

        /// <inheritdoc cref="IExclusiveWriteAccessControlledValue{T}.AcquireWithExclusiveWriteHandle"/>
        public AccessHandle AcquireWithExclusiveWriteHandle()
        {
            return AcquireWithHandle(AccessType.ExclusiveWrite);
        }

        /// <inheritdoc cref="IExclusiveWriteAccessControlledValue{T}.AcquireExclusiveWrite"/>
        public T AcquireExclusiveWrite()
        {
            return Acquire(AccessType.ExclusiveWrite);
        }

        /// <inheritdoc cref="IExclusiveWriteAccessControlledValue{T}.AcquireExclusiveWriteAsync"/>
        public JobHandle AcquireExclusiveWriteAsync(out T value)
        {
            return AcquireAsync(AccessType.ExclusiveWrite, out value);
        }

        /// <inheritdoc cref="IAccessControlledValue{T}.Release"/>
        public void Release()
        {
            m_AccessController.Release();
        }

        /// <inheritdoc cref="IAccessControlledValue{T}.ReleaseAsync"/>
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
            /// Creates an access handle instance that derives from another access handle.
            /// </summary>
            /// <param name="sourceHandle">The access handle to derive from.</param>
            /// <param name="derivedValue">The value being provided to the consumer.</param>
            /// <typeparam name="U">The type of the value provided to the consumer.</typeparam>
            /// <returns>The access handle instance.</returns>
            /// <remarks>
            /// This is useful for implementations of an access control interface that wrap one (or many) other access
            /// controlled values or implementations that transform/wrap an access controlled value before exposing to
            /// the consumer.
            /// </remarks>
            public static AccessHandle CreateDerived<U>(AccessControlledValue<U>.AccessHandle sourceHandle, T derivedValue)
            {
                return new AccessHandle(sourceHandle.m_ControllerHandle, derivedValue);
            }

            /// <summary>
            /// The controlled value
            /// </summary>
            public readonly T Value;

            private readonly AccessController.AccessHandle m_ControllerHandle;

            /// <summary>
            /// Creates a new instance that wraps an <see cref="AccessController.AccessHandle"/> with the value
            /// <see cref="T"/>.
            /// </summary>
            /// <param name="controllerHandle">The handle from the backing <see cref="AccessController"/>.</param>
            /// <param name="value">The value that is access controlled.</param>
            public AccessHandle(in AccessController.AccessHandle controllerHandle, T value)
            {
                m_ControllerHandle = controllerHandle;
                Value = value;
            }

            /// <inheritdoc cref="IDisposable"/>
            public void Dispose()
            {
                m_ControllerHandle.Dispose();
            }
        }
    }
}