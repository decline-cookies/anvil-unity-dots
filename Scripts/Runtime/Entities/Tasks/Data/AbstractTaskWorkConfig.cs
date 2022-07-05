using Anvil.Unity.DOTS.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
            AddAsync,
            Add,
            IterateAsync,
            Iterate,
            UpdateAsync,
            Update,
            ResultsDestinationAsync,
            ResultsDestination
        }

        private enum ConfigState
        {
            Configuring,
            Executing
        }

        private ConfigState m_ConfigState;
#endif

        internal List<AbstractVDWrapper> DataWrappers
        {
            get;
        }

        protected TaskWorkData TaskWorkData
        {
            get;
        }

        protected AbstractTaskWorkConfig(AbstractTaskDriverSystem abstractTaskDriverSystem)
        {
            DataWrappers = new List<AbstractVDWrapper>();
            TaskWorkData = new TaskWorkData(abstractTaskDriverSystem);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_ConfigState = ConfigState.Configuring;
#endif
        }

        private void AddDataWrapper(AbstractVDWrapper dataWrapper)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (m_ConfigState != ConfigState.Configuring)
            {
                throw new InvalidOperationException($"{this} is trying to add a data wrapper of {dataWrapper.Type} but the configuration phase is complete!");
            }
#endif
            TaskWorkData.AddDataWrapper(dataWrapper);
            DataWrappers.Add(dataWrapper);
        }
        
        protected void InternalRequireDataForAdd<TKey, TInstance>(VirtualData<TKey, TInstance> data, bool isAsync)
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            VDWrapperForAdd wrapper = new VDWrapperForAdd(data);
            AddDataWrapper(wrapper);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Debug_NotifyWorkDataOfUsage(wrapper.Type, isAsync ? DataUsage.AddAsync : DataUsage.Add);
#endif
        }
        
        protected void InternalRequireDataForIterate<TKey, TInstance>(VirtualData<TKey, TInstance> data, bool isAsync)
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            VDWrapperForIterate wrapper = new VDWrapperForIterate(data);
            AddDataWrapper(wrapper);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Debug_NotifyWorkDataOfUsage(wrapper.Type, isAsync ? DataUsage.IterateAsync : DataUsage.Iterate);
#endif
        }
        
        protected void InternalRequireDataForUpdate<TKey, TInstance>(VirtualData<TKey, TInstance> data, bool isAsync)
            where TKey : unmanaged, IEquatable<TKey>
            where TInstance : unmanaged, IKeyedData<TKey>
        {
            VDWrapperForUpdate wrapper = new VDWrapperForUpdate(data);
            AddDataWrapper(wrapper);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Debug_NotifyWorkDataOfUsage(wrapper.Type, isAsync ? DataUsage.UpdateAsync : DataUsage.Update);
#endif
        }
        
        protected void InternalRequireDataAsResultsDestination<TKey, TResult>(VirtualData<TKey, TResult> resultData, bool isAsync)
            where TKey : unmanaged, IEquatable<TKey>
            where TResult : unmanaged, IKeyedData<TKey>
        {
            VDWrapperAsResultsDestination wrapper = new VDWrapperAsResultsDestination(resultData);
            AddDataWrapper(wrapper);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Debug_NotifyWorkDataOfUsage(wrapper.Type, isAsync ? DataUsage.ResultsDestinationAsync : DataUsage.ResultsDestination);
#endif
        }

        //*************************************************************************************************************
        // SAFETY CHECKS
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        internal void Debug_SetConfigurationStateComplete()
        {
            m_ConfigState = ConfigState.Executing;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private void Debug_NotifyWorkDataOfUsage(Type type, DataUsage usage)
        {
            TaskWorkData.Debug_NotifyWorkDataOfUsage(type, usage);
        }
#endif
    }
}
