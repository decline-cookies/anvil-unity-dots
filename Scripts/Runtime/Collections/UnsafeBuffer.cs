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
    internal struct BufferInfo
    {
        public int ElementSize;
        public int ElementAlignment;
        public int BlockSize;
        public int LaneCount;
        public Allocator Allocator;
    }

    [BurstCompatible]
    public unsafe struct UnsafeBuffer<T> : INativeDisposable
        where T : struct
    {
        //See Chunk.kChunkSize (Can't access here so we redefine)
        private const int CHUNK_SIZE = 16 * 1024;

        [NativeDisableUnsafePtrRestriction] private BufferInfo* m_BufferInfo;
        [NativeDisableUnsafePtrRestriction] private Lane* m_Lanes;

        public bool IsCreated
        {
            get => m_Lanes != null;
        }

        public int ScheduleBatchCount
        {
            get => m_BufferInfo->LaneCount;
        }

        public int ScheduleBatchMinIndicesPerJobCount
        {
            get => 1;
        }

        public int LaneCount
        {
            get => m_BufferInfo->LaneCount;
        }

        public UnsafeBuffer(Allocator allocator) : this(CHUNK_SIZE / UnsafeUtility.SizeOf<T>(), allocator)
        {
        }

        public UnsafeBuffer(int elementsPerBlock, Allocator allocator)
        {
            m_BufferInfo = (BufferInfo*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BufferInfo>(),
                                                             UnsafeUtility.AlignOf<BufferInfo>(),
                                                             allocator);
            m_BufferInfo->Allocator = allocator;
            m_BufferInfo->ElementSize = UnsafeUtility.SizeOf<T>();
            m_BufferInfo->ElementAlignment = UnsafeUtility.AlignOf<T>();
            m_BufferInfo->BlockSize = elementsPerBlock * m_BufferInfo->ElementSize;
            m_BufferInfo->LaneCount = JobsUtility.JobWorkerMaximumCount + 1;

            int laneSize = UnsafeUtility.SizeOf<Lane>();
            m_Lanes = (Lane*)UnsafeUtility.Malloc(laneSize * m_BufferInfo->LaneCount, laneSize, allocator);
            for (int i = 0; i < m_BufferInfo->LaneCount; ++i)
            {
                Lane* lane = m_Lanes + i;
                lane->FirstBlock = null;
                lane->CurrentWriterBlock = null;
                lane->WriterHead = null;
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

            Allocator allocator = m_BufferInfo->Allocator;

            for (int i = 0; i < m_BufferInfo->LaneCount; ++i)
            {
                Lane* lane = m_Lanes + i;

                Block* block = lane->FirstBlock;
                while (block != null)
                {
                    Block* currentBlock = block;
                    block = currentBlock->Next;

                    UnsafeUtility.Free(currentBlock->Data, allocator);
                    UnsafeUtility.Free(currentBlock, allocator);
                }
            }

            UnsafeUtility.Free(m_Lanes, allocator);
            UnsafeUtility.Free(m_BufferInfo, allocator);
            
            m_Lanes = null;
            m_BufferInfo = null;
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
            for (int i = 0; i < m_BufferInfo->LaneCount; ++i)
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

            for (int i = 0; i < m_BufferInfo->LaneCount; ++i)
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

        public LaneWriter AsLaneWriter(int laneIndex)
        {
            Assert.IsTrue(laneIndex <= m_BufferInfo->LaneCount && laneIndex > 0);

            Lane* lane = m_Lanes + laneIndex - 1;
            return new LaneWriter(lane, m_BufferInfo);
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
        
        [BurstCompatible]
        public struct LaneReader
        {
            [NativeDisableUnsafePtrRestriction] private readonly BufferInfo* m_BufferInfo;
            [NativeDisableUnsafePtrRestriction] private readonly Lane* m_Lane;
            
            private Block* m_CurrentReadBlock;
            private byte* m_EndOfCurrentBlock;
            private byte* m_ReaderHead;

            public int Count
            {
                get => m_Lane->Count;
            }
            
            internal LaneReader(Lane* lane, BufferInfo* bufferInfo)
            {
                m_Lane = lane;
                m_BufferInfo = bufferInfo;
                
                m_CurrentReadBlock = m_Lane->FirstBlock;
                m_ReaderHead = (m_CurrentReadBlock != null) 
                    ? m_CurrentReadBlock->Data
                    : null;
                m_EndOfCurrentBlock = m_ReaderHead + m_BufferInfo->BlockSize;
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
                m_EndOfCurrentBlock = m_ReaderHead + m_BufferInfo->BlockSize;
            }

            public ref T Read()
            {
                //Otherwise nothing has been written
                //TODO: Write test for this
                Assert.IsTrue(Count > 0 && m_CurrentReadBlock != null && m_ReaderHead != null);

                byte* readPointer = m_ReaderHead;
                m_ReaderHead += m_BufferInfo->ElementSize;
                
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
            [NativeDisableUnsafePtrRestriction] private readonly BufferInfo* m_BufferInfo;
            [NativeDisableUnsafePtrRestriction] private readonly Lane* m_Lanes;

            public int LaneCount
            {
                get => m_BufferInfo->LaneCount;
            }
            
            public Reader(ref UnsafeBuffer<T> unsafeBuffer)
            {
                m_Lanes = unsafeBuffer.m_Lanes;
                m_BufferInfo = unsafeBuffer.m_BufferInfo;
            }

            public LaneReader CreateReaderForLane(int laneIndex)
            {
                Assert.IsTrue(laneIndex <= m_BufferInfo->LaneCount && laneIndex > 0);
                
                Lane* lane = m_Lanes + laneIndex - 1;
                return new LaneReader(lane, m_BufferInfo);
            }
        }

        [BurstCompatible]
        public struct LaneWriter
        {
            [NativeDisableUnsafePtrRestriction] private readonly BufferInfo* m_BufferInfo;
            [NativeDisableUnsafePtrRestriction] private readonly Lane* m_Lane;

            internal LaneWriter(Lane* lane, BufferInfo* bufferInfo)
            {
                m_Lane = lane;
                m_BufferInfo = bufferInfo;
            }
            
            public void Write(T value)
            {
                //See if we need to allocate a new block
                CheckForNewBlock();

                //Finally go ahead and write the struct to our block
                UnsafeUtility.CopyStructureToPtr(ref value, m_Lane->WriterHead);
                m_Lane->WriterHead += m_BufferInfo->ElementSize;
                m_Lane->Count++;
            }

            private void CheckForNewBlock()
            {
                if (m_Lane->WriterHead < m_Lane->WriterEndOfBlock)
                {
                    return;
                }
                
                //Create the new block
                Block* blockPointer = (Block*)UnsafeUtility.Malloc(Block.SIZE, Block.ALIGNMENT, m_BufferInfo->Allocator);
                blockPointer->Data = (byte*)UnsafeUtility.Malloc(m_BufferInfo->BlockSize, m_BufferInfo->ElementAlignment, m_BufferInfo->Allocator);
                blockPointer->Next = null;

                m_Lane->WriterHead = blockPointer->Data;
                m_Lane->WriterEndOfBlock = blockPointer->Data + m_BufferInfo->BlockSize;

                //If this is the first block for the thread, we'll assign that
                if (m_Lane->FirstBlock == null)
                {
                    m_Lane->CurrentWriterBlock = m_Lane->FirstBlock = blockPointer;
                }
                //Otherwise we'll update our linked list of blocks
                else
                {
                    m_Lane->CurrentWriterBlock->Next = blockPointer;
                    m_Lane->CurrentWriterBlock = blockPointer;
                }
            }
        }

        [BurstCompatible]
        public readonly struct Writer
        {
            [NativeDisableUnsafePtrRestriction] private readonly BufferInfo* m_BufferInfo;
            [NativeDisableUnsafePtrRestriction] private readonly Lane* m_Lanes;

            public int LaneCount
            {
                get => m_BufferInfo->LaneCount;
            }
            
            public Writer(ref UnsafeBuffer<T> unsafeBuffer)
            {
                m_Lanes = unsafeBuffer.m_Lanes;
                m_BufferInfo = unsafeBuffer.m_BufferInfo;
            }

            public LaneWriter CreateWriterForLane(int laneIndex)
            {
                Assert.IsTrue(laneIndex <= m_BufferInfo->LaneCount && laneIndex > 0);

                Lane* lane = m_Lanes + laneIndex - 1;
                return new LaneWriter(lane, m_BufferInfo);
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
