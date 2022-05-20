using Anvil.Unity.DOTS.Jobs;
using System.Runtime.InteropServices;
using Unity.Assertions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Anvil.Unity.DOTS.Data
{
    //TODO: #15 - Investigate if there's any downfalls to this vs UnsafeStream. Maybe determinism?
    /// <summary>
    /// A collection that allows for parallel reading and writing.
    /// It looks and behaves somewhat similarly to a <see cref="UnsafeStream" /> but has some key differences that make it
    /// more advantageous when different jobs need to write to the container.
    /// 1. Typed Elements. The <see cref="UnsafeStream" /> can have anything written to it which results in arbitrarily
    /// sized and filled "blocks". This <see cref="UnsafeTypedStream{T}" /> is typed and holds an exact amount of the
    /// typed elements in its "blocks".
    /// 2. Multiple writes per thread/lane index. The <see cref="UnsafeStream" /> will only allow for writing to a given
    /// index once. You cannot have two jobs where one writes on index 1 and then later on in the frame a different job continues
    /// writing on index 1. This <see cref="UnsafeTypedStream{T}" /> allows for that while also allowing multiple jobs to
    /// fill up the buffer.
    /// </summary>
    /// <typeparam name="T">The type of elements to store in the collection.</typeparam>
    [BurstCompatible]
    public unsafe struct UnsafeTypedStream<T> : INativeDisposable
        where T : struct
    {
        /// <summary>
        /// Information about the <see cref="UnsafeTypedStream{T}" /> itself.
        /// </summary>
        [BurstCompatible]
        internal struct BufferInfo
        {
            public int BlockSize;
            public int LaneCount;
            public Allocator Allocator;
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
        internal struct LaneInfo
        {
            public static readonly int SIZE = UnsafeUtility.SizeOf<LaneInfo>();
            public static readonly int ALIGNMENT = UnsafeUtility.AlignOf<LaneInfo>();

            public BlockInfo* FirstBlock;
            public BlockInfo* CurrentWriterBlock;
            public byte* WriterHead;
            public byte* WriterEndOfBlock;
            public int Count;
        }

        /// <summary>
        /// Information about a "block" of memory in the buffer.
        /// Contains the pointer to where the "block" begins and a pointer to another <see cref="BlockInfo" /> instance
        /// for the next "block" in the linked list.
        /// </summary>
        [BurstCompatible]
        internal struct BlockInfo
        {
            public static readonly int SIZE = UnsafeUtility.SizeOf<BlockInfo>();
            public static readonly int ALIGNMENT = UnsafeUtility.AlignOf<BlockInfo>();

            public byte* Data;
            public BlockInfo* Next;
        }

        //See Chunk.kChunkSize (Can't access here so we redefine)
        private const int CHUNK_SIZE = 16 * 1024;

        private static readonly int ELEMENT_SIZE = UnsafeUtility.SizeOf<T>();
        private static readonly int ELEMENT_ALIGNMENT = UnsafeUtility.AlignOf<T>();

        [NativeDisableUnsafePtrRestriction] private BufferInfo* m_BufferInfo;
        [NativeDisableUnsafePtrRestriction] private LaneInfo* m_Lanes;

        /// <summary>
        /// Reports whether memory for the container is allocated.
        /// This will be true when the container has had memory allocated AND is not yet disposed.
        /// </summary>
        /// <remarks>
        /// If you use the default constructor, memory will not have been allocated.
        /// </remarks>
        public bool IsCreated
        {
            get => m_Lanes != null;
        }

        /// <summary>
        /// The number of lanes this buffer can read from / write to.
        /// </summary>
        public int LaneCount
        {
            get => m_BufferInfo->LaneCount;
        }

        /// <summary>
        /// The number of elements contained in each block.
        /// </summary>
        public int ElementsPerBlock
        {
            get => m_BufferInfo->BlockSize / ELEMENT_SIZE;
        }


        /// <summary>
        /// Convenience constructor that fits as many elements into a 16kb block size and allows you to specify the
        /// number of lanes.
        /// </summary>
        /// <param name="allocator">The <see cref="Allocator" /> to use when allocating memory.</param>
        /// <param name="laneCount">The number of lanes to allow reading from/writing to.</param>
        public UnsafeTypedStream(Allocator allocator, int laneCount) : this(math.max(CHUNK_SIZE / UnsafeUtility.SizeOf<T>(), 1), allocator, laneCount)
        {
        }

        /// <summary>
        /// Convenience constructor that fits as many elements into a 16kb block size.
        /// Sets the number of lanes to be the maximum amount of worker threads available plus the main thread.
        /// <see cref="JobsUtility.JobWorkerMaximumCount"/> + 1
        /// </summary>
        /// <param name="allocator">The <see cref="Allocator" /> to use when allocating memory.</param>
        public UnsafeTypedStream(Allocator allocator) : this(math.max(CHUNK_SIZE / UnsafeUtility.SizeOf<T>(), 1), allocator, ParallelAccessUtil.CollectionSizeForMaxThreads)
        {
        }

        /// <summary>
        /// More explicit constructor that allows for specifying how many elements to put into each block. Useful for
        /// smaller counts so that large blocks aren't allocated if not needed.
        /// </summary>
        /// <param name="elementsPerBlock">The number of elements to allocate space for in each block.</param>
        /// <param name="allocator">The <see cref="Allocator" /> to use when allocating memory.</param>
        /// <param name="laneCount">The number of lanes to allow reading from/writing to.</param>
        public UnsafeTypedStream(int elementsPerBlock, Allocator allocator, int laneCount)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.IsTrue(elementsPerBlock > 0);
#endif

            m_BufferInfo = (BufferInfo*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<BufferInfo>(),
                                                             UnsafeUtility.AlignOf<BufferInfo>(),
                                                             allocator);
            m_BufferInfo->Allocator = allocator;
            m_BufferInfo->BlockSize = elementsPerBlock * ELEMENT_SIZE;
            m_BufferInfo->LaneCount = laneCount;

            m_Lanes = (LaneInfo*)UnsafeUtility.Malloc(LaneInfo.SIZE * m_BufferInfo->LaneCount, LaneInfo.SIZE, allocator);

            //TODO: #16 - Look into creating an enumerator like NativeArray to allow for foreach - https://github.com/decline-cookies/anvil-unity-dots/pull/14/files#r842968034
            for (int i = 0; i < m_BufferInfo->LaneCount; ++i)
            {
                LaneInfo* lane = m_Lanes + i;
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
        [WriteAccessRequired]
        public void Dispose()
        {
            if (!IsCreated)
            {
                return;
            }

            Allocator allocator = m_BufferInfo->Allocator;

            Clear();

            UnsafeUtility.Free(m_Lanes, allocator);
            UnsafeUtility.Free(m_BufferInfo, allocator);

            m_Lanes = null;
            m_BufferInfo = null;
        }
        
        /// <summary>
        /// Clears all data in the collection
        /// </summary>
        [WriteAccessRequired]
        public void Clear()
        {
            if (!IsCreated)
            {
                return;
            }

            Allocator allocator = m_BufferInfo->Allocator;
            
            for (int i = 0; i < m_BufferInfo->LaneCount; ++i)
            {
                LaneInfo* lane = m_Lanes + i;
                
                BlockInfo* block = lane->FirstBlock;
                while (block != null)
                {
                    BlockInfo* currentBlock = block;
                    block = currentBlock->Next;

                    UnsafeUtility.Free(currentBlock->Data, allocator);
                    UnsafeUtility.Free(currentBlock, allocator);
                }
                
                lane->FirstBlock = null;
                lane->CurrentWriterBlock = null;
                lane->WriterHead = null;
                lane->WriterEndOfBlock = null;
                lane->Count = 0;
            }
        }

        /// <summary>
        /// Schedules the disposal of the collection.
        /// </summary>
        /// <param name="inputDeps">The <see cref="JobHandle" /> to wait on before disposing</param>
        /// <returns>A <see cref="JobHandle"/> for when disposal is complete</returns>
        public JobHandle Dispose(JobHandle inputDeps)
        {
            DisposeJob disposeJob = new DisposeJob(this);
            JobHandle disposeJobHandle = disposeJob.Schedule(inputDeps);
            m_Lanes = null;

            return disposeJobHandle;
        }
        
        /// <summary>
        /// Schedules the clearing of the collection.
        /// </summary>
        /// <param name="inputDeps">The <see cref="JobHandle"/> to wait on before clearing.</param>
        /// <returns>A <see cref="JobHandle"/> for when clearing is complete</returns>
        public JobHandle Clear(JobHandle inputDeps)
        {
            ClearJob clearJob = new ClearJob(this);
            return clearJob.Schedule(inputDeps);
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
                LaneInfo* lane = m_Lanes + i;
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
                LaneInfo* threadData = m_Lanes + i;
                if (threadData->Count > 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns a lightweight <see cref="Writer" /> instance
        /// </summary>
        /// <returns>A <see cref="Writer" /> instance.</returns>
        public Writer AsWriter()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.IsTrue(IsCreated);
#endif
            return new Writer(ref this);
        }

        /// <summary>
        /// Returns a lightweight <see cref="LaneWriter" /> instance
        /// </summary>
        /// <param name="laneIndex">The lane index to lock the lane writer into.
        /// Starts at 0 and goes up to but NOT including <see cref="LaneCount"/>
        /// See <see cref="ParallelAccessUtil.CollectionIndexForThread"/> if lanes are based on different threads.
        /// </param>
        /// <returns>A <see cref="LaneWriter" /> instance.</returns>
        public LaneWriter AsLaneWriter(int laneIndex)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.IsTrue(IsCreated);
            Assert.IsTrue(laneIndex < m_BufferInfo->LaneCount && laneIndex >= 0);
#endif

            LaneInfo* lane = m_Lanes + laneIndex;
            return new LaneWriter(lane, m_BufferInfo);
        }

        /// <summary>
        /// Returns a lightweight <see cref="Reader" /> instance
        /// </summary>
        /// <returns>A <see cref="Reader" /> instance.</returns>
        public Reader AsReader()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.IsTrue(IsCreated);
#endif
            return new Reader(ref this);
        }

        /// <summary>
        /// Returns a lightweight <see cref="LaneReader" /> instance
        /// </summary>
        /// <param name="laneIndex">The lane index to lock the lane reader into.
        /// Starts at 0 and goes up to but NOT including <see cref="LaneCount"/>
        /// See <see cref="ParallelAccessUtil.CollectionIndexForThread"/> if lanes are based on different threads.</param>
        /// <returns>A <see cref="LaneWriter" /> instance.</returns>
        public LaneReader AsLaneReader(int laneIndex)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.IsTrue(laneIndex < m_BufferInfo->LaneCount && laneIndex >= 0);
#endif

            LaneInfo* lane = m_Lanes + laneIndex;
            return new LaneReader(lane, m_BufferInfo);
        }

        /// <summary>
        /// Helper function to convert the collection to a <see cref="NativeArray{T}" />
        /// Note: This will only be accurate if all write jobs have completed before calling this.
        /// </summary>
        /// <param name="allocator">
        /// The <see cref="Allocator" /> to allocate the <see cref="NativeArray{T}" /> memory
        /// with.
        /// </param>
        /// <returns>A <see cref="NativeArray{T}" /> of the elements</returns>
        public NativeArray<T> ToNativeArray(Allocator allocator)
        {
            NativeArray<T> array = new NativeArray<T>(Count(), allocator, NativeArrayOptions.UninitializedMemory);
            Reader reader = AsReader();
            int arrayIndex = 0;
            for (int i = 0; i < reader.LaneCount; ++i)
            {
                LaneReader laneReader = reader.AsLaneReader(i);
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
            [NativeDisableUnsafePtrRestriction] private readonly LaneInfo* m_Lanes;

            /// <summary>
            /// The number of lanes that can be written to.
            /// </summary>
            public int LaneCount
            {
                get => m_BufferInfo->LaneCount;
            }

            internal Writer(ref UnsafeTypedStream<T> unsafeTypedStream)
            {
                m_Lanes = unsafeTypedStream.m_Lanes;
                m_BufferInfo = unsafeTypedStream.m_BufferInfo;
            }

            /// <summary>
            /// Returns a lightweight <see cref="LaneWriter" /> instance
            /// </summary>
            /// <param name="laneIndex">The lane index to lock the lane writer into.
            /// Starts at 0 and goes up to but NOT including <see cref="LaneCount"/>
            /// See <see cref="ParallelAccessUtil.CollectionIndexForThread"/> if lanes are based on different threads.
            /// </param>
            /// <returns>A <see cref="LaneWriter" /> instance.</returns>
            public LaneWriter AsLaneWriter(int laneIndex)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                Assert.IsTrue(laneIndex < m_BufferInfo->LaneCount && laneIndex >= 0);
#endif

                LaneInfo* lane = m_Lanes + laneIndex;
                return new LaneWriter(lane, m_BufferInfo);
            }
        }

        /// <summary>
        /// A lightweight writer instance that is locked to a specific lane
        /// </summary>
        [BurstCompatible]
        public readonly struct LaneWriter
        {
            [NativeDisableUnsafePtrRestriction] private readonly BufferInfo* m_BufferInfo;
            [NativeDisableUnsafePtrRestriction] private readonly LaneInfo* m_Lane;

            internal LaneWriter(LaneInfo* lane, BufferInfo* bufferInfo)
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
                m_Lane->WriterHead += ELEMENT_SIZE;
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
                blockPointer->Data = (byte*)UnsafeUtility.Malloc(m_BufferInfo->BlockSize, ELEMENT_ALIGNMENT, m_BufferInfo->Allocator);
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
        public readonly struct Reader
        {
            [NativeDisableUnsafePtrRestriction] private readonly BufferInfo* m_BufferInfo;
            [NativeDisableUnsafePtrRestriction] private readonly LaneInfo* m_Lanes;

            /// <summary>
            /// How many lanes the collection has available to read from
            /// </summary>
            public int LaneCount
            {
                get => m_BufferInfo->LaneCount;
            }

            internal Reader(ref UnsafeTypedStream<T> unsafeTypedStream)
            {
                m_Lanes = unsafeTypedStream.m_Lanes;
                m_BufferInfo = unsafeTypedStream.m_BufferInfo;
            }

            /// <summary>
            /// Returns a lightweight <see cref="LaneReader" /> instance
            /// </summary>
            /// <param name="laneIndex">The lane index to lock the lane reader into.
            /// Starts at 0 and goes up to but NOT including <see cref="LaneCount"/>
            /// See <see cref="ParallelAccessUtil.CollectionIndexForThread"/> if lanes are based on different threads.</param>
            /// <returns>A <see cref="LaneWriter" /> instance.</returns>
            public LaneReader AsLaneReader(int laneIndex)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                Assert.IsTrue(laneIndex < m_BufferInfo->LaneCount && laneIndex >= 0);
#endif

                LaneInfo* lane = m_Lanes + laneIndex;
                return new LaneReader(lane, m_BufferInfo);
            }

            /// <summary>
            /// Calculates the number of elements in the entire collection across all lanes.
            /// </summary>
            /// <returns>The number of elements</returns>
            public int Count()
            {
                int count = 0;
                for (int i = 0; i < m_BufferInfo->LaneCount; ++i)
                {
                    LaneInfo* lane = m_Lanes + i;
                    count += lane->Count;
                }

                return count;
            }
        }

        /// <summary>
        /// A lightweight reader instance locked to a specific lane.
        /// <see cref="LaneReader" />'s manage their own state so that multiple readers can read from a given lane at
        /// the same time.
        /// </summary>
        [BurstCompatible]
        public struct LaneReader
        {
            [NativeDisableUnsafePtrRestriction] private readonly BufferInfo* m_BufferInfo;
            [NativeDisableUnsafePtrRestriction] private readonly LaneInfo* m_Lane;

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

            internal LaneReader(LaneInfo* lane, BufferInfo* bufferInfo)
            {
                m_Lane = lane;
                m_BufferInfo = bufferInfo;

                m_CurrentReadBlock = m_Lane->FirstBlock;
                m_ReaderHead = m_CurrentReadBlock != null
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
                m_ReaderHead = m_CurrentReadBlock != null
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
                //TODO: #15 - Write test for this
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                Assert.IsTrue(Count > 0 && m_CurrentReadBlock != null && m_ReaderHead != null);
#endif

                byte* readPointer = m_ReaderHead;
                m_ReaderHead += ELEMENT_SIZE;

                CheckForEndOfBlock();

                return ref UnsafeUtility.AsRef<T>(readPointer);
            }

            /// <summary>
            /// Peeks at what the next elements in the reader is but does not advance the reader head.
            /// Will return the same value until <see cref="Read" /> has been called.
            /// </summary>
            /// <returns>The next element</returns>
            public ref T Peek()
            {
                //Otherwise nothing has been written
                //TODO: #15 - Write test for this
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                Assert.IsTrue(Count > 0 && m_CurrentReadBlock != null && m_ReaderHead != null);
#endif

                return ref UnsafeUtility.AsRef<T>(m_ReaderHead);
            }
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct DisposeJob : IJob
        {
            private UnsafeTypedStream<T> m_UnsafeTypedStream;

            public DisposeJob(UnsafeTypedStream<T> unsafeTypedStream)
            {
                m_UnsafeTypedStream = unsafeTypedStream;
            }

            public void Execute()
            {
                m_UnsafeTypedStream.Dispose();
            }
        }

        [BurstCompile]
        private struct ClearJob : IJob
        {
            private UnsafeTypedStream<T> m_UnsafeTypedStream;

            public ClearJob(UnsafeTypedStream<T> unsafeTypedStream)
            {
                m_UnsafeTypedStream = unsafeTypedStream;
            }

            public void Execute()
            {
                m_UnsafeTypedStream.Clear();
            }
        }
    }
}
