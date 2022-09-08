
//TODO: See if this is still needed/useful
// using Anvil.CSharp.Core;
// using Anvil.Unity.DOTS.Jobs;
// using System;
// using System.Collections.Generic;
// using Unity.Jobs;
//
// namespace Anvil.Unity.DOTS.Data
// {
//     /// <summary>
//     /// A lookup collection of <see cref="ProxyDataStream{TData}"/> by <see cref="Type"/>
//     /// </summary>
//     internal class VirtualDataLookup : AbstractAnvilBase
//     {
//         private readonly Dictionary<Type, AbstractProxyDataStream> m_DataLookup;
//
//         public VirtualDataLookup()
//         {
//             m_DataLookup = new Dictionary<Type, AbstractProxyDataStream>();
//         }
//
//         protected override void DisposeSelf()
//         {
//             foreach (AbstractProxyDataStream data in m_DataLookup.Values)
//             {
//                 data.Dispose();
//             }
//             m_DataLookup.Clear();
//
//             base.DisposeSelf();
//         }
//         
//         /// <summary>
//         /// Adds <see cref="ProxyDataStream{TData}"/> to the lookup
//         /// </summary>
//         /// <param name="data">The <see cref="ProxyDataStream{TData}"/> to add</param>
//         /// <typeparam name="TKey">The type of Key</typeparam>
//         /// <typeparam name="TInstance">The type of Instance data</typeparam>
//         public void AddData<TInstance>(ProxyDataStream<TInstance> data)
//             where TInstance : unmanaged, IEntityProxyData
//         {
//             Type type = typeof(ProxyDataStream<TInstance>);
// #if ENABLE_UNITY_COLLECTIONS_CHECKS
//             if (m_DataLookup.ContainsKey(type))
//             {
//                 throw new InvalidOperationException($"VirtualData of type {type} has already been added!");
//             }
// #endif
//             m_DataLookup.Add(type, data);
//         }
//         
//         /// <summary>
//         /// Returns <see cref="ProxyDataStream{TData}"/> from the lookup
//         /// </summary>
//         /// <typeparam name="TKey">The type of Key</typeparam>
//         /// <typeparam name="TInstance">The type of Instance data</typeparam>
//         /// <returns>The <see cref="ProxyDataStream{TData}"/> instance</returns>
//         public ProxyDataStream<TInstance> GetData<TKey, TInstance>()
//             where TKey : unmanaged, IEquatable<TKey>
//             where TInstance : unmanaged, IEntityProxyData
//         {
//             Type type = typeof(ProxyDataStream<TInstance>);
// #if ENABLE_UNITY_COLLECTIONS_CHECKS
//             if (!m_DataLookup.ContainsKey(type))
//             {
//                 throw new InvalidOperationException($"VirtualData of type {type} has not been added! Please ensure {nameof(AddData)} has been called.");
//             }
// #endif
//             
//             return (ProxyDataStream<TInstance>)m_DataLookup[type];
//         }
//         
//         /// <summary>
//         /// Consolidates all <see cref="ProxyDataStream{TData}"/> in the lookup in parallel.
//         /// </summary>
//         /// <param name="dependsOn">The <see cref="JobHandle"/> consolidation work depends on.</param>
//         /// <returns>
//         /// A <see cref="JobHandle"/> that represents when all <see cref="ProxyDataStream{TData}"/>
//         /// consolidation is complete.
//         /// </returns>
//         public JobHandle ConsolidateForFrame(JobHandle dependsOn)
//         {
//             return m_DataLookup.Values.BulkScheduleParallel(dependsOn, AbstractProxyDataStream.CONSOLIDATE_FOR_FRAME_SCHEDULE_DELEGATE);
//         }
//     }
// }
