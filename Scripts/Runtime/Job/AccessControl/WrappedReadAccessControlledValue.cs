using Anvil.CSharp.Core;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// Wraps an <see cref="IReadAccessControlledValue{T}"/> and allows the controlled value to be transformed
    /// before delivering a <see cref="TWrapped"/> instance to the consumer.
    ///
    /// This is an effective way of providing a constrained view of a broader internal access controlled value.
    /// Ex: A reader to an internal stream.
    /// </summary>
    /// <typeparam name="TWrapped">The type of the wrapped instance delivered to the consumer.</typeparam>
    /// <typeparam name="TBase">The base access controlled value that is being wrapped.</typeparam>
    public class WrappedReadAccessControlledValue<TWrapped, TBase> : AbstractAnvilBase,
                                                                     IReadAccessControlledValue<TWrapped>
    {
        /// <summary>
        /// Given the base value <see cref="TBase"/> produces the <see cref="TWrapped"/> to deliver to the consumer
        /// of this instance.
        /// </summary>
        public delegate TWrapped WrapBaseValueDelegate(TBase baseValue);

        private readonly IReadAccessControlledValue<TBase> m_BaseACV;
        private readonly WrapBaseValueDelegate m_WrapBaseValue;

        /// <summary>
        /// Creates a new instance of a <see cref="WrappedReadAccessControlledValue{TWrapped,TBase}"/>.
        /// </summary>
        /// <param name="baseACV">The base <see cref="IReadAccessControlledValue{T}"/> to drive access.</param>
        /// <param name="wrapBaseValue">Given the base value produces the wrapper value to deliver to the consumer.</param>
        public WrappedReadAccessControlledValue(IReadAccessControlledValue<TBase> baseACV, WrapBaseValueDelegate wrapBaseValue)
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

        /// <inheritdoc cref="IReadAccessControlledValue{T}.AcquireRead"/>
        public TWrapped AcquireRead()
        {
            return m_WrapBaseValue(m_BaseACV.AcquireRead());
        }

        /// <inheritdoc cref="IReadAccessControlledValue{T}.AcquireReadAsync"/>
        public JobHandle AcquireReadAsync(out TWrapped value)
        {
            JobHandle handle = m_BaseACV.AcquireReadAsync(out TBase baseValue);
            value = m_WrapBaseValue(baseValue);
            return handle;
        }

        /// <inheritdoc cref="IReadAccessControlledValue{T}.Release"/>
        public void Release()
        {
            m_BaseACV.Release();
        }

        /// <inheritdoc cref="IReadAccessControlledValue{T}.ReleaseAsync"/>
        public void ReleaseAsync(JobHandle releaseAccessDependency)
        {
            m_BaseACV.ReleaseAsync(releaseAccessDependency);
        }
    }
}