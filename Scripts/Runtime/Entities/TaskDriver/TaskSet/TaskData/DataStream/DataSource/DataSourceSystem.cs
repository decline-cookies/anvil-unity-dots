using Anvil.CSharp.Collections;
using System;
using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    //TODO: Refactor into one system to manage TaskDriver stuff
    internal partial class DataSourceSystem : AbstractAnvilSystemBase
    {
        private readonly Dictionary<Type, IDataSource> m_DataSourcesByType;
        private readonly List<AbstractTaskDriver> m_TopLevelTaskDrivers;

        public DataSourceSystem()
        {
            m_DataSourcesByType = new Dictionary<Type, IDataSource>();
            m_TopLevelTaskDrivers = new List<AbstractTaskDriver>();
        }

        protected sealed override void OnCreate()
        {
            base.OnCreate();
            Enabled = false;
        }

        protected sealed override void OnDestroy()
        {
            m_DataSourcesByType.DisposeAllValuesAndClear();
            base.OnDestroy();
        }

        public DataSource<T> GetOrCreateDataSource<T>()
            where T : unmanaged, IEquatable<T>
        {
            Type type = typeof(T);
            if (!m_DataSourcesByType.TryGetValue(type, out IDataSource dataSource))
            {
                dataSource = new DataSource<T>();
                m_DataSourcesByType.Add(type, dataSource);
            }

            return (DataSource<T>)dataSource;
        }

        public void RegisterTopLevelTaskDriver(AbstractTaskDriver taskDriver)
        {
            //TODO: Ensure top level
            m_TopLevelTaskDrivers.Add(taskDriver);
        }

        protected sealed override void OnUpdate()
        {
            Enabled = false;
        }
    }
}
