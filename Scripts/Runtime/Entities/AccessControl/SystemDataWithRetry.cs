// using Anvil.CSharp.Core;
// using Anvil.Unity.DOTS.Data;
// using Anvil.Unity.DOTS.Jobs;
// using Unity.Collections;
// using Unity.Collections.LowLevel.Unsafe;
// using Unity.Jobs;
//
// namespace Anvil.Unity.DOTS.Entities
// {
//     public struct SystemJobDataWithRetry<T>
//         where T:struct
//     {
//         [NativeSetThreadIndex][ReadOnly] private readonly int m_NativeThreadIndex;
//         
//         private readonly NativeArray<T> m_ReaderData;
//         
//         
//         private UnsafeTypedStream<T>.Writer m_RetryWriter;
//
//         
//
//         private UnsafeTypedStream<T>.LaneWriter m_RetryLaneWriter;
//         private UnsafeHashMap<int, bool> m_HandledIndices;
//
//         private UnsafeTypedStream<T>.LaneWriter RetryLaneWriter
//         {
//             get
//             {
//                 if (!m_RetryLaneWriter.IsCreated)
//                 {
//                     m_RetryLaneWriter = m_RetryWriter.AsLaneWriter(ParallelAccessUtil.CollectionIndexForThread(m_NativeThreadIndex));
//                 }
//
//                 return m_RetryLaneWriter;
//             }
//         }
//         
//         private UnsafeHashMap<int, bool> HandledIndices
//         {
//             get
//             {
//                 if (!m_HandledIndices.IsCreated)
//                 {
//                     m_HandledIndices = new UnsafeHashMap<int, bool>(m_ReaderData.Length, Allocator.Temp);
//                 }
//
//                 return m_HandledIndices;
//             }
//         }
//
//         public int Length
//         {
//             get => m_ReaderData.Length;
//         }
//
//         internal SystemJobDataWithRetry(NativeArray<T> readerData, UnsafeTypedStream<T>.Writer retryWriter)
//         {
//             m_ReaderData = readerData;
//             m_RetryWriter = retryWriter;
//             m_RetryLaneWriter = default;
//             m_HandledIndices = default;
//             
//             m_NativeThreadIndex = -1;
//         }
//         
//         public T this[int index]
//         {
//             get => m_ReaderData[index];
//         }
//
//         public bool IsHandled(int index)
//         {
//             return HandledIndices.ContainsKey(index);
//         }
//
//         public void Retry(ref T value)
//         {
//             RetryLaneWriter.Write(ref value);
//         }
//
//         public void MarkAsHandled(int index)
//         {
//             HandledIndices.Add(index, true);
//         }
//
//         public void RetryAllNotHandled()
//         {
//             for (int i = 0; i < m_ReaderData.Length; ++i)
//             {
//                 if (HandledIndices.ContainsKey(i))
//                 {
//                     continue;
//                 }
//                 T data = m_ReaderData[i];
//                 Retry(ref data);
//             }
//         }
//     }
//     
//     public class SystemDataWithRetry<T> : AbstractAnvilBase
//         where T : struct
//     {
//         private readonly AccessController m_WriterAccessController;
//         private readonly AccessController m_ReaderAccessController;
//
//         private UnsafeTypedStream<T> m_WriterData;
//         private UnsafeTypedStream<T>.LaneWriter m_WriterDataMainThreadLaneWriter;
//
//         //TODO: Could do the inner allocator method here and have this be readonly
//         private DeferredNativeArray<T> m_ReaderData;
//         private NativeArray<T> m_ReaderNativeArray;
//         
//
//         public SystemDataWithRetry()
//         {
//             m_WriterAccessController = new AccessController();
//             m_WriterData = new UnsafeTypedStream<T>(Allocator.Persistent, Allocator.TempJob);
//             m_WriterDataMainThreadLaneWriter = m_WriterData.AsLaneWriter(ParallelAccessUtil.CollectionIndexForMainThread());
//
//             m_ReaderAccessController = new AccessController();
//             m_ReaderData = new DeferredNativeArray<T>(Allocator.TempJob);
//         }
//
//         protected override void DisposeSelf()
//         {
//             m_ReaderAccessController.Dispose();
//             m_WriterAccessController.Dispose();
//             m_WriterData.Dispose();
//             m_ReaderData.Dispose();
//             base.DisposeSelf();
//         }
//
//         public JobHandle Update(JobHandle dependsOn)
//         {
//             //Dispose the previous frame's native array when everything external is done reading from it
//             m_ReaderData.Dispose(m_ReaderAccessController.AcquireAsync(AccessType.Disposal));
//
//             //Reset the access controller because we're creating a new instance for this frame
//             m_ReaderAccessController.Reset();
//             m_ReaderData = new DeferredNativeArray<T>(Allocator.TempJob);
//             m_ReaderNativeArray = default;
//
//             //Consolidate from a wide collection to a streamlined one so get the right access
//             JobHandle readFromWriterHandle = m_WriterAccessController.AcquireAsync(AccessType.SharedRead);
//             JobHandle writeToReaderHandle = m_ReaderAccessController.AcquireAsync(AccessType.ExclusiveWrite);
//
//             //Create and schedule the job to consolidate
//             ConsolidateToNativeArrayJob<T> consolidateJob = new ConsolidateToNativeArrayJob<T>(m_WriterData.AsReader(),
//                                                                                                m_ReaderData);
//
//             JobHandle prereqs = JobHandle.CombineDependencies(readFromWriterHandle,
//                                                               writeToReaderHandle,
//                                                               dependsOn);
//             JobHandle consolidateHandle = consolidateJob.Schedule(prereqs);
//             
//             //The writer can be used again once consolidation is complete
//             m_WriterAccessController.ReleaseAsync(consolidateHandle);
//             //The reader can be used now that consolidation is complete
//             m_ReaderAccessController.ReleaseAsync(consolidateHandle);
//             
//             //Clear the data in the writer 
//             JobHandle clearWriterHandle = m_WriterData.Clear(m_WriterAccessController.AcquireAsync(AccessType.ExclusiveWrite));
//             m_WriterAccessController.ReleaseAsync(clearWriterHandle);
//             
//             return clearWriterHandle;
//         }
//
//         public UnsafeTypedStream<T>.LaneWriter AcquireMainThreadWriter()
//         {
//             m_WriterAccessController.Acquire(AccessType.ExclusiveWrite);
//             return m_WriterDataMainThreadLaneWriter;
//         }
//
//         public void ReleaseMainThreadWriter()
//         {
//             m_WriterAccessController.Release();
//         }
//
//         public JobHandle AcquireWriterAsync(AccessType accessType, out UnsafeTypedStream<T>.Writer writer)
//         {
//             writer = m_WriterData.AsWriter();
//             return m_WriterAccessController.AcquireAsync(accessType);
//         }
//
//         public void ReleaseWriterAsync(JobHandle releaseAccessDependency)
//         {
//             m_WriterAccessController.ReleaseAsync(releaseAccessDependency);
//         }
//
//         public JobHandle AcquireReaderAsync(out NativeArray<T> reader)
//         {
//             if (!m_ReaderNativeArray.IsCreated)
//             {
//                 m_ReaderNativeArray = m_ReaderData.AsDeferredJobArray();
//             }
//
//             reader = m_ReaderNativeArray;
//             return m_ReaderAccessController.AcquireAsync(AccessType.SharedRead);
//         }
//
//         public void ReleaseReaderAsync(JobHandle releaseAccessDependency)
//         {
//             m_ReaderAccessController.ReleaseAsync(releaseAccessDependency);
//         }
//
//         public JobHandle AcquireSystemJobData(out SystemJobDataWithRetry<T> systemJobDataWithRetry)
//         {
//             JobHandle readHandle = AcquireReaderAsync(out NativeArray<T> reader);
//             JobHandle writeHandle = AcquireWriterAsync(AccessType.ExclusiveWrite, out UnsafeTypedStream<T>.Writer writer);
//
//             systemJobDataWithRetry = new SystemJobDataWithRetry<T>(reader, writer);
//             
//             return JobHandle.CombineDependencies(readHandle, writeHandle);
//         }
//
//         public void ReleaseSystemJobData(JobHandle releaseAccessDependency)
//         {
//             ReleaseReaderAsync(releaseAccessDependency);
//             ReleaseWriterAsync(releaseAccessDependency);
//         }
//
//     }
// }
