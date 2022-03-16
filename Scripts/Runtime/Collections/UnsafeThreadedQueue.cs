using System.Runtime.InteropServices;
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
        public byte* Pointer;
        public BlockPointer* Next;
    }

    [BurstCompatible]
    [StructLayout(LayoutKind.Sequential, Size = 64)]
    internal unsafe struct ThreadData
    {
        public BlockPointer* FirstBlock;
        public BlockPointer* CurrentBlock;
        public byte* WriterHead;
        public byte* EndOfBlock;
        public int Count;
    }

    [BurstCompatible]
    public unsafe struct UnsafeThreadedQueue<T> : INativeDisposable
        where T : struct
    {
        private readonly int m_ElementsPerBlock;
        private readonly Allocator m_Allocator;
        private readonly int m_ElementSize;
        private readonly int m_ElementAlignment;
        private readonly int m_BlockSize;
        private readonly int m_MaxThreads;

        private ThreadData* m_ThreadData;

        public bool IsCreated
        {
            get => m_ThreadData != null;
        }

        public UnsafeThreadedQueue(int elementsPerBlock, Allocator allocator)
        {
            m_ElementsPerBlock = elementsPerBlock;
            m_Allocator = allocator;
            m_ElementSize = UnsafeUtility.SizeOf<T>();
            m_ElementAlignment = UnsafeUtility.AlignOf<T>();
            m_BlockSize = m_ElementsPerBlock * m_ElementSize;
            m_MaxThreads = JobsUtility.JobWorkerMaximumCount;

            m_ThreadData = (ThreadData*)UnsafeUtility.Malloc(64 * m_MaxThreads, 64, allocator);
            for (int i = 0; i < m_MaxThreads; ++i)
            {
                m_ThreadData[i].FirstBlock = null;
                m_ThreadData[i].Count = 0;
                //https://forum.unity.com/threads/looking-for-nativecollection-advice-buffer-of-buffers.1107035/
                m_ThreadData[i].WriterHead = null;
                m_ThreadData[i].WriterHead++;
                m_ThreadData[i].EndOfBlock = null;
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
                    UnsafeUtility.Free(blockPointer->Pointer, m_Allocator);
                    blockPointer = blockPointer->Next;
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
            [NativeSetThreadIndex] private readonly int m_ThreadIndex;
            //TODO: Probably needs [NativeDisableUnsafePtrRestriction]
            private readonly ThreadData* m_ThreadData;
            
            public Reader(ref UnsafeThreadedQueue<T> unsafeThreadedQueue)
            {
                m_ThreadData = unsafeThreadedQueue.m_ThreadData;
                m_ThreadIndex = 0;
            }

            // public T Read()
            // {
            //     
            // }
        }

        [BurstCompatible]
        public readonly struct Writer
        {
            private readonly int m_BlockSize;
            private readonly int m_ElementSize;
            private readonly int m_ElementAlignment;
            private readonly Allocator m_Allocator;
            
            [NativeSetThreadIndex] private readonly int m_ThreadIndex;
            //TODO: Probably needs [NativeDisableUnsafePtrRestriction]
            private readonly ThreadData* m_ThreadData;
            
            
            public Writer(ref UnsafeThreadedQueue<T> unsafeThreadedQueue)
            {
                m_ThreadData = unsafeThreadedQueue.m_ThreadData;
                m_BlockSize = unsafeThreadedQueue.m_BlockSize;
                m_ElementSize = unsafeThreadedQueue.m_ElementSize;
                m_ElementAlignment = unsafeThreadedQueue.m_ElementAlignment;
                m_Allocator = unsafeThreadedQueue.m_Allocator;
                m_ThreadIndex = 0;
            }
            
            public void Write(T value)
            {
                ThreadData* threadData = m_ThreadData + m_ThreadIndex;
                //Checks if we need a new block to be allocated
                if (threadData->WriterHead > threadData->EndOfBlock)
                {
                    //Creates the new block
                    BlockPointer blockPointer = new BlockPointer
                    {
                        Pointer = (byte*)UnsafeUtility.Malloc(m_BlockSize, m_ElementAlignment, m_Allocator)
                    };
                    threadData->WriterHead = blockPointer.Pointer;
                    threadData->EndOfBlock = blockPointer.Pointer + m_BlockSize - 1;
                
                    //If this is the first block for the thread, we'll assign that
                    if (threadData->FirstBlock == null)
                    {
                        threadData->CurrentBlock = threadData->FirstBlock = &blockPointer;
                    }
                    //Otherwise we'll update our linked list of blocks
                    else
                    {
                        threadData->CurrentBlock->Next = &blockPointer;
                        threadData->CurrentBlock = &blockPointer;
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
