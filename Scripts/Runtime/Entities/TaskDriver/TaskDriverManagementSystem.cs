using Anvil.CSharp.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    //TODO: #86 - Revisit with Entities 1.0 for "Create Before/After"
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    internal partial class TaskDriverManagementSystem : AbstractAnvilSystemBase
    {
        private readonly Dictionary<Type, IDataSource> m_DataSourcesByType;
        private readonly HashSet<AbstractTaskDriver> m_AllTaskDrivers;
        private readonly HashSet<AbstractTaskDriverSystem> m_AllTaskDriverSystems;
        private readonly List<AbstractTaskDriver> m_TopLevelTaskDrivers;

        private bool m_IsInitialized;
        private bool m_IsHardened;

        public TaskDriverManagementSystem()
        {
            m_DataSourcesByType = new Dictionary<Type, IDataSource>();
            m_AllTaskDrivers = new HashSet<AbstractTaskDriver>();
            m_AllTaskDriverSystems = new HashSet<AbstractTaskDriverSystem>();
            m_TopLevelTaskDrivers = new List<AbstractTaskDriver>();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();

            if (m_IsInitialized)
            {
                return;
            }
            m_IsInitialized = true;

            Harden();
        }

        protected sealed override void OnDestroy()
        {
            m_DataSourcesByType.DisposeAllValuesAndClear();
            base.OnDestroy();
        }
        
        private void Harden()
        {
            Debug_EnsureNotHardened();
            m_IsHardened = true;
            
            //For all the TaskDrivers, filter to find the ones that don't have Parents.
            //Those are our top level TaskDrivers
            foreach (AbstractTaskDriver taskDriver in m_AllTaskDrivers.Where(taskDriver => taskDriver.Parent == null))
            {
                m_TopLevelTaskDrivers.Add(taskDriver);
            }

            //Then tell each top level Task Driver to Harden
            foreach (AbstractTaskDriver topLevelTaskDriver in m_TopLevelTaskDrivers)
            {
                topLevelTaskDriver.Harden();
            }
            
            //Then harden all the systems. All TaskDrivers are guaranteed to be hardened now so we can schedule all the jobs
            foreach (AbstractTaskDriverSystem taskDriverSystem in m_AllTaskDriverSystems)
            {
                taskDriverSystem.Harden();
            }
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

        public void RegisterTaskDriver(AbstractTaskDriver taskDriver)
        {
            m_AllTaskDrivers.Add(taskDriver);
            m_AllTaskDriverSystems.Add(taskDriver.TaskDriverSystem);
        }

        protected sealed override void OnUpdate()
        {
            
        }
        
        
        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNotHardened()
        {
            if (m_IsHardened)
            {
                throw new InvalidOperationException($"Trying to Harden {this} but {nameof(Harden)} has already been called!");
            }
        }
    }
}
