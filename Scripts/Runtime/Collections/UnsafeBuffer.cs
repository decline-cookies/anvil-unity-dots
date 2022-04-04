using System.Runtime.InteropServices;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Collections
{
    /// <summary>
    /// Information about a "block" of memory in the buffer.
    /// Contains the pointer to where the "block" begins and a pointer to another <see cref="BlockInfo"/> instance
    /// for the next "block" in the linked list. 
    /// </summary>
    [BurstCompatible]
    internal unsafe struct BlockInfo
    {
        public static readonly int SIZE = UnsafeUtility.SizeOf<BlockInfo>();
        public static readonly int ALIGNMENT = UnsafeUtility.AlignOf<BlockInfo>();

        public byte* Data;
        public BlockInfo* Next;
    }
    
    /// <summary>
    /// Information about a "lane" which is an indexed sub-section of the buffer that can be read from or written to.
    /// A lane is commonly used in parallel situations so that each thread stays in its own lane.
    /// Lane Info preserves the state of writing so that multiple jobs can write to a buffer over time.
    /// </summary>
    /// <remarks>
    /// Unity will refer to this as a "foreachindex" which is a little bit confusing.
    /// </remarks>
    [BurstCompatible]
    [StructLayout(LayoutKind.Sequential, Size = 64)]
    internal unsafe struct Lane
    {
        public BlockInfo* FirstBlock;
        public BlockInfo* CurrentWriterBlock;
        public byte* WriterHead;
        public byte* WriterEndOfBlock;
        public int Count;
    }
    
    /// <summary>
    /// Information about the <see cref="UnsafeBuffer{T}"/> itself.
    /// </summary>
    [BurstCompatible]
    internal struct BufferInfo
    {
        public int ElementSize;
        public int ElementAlignment;
        public int BlockSize;
        public int LaneCount;
        public Allocator Allocator;
    }
    
    /// <summary>
    /// A collection that allows for parallel reading and writing.
    /// It looks and somewhat behaves similar to a <see cref="UnsafeStream"/> but has some key differences that make it
    /// more advantageous.
    ///
    /// 1. Typed. The <see cref="UnsafeStream"/> can have anything written to it which results in arbitrarily sized and
    /// filled "blocks". This <see cref="UnsafeBuffer{T}"/> is typed and holds an exact amount of the elements in its
    /// "blocks".
    /// 2. Multiple writing. The <see cref="UnsafeStream"/> will only allow for writing on a given index once. You
    /// cannot have two jobs where one writes to index 1 and then later on in the frame a different job continues
    /// writing on index 1. This <see cref="UnsafeBuffer{T}"/> allows for that and is geared for different jobs to fill
    /// up the buffer.
    /// </summary>
    /// <typeparam name="T">The type of elements to store in the collection.</typeparam>
    [BurstCompatible]
    public unsafe struct UnsafeBuffer<T> : INativeDisposable
        where T : struct
    {
        //See Chunk.kChunkSize (Can't access here so we redefine)
        private const int CHUNK_SIZE = 16 * 1024;

        [NativeDisableUnsafePtrRestriction] private BufferInfo* m_BufferInfo;
        [NativeDisableUnsafePtrRestriction] private Lane* m_Lanes;
        
        /// <summary>
        /// Is the collection created or not?
        /// </summary>
        public bool IsCreated
        {
            get => m_Lanes != null;
        }
        
        /// <summary>
        /// For use with a <see cref="IJobParallelForBatch"/>, use this as the "arrayLength" parameter when scheduling
        /// your job.
        /// </summary>
        /// <example>
        /// myJob.ScheduleBatch(myBuffer.ScheduleBatchArrayLength, myBuffer.ScheduleBatchMinIndicesPerJobCount, dependsOn);
        /// </example>
        public int ScheduleBatchArrayLength
        {
            get => m_BufferInfo->LaneCount;
        }
        
        /// <summary>
        /// For use with a <see cref="IJobParallelForBatch"/>, use this as the "minIndicesPerJobCount" parameter when
        /// scheduling your job.
        /// </summary>
        /// <example>
        /// myJob.ScheduleBatch(myBuffer.ScheduleBatchArrayLength, myBuffer.ScheduleBatchMinIndicesPerJobCount, dependsOn);
        /// </example>
        public int ScheduleBatchMinIndicesPerJobCount
        {
            get => 1;
        }
        
        /// <summary>
        /// The number of lanes this buffer can read from / write to.
        /// </summary>
        public int LaneCount
        {
            get => m_BufferInfo->LaneCount;
        }
        
        /// <summary>
        /// Convenience constructor that fits as many elements into a 16kb block size.
        /// </summary>
        /// <param name="allocator">The <see cref="Allocator"/> to use when allocating memory.</param>
        public UnsafeBuffer(Allocator allocator) : this(CHUNK_SIZE / UnsafeUtility.SizeOf<T>(), allocator)
        {
        }
        
        /// <summary>
        /// More explicit constructor that allows for specifying how many elements to put into each block. Useful for
        /// smaller counts so that large blocks aren't allocated if not needed.
        /// </summary>
        /// <param name="elementsPerBlock">The number of elements to allocate space for in each block.</param>
        /// <param name="allocator">The <see cref="Allocator"/> to use when allocating memory.</param>
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
        
        /// <summary>
        /// Disposes the collection
        /// </summary>
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

                BlockInfo* block = lane->FirstBlock;
                while (block != null)
                {
                    BlockInfo* currentBlock = block;
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
        
        /// <summary>
        /// Schedules the disposal of the collection.
        /// </summary>
        /// <param name="inputDeps">The <see cref="JobHandle"/> to wait on before disposing</param>
        /// <returns>A <see cref="JobHandle"/> for when disposal is complete</returns>
        public JobHandle Dispose(JobHandle inputDeps)
        {
            DisposeJob disposeJob = new DisposeJob(this);
            JobHandle disposeJobHandle = disposeJob.Schedule(inputDeps);
            m_Lanes = null;

            return disposeJobHandle;
        }
        
        /// <summary>
        /// Calculates the number of elements in the entire collection across all lanes.
        /// Note: This will only be accurate if all write jobs have completed before calling this.
        /// </summary>
        /// <returns>The number of elements</returns>
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
        
        /// <summary>
        /// Whether the collection is empty or not.
        /// Note: This will only be accurate if all write jobs have completed before calling this.
        /// </summary>
        /// <returns>true if empty, false if not</returns>
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
        
        /// <summary>
        /// Returns a lightweight <see cref="Writer"/> instance
        /// </summary>
        /// <returns>A <see cref="Writer"/> instance.</returns>
        public Writer AsWriter()
        {
            return new Writer(ref this);
        }
        
        /// <summary>
        /// Returns a lightweight <see cref="LaneWriter"/> instance
        /// </summary>
        /// <param name="laneIndex">The lane index to lock the lane writer into.</param>
        /// <returns>A <see cref="LaneWriter"/> instance.</returns>
        public LaneWriter AsLaneWriter(int laneIndex)
        {
            Assert.IsTrue(laneIndex < m_BufferInfo->LaneCount && laneIndex >= 0);

            Lane* lane = m_Lanes + laneIndex;
            return new LaneWriter(lane, m_BufferInfo);
        }
        
        /// <summary>
        /// Returns a lightweight <see cref="Reader"/> instance
        /// </summary>
        /// <returns>A <see cref="Reader"/> instance.</returns>
        public Reader AsReader()
        {
            return new Reader(ref this);
        }
        
        /// <summary>
        /// Helper function to convert the collection to a <see cref="NativeArray{T}"/>
        /// Note: This will only be accurate if all write jobs have completed before calling this.
        /// </summary>
        /// <param name="allocator">The <see cref="Allocator"/> to allocate the <see cref="NativeArray{T}"/> memory
        /// with.</param>
        /// <returns>A <see cref="NativeArray{T}"/> of the elements</returns>
        public NativeArray<T> ToNativeArray(Allocator allocator)
        {
            NativeArray<T> array = new NativeArray<T>(Count(), allocator, NativeArrayOptions.UninitializedMemory);
            Reader reader = AsReader();
            int arrayIndex = 0;
            for (int i = 0; i < reader.LaneCount; ++i)
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
        
        //*************************************************************************************************************
        // WRITERS
        //*************************************************************************************************************
        
        /// <summary>
        /// A lightweight struct to allow for writing to the collection
        /// </summary>
        [BurstCompatible]
        public readonly struct Writer
        {
            [NativeDisableUnsafePtrRestriction] private readonly BufferInfo* m_BufferInfo;
            [NativeDisableUnsafePtrRestriction] private readonly Lane* m_Lanes;
            
            /// <summary>
            /// The number of lanes that can be written to.
            /// </summary>
            public int LaneCount
            {
                get => m_BufferInfo->LaneCount;
            }
            
            internal Writer(ref UnsafeBuffer<T> unsafeBuffer)
            {
                m_Lanes = unsafeBuffer.m_Lanes;
                m_BufferInfo = unsafeBuffer.m_BufferInfo;
            }
            
            /// <summary>
            /// Creates a <see cref="LaneWriter"/> instance for a given lane index.
            /// </summary>
            /// <param name="laneIndex">The lane index to use</param>
            /// <returns>A <see cref="LaneWriter"/> instance.</returns>
            public LaneWriter CreateWriterForLane(int laneIndex)
            {
                Assert.IsTrue(laneIndex < m_BufferInfo->LaneCount && laneIndex >= 0);

                Lane* lane = m_Lanes + laneIndex;
                return new LaneWriter(lane, m_BufferInfo);
            }
        }
        
        /// <summary>
        /// A lightweight writer instance that is locked to a specific lane
        /// </summary>
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
            
            /// <summary>
            /// Writes the element to the next spot in the lane's current block.
            /// </summary>
            /// <param name="value">The element to write</param>
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
                BlockInfo* blockPointer = (BlockInfo*)UnsafeUtility.Malloc(BlockInfo.SIZE, BlockInfo.ALIGNMENT, m_BufferInfo->Allocator);
                blockPointer->Data = (byte*)UnsafeUtility.Malloc(m_BufferInfo->BlockSize, m_BufferInfo->ElementAlignment, m_BufferInfo->Allocator);
                blockPointer->Next = null;
                
                //Update lane writing info
                m_Lane->WriterHead = blockPointer->Data;
                m_Lane->WriterEndOfBlock = blockPointer->Data + m_BufferInfo->BlockSize;

                //If this is the first block for the lane, we'll assign that
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
        
        //*************************************************************************************************************
        // READERS
        //*************************************************************************************************************
        
        /// <summary>
        /// A lightweight struct to allow for reader from the collection
        /// </summary>
        [BurstCompatible]
        public struct Reader
        {
            [NativeDisableUnsafePtrRestriction] private readonly BufferInfo* m_BufferInfo;
            [NativeDisableUnsafePtrRestriction] private readonly Lane* m_Lanes;
            
            /// <summary>
            /// How many lanes the collection has available to read from
            /// </summary>
            public int LaneCount
            {
                get => m_BufferInfo->LaneCount;
            }
            
            internal Reader(ref UnsafeBuffer<T> unsafeBuffer)
            {
                m_Lanes = unsafeBuffer.m_Lanes;
                m_BufferInfo = unsafeBuffer.m_BufferInfo;
            }
            
            /// <summary>
            /// Creates a <see cref="LaneReader"/> instance locked to a specific lane index.
            /// </summary>
            /// <param name="laneIndex">The lane index to use</param>
            /// <returns>A <see cref="LaneReader"/> instance</returns>
            public LaneReader CreateReaderForLane(int laneIndex)
            {
                Assert.IsTrue(laneIndex < m_BufferInfo->LaneCount && laneIndex >= 0);
                
                Lane* lane = m_Lanes + laneIndex;
                return new LaneReader(lane, m_BufferInfo);
            }
        }
        
        /// <summary>
        /// A lightweight reader instance locked to a specific lane.
        /// <see cref="LaneReader"/>'s manage their own state so that multiple readers can read from a given lane at
        /// the same time.
        /// </summary>
        [BurstCompatible]
        public struct LaneReader
        {
            [NativeDisableUnsafePtrRestriction] private readonly BufferInfo* m_BufferInfo;
            [NativeDisableUnsafePtrRestriction] private readonly Lane* m_Lane;
            
            private BlockInfo* m_CurrentReadBlock;
            private byte* m_EndOfCurrentBlock;
            private byte* m_ReaderHead;
            
            /// <summary>
            /// How many elements are available to read on this lane
            /// </summary>
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
            
            /// <summary>
            /// Reads the next element from the reader
            /// </summary>
            /// <returns>The next element</returns>
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
            
            /// <summary>
            /// Peeks at what the next elements in the reader is but does not advance the reader head.
            /// Will return the same value until <see cref="Read"/> has been called.
            /// </summary>
            /// <returns>The next element</returns>
            public ref T Peek()
            {
                ///Otherwise nothing has been written
                //TODO: Write test for this
                Assert.IsTrue(Count > 0 && m_CurrentReadBlock != null && m_ReaderHead != null);
                
                return ref UnsafeUtility.AsRef<T>(m_ReaderHead);
            }
        }
        
        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************
        
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
