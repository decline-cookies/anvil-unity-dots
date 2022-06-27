using System;
using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Configuration object to schedule a job that will be executed during an
    /// <see cref="AbstractTaskDriver{TTaskDriverSystem}"/> or <see cref="AbstractTaskDriverSystem"/>'s update phase.
    /// </summary>
    public abstract class AbstractTaskWorkConfig
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal enum DataUsage
        {
            Add,
            Iterate,
            Update,
            ResultsDestination
        }
#endif

        internal List<IDataWrapper> DataWrappers
        {
            get;
        }

        private AbstractTaskWorkData m_TaskWorkData;

        protected AbstractTaskWorkConfig()
        {
            DataWrappers = new List<IDataWrapper>();
        }

        protected void SetTaskWorkData(AbstractTaskWorkData taskWorkData)
        {
            m_TaskWorkData = taskWorkData;
        }

        internal void AddDataWrapper(IDataWrapper dataWrapper)
        {
            m_TaskWorkData.AddDataWrapper(dataWrapper);
            DataWrappers.Add(dataWrapper);
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal void DebugNotifyWorkDataOfUsage(Type type, DataUsage usage)
        {
            m_TaskWorkData.DebugNotifyWorkDataOfUsage(type, usage);
        }
#endif
    }
}
