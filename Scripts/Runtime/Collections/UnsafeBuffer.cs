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
    internal unsafe struct Block
    {
        public static readonly int SIZE = UnsafeUtility.SizeOf<Block>();
        public static readonly int ALIGNMENT = UnsafeUtility.AlignOf<Block>();

        public byte* Data;
        public Block* Next;
    }

    [BurstCompatible]
    [StructLayout(LayoutKind.Sequential, Size = 64)]
    internal unsafe struct Lane
    {
        public Block* FirstBlock;
        public Block* CurrentWriterBlock;
        public byte* WriterHead;
        public byte* WriterEndOfBlock;
        public int Count;
    }

    [BurstCompatible]
    public unsafe struct UnsafeBuffer<T> : INativeDisposable
        where T : struct
    {
        //See Chunk.kChunkSize (Can't access here so we redefine)
        private const int CHUNK_SIZE = 16 * 1024;

        private readonly Allocator m_Allocator;
        private readonly int m_ElementSize;
        private readonly int m_ElementAlignment;
        private readonly int m_BlockSize;
        private readonly int m_LaneCount;

        [NativeDisableUnsafePtrRestriction] private Lane* m_Lanes;

        public bool IsCreated
        {
            get => m_Lanes != null;
        }

        public int ScheduleBatchCount
        {
            get => m_LaneCount;
        }

        public int ScheduleBatchMinIndicesPerJobCount
        {
            get => 1;
        }

        public UnsafeBuffer(Allocator allocator) : this(CHUNK_SIZE / UnsafeUtility.SizeOf<T>(), allocator)
        {
        }

        public UnsafeBuffer(int elementsPerBlock, Allocator allocator)
        {
            m_Allocator = allocator;
            m_ElementSize = UnsafeUtility.SizeOf<T>();
            m_ElementAlignment = UnsafeUtility.AlignOf<T>();
            m_BlockSize = elementsPerBlock * m_ElementSize;
            m_LaneCount = JobsUtility.JobWorkerMaximumCount + 1;
            
            int laneSize = UnsafeUtility.SizeOf<Lane>();
            m_Lanes = (Lane*)UnsafeUtility.Malloc(laneSize * m_LaneCount, laneSize, allocator);
            for (int i = 0; i < m_LaneCount; ++i)
            {
                Lane* lane = m_Lanes + i;
                lane->FirstBlock = null;
                lane->CurrentWriterBlock = null;
                //https://forum.unity.com/threads/looking-for-nativecollection-advice-buffer-of-buffers.1107035/
                lane->WriterHead = null;
                lane->WriterHead++;
                lane->WriterEndOfBlock = null;
                lane->Count = 0;
            }
        }

        public void Dispose()
        {
            if (!IsCreated)
            {
                return;
            }

            for (int i = 0; i < m_LaneCount; ++i)
            {
                Lane* lane = m_Lanes + i;

                Block* block = lane->FirstBlock;
                while (block != null)
                {
                    Block* currentBlock = block;
                    block = currentBlock->Next;

                    UnsafeUtility.Free(currentBlock->Data, m_Allocator);
                    UnsafeUtility.Free(currentBlock, m_Allocator);
                }
            }

            UnsafeUtility.Free(m_Lanes, m_Allocator);
            m_Lanes = null;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            DisposeJob disposeJob = new DisposeJob(this);
            JobHandle disposeJobHandle = disposeJob.Schedule(inputDeps);
            m_Lanes = null;

            return disposeJobHandle;
        }

        public int Count()
        {
            int count = 0;
            for (int i = 0; i < m_LaneCount; ++i)
            {
                Lane* lane = m_Lanes + i;
                count += lane->Count;
            }

            return count;
        }

        public bool IsEmpty()
        {
            if (!IsCreated)
            {
                return true;
            }

            for (int i = 0; i < m_LaneCount; ++i)
            {
                Lane* threadData = m_Lanes + i;
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
        

        public NativeArray<T> ToNativeArray(Allocator allocator)
        {
            NativeArray<T> array = new NativeArray<T>(Count(), allocator, NativeArrayOptions.UninitializedMemory);
            Reader reader = AsReader();
            int arrayIndex = 0;
            for (int i = 1; i <= reader.LaneCount; ++i)
            {
                LaneReader laneReader = reader.CreateReaderForLane(i);
                int len = laneReader.Count;
                for (int j = 0; j < len; ++j)
                {
                    array[arrayIndex] = laneReader.Read();
                    arrayIndex++;
                }
            }

            return array;
        }

        public struct LaneReader
        {
            private readonly int m_ElementSize;
            private readonly int m_BlockSize;
            private readonly Lane* m_Lane;
            
            private Block* m_CurrentReadBlock;
            private byte* m_EndOfCurrentBlock;
            private byte* m_ReaderHead;

            public int Count
            {
                get => m_Lane->Count;
            }
            
            internal LaneReader(Lane* lane, int elementSize, int blockSize)
            {
                m_Lane = lane;
                m_ElementSize = elementSize;
                m_BlockSize = blockSize;
                
                m_CurrentReadBlock = m_Lane->FirstBlock;
                m_ReaderHead = (m_CurrentReadBlock != null) 
                    ? m_CurrentReadBlock->Data
                    : null;
                m_EndOfCurrentBlock = m_ReaderHead + m_BlockSize;
            }

            private void CheckForEndOfBlock()
            {
                if (m_ReaderHead < m_EndOfCurrentBlock)
                {
                    return;
                }
                
                m_CurrentReadBlock = m_CurrentReadBlock->Next;
                m_ReaderHead = (m_CurrentReadBlock != null) 
                    ? m_CurrentReadBlock->Data
                    : null;
                m_EndOfCurrentBlock = m_ReaderHead + m_BlockSize;
            }

            public ref T Read()
            {
                //Otherwise nothing has been written
                //TODO: Write test for this
                Assert.IsTrue(Count > 0 && m_CurrentReadBlock != null && m_ReaderHead != null);

                byte* readPointer = m_ReaderHead;
                m_ReaderHead += m_ElementSize;
                
                CheckForEndOfBlock();
                
                return ref UnsafeUtility.AsRef<T>(readPointer);
            }
            
            public ref T Peek()
            {
                ///Otherwise nothing has been written
                //TODO: Write test for this
                Assert.IsTrue(Count > 0 && m_CurrentReadBlock != null && m_ReaderHead != null);
                
                return ref UnsafeUtility.AsRef<T>(m_ReaderHead);
            }
        }

        [BurstCompatible]
        public struct Reader
        {
            private readonly int m_ElementSize;
            private readonly int m_BlockSize;
            private readonly int m_LaneCount;
            
            [NativeDisableUnsafePtrRestriction] private readonly Lane* m_Lanes;

            public int LaneCount
            {
                get => m_LaneCount;
            }
            
            public Reader(ref UnsafeBuffer<T> unsafeBuffer)
            {
                m_Lanes = unsafeBuffer.m_Lanes;
                m_ElementSize = unsafeBuffer.m_ElementSize;
                m_BlockSize = unsafeBuffer.m_BlockSize;
                m_LaneCount = unsafeBuffer.m_LaneCount;
            }

            public LaneReader CreateReaderForLane(int laneIndex)
            {
                Assert.IsTrue(laneIndex <= m_LaneCount && laneIndex > 0);
                
                Lane* lane = m_Lanes + laneIndex - 1;
                return new LaneReader(lane, m_ElementSize, m_BlockSize);
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
            [NativeDisableUnsafePtrRestriction] private readonly Lane* m_ThreadData;
            
            public int ThreadIndex
            {
                get => m_ThreadIndex;
            }

            public Writer(ref UnsafeBuffer<T> unsafeBuffer)
            {
                m_ThreadData = unsafeBuffer.m_Lanes;
                m_BlockSize = unsafeBuffer.m_BlockSize;
                m_ElementSize = unsafeBuffer.m_ElementSize;
                m_ElementAlignment = unsafeBuffer.m_ElementAlignment;
                m_Allocator = unsafeBuffer.m_Allocator;
                m_MaxThreads = unsafeBuffer.m_LaneCount;
                m_ThreadIndex = 0;
            }

            public void Write(T value, int threadIndex)
            {
                Assert.IsTrue(threadIndex <= m_MaxThreads && threadIndex > 0);

                Lane* threadData = m_ThreadData + threadIndex - 1;

                //Checks if we need a new block to be allocated
                if (threadData->WriterHead > threadData->WriterEndOfBlock)
                {
                    //Creates the new block
                    Block* blockPointer = (Block*)UnsafeUtility.Malloc(Block.SIZE, Block.ALIGNMENT, m_Allocator);
                    blockPointer->Data = (byte*)UnsafeUtility.Malloc(m_BlockSize, m_ElementAlignment, m_Allocator);
                    blockPointer->Next = null;

                    threadData->WriterHead = blockPointer->Data;
                    threadData->WriterEndOfBlock = blockPointer->Data + m_BlockSize - 1;

                    //If this is the first block for the thread, we'll assign that
                    if (threadData->FirstBlock == null)
                    {
                        threadData->CurrentWriterBlock = threadData->FirstBlock = blockPointer;
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
            private UnsafeBuffer<T> m_UnsafeBuffer;

            public DisposeJob(UnsafeBuffer<T> unsafeBuffer)
            {
                m_UnsafeBuffer = unsafeBuffer;
            }

            public void Execute()
            {
                m_UnsafeBuffer.Dispose();
            }
        }
    }
}
