using Anvil.CSharp.Core;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Jobs
{
    /// <summary>
    /// A handle to allow for figuring out the dependency of scheduling a job that will shared write to a
    /// <see cref="DynamicBuffer{T}"/>.
    ///
    /// See <see cref="DynamicBufferSharedWriteController{T}"/> and <see cref="DynamicBufferSharedWriteDataSystem"/>
    /// for more in depth background.
    /// </summary>
    public class DynamicBufferSharedWriteHandle : AbstractAnvilBase
    {
        private readonly IDynamicBufferSharedWriteController m_Controller;
        private readonly SystemBase m_System;

        internal DynamicBufferSharedWriteHandle(IDynamicBufferSharedWriteController controller,
                                                SystemBase system)
        {
            m_Controller = controller;
            m_System = system;
            
            m_Controller.RegisterSystemForSharedWrite(m_System);
        }

        protected override void DisposeSelf()
        {
            m_Controller.UnregisterSystemForSharedWrite(m_System);
            base.DisposeSelf();
        }

        /// <summary>
        /// Gets a <see cref="JobHandle"/> to be used to schedule the jobs that will shared writing to the
        /// <see cref="DynamicBuffer{T}"/>.
        /// </summary>
        /// <param name="callingSystemDependency">The incoming Dependency <see cref="JobHandle"/> for the calling system.</param>
        /// <returns>The <see cref="JobHandle"/> to schedule shared writing jobs</returns>
        public JobHandle GetSharedWriteJobHandle(JobHandle callingSystemDependency)
        {
            return m_Controller.GetSharedWriteJobHandle(m_System, callingSystemDependency);
        }
    }
}
