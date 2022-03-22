using System.Runtime.InteropServices;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Collections
{
    [BurstCompatible]
    internal unsafe struct BlockPointer
    {
        public static readonly int SIZE = UnsafeUtility.SizeOf<BlockPointer>();
        public static readonly int ALIGNMENT = UnsafeUtility.AlignOf<BlockPointer>();

        public byte* Pointer;
        public BlockPointer* Next;
    }

    [BurstCompatible]
    [StructLayout(LayoutKind.Sequential, Size = 64)]
    internal unsafe struct ThreadData
    {
        public BlockPointer* FirstBlock;
        public BlockPointer* CurrentWriterBlock;
        public BlockPointer* CurrentReadBlock;
        public byte* WriterHead;
        public byte* WriterEndOfBlock;
        public byte* ReaderHead;
        public byte* ReaderEndOfBlock;
        public int Count;
    }

    [BurstCompatible]
    public unsafe struct UnsafeThreadedQueue<T> : INativeDisposable
        where T : struct
    {
        private readonly Allocator m_Allocator;
        private readonly int m_ElementSize;
        private readonly int m_ElementAlignment;
        private readonly int m_BlockSize;
        private readonly int m_MaxThreads;

        [NativeDisableUnsafePtrRestriction] private ThreadData* m_ThreadData;

        public bool IsCreated
        {
            get => m_ThreadData != null;
        }

        public UnsafeThreadedQueue(int elementsPerBlock, Allocator allocator)
        {
            m_Allocator = allocator;
            m_ElementSize = UnsafeUtility.SizeOf<T>();
            m_ElementAlignment = UnsafeUtility.AlignOf<T>();
            m_BlockSize = elementsPerBlock * m_ElementSize;
            m_MaxThreads = JobsUtility.JobWorkerMaximumCount + 1;

            int threadDataSize = UnsafeUtility.SizeOf<ThreadData>();
            m_ThreadData = (ThreadData*)UnsafeUtility.Malloc(threadDataSize * m_MaxThreads, threadDataSize, allocator);
            for (int i = 0; i < m_MaxThreads; ++i)
            {
                ThreadData* threadData = m_ThreadData + i;
                threadData->FirstBlock = null;
                threadData->CurrentWriterBlock = null;
                threadData->CurrentReadBlock = null;
                //https://forum.unity.com/threads/looking-for-nativecollection-advice-buffer-of-buffers.1107035/
                threadData->WriterHead = null;
                threadData->WriterHead++;
                threadData->WriterEndOfBlock = null;
                threadData->ReaderHead = null;
                threadData->ReaderEndOfBlock = null;
                threadData->Count = 0;
            }
        }

        public void Dispose()
        {
            if (!IsCreated)
            {
                return;
            }
            
            for (int i = 0; i < m_MaxThreads; ++i)
            {
                ThreadData* threadData = m_ThreadData + i;

                BlockPointer* blockPointer = threadData->FirstBlock;
                while (blockPointer != null)
                {
                    BlockPointer* currentBlockPointer = blockPointer;
                    blockPointer = currentBlockPointer->Next;
                    
                    UnsafeUtility.Free(currentBlockPointer->Pointer, m_Allocator);
                    UnsafeUtility.Free(currentBlockPointer, m_Allocator);
                }
            }
            
            UnsafeUtility.Free(m_ThreadData, m_Allocator);
            m_ThreadData = null;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            DisposeJob disposeJob = new DisposeJob(this);
            JobHandle disposeJobHandle = disposeJob.Schedule(inputDeps);
            m_ThreadData = null;
            
            return disposeJobHandle;
        }

        public int Count()
        {
            int count = 0;
            for (int i = 0; i < m_MaxThreads; ++i)
            {
                ThreadData* threadData = m_ThreadData + i;
                count += threadData->Count;
            }

            return count;
        }

        public bool IsEmpty()
        {
            if (!IsCreated)
            {
                return true;
            }
            
            for (int i = 0; i < m_MaxThreads; ++i)
            {
                ThreadData* threadData = m_ThreadData + i;
                if (threadData->Count > 0)
                {
                    return false;
                }
            }

            return true;
        }

        public Writer AsWriter()
        {
            return new Writer(ref this);
        }

        public Reader AsReader()
        {
            return new Reader(ref this);
        }

        [BurstCompatible]
        public struct Reader
        {
            private readonly int m_ElementSize;
            private readonly int m_MaxThreads;
            
            [NativeSetThreadIndex] private readonly int m_ThreadIndex;
            [NativeDisableUnsafePtrRestriction] private readonly ThreadData* m_ThreadData;

            public Reader(ref UnsafeThreadedQueue<T> unsafeThreadedQueue)
            {
                m_ThreadData = unsafeThreadedQueue.m_ThreadData;
                m_ElementSize = unsafeThreadedQueue.m_ElementSize;
                m_MaxThreads = unsafeThreadedQueue.m_MaxThreads;
                m_ThreadIndex = 0;
            }

            public bool CanRead()
            {
                ThreadData* threadData = m_ThreadData + m_ThreadIndex - 1;
                return threadData->ReaderHead != null;
            }

            public ref T Peek()
            {
                Assert.IsTrue(m_ThreadIndex <= m_MaxThreads && m_ThreadIndex > 0);
                ThreadData* threadData = m_ThreadData + m_ThreadIndex - 1;
                
                //Otherwise nothing has been written
                //TODO: Write test for this
                Assert.IsTrue(threadData->CurrentReadBlock != null && threadData->ReaderHead != null);
                
                byte* readPointer = threadData->ReaderHead;
                
                return ref UnsafeUtility.AsRef<T>(readPointer);
            }
            
            public ref T Read()
            {
                Assert.IsTrue(m_ThreadIndex <= m_MaxThreads && m_ThreadIndex > 0);
                ThreadData* threadData = m_ThreadData + m_ThreadIndex - 1;
                
                //Otherwise nothing has been written
                //TODO: Write test for this
                Assert.IsTrue(threadData->CurrentReadBlock != null && threadData->ReaderHead != null);

                byte* readPointer = threadData->ReaderHead;
                threadData->ReaderHead += m_ElementSize;
                
                if (threadData->ReaderHead > threadData->ReaderEndOfBlock)
                {
                    threadData->CurrentReadBlock = threadData->CurrentReadBlock->Next;
                    threadData->ReaderHead = (threadData->CurrentReadBlock != null)
                        ? threadData->CurrentReadBlock->Pointer
                        : null;
                }

                return ref UnsafeUtility.AsRef<T>(readPointer);
            }
        }

        [BurstCompatible]
        public readonly struct Writer
        {
            private readonly int m_BlockSize;
            private readonly int m_ElementSize;
            private readonly int m_ElementAlignment;
            private readonly Allocator m_Allocator;
            private readonly int m_MaxThreads;
            
            [NativeSetThreadIndex] private readonly int m_ThreadIndex;
            [NativeDisableUnsafePtrRestriction] private readonly ThreadData* m_ThreadData;

            public Writer(ref UnsafeThreadedQueue<T> unsafeThreadedQueue)
            {
                m_ThreadData = unsafeThreadedQueue.m_ThreadData;
                m_BlockSize = unsafeThreadedQueue.m_BlockSize;
                m_ElementSize = unsafeThreadedQueue.m_ElementSize;
                m_ElementAlignment = unsafeThreadedQueue.m_ElementAlignment;
                m_Allocator = unsafeThreadedQueue.m_Allocator;
                m_MaxThreads = unsafeThreadedQueue.m_MaxThreads;
                m_ThreadIndex = 0;
            }
            
            public void Write(T value)
            {
                Assert.IsTrue(m_ThreadIndex <= m_MaxThreads && m_ThreadIndex > 0);

                ThreadData* threadData = m_ThreadData + m_ThreadIndex - 1;

                //Checks if we need a new block to be allocated
                if (threadData->WriterHead > threadData->WriterEndOfBlock)
                {
                    //Creates the new block
                    BlockPointer* blockPointer = (BlockPointer*)UnsafeUtility.Malloc(BlockPointer.SIZE, BlockPointer.ALIGNMENT, m_Allocator);
                    blockPointer->Pointer = (byte*)UnsafeUtility.Malloc(m_BlockSize, m_ElementAlignment, m_Allocator);
                    blockPointer->Next = null;
                    
                    threadData->WriterHead = blockPointer->Pointer;
                    threadData->WriterEndOfBlock = blockPointer->Pointer + m_BlockSize - 1;
                
                    //If this is the first block for the thread, we'll assign that
                    if (threadData->FirstBlock == null)
                    {
                        threadData->CurrentWriterBlock = threadData->FirstBlock = blockPointer;
                        threadData->CurrentReadBlock = blockPointer;
                        threadData->ReaderHead = blockPointer->Pointer;
                    }
                    //Otherwise we'll update our linked list of blocks
                    else
                    {
                        threadData->CurrentWriterBlock->Next = blockPointer;
                        threadData->CurrentWriterBlock = blockPointer;
                    }
                }

                //Finally go ahead and write the struct to our block
                UnsafeUtility.CopyStructureToPtr(ref value, threadData->WriterHead);
                threadData->WriterHead += m_ElementSize;
                threadData->Count++;
            }
        }


        [BurstCompile]
        private struct DisposeJob : IJob
        {
            private UnsafeThreadedQueue<T> m_UnsafeThreadedQueue;

            public DisposeJob(UnsafeThreadedQueue<T> unsafeThreadedQueue)
            {
                m_UnsafeThreadedQueue = unsafeThreadedQueue;
            }
            
            public void Execute()
            {
                m_UnsafeThreadedQueue.Dispose();
            }
        }
        
    }
}
