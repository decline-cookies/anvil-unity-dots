using System;
using Anvil.CSharp.Core;

namespace Anvil.Unity.DOTS.Data
{
    /// <summary>
    /// A wrapper for a value that checks a trigger source on each get and updates its value before returning the value.
    /// </summary>
    /// <typeparam name="TValue">The type of the value</typeparam>
    /// <typeparam name="TTriggerType">The type of the trigger source's value</typeparam>
    /// <example>
    /// The most common use case for this type is to lazily recompute or cache an <see cref="AccessControlledValue"/>. The below
    /// example is contrived but demonstrates usage. Typically the data fetched from the AccessControlledValue would be
    /// transformed in a way that would make caching worthwhile.
    /// <code>
    /// AccessControlledValue<int> myACV = new AccessControlledValue<int>(5);
    /// LazyReactiveValue<uint, JobHandle> myCachedValue = new LazyReactiveValue(
    ///     () => myACV.GetDependencyFor(AccessType.SharedRead),
    ///     (ref uint value) =>
    ///     {
    ///         using var myACVHandle = myACV.AcquireWithHandle(AccessType.SharedRead);
    ///         value = myACVHandle.Value;
    ///     }
    /// );
    /// </code>
    /// </example>
    public class LazyReactiveValue<TValue, TTriggerType> : AbstractAnvilBase
    {
        /// <summary>
        /// A delegate that is expected to modify the <see cref="value"/> parameter in place with up to date value(s).
        /// </summary>
        /// <param name="value">A reference to the value to update.</param>
        public delegate void UpdateValueDelegate(ref TValue value);

        private readonly Func<TTriggerType> m_GetCurrentTriggerValue;
        private readonly UpdateValueDelegate m_UpdateValue;

        private TValue m_Value;
        private TTriggerType m_LastDependencyHandle;

        /// <summary>
        /// The value. This will always be up to date but may incur an update cost if the source has changed.
        /// </summary>
        public TValue Value
        {
            get
            {
                UpdateIfDirty();
                return m_Value;
            }
        }

        /// <summary>
        /// Creates an instance of the wrapper.
        /// </summary>
        /// <param name="getCurrentTriggerValue">
        /// A method that returns the current trigger value. If the returned value is different than the last call,
        /// <see cref="updateValue"/> is called to update the value.
        /// </param>
        /// <param name="updateValue">
        /// A method that is provided reference to the current value to mutate into an up to date representation. This
        /// is typically a recomputing or re-caching of the value.
        /// </param>
        /// <param name="initialValue">(optional) The initial value (Default: default(TValue)/>.</param>
        public LazyReactiveValue(Func<TTriggerType> getCurrentTriggerValue, UpdateValueDelegate updateValue, TValue initialValue = default)
        {
            m_GetCurrentTriggerValue = getCurrentTriggerValue;
            m_UpdateValue = updateValue;
            m_Value = initialValue;
        }

        protected override void DisposeSelf()
        {
            (m_Value as IDisposable)?.Dispose();

            base.DisposeSelf();
        }

        /// <summary>
        /// Pro-actively update the value. Typically used if it's known that the source trigger has changed and the
        /// application is in an optimal state to update the value.
        /// </summary>
        public void UpdateIfDirty()
        {
            TTriggerType currentReadHandle = m_GetCurrentTriggerValue();

            if (m_LastDependencyHandle.Equals(currentReadHandle))
            {
                return;
            }

            m_UpdateValue(ref m_Value);
            m_LastDependencyHandle = currentReadHandle;
        }
    }
}