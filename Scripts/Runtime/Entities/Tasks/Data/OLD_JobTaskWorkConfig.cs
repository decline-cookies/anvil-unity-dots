//TODO: RE-ENABLE IF NEEDED
// using Anvil.Unity.DOTS.Data;
// using Anvil.Unity.DOTS.Jobs;
// using System;
// using System.Diagnostics;
// using System.Reflection;
// using Unity.Collections;
// using Unity.Jobs;
//
// namespace Anvil.Unity.DOTS.Entities
// {
//     /// <summary>
//     /// A <see cref="AbstractTaskWorkConfig"/> specific for Jobs
//     /// </summary>
//     public class JobTaskWorkConfig : AbstractTaskWorkConfig
//     {
//         internal static readonly BulkScheduleDelegate<JobTaskWorkConfig> PREPARE_AND_SCHEDULE_SCHEDULE_DELEGATE = BulkSchedulingUtil.CreateSchedulingDelegate<JobTaskWorkConfig>(nameof(PrepareAndSchedule), BindingFlags.Instance | BindingFlags.NonPublic);
//         
//         /// <summary>
//         /// The scheduling callback that is called when the job struct needs to be created and run through the job scheduler.
//         /// </summary>
//         public delegate JobHandle ScheduleJobDelegate(JobHandle dependsOn, TaskWorkData jobTaskWorkData, IScheduleInfo scheduleInfo);
//
//         private readonly ScheduleJobDelegate m_ScheduleJobDelegate;
//         private IScheduleInfo m_ScheduleInfo;
//
//         internal JobTaskWorkConfig(ScheduleJobDelegate scheduleJobDelegate, AbstractTaskSystem<> abstractTaskSystem, byte context) : base(abstractTaskSystem, context)
//         {
//             m_ScheduleJobDelegate = scheduleJobDelegate;
//         }
//
//         /// <summary>
//         /// Specifies an instance of <see cref="ProxyDataStream{TData}"/> to use for scheduling.
//         /// This will calculate the batch size based on the <see cref="BatchStrategy"/> and size of
//         /// the <typeparamref name="TInstance"/> data.
//         /// NOTE: This does not ensure any access on the <see cref="ProxyDataStream{TData}"/> at the time the job
//         /// is actually scheduled. Please ensure that a Require function is also called for the access needed.
//         /// </summary>
//         /// <param name="data">The <see cref="ProxyDataStream{TData}"/> to schedule on.</param>
//         /// <param name="batchStrategy">The <see cref="BatchStrategy"/> to calculate scheduling with.</param>
//         /// <typeparam name="TKey">The type of the key</typeparam>
//         /// <typeparam name="TInstance">The type of the data</typeparam>
//         /// <returns>This <see cref="JobTaskWorkConfig"/> for chaining additional configuration.</returns>
//         public JobTaskWorkConfig ScheduleOn<TInstance>(ProxyDataStream<TInstance> data, BatchStrategy batchStrategy)
//             where TInstance : unmanaged, IProxyData
//         {
//             Debug_EnsureNoDuplicateScheduleInfo();
//             m_ScheduleInfo = new ProxyDataStreamScheduleInfo<TInstance>(data, batchStrategy);
//             return this;
//         }
//
//         /// <summary>
//         /// Specifies an instance of <see cref="NativeArray{T}"/> to use for scheduling.
//         /// This will calculate the batch size based on <see cref="BatchStrategy"/> and size of the
//         /// <typeparamref name="T"/> data.
//         /// NOTE: Access to the <see cref="NativeArray{T}"/> must be handled by the developer.
//         /// </summary>
//         /// <param name="array">The <see cref="NativeArray{T}"/> to schedule on.</param>
//         /// <param name="batchStrategy">The <see cref="BatchStrategy"/> to calculate scheduling with.</param>
//         /// <typeparam name="T">The type of the data.</typeparam>
//         /// <returns>This <see cref="JobTaskWorkConfig"/> for chaining additional configuration.</returns>
//         public JobTaskWorkConfig ScheduleOn<T>(NativeArray<T> array, BatchStrategy batchStrategy)
//             where T : unmanaged
//         {
//             Debug_EnsureNoDuplicateScheduleInfo();
//             m_ScheduleInfo = new NativeArrayScheduleInfo<T>(array, batchStrategy);
//             return this;
//         }
//
//         /// <summary>
//         /// Specifies an instance of <see cref="ProxyDataStream{TData}"/> that will be used in the job in an
//         /// Add context. 
//         /// </summary>
//         /// <param name="data">The <see cref="ProxyDataStream{TData}"/> that requires access.</param>
//         /// <typeparam name="TKey">The type of the key</typeparam>
//         /// <typeparam name="TInstance">The type of the data</typeparam>
//         /// <returns>This <see cref="JobTaskWorkConfig"/> for chaining additional configuration.</returns>
//         public JobTaskWorkConfig RequireDataForAddAsync<TInstance>(ProxyDataStream<TInstance> data)
//             where TInstance : unmanaged, IProxyData
//         {
//             InternalRequireDataForAdd(data, true);
//             return this;
//         }
//
//         public JobTaskWorkConfig RequireResultsDestinationLookup<TInstance>(ProxyDataStream<TInstance> data)
//             where TInstance : unmanaged, IProxyData
//         {
//             InternalRequireResultsDestinationLookup(data, true);
//             return this;
//         }
//
//         /// <summary>
//         /// Specifies and instance of <see cref="ProxyDataStream{TData}"/> that will be used in the job in an
//         /// Iterate context. 
//         /// </summary>
//         /// <param name="data">The <see cref="ProxyDataStream{TData}"/> that requires access.</param>
//         /// <typeparam name="TKey">The type of the key</typeparam>
//         /// <typeparam name="TInstance">The type of the data</typeparam>
//         /// <returns>This <see cref="JobTaskWorkConfig"/> for chaining additional configuration.</returns>
//         public JobTaskWorkConfig RequireDataForIterateAsync<TInstance>(ProxyDataStream<TInstance> data)
//             where TInstance : unmanaged, IProxyData
//         {
//             InternalRequireDataForIterate(data, true);
//             return this;
//         }
//
//         /// <summary>
//         /// Specifies and instance of <see cref="ProxyDataStream{TData}"/> that will be used in the job in an
//         /// Update context. 
//         /// </summary>
//         /// <param name="data">The <see cref="ProxyDataStream{TData}"/> that requires access.</param>
//         /// <typeparam name="TKey">The type of the key</typeparam>
//         /// <typeparam name="TInstance">The type of the data</typeparam>
//         /// <returns>This <see cref="JobTaskWorkConfig"/> for chaining additional configuration.</returns>
//         public JobTaskWorkConfig RequireDataForUpdateAsync<TInstance>(ProxyDataStream<TInstance> data)
//             where TInstance : unmanaged, IProxyData
//         {
//             InternalRequireDataForUpdate(data, true);
//             return this;
//         }
//
//         /// <summary>
//         /// Specifies and instance of <see cref="ProxyDataStream{TData}"/> that will be used in the job in an
//         /// Results Destination context. 
//         /// </summary>
//         /// <param name="resultData">
//         /// The <see cref="ProxyDataStream{TData}"/> to use as a results destination.
//         /// NOTE: No access is necessary for this as it is just being used to point to where to write later on.
//         /// </param>
//         /// <typeparam name="TKey">The type of the key</typeparam>
//         /// <typeparam name="TResult">The type of the result data</typeparam>
//         /// <returns>This <see cref="JobTaskWorkConfig"/> for chaining additional configuration.</returns>
//         public JobTaskWorkConfig RequireDataAsResultsDestination<TResult>(ProxyDataStream<TResult> resultData)
//             where TResult : unmanaged, IProxyData
//         {
//             InternalRequireDataAsResultsDestination(resultData, true);
//             return this;
//         }
//         
//         //TODO: Move this back to private once TaskDriverSystem supports "many" job configs
//         internal JobHandle PrepareAndSchedule(JobHandle dependsOn)
//         {
//             Debug_SetConfigurationStateComplete();
//             Debug_EnsureScheduleInfoPresent();
//
//             int len = DataWrappers.Count;
//             if (len == 0)
//             {
//                 return dependsOn;
//             }
//
//             NativeArray<JobHandle> dataDependencies = new NativeArray<JobHandle>(len + 1, Allocator.Temp);
//
//             for (int i = 0; i < len; ++i)
//             {
//                 AbstractVDWrapper wrapper = DataWrappers[i];
//                 dataDependencies[i] = wrapper.AcquireAsync();
//             }
//
//             dataDependencies[len] = dependsOn;
//
//             JobHandle delegateDependency = m_ScheduleJobDelegate(JobHandle.CombineDependencies(dataDependencies), TaskWorkData, m_ScheduleInfo);
//
//             foreach (AbstractVDWrapper data in DataWrappers)
//             {
//                 data.ReleaseAsync(delegateDependency);
//             }
//
//             return delegateDependency;
//         }
//
//         //*************************************************************************************************************
//         // SAFETY CHECKS
//         //*************************************************************************************************************
//         
//         [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
//         private void Debug_EnsureNoDuplicateScheduleInfo()
//         {
//             if (m_ScheduleInfo != null)
//             {
//                 throw new InvalidOperationException($"{nameof(ScheduleOn)} has already been called. This should only be called once");
//             }
//         }
//         
//         [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
//         private void Debug_EnsureScheduleInfoPresent()
//         {
//             if (m_ScheduleInfo == null)
//             {
//                 throw new InvalidOperationException($"No {nameof(IScheduleInfo)} was present. Please ensure that {nameof(ScheduleOn)} was called when configuring.");
//             }
//         }
//     }
// }
