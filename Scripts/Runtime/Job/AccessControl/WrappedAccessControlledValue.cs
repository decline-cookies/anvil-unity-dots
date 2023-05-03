using Anvil.CSharp.Core;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// Wraps an <see cref="AccessControlledValue{T}"/> and allows the controlled value to be transformed
    /// before delivering a <see cref="TWrapped"/> instance to the consumer.
    ///
    /// This is an effective way of providing a constrained view of a broader internal access controlled value.
    /// Ex: A reader/writer to an internal stream.
    /// </summary>
    /// <typeparam name="TWrapped">The type of the wrapped instance delivered to the consumer.</typeparam>
    /// <typeparam name="TBase">The base access controlled value that is being wrapped.</typeparam>
    public class WrappedAccessControlledValue<TWrapped, TBase> : AbstractAnvilBase,
                                                                 IAccessControlledValue<TWrapped>,
                                                                 IReadAccessControlledValue<TWrapped>,
                                                                 ISharedWriteAccessControlledValue<TWrapped>,
                                                                 IExclusiveWriteAccessControlledValue<TWrapped>
    {
        /// <summary>
        /// Given the base value <see cref="TBase"/> produces the <see cref="TWrapped"/> to deliver to the consumer
        /// of this instance.
        /// </summary>
        public delegate TWrapped WrapBaseValueDelegate(TBase baseValue);

        private readonly AccessControlledValue<TBase> m_BaseACV;
        private readonly WrapBaseValueDelegate m_WrapBaseValue;

        /// <summary>
        /// Creates a new instance of a <see cref="WrappedAccessControlledValue{TWrapped,TBase}"/>.
        /// </summary>
        /// <param name="baseACV">The base <see cref="AccessControlledValue{T}"/> to drive access.</param>
        /// <param name="wrapBaseValue">Given the base value produces the wrapper value to deliver to the consumer.</param>
        public WrappedAccessControlledValue(AccessControlledValue<TBase> baseACV, WrapBaseValueDelegate wrapBaseValue)
        {
            m_BaseACV = baseACV;
            m_WrapBaseValue = wrapBaseValue;
        }

        /// <inheritdoc cref="IReadAccessControlledValue{T}.AcquireWithReadHandle"/>
        public AccessControlledValue<TWrapped>.AccessHandle AcquireWithReadHandle()
        {
            var handle = m_BaseACV.AcquireWithReadHandle();
            TWrapped wrappedValue = m_WrapBaseValue(handle.Value);

            return AccessControlledValue<TWrapped>.AccessHandle.CreateDerived(handle, wrappedValue);
        }

        /// <inheritdoc cref="ISharedWriteAccessControlledValue{T}.AcquireWithSharedWriteHandle"/>
        public AccessControlledValue<TWrapped>.AccessHandle AcquireWithSharedWriteHandle()
        {
            var handle = m_BaseACV.AcquireWithSharedWriteHandle();
            TWrapped wrappedValue = m_WrapBaseValue(handle.Value);

            return AccessControlledValue<TWrapped>.AccessHandle.CreateDerived(handle, wrappedValue);
        }

        /// <inheritdoc cref="IExclusiveWriteAccessControlledValue{T}.AcquireWithExclusiveWriteHandle"/>
        public AccessControlledValue<TWrapped>.AccessHandle AcquireWithExclusiveWriteHandle()
        {
            var handle = m_BaseACV.AcquireWithExclusiveWriteHandle();
            TWrapped wrappedValue = m_WrapBaseValue(handle.Value);

            return AccessControlledValue<TWrapped>.AccessHandle.CreateDerived(handle, wrappedValue);
        }

        /// <inheritdoc cref="IAccessControlledValue{T}.AcquireWithHandle"/>
        public AccessControlledValue<TWrapped>.AccessHandle AcquireWithHandle(AccessType accessType)
        {
            var handle = m_BaseACV.AcquireWithHandle(accessType);
            TWrapped wrappedValue = m_WrapBaseValue(handle.Value);

            return AccessControlledValue<TWrapped>.AccessHandle.CreateDerived(handle, wrappedValue);
        }


        /// <inheritdoc cref="IReadAccessControlledValue{T}.AcquireRead"/>
        public TWrapped AcquireRead()
        {
            return m_WrapBaseValue(m_BaseACV.AcquireRead());
        }

        /// <inheritdoc cref="ISharedWriteAccessControlledValue{T}.AcquireSharedWrite"/>
        public TWrapped AcquireSharedWrite()
        {
            return m_WrapBaseValue(m_BaseACV.AcquireSharedWrite());
        }

        /// <inheritdoc cref="IExclusiveWriteAccessControlledValue{T}.AcquireExclusiveWrite"/>
        public TWrapped AcquireExclusiveWrite()
        {
            return m_WrapBaseValue(m_BaseACV.AcquireExclusiveWrite());
        }

        /// <inheritdoc cref="IAccessControlledValue{T}.Acquire"/>
        public TWrapped Acquire(AccessType accessType)
        {
            return m_WrapBaseValue(m_BaseACV.Acquire(accessType));
        }


        /// <inheritdoc cref="IReadAccessControlledValue{T}.AcquireReadAsync"/>
        public JobHandle AcquireReadAsync(out TWrapped value)
        {
            JobHandle handle = m_BaseACV.AcquireReadAsync(out TBase baseValue);
            value = m_WrapBaseValue(baseValue);
            return handle;
        }

        /// <inheritdoc cref="ISharedWriteAccessControlledValue{T}.AcquireSharedWriteAsync"/>
        public JobHandle AcquireSharedWriteAsync(out TWrapped value)
        {
            JobHandle handle = m_BaseACV.AcquireSharedWriteAsync(out TBase baseValue);
            value = m_WrapBaseValue(baseValue);
            return handle;
        }

        /// <inheritdoc cref="IExclusiveWriteAccessControlledValue{T}.AcquireExclusiveWriteAsync"/>
        public JobHandle AcquireExclusiveWriteAsync(out TWrapped value)
        {
            JobHandle handle = m_BaseACV.AcquireExclusiveWriteAsync(out TBase baseValue);
            value = m_WrapBaseValue(baseValue);
            return handle;
        }

        /// <inheritdoc cref="IAccessControlledValue{T}.AcquireAsync"/>
        public JobHandle AcquireAsync(AccessType accessType, out TWrapped value)
        {
            JobHandle handle = m_BaseACV.AcquireAsync(accessType, out TBase baseValue);
            value = m_WrapBaseValue(baseValue);
            return handle;
        }


        /// <inheritdoc cref="IAccessControlledValue{T}.Release"/>
        public void Release()
        {
            m_BaseACV.Release();
        }

        /// <inheritdoc cref="IAccessControlledValue{T}.ReleaseAsync"/>
        public void ReleaseAsync(JobHandle releaseAccessDependency)
        {
            m_BaseACV.ReleaseAsync(releaseAccessDependency);
        }
    }
}