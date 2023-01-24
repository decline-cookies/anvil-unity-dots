using Anvil.Unity.DOTS.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Data
{
    //NOTE - Taken from: https://github.com/Amarcolina/NativeHeap (MIT LICENCE)
    //This is a lightly cleaned up version of NativeHeap from Amarcolina that removes
    //any safety aspects so that it can be used inside a different Native Collection.
    
    /// <summary>
    /// This is a basic implementation of the MinHeap/MaxHeap data structure.  It allows you
    /// to insert objects into the container with a O(log(n)) cost per item, and it allows you
    /// to extract the min/max from the container with a O(log(n)) cost per item.
    /// 
    /// This implementation provides the ability to remove items from the middle of the container
    /// as well.  This is a critical operation when implementing algorithms like astar.  When an
    /// item is added to the container, an index is returned which can be used to later remove
    /// the item no matter where it is in the heap, for the same cost of removing it if it was
    /// popped normally.
    /// 
    /// This container is parameterized with a comparer type that defines the ordering of the
    /// container.  The default form of the comparer can be used, or you can specify your own.
    /// The item that comes first in the ordering is the one that will be returned by the Pop
    /// operation.  This allows you to use the comparer to parameterize this collection into a 
    /// MinHeap, MaxHeap, or other type of ordered heap using your own custom type.
    /// 
    /// For convenience, this library contains the Min and Max comparer, which provide
    /// comparisons for all built in primitives.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    [StructLayout(LayoutKind.Sequential)]
    public struct UnsafeHeap<T, TComparer> : IDisposable
        where T : unmanaged
        where TComparer : unmanaged, IComparer<T>
    {
        /// <summary>
        /// Returns the number of elements that this collection can hold before the internal structures
        /// need to be reallocated.
        /// </summary>
        public int Capacity
        {
            get
            {
                unsafe
                {
                    return m_Data->Capacity;
                }
            }
            set
            {
                unsafe
                {
                    TableValue* newTable = (TableValue*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<TableValue>() * value, UnsafeUtility.AlignOf<TableValue>(), m_Allocator);
                    void* newHeap = UnsafeUtility.Malloc(UnsafeUtility.SizeOf<HeapNode>() * value, UnsafeUtility.AlignOf<HeapNode>(), m_Allocator);

                    int toCopy = m_Data->Capacity < value
                        ? m_Data->Capacity
                        : value;
                    UnsafeUtility.MemCpy(newTable, m_Data->Table, toCopy * UnsafeUtility.SizeOf<TableValue>());
                    UnsafeUtility.MemCpy(newHeap, m_Data->Heap, toCopy * UnsafeUtility.SizeOf<HeapNode>());

                    for (int i = 0; i < value - m_Data->Capacity; i++)
                    {
                        //For each new heap node, make sure that it has a new unique index
                        UnsafeUtility.WriteArrayElement(newHeap,
                                                        i + m_Data->Capacity,
                                                        new HeapNode
                                                        {
                                                            TableIndex = i + m_Data->Capacity
                                                        });
                    }

                    UnsafeUtility.Free(m_Data->Table, m_Allocator);
                    UnsafeUtility.Free(m_Data->Heap, m_Allocator);

                    m_Data->Table = newTable;
                    m_Data->Heap = newHeap;

                    m_Data->Capacity = value;
                }
            }
        }

        /// <summary>
        /// Returns the number of elements currently contained inside this collection.
        /// </summary>
        public int Count
        {
            get
            {
                unsafe
                {
                    return m_Data->Count;
                }
            }
        }

        /// <summary>
        /// Constructs a new UnsafeHeap using the given Allocator.  You must call Dispose on this collection
        /// when you are finished with it.
        /// </summary>
        /// <param name="allocator">
        /// You must specify an allocator to use for the creation of the internal data structures.
        /// </param>
        /// <param name="comparer">
        /// You can optionally specify the comparer used to order the elements in this collection.  The Pop operation will
        /// always return the smallest element according to the ordering specified by this comparer.
        /// </param>
        public UnsafeHeap(Allocator allocator, TComparer comparer = default) :
            this(ChunkUtil.MaxElementsPerChunk<T>(), comparer, allocator)
        {
        }

        /// <summary>
        /// Constructs a new UnsafeHeap using the given Allocator.  You must call Dispose on this collection
        /// when you are finished with it.
        /// </summary>
        /// <param name="allocator">
        /// You must specify an allocator to use for the creation of the internal data structures.
        /// </param>
        /// <param name="initialCapacity">
        /// You can optionally specify the default number of elements this collection can contain before the internal
        /// data structures need to be reallocated.
        /// </param>
        /// <param name="comparer">
        /// You can optionally specify the comparer used to order the elements in this collection.  The Pop operation will
        /// always return the smallest element according to the ordering specified by this comparer.
        /// </param>
        public UnsafeHeap(Allocator allocator, int initialCapacity, TComparer comparer = default)
            : this(initialCapacity, comparer, allocator)
        {
        }

        /// <summary>
        /// Disposes of this container and deallocates its memory immediately.
        /// 
        /// Any UnsafeHeapIndex structures obtained will be invalidated and cannot be used again.
        /// </summary>
        public void Dispose()
        {
            unsafe
            {
                m_Data->Count = 0;
                m_Data->Capacity = 0;

                UnsafeUtility.Free(m_Data->Heap, m_Allocator);
                UnsafeUtility.Free(m_Data->Table, m_Allocator);
                UnsafeUtility.Free(m_Data, m_Allocator);
            }
        }

        /// <summary>
        /// Removes all elements from this container.  Any UnsafeHeapIndex structures obtained will be
        /// invalidated and cannot be used again.
        /// </summary>
        public void Clear()
        {
            unsafe
            {
                m_Data->Count = 0;
            }
        }

        /// <summary>
        /// Returns the next element that would be obtained if Pop was called.  This is the first/smallest
        /// item according to the ordering specified by the comparer.
        /// 
        /// This method is an O(1) operation.
        /// 
        /// This method will throw an InvalidOperationException if the collection is empty.
        /// </summary>
        public T Peek()
        {
            if (!TryPeek(out T t))
            {
                throw new InvalidOperationException("Cannot Peek UnsafeHeap when the count is zero.");
            }

            return t;
        }

        /// <summary>
        /// Returns the next element that would be obtained if Pop was called.  This is the first/smallest
        /// item according to the ordering specified by the comparer.
        /// 
        /// This method is an O(1) operation.
        /// 
        /// This method will return true if an element could be obtained, or false if the container is empty.
        /// </summary>
        public bool TryPeek(out T t)
        {
            unsafe
            {
                if (m_Data->Count == 0)
                {
                    t = default;
                    return false;
                }

                UnsafeUtility.CopyPtrToStructure(m_Data->Heap, out t);
                return true;
            }
        }

        /// <summary>
        /// Removes the first/smallest element from the container and returns it.
        /// 
        /// This method is an O(log(n)) operation.
        /// 
        /// This method will throw an InvalidOperationException if the collection is empty.
        /// </summary>
        public T Pop()
        {
            if (!TryPop(out T t))
            {
                throw new InvalidOperationException("Cannot Pop UnsafeHeap when the count is zero.");
            }

            return t;
        }

        /// <summary>
        /// Removes the first/smallest element from the container and returns it.
        /// 
        /// This method is an O(log(n)) operation.
        /// 
        /// This method will return true if an element could be obtained, or false if the container is empty.
        /// </summary>
        public bool TryPop(out T t)
        {
            unsafe
            {
                if (m_Data->Count == 0)
                {
                    t = default;
                    return false;
                }

                HeapNode rootNode = UnsafeUtility.ReadArrayElement<HeapNode>(m_Data->Heap, 0);

                //Grab the last node off the end and remove it
                int lastNodeIndex = --m_Data->Count;
                HeapNode lastNode = UnsafeUtility.ReadArrayElement<HeapNode>(m_Data->Heap, lastNodeIndex);

                //Move the previous root to the end of the array to fill the space we just made
                UnsafeUtility.WriteArrayElement(m_Data->Heap, lastNodeIndex, rootNode);

                //Finally insert the previously last node at the root and bubble it down
                InsertAndBubbleDown(lastNode, 0);

                t = rootNode.Item;
                return true;
            }
        }

        /// <summary>
        /// Inserts the provided element into the container.  It may later be removed by a call to Pop,
        /// TryPop, or Remove.
        /// 
        /// This method returns a UnsafeHeapIndex.  This index can later be used to Remove the item from
        /// the collection.  Once the item is removed by any means, this UnsafeHeapIndex will become invalid.
        /// If an item is re-added to the collection after it has been removed, Insert will return a NEW
        /// index that is distinct from the previous index.  Each index can only be used exactly once to
        /// remove a single item.
        /// 
        /// This method is an O(log(n)) operation.
        /// </summary>
        public UnsafeHeapIndex Insert(in T t)
        {
            unsafe
            {
                if (m_Data->Count == m_Data->Capacity)
                {
                    Capacity *= 2;
                }

                HeapNode node = UnsafeUtility.ReadArrayElement<HeapNode>(m_Data->Heap, m_Data->Count);
                node.Item = t;

                int insertIndex = m_Data->Count++;

                InsertAndBubbleUp(node, insertIndex);

                return new UnsafeHeapIndex
                {
                    TableIndex = node.TableIndex,
                };
            }
        }

        /// <summary>
        /// Removes the element tied to this UnsafeHeapIndex from the container.  The UnsafeHeapIndex must be
        /// the result of a previous call to Insert on this container.  If the item has already been removed by
        /// any means, this method will throw an ArgumentException.
        /// 
        /// This method will invalidate the provided index.  If you re-insert the removed object, you must use
        /// the NEW index to remove it again.
        /// 
        /// This method is an O(log(n)) operation.
        /// </summary>
        public T Remove(UnsafeHeapIndex index)
        {
            unsafe
            {
                int indexToRemove = m_Data->Table[index.TableIndex].HeapIndex;

                HeapNode toRemove = UnsafeUtility.ReadArrayElement<HeapNode>(m_Data->Heap, indexToRemove);
                HeapNode lastNode = UnsafeUtility.ReadArrayElement<HeapNode>(m_Data->Heap, --m_Data->Count);

                //First we move the node to remove to the end of the heap
                UnsafeUtility.WriteArrayElement(m_Data->Heap, m_Data->Count, toRemove);

                if (indexToRemove != 0)
                {
                    int parentIndex = (indexToRemove - 1) / 2;
                    HeapNode parentNode = UnsafeUtility.ReadArrayElement<HeapNode>(m_Data->Heap, parentIndex);
                    if (m_Comparer.Compare(lastNode.Item, parentNode.Item) < 0)
                    {
                        InsertAndBubbleUp(lastNode, indexToRemove);
                        return toRemove.Item;
                    }
                }

                //If we couldn't bubble up, bubbling down instead
                InsertAndBubbleDown(lastNode, indexToRemove);

                return toRemove.Item;
            }
        }

        [NativeDisableUnsafePtrRestriction] private readonly unsafe HeapData* m_Data;
        private readonly Allocator m_Allocator;
        private TComparer m_Comparer;

        internal UnsafeHeap(int initialCapacity, TComparer comparer, Allocator allocator)
        {
            unsafe
            {
                m_Data = (HeapData*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<HeapData>(), UnsafeUtility.AlignOf<HeapData>(), allocator);
                m_Data->Heap = UnsafeUtility.Malloc(UnsafeUtility.SizeOf<HeapNode>() * initialCapacity, UnsafeUtility.AlignOf<HeapNode>(), allocator);
                m_Data->Table = (TableValue*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<TableValue>() * initialCapacity, UnsafeUtility.AlignOf<TableValue>(), allocator);

                m_Allocator = allocator;

                for (int i = 0; i < initialCapacity; i++)
                {
                    UnsafeUtility.WriteArrayElement(m_Data->Heap,
                                                    i,
                                                    new HeapNode
                                                    {
                                                        TableIndex = i
                                                    });
                }

                m_Data->Count = 0;
                m_Data->Capacity = initialCapacity;
                m_Comparer = comparer;
            }
        }

        private void InsertAndBubbleDown(HeapNode node, int insertIndex)
        {
            unsafe
            {
                while (true)
                {
                    int indexL = insertIndex * 2 + 1;
                    int indexR = insertIndex * 2 + 2;

                    //If the left index is off the end, we are finished
                    if (indexL >= m_Data->Count)
                    {
                        break;
                    }

                    if (indexR >= m_Data->Count
                     || m_Comparer.Compare(UnsafeUtility.ReadArrayElement<HeapNode>(m_Data->Heap, indexL).Item,
                                           UnsafeUtility.ReadArrayElement<HeapNode>(m_Data->Heap, indexR).Item)
                     <= 0)
                    {
                        //left is smaller (or the only child)
                        HeapNode leftNode = UnsafeUtility.ReadArrayElement<HeapNode>(m_Data->Heap, indexL);

                        if (m_Comparer.Compare(node.Item, leftNode.Item) <= 0)
                        {
                            //Last is smaller or equal to left, we are done
                            break;
                        }

                        UnsafeUtility.WriteArrayElement(m_Data->Heap, insertIndex, leftNode);
                        m_Data->Table[leftNode.TableIndex].HeapIndex = insertIndex;
                        insertIndex = indexL;
                    }
                    else
                    {
                        //right is smaller
                        HeapNode rightNode = UnsafeUtility.ReadArrayElement<HeapNode>(m_Data->Heap, indexR);

                        if (m_Comparer.Compare(node.Item, rightNode.Item) <= 0)
                        {
                            //Last is smaller than or equal to right, we are done
                            break;
                        }

                        UnsafeUtility.WriteArrayElement(m_Data->Heap, insertIndex, rightNode);
                        m_Data->Table[rightNode.TableIndex].HeapIndex = insertIndex;
                        insertIndex = indexR;
                    }
                }

                UnsafeUtility.WriteArrayElement(m_Data->Heap, insertIndex, node);
                m_Data->Table[node.TableIndex].HeapIndex = insertIndex;
            }
        }

        private void InsertAndBubbleUp(HeapNode node, int insertIndex)
        {
            unsafe
            {
                while (insertIndex != 0)
                {
                    int parentIndex = (insertIndex - 1) / 2;
                    HeapNode parentNode = UnsafeUtility.ReadArrayElement<HeapNode>(m_Data->Heap, parentIndex);

                    //If parent is actually less or equal to us, we are ok and can break out
                    if (m_Comparer.Compare(parentNode.Item, node.Item) <= 0)
                    {
                        break;
                    }

                    //We need to swap parent down
                    UnsafeUtility.WriteArrayElement(m_Data->Heap, insertIndex, parentNode);
                    //Update table to point to new heap index
                    m_Data->Table[parentNode.TableIndex].HeapIndex = insertIndex;

                    //Restart loop trying to insert at parent index
                    insertIndex = parentIndex;
                }

                UnsafeUtility.WriteArrayElement(m_Data->Heap, insertIndex, node);
                m_Data->Table[node.TableIndex].HeapIndex = insertIndex;
            }
        }
        
        /// <summary>
        /// Represents an index into the Heap
        /// </summary>
        public struct UnsafeHeapIndex
        {
            internal int TableIndex;
        }

        /// <summary>
        /// Comparer that returns the larger of two values
        /// </summary>
        public struct Max : IComparer<byte>,
                            IComparer<ushort>,
                            IComparer<short>,
                            IComparer<uint>,
                            IComparer<int>,
                            IComparer<ulong>,
                            IComparer<long>,
                            IComparer<float>,
                            IComparer<double>,
                            IComparer<decimal>
        {
            public int Compare(byte x, byte y)
            {
                return y.CompareTo(x);
            }

            public int Compare(ushort x, ushort y)
            {
                return y.CompareTo(x);
            }

            public int Compare(short x, short y)
            {
                return y.CompareTo(x);
            }

            public int Compare(uint x, uint y)
            {
                return y.CompareTo(x);
            }

            public int Compare(int x, int y)
            {
                return y.CompareTo(x);
            }

            public int Compare(ulong x, ulong y)
            {
                return y.CompareTo(x);
            }

            public int Compare(long x, long y)
            {
                return y.CompareTo(x);
            }

            public int Compare(float x, float y)
            {
                return y.CompareTo(x);
            }

            public int Compare(double x, double y)
            {
                return y.CompareTo(x);
            }

            public int Compare(decimal x, decimal y)
            {
                return y.CompareTo(x);
            }
        }

        /// <summary>
        /// Comparer that returns the smallest of two values
        /// </summary>
        public struct Min : IComparer<byte>,
                            IComparer<ushort>,
                            IComparer<short>,
                            IComparer<uint>,
                            IComparer<int>,
                            IComparer<ulong>,
                            IComparer<long>,
                            IComparer<float>,
                            IComparer<double>,
                            IComparer<decimal>
        {
            public int Compare(byte x, byte y)
            {
                return x.CompareTo(y);
            }

            public int Compare(ushort x, ushort y)
            {
                return x.CompareTo(y);
            }

            public int Compare(short x, short y)
            {
                return x.CompareTo(y);
            }

            public int Compare(uint x, uint y)
            {
                return x.CompareTo(y);
            }

            public int Compare(int x, int y)
            {
                return x.CompareTo(y);
            }

            public int Compare(ulong x, ulong y)
            {
                return x.CompareTo(y);
            }

            public int Compare(long x, long y)
            {
                return x.CompareTo(y);
            }

            public int Compare(float x, float y)
            {
                return x.CompareTo(y);
            }

            public int Compare(double x, double y)
            {
                return x.CompareTo(y);
            }

            public int Compare(decimal x, decimal y)
            {
                return x.CompareTo(y);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HeapNode
        {
            public T Item;
            public int TableIndex;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TableValue
        {
            public int HeapIndex;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HeapData
        {
            public int Count;
            public int Capacity;
            public unsafe void* Heap;
            public unsafe TableValue* Table;
        }
    }
}
