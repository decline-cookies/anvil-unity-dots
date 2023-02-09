using Anvil.Unity.DOTS.Entities;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
    //TODO: #141 - CopyAsync methods
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
    public unsafe struct UnsafeTypedStream<T> : INativeDisposable where T : unmanaged
    {
        /// <summary>
        /// Information about the <see cref="UnsafeTypedStream{T}" /> itself.
        /// </summary>
        [BurstCompatible]
        internal struct BufferInfo
        {
            public static readonly int SIZE = UnsafeUtility.SizeOf<BufferInfo>();
            public static readonly int ALIGNMENT = UnsafeUtility.AlignOf<BufferInfo>();

            public int BlockSize;
            public int LaneCount;
            public Allocator Allocator;
            public Allocator BlockAllocator;
            [NativeDisableUnsafePtrRestriction] public LaneInfo* LaneInfos;
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
            public int Count;
            public int BlockCount;
            public byte* WriterHead;
            public byte* WriterEndOfBlock;
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

        private static readonly int ELEMENT_SIZE = UnsafeUtility.SizeOf<T>();
        private static readonly int ELEMENT_ALIGNMENT = UnsafeUtility.AlignOf<T>();

        [NativeDisableUnsafePtrRestriction] private BufferInfo* m_BufferInfo;


        /// <summary>
        /// Reports whether memory for the container is allocated.
        /// This will be true when the container has had memory allocated AND is not yet disposed.
        /// </summary>
        /// <remarks>
        /// If you use the default constructor, memory will not have been allocated.
        /// </remarks>
        public bool IsCreated
        {
            get => m_BufferInfo != null;
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
            get;
        }

        /// <summary>
        /// Convenience constructor that fits as many elements into a 16kb block size as possible and allows you to
        /// specify the number of lanes.
        /// </summary>
        /// <param name="allocator">The <see cref="Allocator"/> to use when allocating memory for the collection and the content</param>
        /// <param name="laneCount">The number of lanes to allow reading from/writing to.</param>
        public UnsafeTypedStream(Allocator allocator, int laneCount)
            : this(ChunkUtil.MaxElementsPerChunk<T>(), allocator, allocator, laneCount) { }


        /// <summary>
        /// Convenience constructor that fits as many elements into a 16kb block size as possible and allows you to
        /// specify the number of lanes.
        /// </summary>
        /// <param name="allocator">The <see cref="Allocator"/> to use when allocating memory for the collection but not the content</param>
        /// <param name="blockAllocator">The <see cref="Allocator"/> to use when allocating memory for the content in the collection.</param>
        /// <param name="laneCount">The number of lanes to allow reading from/writing to.</param>
        public UnsafeTypedStream(Allocator allocator, Allocator blockAllocator, int laneCount)
            : this(ChunkUtil.MaxElementsPerChunk<T>(), allocator, blockAllocator, laneCount) { }

        /// <summary>
        /// Convenience constructor that fits as many elements into a 16kb block size as possible.
        /// Sets the number of lanes to be the maximum amount of worker threads available plus the main thread.
        /// <see cref="JobsUtility.JobWorkerMaximumCount"/> + 1
        /// </summary>
        /// <param name="allocator">The <see cref="Allocator"/> to use when allocating memory for the collection and the content</param>
        public UnsafeTypedStream(Allocator allocator)
            : this(
                ChunkUtil.MaxElementsPerChunk<T>(),
                allocator,
                allocator,
                ParallelAccessUtil.CollectionSizeForMaxThreads) { }

        /// <summary>
        /// Convenience constructor that fits as many elements into a 16kb block size as possible.
        /// Sets the number of lanes to be the maximum amount of worker threads available plus the main thread.
        /// <see cref="JobsUtility.JobWorkerMaximumCount"/> + 1
        /// </summary>
        /// <param name="allocator">The <see cref="Allocator"/> to use when allocating memory for the collection but not the content</param>
        /// <param name="blockAllocator">The <see cref="Allocator"/> to use when allocating memory for the content in the collection.</param>
        public UnsafeTypedStream(Allocator allocator, Allocator blockAllocator)
            : this(
                ChunkUtil.MaxElementsPerChunk<T>(),
                allocator,
                blockAllocator,
                ParallelAccessUtil.CollectionSizeForMaxThreads) { }

        /// <summary>
        /// More explicit constructor that allows for specifying how many elements to put into each block. Useful for
        /// smaller counts so that large blocks aren't allocated if not needed.
        /// </summary>
        /// <param name="elementsPerBlock">The number of elements to allocate space for in each block.</param>
        /// <param name="allocator">The <see cref="Allocator"/> to use when allocating memory for the collection and the content</param>
        /// <param name="laneCount">The number of lanes to allow reading from/writing to.</param>
        public UnsafeTypedStream(int elementsPerBlock, Allocator allocator, int laneCount)
            : this(elementsPerBlock, allocator, allocator, laneCount) { }

        /// <summary>
        /// More explicit constructor that allows for specifying how many elements to put into each block. Useful for
        /// smaller counts so that large blocks aren't allocated if not needed.
        /// </summary>
        /// <param name="elementsPerBlock">The number of elements to allocate space for in each block.</param>
        /// <param name="allocator">The <see cref="Allocator"/> to use when allocating memory for the collection but not the content</param>
        /// <param name="blockAllocator">The <see cref="Allocator"/> to use when allocating memory for the content in the collection.</param>
        /// <param name="laneCount">The number of lanes to allow reading from/writing to.</param>
        public UnsafeTypedStream(int elementsPerBlock, Allocator allocator, Allocator blockAllocator, int laneCount)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.IsTrue(elementsPerBlock > 0);
            //Ensures that block allocator can only be Temp, TempJob or Persistent and that the allocator is at the same or higher level.
            //Can't have a block allocator that is persistent and a temp allocator.
            Assert.IsTrue(blockAllocator <= allocator && blockAllocator > Allocator.None);
#endif
            ElementsPerBlock = elementsPerBlock;

            m_BufferInfo = (BufferInfo*)UnsafeUtility.Malloc(
                BufferInfo.SIZE,
                BufferInfo.ALIGNMENT,
                allocator);
            m_BufferInfo->Allocator = allocator;
            m_BufferInfo->BlockAllocator = blockAllocator;
            m_BufferInfo->BlockSize = elementsPerBlock * ELEMENT_SIZE;
            m_BufferInfo->LaneCount = laneCount;

            m_BufferInfo->LaneInfos = (LaneInfo*)UnsafeUtility.Malloc(LaneInfo.SIZE * m_BufferInfo->LaneCount, LaneInfo.SIZE, allocator);

            for (int i = 0; i < m_BufferInfo->LaneCount; ++i)
            {
                LaneInfo* lane = m_BufferInfo->LaneInfos + i;
                lane->FirstBlock = null;
                lane->CurrentWriterBlock = null;
                lane->WriterHead = null;
                lane->WriterEndOfBlock = null;
                lane->Count = 0;
                lane->BlockCount = 0;
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

            Clear();
            TrimExcess();

            Allocator allocator = m_BufferInfo->Allocator;
            UnsafeUtility.Free(m_BufferInfo->LaneInfos, allocator);
            UnsafeUtility.Free(m_BufferInfo, allocator);

            m_BufferInfo->LaneInfos = null;
            m_BufferInfo = null;
        }

        /// <summary>
        /// Frees memory for blocks of elements that are not being used.
        /// </summary>
        /// <remarks>
        /// Because this is a block based collection, trimming will not be a tight fit.
        /// Ex. 10 blocks allocated, 4 elements per block, 40 elements capacity total.
        /// A <see cref="Clear()"/> is called and then 5 elements are written and then <see cref="TrimExcess"/> is
        /// called.
        /// The first block of 4 elements will stay. The 5th element will keep the second block around. The remaining
        /// 8 blocks will be freed.
        /// In the end, 2 blocks will remain, 5 elements in the collection, 8 elements capacity.
        /// </remarks>
        [WriteAccessRequired]
        public void TrimExcess()
        {
            if (!IsCreated)
            {
                return;
            }

            Allocator blockAllocator = m_BufferInfo->BlockAllocator;

            for (int i = 0; i < m_BufferInfo->LaneCount; ++i)
            {
                LaneInfo* lane = m_BufferInfo->LaneInfos + i;

                //If our current writing block is null, we can just skip, there's nothing to free
                if (lane->CurrentWriterBlock == null)
                {
                    continue;
                }

                //If we have no elements at all in the lane (Clear was called), then we want to start with clearing
                //the current writer block. If there is anything written, (We're calling this to free up excess memory),
                //then the current writer block is still valid and we only want to free blocks after.
                BlockInfo* block = (lane->Count == 0) ? lane->CurrentWriterBlock : lane->CurrentWriterBlock->Next;
                while (block != null)
                {
                    BlockInfo* currentBlock = block;
                    block = currentBlock->Next;

                    UnsafeUtility.Free(currentBlock->Data, blockAllocator);
                    UnsafeUtility.Free(currentBlock, blockAllocator);
                    lane->BlockCount--;
                }
            }
        }

        /// <summary>
        /// Clears the collection so that it has 0 elements in it across any of the lanes.
        /// Note: The memory is NOT freed and will be reused as elements are written.
        /// See <see cref="TrimExcess"/> to free memory afterwards if desired.
        /// </summary>
        [WriteAccessRequired]
        public void Clear()
        {
            if (!IsCreated)
            {
                return;
            }

            for (int i = 0; i < m_BufferInfo->LaneCount; ++i)
            {
                LaneInfo* lane = m_BufferInfo->LaneInfos + i;
                //Reset current writer block to the first block and reset writer heads and counts
                lane->CurrentWriterBlock = lane->FirstBlock;
                if (lane->CurrentWriterBlock == null)
                {
                    lane->WriterHead = null;
                    lane->WriterEndOfBlock = null;
                }
                else
                {
                    lane->WriterHead = lane->CurrentWriterBlock->Data;
                    lane->WriterEndOfBlock = lane->WriterHead + m_BufferInfo->BlockSize;
                }
                lane->Count = 0;
            }
        }

        /// <summary>
        /// Gets an enumerator for iterating through all elements in the stream across all lanes
        /// </summary>
        /// <returns>The <see cref="Enumerator"/></returns>
        public Enumerator GetEnumerator() => new Enumerator(m_BufferInfo);

        /// <summary>
        /// Schedules the disposal of the collection.
        /// </summary>
        /// <param name="inputDeps">The <see cref="JobHandle" /> to wait on before disposing</param>
        /// <returns>A <see cref="JobHandle"/> for when disposal is complete</returns>
        public JobHandle Dispose(JobHandle inputDeps)
        {
            DisposeJob disposeJob = new DisposeJob(this);
            JobHandle disposeJobHandle = disposeJob.Schedule(inputDeps);

            m_BufferInfo = null;

            return disposeJobHandle;
        }

        /// <summary>
        /// Schedules the clearing of the collection.
        /// </summary>
        /// <param name="inputDeps">The <see cref="JobHandle"/> to wait on before clearing.</param>
        /// <param name="shouldTrimExcess">Whether the collection should trim excess memory or not after clearing. <see cref="TrimExcess"/></param>
        /// <returns>A <see cref="JobHandle"/> for when clearing is complete</returns>
        public JobHandle Clear(JobHandle inputDeps, bool shouldTrimExcess)
        {
            ClearJob clearJob = new ClearJob(this, shouldTrimExcess);
            return clearJob.Schedule(inputDeps);
        }

        /// <summary>
        /// Calculates the number of elements in the entire collection across all lanes.
        /// Note: This will only be accurate if all write jobs have completed before calling this.
        /// </summary>
        /// <returns>The number of elements</returns>
        [WriteAccessRequired]
        public int Count()
        {
            int count = 0;
            for (int i = 0; i < m_BufferInfo->LaneCount; ++i)
            {
                LaneInfo* lane = m_BufferInfo->LaneInfos + i;
                count += lane->Count;
            }

            return count;
        }

        /// <summary>
        /// Calculates the total number of elements that can be stored in the entire collection across all lanes.
        /// Note: This will only be accurate if all write jobs have completed before calling this.
        /// </summary>
        /// <returns>The total number of elements possible in the currently allocated memory</returns>
        [WriteAccessRequired]
        public int Capacity()
        {
            int blocksAllocated = 0;
            for (int i = 0; i < m_BufferInfo->LaneCount; ++i)
            {
                LaneInfo* lane = m_BufferInfo->LaneInfos + i;
                blocksAllocated += lane->BlockCount;
            }

            return blocksAllocated * ElementsPerBlock;
        }

        /// <summary>
        /// Copies everything from this <see cref="UnsafeTypedStream{T}"/> into a <see cref="NativeArray{T}"/>
        /// through optimized memory copying of the blocks.
        /// </summary>
        /// <param name="array">The array to populate</param>
        public void CopyTo(ref NativeArray<T> array)
        {
            Reader reader = AsReader();
            reader.CopyTo(ref array);
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
                LaneInfo* threadData = m_BufferInfo->LaneInfos + i;
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
            //TODO: #16 - Convert to exceptions
            Assert.IsTrue(IsCreated);
            Assert.IsTrue(laneIndex < m_BufferInfo->LaneCount && laneIndex >= 0);
#endif

            return new LaneWriter(m_BufferInfo, laneIndex);
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
            return new Reader(m_BufferInfo);
        }

        /// <summary>
        /// Returns a lightweight <see cref="LaneReader" /> instance
        /// </summary>
        /// <param name="laneIndex">
        /// The lane index to lock the lane reader into.
        /// Starts at 0 and goes up to but NOT including <see cref="LaneCount"/>
        /// See <see cref="ParallelAccessUtil.CollectionIndexForThread"/> if lanes are based on different threads.
        /// </param>
        /// <returns>A <see cref="LaneWriter" /> instance.</returns>
        public LaneReader AsLaneReader(int laneIndex)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.IsTrue(IsCreated);
            Assert.IsTrue(laneIndex < m_BufferInfo->LaneCount && laneIndex >= 0);
#endif

            return new LaneReader(m_BufferInfo, laneIndex);
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
            return AsReader().ToNativeArray(allocator);
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
            internal static Writer ReinterpretFromPointer(void* ptr)
            {
                Debug_EnsurePointerNotNull(ptr);
                Writer writer = new Writer(ptr);
                return writer;
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private static void Debug_EnsurePointerNotNull(void* ptr)
            {
                if (ptr == null)
                {
                    throw new InvalidOperationException($"Trying to reinterpret the writer from a pointer but the pointer is null!");
                }
            }

            [NativeDisableUnsafePtrRestriction] private readonly BufferInfo* m_BufferInfo;

            /// <summary>
            /// The number of lanes that can be written to.
            /// </summary>
            public int LaneCount
            {
                get => m_BufferInfo->LaneCount;
            }

            /// <summary>
            /// Reports if this <see cref="Writer"/> has been created and is valid to use.
            /// </summary>
            public bool IsCreated
            {
                get => m_BufferInfo != null;
            }

            internal Writer(ref UnsafeTypedStream<T> unsafeTypedStream)
            {
                m_BufferInfo = unsafeTypedStream.m_BufferInfo;
            }

            private Writer(void* bufferInfoPtr)
            {
                m_BufferInfo = (BufferInfo*)bufferInfoPtr;
            }

            internal void* GetBufferPointer()
            {
                return m_BufferInfo;
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
                //TODO: #16 - Convert to exceptions
                Assert.IsTrue(IsCreated);
                Assert.IsTrue(laneIndex < m_BufferInfo->LaneCount && laneIndex >= 0);
#endif
                return new LaneWriter(m_BufferInfo, laneIndex);
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

            /// <summary>
            /// Reports if this <see cref="LaneWriter"/> has been created and is valid to use.
            /// </summary>
            public bool IsCreated
            {
                get => m_BufferInfo != null;
            }

            internal LaneWriter(BufferInfo* bufferInfo, int laneIndex)
            {
                m_BufferInfo = bufferInfo;
                m_Lane = m_BufferInfo->LaneInfos + laneIndex;
            }

            /// <inheritdoc cref="Write(T)"/>
            public void Write(ref T value)
            {
                //See if we need to allocate a new block
                CheckForNewBlock();

                //Finally go ahead and write the struct to our block
                UnsafeUtility.CopyStructureToPtr(ref value, m_Lane->WriterHead);
                m_Lane->WriterHead += ELEMENT_SIZE;
                m_Lane->Count++;
            }

            /// <summary>
            /// Writes the element to the next spot in the lane's current block.
            /// </summary>
            /// <param name="value">The element to write</param>
            public void Write(T value)
            {
                Write(ref value);
            }

            private void CheckForNewBlock()
            {
                if (m_Lane->WriterHead < m_Lane->WriterEndOfBlock)
                {
                    return;
                }

                //If we've never allocated a block for this lane or we're at the end of blocks to allocate and we need a new one...
                if (m_Lane->CurrentWriterBlock == null || m_Lane->CurrentWriterBlock->Next == null)
                {
                    //Create the new block
                    BlockInfo* blockPointer = (BlockInfo*)UnsafeUtility.Malloc(
                        BlockInfo.SIZE,
                        BlockInfo.ALIGNMENT,
                        m_BufferInfo->BlockAllocator);
                    blockPointer->Data = (byte*)UnsafeUtility.Malloc(
                        m_BufferInfo->BlockSize,
                        ELEMENT_ALIGNMENT,
                        m_BufferInfo->BlockAllocator);
                    blockPointer->Next = null;
                    m_Lane->BlockCount++;

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
                //We already had the blocks allocated, so we can just jump to the next block
                else
                {
                    m_Lane->CurrentWriterBlock = m_Lane->CurrentWriterBlock->Next;
                    m_Lane->WriterHead = m_Lane->CurrentWriterBlock->Data;
                    m_Lane->WriterEndOfBlock = m_Lane->WriterHead + m_BufferInfo->BlockSize;
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

            /// <summary>
            /// Reports if this <see cref="Reader"/> has been created and is valid to use.
            /// </summary>
            public bool IsCreated
            {
                get => m_BufferInfo != null;
            }

            /// <summary>
            /// How many lanes the collection has available to read from
            /// </summary>
            public int LaneCount
            {
                get => m_BufferInfo->LaneCount;
            }

            internal Reader(BufferInfo* bufferInfo)
            {
                m_BufferInfo = bufferInfo;
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
                Assert.IsTrue(IsCreated);
                Assert.IsTrue(laneIndex < m_BufferInfo->LaneCount && laneIndex >= 0);
#endif
                return new LaneReader(m_BufferInfo, laneIndex);
            }

            /// <summary>
            /// Gets an enumerator for iterating through all elements in the stream across all lanes
            /// </summary>
            /// <returns>The <see cref="Enumerator"/></returns>
            public Enumerator GetEnumerator() => new Enumerator(m_BufferInfo);

            /// <summary>
            /// Calculates the number of elements in the entire collection across all lanes.
            /// </summary>
            /// <returns>The number of elements</returns>
            public int Count()
            {
                int count = 0;
                for (int i = 0; i < m_BufferInfo->LaneCount; ++i)
                {
                    LaneInfo* lane = m_BufferInfo->LaneInfos + i;
                    count += lane->Count;
                }

                return count;
            }

            /// <inheritdoc cref="UnsafeTypedStream{T}.CopyTo"/>
            public void CopyTo(ref NativeArray<T> array)
            {
                byte* arrayPtr = (byte*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(array);
                int elementsPerLaneBlock = m_BufferInfo->BlockSize / ELEMENT_SIZE;
                int arrayIndex = 0;
                for (int laneIndex = 0; laneIndex < LaneCount; ++laneIndex)
                {
                    LaneReader laneReader = AsLaneReader(laneIndex);
                    int laneElementsRemaining = laneReader.Count;

                    while (laneElementsRemaining > 0)
                    {
                        int numElementsToRead = math.min(elementsPerLaneBlock, laneElementsRemaining);
                        laneReader.ReadBlock(arrayPtr, arrayIndex, numElementsToRead);
                        arrayIndex += numElementsToRead;
                        laneElementsRemaining -= numElementsToRead;
                    }
                }
            }

            /// <inheritdoc cref="UnsafeTypedStream{T}.ToNativeArray"/>
            public NativeArray<T> ToNativeArray(Allocator allocator)
            {
                NativeArray<T> array = new NativeArray<T>(Count(), allocator, NativeArrayOptions.UninitializedMemory);
                CopyTo(ref array);

                return array;
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
            /// Reports if this <see cref="LaneReader"/> has been created and is valid to use.
            /// </summary>
            public bool IsCreated
            {
                get => m_BufferInfo != null;
            }

            /// <summary>
            /// How many elements are available to read on this lane
            /// </summary>
            public int Count
            {
                get => m_Lane->Count;
            }

            internal LaneReader(BufferInfo* bufferInfo, int laneIndex)
            {
                m_BufferInfo = bufferInfo;
                m_Lane = m_BufferInfo->LaneInfos + laneIndex;

                m_CurrentReadBlock = m_Lane->FirstBlock;
                m_ReaderHead = m_CurrentReadBlock != null ? m_CurrentReadBlock->Data : null;
                m_EndOfCurrentBlock = m_ReaderHead + m_BufferInfo->BlockSize;
            }

            private void CheckForEndOfBlock()
            {
                if (m_ReaderHead < m_EndOfCurrentBlock)
                {
                    return;
                }

                m_CurrentReadBlock = m_CurrentReadBlock->Next;
                m_ReaderHead = m_CurrentReadBlock != null ? m_CurrentReadBlock->Data : null;
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

            internal void ReadBlock(byte* dstStart, int dstStartIndex, int elementsToRead)
            {
                long bytesToRead = elementsToRead * ELEMENT_SIZE;
                long bytesToOffset = dstStartIndex * ELEMENT_SIZE;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                Assert.IsTrue(Count > 0 && m_CurrentReadBlock != null && m_ReaderHead != null);
                Assert.IsTrue(m_ReaderHead + bytesToRead <= m_EndOfCurrentBlock);
#endif

                byte* dstPtr = dstStart + bytesToOffset;
                UnsafeUtility.MemCpy(dstPtr, m_ReaderHead, bytesToRead);

                m_ReaderHead += bytesToRead;
                CheckForEndOfBlock();
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
        // ENUMERATOR
        //*************************************************************************************************************

        [BurstCompile]
        public struct Enumerator : IEnumerator<T>
        {
            [NativeDisableUnsafePtrRestriction] private readonly BufferInfo* m_BufferInfo;
            private int m_LaneIndex;
            private LaneReader m_LaneReader;
            private int m_ArrayIndex;
            private T m_Current;

            public T Current
            {
                get => m_Current;
            }

            internal Enumerator(BufferInfo* bufferInfo)
            {
                m_BufferInfo = bufferInfo;
                m_LaneIndex = 0;
                m_ArrayIndex = -1;
                m_LaneReader = new LaneReader(m_BufferInfo, m_LaneIndex);
                m_Current = default;
            }

            public bool MoveNext()
            {
                //Progress the array index
                m_ArrayIndex++;
                //So long as our lane reader has an entry we're good
                if (m_ArrayIndex < m_LaneReader.Count)
                {
                    m_Current = m_LaneReader.Read();
                    return true;
                }

                //Otherwise we need to find the next lane that has something
                m_LaneIndex++;
                for (; m_LaneIndex < m_BufferInfo->LaneCount; ++m_LaneIndex)
                {
                    //Update lane reader and array index
                    m_LaneReader = new LaneReader(m_BufferInfo, m_LaneIndex);
                    m_ArrayIndex = 0;
                    //If the lane doesn't have anything in it, skip
                    if (m_ArrayIndex >= m_LaneReader.Count)
                    {
                        continue;
                    }

                    m_Current = m_LaneReader.Read();
                    return true;
                }

                //We've gone through all lanes and nothing is left, we're done
                m_Current = default;
                return false;
            }

            public void Reset()
            {
                m_LaneIndex = 0;
                m_ArrayIndex = -1;
                m_LaneReader = new LaneReader(m_BufferInfo, m_LaneIndex);
                m_Current = default;
            }

            //We don't want anyone calling this in Burst since it will box the current value.
            //But maybe it's useful for debugging purposes.
            [BurstDiscard] object IEnumerator.Current
            {
                get => m_Current;
            }

            public void Dispose()
            {
                //Does nothing
            }
        }

        //*************************************************************************************************************
        // JOBS
        //*************************************************************************************************************

        [BurstCompile]
        private struct DisposeJob : IJob
        {
            [WriteOnly] private UnsafeTypedStream<T> m_UnsafeTypedStream;

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
            [WriteOnly] private UnsafeTypedStream<T> m_UnsafeTypedStream;
            [ReadOnly] private readonly bool m_ShouldTrimExcess;

            public ClearJob(UnsafeTypedStream<T> unsafeTypedStream, bool shouldTrimExcess)
            {
                m_UnsafeTypedStream = unsafeTypedStream;
                m_ShouldTrimExcess = shouldTrimExcess;
            }

            public void Execute()
            {
                m_UnsafeTypedStream.Clear();
                if (m_ShouldTrimExcess)
                {
                    m_UnsafeTypedStream.TrimExcess();
                }
            }
        }
    }
}