//TODO: RE-ENABLE IF NEEDED
// using Anvil.Unity.DOTS.Data;
// using System;
//
// namespace Anvil.Unity.DOTS.Entities
// {
//     /// <summary>
//     /// A <see cref="AbstractTaskWorkConfig"/> specific for Main Thread work.
//     /// </summary>
//     public class MainThreadTaskWorkConfig : AbstractTaskWorkConfig
//     {
//         internal MainThreadTaskWorkConfig(AbstractTaskSystem<> abstractTaskSystem, byte context) : base(abstractTaskSystem, context)
//         {
//         }
//
//         /// <summary>
//         /// Call when configuration of the <see cref="MainThreadTaskWorkConfig"/> is complete
//         /// and all the data needed should be acquired to operate on.
//         /// </summary>
//         /// <returns>
//         /// A <see cref="TaskWorkData"/> to use for doing the work.
//         /// All required data will have proper access.
//         /// </returns>
//         public TaskWorkData Acquire()
//         {
//             foreach (AbstractVDWrapper wrapper in DataWrappers)
//             {
//                 wrapper.Acquire();
//             }
//
//             Debug_SetConfigurationStateComplete();
//
//             return TaskWorkData;
//         }
//
//         internal void Release()
//         {
//             foreach (AbstractVDWrapper wrapper in DataWrappers)
//             {
//                 wrapper.Release();
//             }
//         }
//
//         /// <summary>
//         /// Specifies an instance of <see cref="ProxyDataStream{TData}"/> that will be used on the main thread
//         /// in an Add context. 
//         /// </summary>
//         /// <param name="data">The <see cref="ProxyDataStream{TData}"/> that requires access.</param>
//         /// <typeparam name="TKey">The type of the key</typeparam>
//         /// <typeparam name="TInstance">The type of the data</typeparam>
//         /// <returns>This <see cref="MainThreadTaskWorkConfig"/> for chaining additional configuration.</returns>
//         public MainThreadTaskWorkConfig RequireDataForAdd<TInstance>(ProxyDataStream<TInstance> data)
//             where TInstance : unmanaged, IProxyData
//         {
//             InternalRequireDataForAdd(data, false);
//             return this;
//         }
//
//         public MainThreadTaskWorkConfig RequireResultsDestinationLookup<TInstance>(ProxyDataStream<TInstance> data)
//             where TInstance : unmanaged, IProxyData
//         {
//             InternalRequireResultsDestinationLookup(data, false);
//             return this;
//         }
//
//
//         /// <summary>
//         /// Specifies and instance of <see cref="ProxyDataStream{TData}"/> that will be used on the main thread in an
//         /// Iterate context. 
//         /// </summary>
//         /// <param name="data">The <see cref="ProxyDataStream{TData}"/> that requires access.</param>
//         /// <typeparam name="TKey">The type of the key</typeparam>
//         /// <typeparam name="TInstance">The type of the data</typeparam>
//         /// <returns>This <see cref="MainThreadTaskWorkConfig"/> for chaining additional configuration.</returns>
//         public MainThreadTaskWorkConfig RequireDataForIterate<TInstance>(ProxyDataStream<TInstance> data)
//             where TInstance : unmanaged, IProxyData
//         {
//             InternalRequireDataForIterate(data, false);
//             return this;
//         }
//
//         /// <summary>
//         /// Specifies and instance of <see cref="ProxyDataStream{TData}"/> that will be used on the main thread in an
//         /// Update context. 
//         /// </summary>
//         /// <param name="data">The <see cref="ProxyDataStream{TData}"/> that requires access.</param>
//         /// <typeparam name="TKey">The type of the key</typeparam>
//         /// <typeparam name="TInstance">The type of the data</typeparam>
//         /// <returns>This <see cref="MainThreadTaskWorkConfig"/> for chaining additional configuration.</returns>
//         public MainThreadTaskWorkConfig RequireDataForUpdate<TKey, TInstance>(ProxyDataStream<TInstance> data)
//             where TInstance : unmanaged, IProxyData
//         {
//             InternalRequireDataForUpdate(data, false);
//             return this;
//         }
//
//         /// <summary>
//         /// Specifies and instance of <see cref="ProxyDataStream{TData}"/> that will be used on the main thread in an
//         /// Results Destination context. 
//         /// </summary>
//         /// <param name="resultData">
//         /// The <see cref="ProxyDataStream{TData}"/> to use as a results destination.
//         /// NOTE: No access is necessary for this as it is just being used to point to where to write later on.
//         /// </param>
//         /// <typeparam name="TKey">The type of the key</typeparam>
//         /// <typeparam name="TResult">The type of the result data</typeparam>
//         /// <returns>This <see cref="MainThreadTaskWorkConfig"/> for chaining additional configuration.</returns>
//         public MainThreadTaskWorkConfig RequireDataAsResultsDestination<TResult>(ProxyDataStream<TResult> resultData)
//             where TResult : unmanaged, IProxyData
//         {
//             InternalRequireDataAsResultsDestination(resultData, false);
//             return this;
//         }
//     }
// }
