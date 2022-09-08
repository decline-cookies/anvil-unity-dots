// //TODO: RE-ENABLE IF NEEDED
// using Anvil.Unity.DOTS.Data;
// using System;
// using System.Collections.Generic;
// using System.Diagnostics;
//
// namespace Anvil.Unity.DOTS.Entities
// {
//     /// <summary>
//     /// Configuration object to schedule a job that will be executed during an
//     /// <see cref="AbstractTaskDriver{TTaskDriverSystem}"/> or <see cref="AbstractTaskSystem{TTaskDriver}"/>'s update phase.
//     /// </summary>
//     public abstract class AbstractTaskWorkConfig
//     {
// #if ENABLE_UNITY_COLLECTIONS_CHECKS
//         internal enum DataUsage
//         {
//             //TODO: Add comments for what these usages mean
//             AddAsync,
//             Add,
//             IterateAsync,
//             Iterate,
//             UpdateAsync,
//             Update,
//             ResultsDestinationAsync,
//             ResultsDestination,
//             ResultsDestinationLookupAsync,
//             ResultsDestinationLookup
//         }
//
//         private enum ConfigState
//         {
//             Configuring,
//             Executing
//         }
//
//         private ConfigState m_ConfigState;
// #endif
//
//         internal List<AbstractVDWrapper> DataWrappers
//         {
//             get;
//         }
//
//         protected TaskWorkData TaskWorkData
//         {
//             get;
//         }
//
//         protected AbstractTaskWorkConfig(AbstractTaskSystem<> abstractTaskSystem, byte context)
//         {
//             DataWrappers = new List<AbstractVDWrapper>();
//             TaskWorkData = new TaskWorkData(abstractTaskSystem, context);
//
// #if ENABLE_UNITY_COLLECTIONS_CHECKS
//             m_ConfigState = ConfigState.Configuring;
// #endif
//         }
//
//         private void AddDataWrapper(AbstractVDWrapper dataWrapper)
//         {
// #if ENABLE_UNITY_COLLECTIONS_CHECKS
//             if (m_ConfigState != ConfigState.Configuring)
//             {
//                 throw new InvalidOperationException($"{this} is trying to add a data wrapper of {dataWrapper.Type} but the configuration phase is complete!");
//             }
// #endif
//             TaskWorkData.AddDataWrapper(dataWrapper);
//             DataWrappers.Add(dataWrapper);
//         }
//
//         protected void InternalRequireDataForAdd<TInstance>(ProxyDataStream<TInstance> data, bool isAsync)
//             where TInstance : unmanaged, IProxyData
//         {
//             VDWrapperForAdd wrapper = new VDWrapperForAdd(data);
//             AddDataWrapper(wrapper);
// #if ENABLE_UNITY_COLLECTIONS_CHECKS
//             Debug_NotifyWorkDataOfUsage(wrapper.Type, isAsync ? DataUsage.AddAsync : DataUsage.Add);
// #endif
//         }
//
//         //TODO: This might be able to be a DEBUG_ function. https://github.com/decline-cookies/anvil-unity-dots/pull/52/files#r960872902
//         protected void InternalRequireResultsDestinationLookup<TInstance>(ProxyDataStream<TInstance> data, bool isAsync)
//             where TInstance : unmanaged, IProxyData
//         {
//             //There's no need to add a wrapper since we don't need to actually get the data, we're just getting the pointers for writing results
// #if ENABLE_UNITY_COLLECTIONS_CHECKS
//             //TODO: Someway to notify that we want to use the pointers without type conflicts
//             // Debug_NotifyWorkDataOfUsage(wrapper.Type, isAsync ? DataUsage.ResultsDestinationLookupAsync : DataUsage.ResultsDestinationLookup);
// #endif
//         }
//
//         protected void InternalRequireDataForIterate<TInstance>(ProxyDataStream<TInstance> data, bool isAsync)
//             where TInstance : unmanaged, IProxyData
//         {
//             VDWrapperForIterate wrapper = new VDWrapperForIterate(data);
//             AddDataWrapper(wrapper);
// #if ENABLE_UNITY_COLLECTIONS_CHECKS
//             Debug_NotifyWorkDataOfUsage(wrapper.Type, isAsync ? DataUsage.IterateAsync : DataUsage.Iterate);
// #endif
//         }
//
//         protected void InternalRequireDataForUpdate<TInstance>(ProxyDataStream<TInstance> data, bool isAsync)
//             where TInstance : unmanaged, IProxyData
//         {
//             VDWrapperForUpdate wrapper = new VDWrapperForUpdate(data);
//             AddDataWrapper(wrapper);
// #if ENABLE_UNITY_COLLECTIONS_CHECKS
//             Debug_NotifyWorkDataOfUsage(wrapper.Type, isAsync ? DataUsage.UpdateAsync : DataUsage.Update);
// #endif
//         }
//
//         protected void InternalRequireDataAsResultsDestination<TResult>(ProxyDataStream<TResult> resultData, bool isAsync)
//             where TResult : unmanaged, IProxyData
//         {
//             VDWrapperAsResultsDestination wrapper = new VDWrapperAsResultsDestination(resultData);
//             AddDataWrapper(wrapper);
// #if ENABLE_UNITY_COLLECTIONS_CHECKS
//             Debug_NotifyWorkDataOfUsage(wrapper.Type, isAsync ? DataUsage.ResultsDestinationAsync : DataUsage.ResultsDestination);
// #endif
//         }
//
//         //*************************************************************************************************************
//         // SAFETY CHECKS
//         //*************************************************************************************************************
//
//         [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
//         internal void Debug_SetConfigurationStateComplete()
//         {
// // HACK: This shouldn't be required in addition to the `Conditional` attribute above but Unity's build system doesn't
// // seem to respect the attribute.
// #if ENABLE_UNITY_COLLECTIONS_CHECKS
//             m_ConfigState = ConfigState.Executing;
// #endif
//         }
//
//
// // HACK: This shouldn't be required in addition to the `Conditional` attribute above but Unity's build system doesn't
// // seem to respect the attribute.
// #if ENABLE_UNITY_COLLECTIONS_CHECKS
//         [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
//         private void Debug_NotifyWorkDataOfUsage(Type type, DataUsage usage)
//         {
//             TaskWorkData.Debug_NotifyWorkDataOfUsage(type, usage);
//         }
// #endif
//
//     }
// }