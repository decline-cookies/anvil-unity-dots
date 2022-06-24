using System;
using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities
{
    public abstract class AbstractTaskWorkConfig
    {
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

        internal void AddDataWrapper(Type type, IDataWrapper dataWrapper)
        {
            m_TaskWorkData.AddDataWrapper(type, dataWrapper);
            DataWrappers.Add(dataWrapper);
        }
    }
}
