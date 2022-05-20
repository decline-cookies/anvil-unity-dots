using System;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// A concrete implementation of <see cref="AbstractAccessController{T}"/> that assumes
    /// wrapping a singular unchanging piece of data which is the most common case.
    /// </summary>
    /// <typeparam name="T">The type of data to wrap access control around.</typeparam>
    public class AccessController<T> : AbstractAccessController<T>
    {
        private readonly T m_Value;

        /// <summary>
        /// Creates a new instance of <see cref="AccessController{T}"/> for the passed in
        /// data.
        /// </summary>
        /// <param name="value">The data instance to wrap access control around.</param>
        public AccessController(T value)
        {
            m_Value = value;
        }

        protected override void DisposeSelf()
        {
            (m_Value as IDisposable)?.Dispose();
            base.DisposeSelf();
        }

        protected override T AcquireData(AccessType accessType)
        {
            return m_Value;
        }
    }
}
