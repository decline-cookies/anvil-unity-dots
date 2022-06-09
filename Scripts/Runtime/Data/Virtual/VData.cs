// using Anvil.Unity.DOTS.Entities;
// using Anvil.Unity.DOTS.Jobs;
// using Unity.Burst;
// using Unity.Collections;
// using Unity.Collections.LowLevel.Unsafe;
// using Unity.Jobs;
//
// namespace Anvil.Unity.DOTS.Data
// {
//     public class VData<T> : AbstractVData
//         where T : struct
//     {
// #if ENABLE_UNITY_COLLECTIONS_CHECKS
//         //TODO: Can we make this an enum
//         private const string STATE_ENTITIES_ADD = "EntitiesAdd";
//         private const string STATE_ADD = "Add";
//         private const string STATE_WORK = "Work";
//         private const string STATE_CANCEL = "Cancel";
// #endif
//
//         private UnsafeTypedStream<T> m_Pending;
//         private UnsafeTypedStream<T> m_PendingCancel;
//         private DeferredNativeArray<T> m_Current;
//
//         public DeferredNativeArray<T> ArrayForScheduling
//         {
//             get => m_Current;
//         }
//
//         public int BatchSize
//         {
//             get;
//         }
//
//         public VData(BatchStrategy batchStrategy) : this(batchStrategy, NULL_VDATA)
//         {
//         }
//
//         public VData(BatchStrategy batchStrategy, AbstractVData input) : base(input)
//         {
//             m_Pending = new UnsafeTypedStream<T>(Allocator.Persistent,
//                                                  Allocator.TempJob);
//             m_PendingCancel = new UnsafeTypedStream<T>(Allocator.Persistent,
//                                                        Allocator.TempJob);
//             m_Current = new DeferredNativeArray<T>(Allocator.Persistent,
//                                                    Allocator.TempJob);
//
//             //TODO: Check on this
//             BatchSize = batchStrategy == BatchStrategy.MaximizeChunk
//                 ? ChunkUtil.MaxElementsPerChunk<T>()
//                 : 1;
//         }
//
//         protected override void DisposeSelf()
//         {
//             m_Pending.Dispose();
//             m_PendingCancel.Dispose();
//             m_Current.Dispose();
//
//             base.DisposeSelf();
//         }
//
//
//         public JobDataForCompletion<T> GetCompletionWriter()
//         {
//             return new JobDataForCompletion<T>(m_Pending.AsWriter());
//         }
//
//         public JobHandle AcquireForCancel(out JobDataForCancel<T> workStruct)
//         {
//             ValidateAcquireState(STATE_CANCEL);
//             JobHandle sharedWriteHandle = AccessController.AcquireAsync(AccessType.SharedWrite);
//
//             workStruct = new JobDataForCancel<T>(m_PendingCancel.AsWriter());
//
//             return sharedWriteHandle;
//         }
//
//         public void ReleaseForCancel(JobHandle releaseAccessDependency)
//         {
//             ValidateReleaseState(STATE_CANCEL);
//             AccessController.ReleaseAsync(releaseAccessDependency);
//         }
//         
//         //TODO: Main thread variants
//
//         public JobHandle AcquireForEntitiesAdd(out JobDataForEntitiesAdd<T> workStruct)
//         {
//             ValidateAcquireState(STATE_ENTITIES_ADD);
//             JobHandle sharedWriteHandle = AccessController.AcquireAsync(AccessType.SharedWrite);
//
//             workStruct = new JobDataForEntitiesAdd<T>(m_Pending.AsWriter());
//
//             return sharedWriteHandle;
//         }
//
//         public void ReleaseForEntitiesAdd(JobHandle releaseAccessDependency)
//         {
//             ValidateReleaseState(STATE_ENTITIES_ADD);
//             AccessController.ReleaseAsync(releaseAccessDependency);
//         }
//
//
//         public JobHandle AcquireForAdd(out JobDataForAdd<T> workStruct)
//         {
//             ValidateAcquireState(STATE_ADD);
//             JobHandle sharedWriteHandle = AccessController.AcquireAsync(AccessType.SharedWrite);
//
//             workStruct = new JobDataForAdd<T>(m_Pending.AsWriter());
//
//             return sharedWriteHandle;
//         }
//
//         public void ReleaseForAdd(JobHandle releaseAccessDependency)
//         {
//             ValidateReleaseState(STATE_ADD);
//             AccessController.ReleaseAsync(releaseAccessDependency);
//         }
//
//         public override JobHandle ConsolidateForFrame(JobHandle dependsOn)
//         {
//             JobHandle exclusiveWriteHandle = AccessController.AcquireAsync(AccessType.ExclusiveWrite);
//             
//             //Consolidate everything in pending into current so it can be balanced across threads
//             ConsolidateJob consolidateJob = new ConsolidateJob(m_Pending,
//                                                                m_Current);
//             JobHandle consolidateHandle = consolidateJob.Schedule(JobHandle.CombineDependencies(dependsOn, exclusiveWriteHandle));
//             
//             AccessController.ReleaseAsync(consolidateHandle);
//
//             return consolidateHandle;
//         }
//
//         public JobHandle AcquireForWork(out JobDataForWork<T> workStruct)
//         {
//             ValidateAcquireState(STATE_WORK);
//             JobHandle sharedWriteHandle = AccessController.AcquireAsync(AccessType.SharedWrite);
//             
//             //Create the work struct
//             workStruct = new JobDataForWork<T>(m_Pending.AsWriter(),
//                                                m_Current.AsDeferredJobArray());
//
//
//             return AcquireOutputsAsync(sharedWriteHandle);
//         }
//
//         public void ReleaseForWork(JobHandle releaseAccessDependency)
//         {
//             ValidateReleaseState(STATE_WORK);
//             AccessController.ReleaseAsync(releaseAccessDependency);
//             ReleaseOutputsAsync(releaseAccessDependency);
//         }
//
//         //*************************************************************************************************************
//         // JOBS
//         //*************************************************************************************************************
//
//         [BurstCompile]
//         private struct ConsolidateJob : IJob
//         {
//             private UnsafeTypedStream<T> m_Pending;
//             private UnsafeTypedStream<T> m_PendingCancel;
//             private DeferredNativeArray<T> m_Current;
//
//             public ConsolidateJob(UnsafeTypedStream<T> pending,
//                                   UnsafeTypedStream<T> pendingCancel,
//                                   DeferredNativeArray<T> current)
//             {
//                 m_Pending = pending;
//                 m_PendingCancel = pendingCancel;
//                 m_Current = current;
//             }
//
//             public void Execute()
//             {
//                 m_Current.Clear();
//                 int newLength = m_Pending.Count();
//
//                 if (newLength == 0)
//                 {
//                     return;
//                 }
//
//                 NativeArray<T> array = m_Current.DeferredCreate(newLength);
//                 m_Pending.CopyTo(ref array);
//                 m_Pending.Clear();
//             }
//         }
//     }
// }
