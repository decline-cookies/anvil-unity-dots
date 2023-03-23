using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine.Internal;

namespace Anvil.Unity.DOTS.Data
{
    [NativeContainerSupportsDeferredConvertListToArray]
    [DebuggerDisplay("Length = {Length}")]
    [NativeContainerSupportsDeallocateOnJobCompletion]
    [NativeContainerSupportsMinMaxWriteRestriction]
    public struct UnsafeArray<T> : IDisposable, IEnumerable<T>, IEnumerable, IEquatable<UnsafeArray<T>> where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction] internal unsafe void* m_Buffer;
        internal int m_Length;
        internal int m_MinIndex;
        internal int m_MaxIndex;
        internal Allocator m_AllocatorLabel;


        public unsafe UnsafeArray(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            Allocate(length, allocator, out this);
            if ((options & NativeArrayOptions.ClearMemory) != NativeArrayOptions.ClearMemory)
            {
                return;
            }

            UnsafeUtility.MemClear(m_Buffer, Length * (long)UnsafeUtility.SizeOf<T>());
        }

        public UnsafeArray(T[] array, Allocator allocator)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            Allocate(array.Length, allocator, out this);
            Copy(array, this);
        }

        public UnsafeArray(UnsafeArray<T> array, Allocator allocator)
        {
            Allocate(array.Length, allocator, out this);
            Copy(array, 0, this, 0, array.Length);
        }

        public UnsafeArray(NativeArray<T>.ReadOnly array, Allocator allocator)
        {
            Allocate(array.Length, allocator, out this);
            Copy(array, 0, this, 0, array.Length);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckAllocateArguments(int length, Allocator allocator, long totalSize)
        {
            if (allocator <= Allocator.None)
            {
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be >= 0");
            }

            IsUnmanagedAndThrow();
        }

        private static unsafe void Allocate(int length, Allocator allocator, out UnsafeArray<T> array)
        {
            long num = UnsafeUtility.SizeOf<T>() * (long)length;
            CheckAllocateArguments(length, allocator, num);
            array = new UnsafeArray<T>();
            array.m_Buffer = UnsafeUtility.Malloc(num, UnsafeUtility.AlignOf<T>(), allocator);
            array.m_Length = length;
            array.m_AllocatorLabel = allocator;
            array.m_MinIndex = 0;
            array.m_MaxIndex = length - 1;
        }

        public int Length
        {
            get => m_Length;
        }

        [BurstDiscard]
        internal static void IsUnmanagedAndThrow()
        {
            if (!UnsafeUtility.IsValidNativeContainerElementType<T>())
            {
                throw new InvalidOperationException($"{(object)typeof(T)} used in UnsafeArray<{(object)typeof(T)}> must be unmanaged (contain no managed types) and cannot itself be a native container type.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckElementReadAccess(int index)
        {
            if (index < m_MinIndex || index > m_MaxIndex)
            {
                FailOutOfRangeError(index);
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckElementWriteAccess(int index)
        {
            if (index < m_MinIndex || index > m_MaxIndex)
            {
                FailOutOfRangeError(index);
            }
        }

        public unsafe T this[int index]
        {
            get
            {
                CheckElementReadAccess(index);
                return UnsafeUtility.ReadArrayElement<T>(m_Buffer, index);
            }
            [WriteAccessRequired] set
            {
                CheckElementWriteAccess(index);
                UnsafeUtility.WriteArrayElement(m_Buffer, index, value);
            }
        }

        public unsafe bool IsCreated
        {
            get => (IntPtr)m_Buffer != IntPtr.Zero;
        }

        [WriteAccessRequired]
        public unsafe void Dispose()
        {
            if ((IntPtr)m_Buffer == IntPtr.Zero)
            {
                throw new ObjectDisposedException("The UnsafeArray is already disposed.");
            }

            if (m_AllocatorLabel == Allocator.Invalid)
            {
                throw new InvalidOperationException("The UnsafeArray can not be Disposed because it was not allocated with a valid allocator.");
            }

            if (m_AllocatorLabel > Allocator.None)
            {
                UnsafeUtility.Free(m_Buffer, m_AllocatorLabel);
                m_AllocatorLabel = Allocator.Invalid;
            }

            m_Buffer = null;
        }

        public unsafe JobHandle Dispose(JobHandle inputDeps)
        {
            if (m_AllocatorLabel == Allocator.Invalid)
            {
                throw new InvalidOperationException("The UnsafeArray can not be Disposed because it was not allocated with a valid allocator.");
            }

            if ((IntPtr)m_Buffer == IntPtr.Zero)
            {
                throw new InvalidOperationException("The UnsafeArray is already disposed.");
            }

            if (m_AllocatorLabel > Allocator.None)
            {
                JobHandle jobHandle = new UnsafeArrayDisposeJob
                {
                    Data = new UnsafeArrayDispose
                    {
                        m_Buffer = m_Buffer, m_AllocatorLabel = m_AllocatorLabel
                    }
                }.Schedule(inputDeps);
                m_Buffer = null;
                m_AllocatorLabel = Allocator.Invalid;

                return jobHandle;
            }

            m_Buffer = null;

            return inputDeps;
        }

        [WriteAccessRequired]
        public void CopyFrom(T[] array) => Copy(array, this);

        [WriteAccessRequired]
        public void CopyFrom(UnsafeArray<T> array) => Copy(array, this);

        public void CopyTo(T[] array) => Copy(this, array);

        public void CopyTo(UnsafeArray<T> array) => Copy(this, array);

        public T[] ToArray()
        {
            T[] dst = new T[Length];
            Copy(this, dst, Length);
            return dst;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void FailOutOfRangeError(int index)
        {
            if (index < Length && (m_MinIndex != 0 || m_MaxIndex != Length - 1))
            {
                throw new IndexOutOfRangeException($"Index {(object)index} is out of restricted IJobParallelFor range [{(object)m_MinIndex}...{(object)m_MaxIndex}] in ReadWriteBuffer.\n" + "ReadWriteBuffers are restricted to only read & write the element at the job index. You can use double buffering strategies to avoid race conditions due to reading & writing in parallel to the same elements from a job.");
            }

            throw new IndexOutOfRangeException($"Index {(object)index} is out of range of '{(object)Length}' Length.");
        }

        public Enumerator GetEnumerator() => new Enumerator(ref this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(ref this);

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public unsafe bool Equals(UnsafeArray<T> other) => m_Buffer == other.m_Buffer && m_Length == other.m_Length;

        public override bool Equals(object obj) => obj is UnsafeArray<T> other && Equals(other);

        public override unsafe int GetHashCode() => (int)m_Buffer * 397 ^ m_Length;

        public static bool operator ==(UnsafeArray<T> left, UnsafeArray<T> right) => left.Equals(right);

        public static bool operator !=(UnsafeArray<T> left, UnsafeArray<T> right) => !left.Equals(right);

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckCopyLengths(int srcLength, int dstLength)
        {
            if (srcLength != dstLength)
            {
                throw new ArgumentException("source and destination length must be the same");
            }
        }

        public static void Copy(UnsafeArray<T> src, UnsafeArray<T> dst)
        {
            CheckCopyLengths(src.Length, dst.Length);
            Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(NativeArray<T>.ReadOnly src, UnsafeArray<T> dst)
        {
            CheckCopyLengths(src.Length, dst.Length);
            Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(ReadOnly src, UnsafeArray<T> dst)
        {
            CheckCopyLengths(src.Length, dst.Length);
            Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(T[] src, UnsafeArray<T> dst)
        {
            CheckCopyLengths(src.Length, dst.Length);
            Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(UnsafeArray<T> src, T[] dst)
        {
            CheckCopyLengths(src.Length, dst.Length);
            Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(ReadOnly src, T[] dst)
        {
            CheckCopyLengths(src.Length, dst.Length);
            Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(UnsafeArray<T> src, UnsafeArray<T> dst, int length) => Copy(src, 0, dst, 0, length);

        public static void Copy(NativeArray<T>.ReadOnly src, UnsafeArray<T> dst, int length) => Copy(src, 0, dst, 0, length);

        public static void Copy(ReadOnly src, UnsafeArray<T> dst, int length) => Copy(src, 0, dst, 0, length);

        public static void Copy(T[] src, UnsafeArray<T> dst, int length) => Copy(src, 0, dst, 0, length);

        public static void Copy(UnsafeArray<T> src, T[] dst, int length) => Copy(src, 0, dst, 0, length);

        public static void Copy(ReadOnly src, T[] dst, int length) => Copy(src, 0, dst, 0, length);

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckCopyArguments(int srcLength, int srcIndex, int dstLength, int dstIndex, int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "length must be equal or greater than zero.");
            }

            if (srcIndex < 0 || srcIndex > srcLength || srcIndex == srcLength && srcLength > 0)
            {
                throw new ArgumentOutOfRangeException(nameof(srcIndex), "srcIndex is outside the range of valid indexes for the source UnsafeArray.");
            }

            if (dstIndex < 0 || dstIndex > dstLength || dstIndex == dstLength && dstLength > 0)
            {
                throw new ArgumentOutOfRangeException(nameof(dstIndex), "dstIndex is outside the range of valid indexes for the destination UnsafeArray.");
            }

            if (srcIndex + length > srcLength)
            {
                throw new ArgumentException("length is greater than the number of elements from srcIndex to the end of the source UnsafeArray.", nameof(length));
            }

            if (srcIndex + length < 0)
            {
                throw new ArgumentException("srcIndex + length causes an integer overflow");
            }

            if (dstIndex + length > dstLength)
            {
                throw new ArgumentException("length is greater than the number of elements from dstIndex to the end of the destination UnsafeArray.", nameof(length));
            }

            if (dstIndex + length < 0)
            {
                throw new ArgumentException("dstIndex + length causes an integer overflow");
            }
        }

        public static unsafe void Copy(UnsafeArray<T> src, int srcIndex, UnsafeArray<T> dst, int dstIndex, int length)
        {
            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            UnsafeUtility.MemCpy((void*)((IntPtr)dst.m_Buffer + dstIndex * UnsafeUtility.SizeOf<T>()), (void*)((IntPtr)src.m_Buffer + srcIndex * UnsafeUtility.SizeOf<T>()), length * UnsafeUtility.SizeOf<T>());
        }

        public static unsafe void Copy(NativeArray<T>.ReadOnly src, int srcIndex, UnsafeArray<T> dst, int dstIndex, int length)
        {
            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            UnsafeUtility.MemCpy((void*)((IntPtr)dst.m_Buffer + dstIndex * UnsafeUtility.SizeOf<T>()), (void*)((IntPtr)src.GetUnsafeReadOnlyPtr() + srcIndex * UnsafeUtility.SizeOf<T>()), length * UnsafeUtility.SizeOf<T>());
        }

        public static unsafe void Copy(ReadOnly src, int srcIndex, UnsafeArray<T> dst, int dstIndex, int length)
        {
            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            UnsafeUtility.MemCpy((void*)((IntPtr)dst.m_Buffer + dstIndex * UnsafeUtility.SizeOf<T>()), (void*)((IntPtr)src.m_Buffer + srcIndex * UnsafeUtility.SizeOf<T>()), length * UnsafeUtility.SizeOf<T>());
        }

        public static unsafe void Copy(T[] src, int srcIndex, UnsafeArray<T> dst, int dstIndex, int length)
        {
            if (src == null)
            {
                throw new ArgumentNullException(nameof(src));
            }

            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            GCHandle gcHandle = GCHandle.Alloc(src, GCHandleType.Pinned);
            IntPtr num = gcHandle.AddrOfPinnedObject();
            UnsafeUtility.MemCpy((void*)((IntPtr)dst.m_Buffer + dstIndex * UnsafeUtility.SizeOf<T>()), (void*)((IntPtr)(void*)num + srcIndex * UnsafeUtility.SizeOf<T>()), length * UnsafeUtility.SizeOf<T>());
            gcHandle.Free();
        }

        public static unsafe void Copy(UnsafeArray<T> src, int srcIndex, T[] dst, int dstIndex, int length)
        {
            if (dst == null)
            {
                throw new ArgumentNullException(nameof(dst));
            }

            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            GCHandle gcHandle = GCHandle.Alloc(dst, GCHandleType.Pinned);
            UnsafeUtility.MemCpy((void*)((IntPtr)(void*)gcHandle.AddrOfPinnedObject() + dstIndex * UnsafeUtility.SizeOf<T>()), (void*)((IntPtr)src.m_Buffer + srcIndex * UnsafeUtility.SizeOf<T>()), length * UnsafeUtility.SizeOf<T>());
            gcHandle.Free();
        }

        public static unsafe void Copy(ReadOnly src, int srcIndex, T[] dst, int dstIndex, int length)
        {
            if (dst == null)
            {
                throw new ArgumentNullException(nameof(dst));
            }

            CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            GCHandle gcHandle = GCHandle.Alloc(dst, GCHandleType.Pinned);
            UnsafeUtility.MemCpy((void*)((IntPtr)(void*)gcHandle.AddrOfPinnedObject() + dstIndex * UnsafeUtility.SizeOf<T>()), (void*)((IntPtr)src.m_Buffer + srcIndex * UnsafeUtility.SizeOf<T>()), length * UnsafeUtility.SizeOf<T>());
            gcHandle.Free();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckReinterpretLoadRange<U>(int sourceIndex) where U : unmanaged
        {
            long num1 = UnsafeUtility.SizeOf<T>();
            long num2 = UnsafeUtility.SizeOf<U>();
            long num3 = Length * num1;
            long num4 = sourceIndex * num1;
            long num5 = num4 + num2;

            if (num4 < 0L || num5 > num3)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), "loaded byte range must fall inside container bounds");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckReinterpretStoreRange<U>(int destIndex) where U : unmanaged
        {
            long num1 = UnsafeUtility.SizeOf<T>();
            long num2 = UnsafeUtility.SizeOf<U>();
            long num3 = Length * num1;
            long num4 = destIndex * num1;
            long num5 = num4 + num2;

            if (num4 < 0L || num5 > num3)
            {
                throw new ArgumentOutOfRangeException(nameof(destIndex), "stored byte range must fall inside container bounds");
            }
        }

        public unsafe U ReinterpretLoad<U>(int sourceIndex) where U : unmanaged
        {
            CheckReinterpretLoadRange<U>(sourceIndex);
            long offset = UnsafeUtility.SizeOf<T>() * sourceIndex;
            return UnsafeUtility.ReadArrayElement<U>((byte*)m_Buffer + sourceIndex, 0);
        }

        public unsafe void ReinterpretStore<U>(int destIndex, U data) where U : unmanaged
        {
            CheckReinterpretStoreRange<U>(destIndex);
            long offset = UnsafeUtility.SizeOf<T>() * destIndex;
            UnsafeUtility.WriteArrayElement((byte*)m_Buffer + offset, 0, data);
        }

        private unsafe UnsafeArray<U> InternalReinterpret<U>(int length) where U : unmanaged
        {
            UnsafeArray<U> unsafeArray = UnsafeArrayUtility.ConvertExistingDataToUnsafeArray<U>(m_Buffer, length, m_AllocatorLabel);
            return unsafeArray;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckReinterpretSize<U>() where U : unmanaged
        {
            if (UnsafeUtility.SizeOf<T>() != UnsafeUtility.SizeOf<U>())
            {
                throw new InvalidOperationException($"Types {(object)typeof(T)} and {(object)typeof(U)} are different sizes - direct reinterpretation is not possible. If this is what you intended, use Reinterpret(<type size>)");
            }
        }

        public UnsafeArray<U> Reinterpret<U>() where U : unmanaged
        {
            CheckReinterpretSize<U>();
            return InternalReinterpret<U>(Length);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckReinterpretSize<U>(long tSize, long uSize, int expectedTypeSize, long byteLen, long uLen)
        {
            if (tSize != expectedTypeSize)
            {
                throw new InvalidOperationException($"Type {(object)typeof(T)} was expected to be {(object)expectedTypeSize} but is {(object)tSize} bytes");
            }

            if (uLen * uSize != byteLen)
            {
                throw new InvalidOperationException($"Types {(object)typeof(T)} (array length {(object)Length}) and {(object)typeof(U)} cannot be aliased due to size constraints. The size of the types and lengths involved must line up.");
            }
        }

        public UnsafeArray<U> Reinterpret<U>(int expectedTypeSize) where U : unmanaged
        {
            long tSize = UnsafeUtility.SizeOf<T>();
            long uSize = UnsafeUtility.SizeOf<U>();
            long byteLen = Length * tSize;
            long num = byteLen / uSize;
            CheckReinterpretSize<U>(tSize, uSize, expectedTypeSize, byteLen, num);

            return InternalReinterpret<U>((int)num);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckGetSubArrayArguments(int start, int length)
        {
            if (start < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(start), "start must be >= 0");
            }

            if (start + length > Length)
            {
                throw new ArgumentOutOfRangeException(nameof(length), $"sub array range {(object)start}-{(object)(start + length - 1)} is outside the range of the native array 0-{(object)(Length - 1)}");
            }

            if (start + length < 0)
            {
                throw new ArgumentException($"sub array range {(object)start}-{(object)(start + length - 1)} caused an integer overflow and is outside the range of the native array 0-{(object)(Length - 1)}");
            }
        }

        public unsafe UnsafeArray<T> GetSubArray(int start, int length)
        {
            CheckGetSubArrayArguments(start, length);

            long offset = UnsafeUtility.SizeOf<T>() * start;
            UnsafeArray<T> unsafeArray = UnsafeArrayUtility.ConvertExistingDataToUnsafeArray<T>((byte*)m_Buffer + offset, length, Allocator.None);

            return unsafeArray;
        }

        public unsafe ReadOnly AsReadOnly() => new ReadOnly(m_Buffer, m_Length);

        [ExcludeFromDocs]
        public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
        {
            private UnsafeArray<T> m_Array;
            private int m_Index;

            public Enumerator(ref UnsafeArray<T> array)
            {
                m_Array = array;
                m_Index = -1;
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                ++m_Index;
                return m_Index < m_Array.Length;
            }

            public void Reset() => m_Index = -1;

            public T Current
            {
                get => m_Array[m_Index];
            }

            object IEnumerator.Current
            {
                get => Current;
            }
        }

        /// <summary>
        ///   <para>UnsafeArray interface constrained to read-only operation.</para>
        /// </summary>
        [NativeContainerIsReadOnly]
        [DebuggerDisplay("Length = {Length}")]
        public struct ReadOnly : IEnumerable<T>, IEnumerable
        {
            [NativeDisableUnsafePtrRestriction] internal unsafe void* m_Buffer;
            internal int m_Length;

            internal unsafe ReadOnly(void* buffer, int length)
            {
                m_Buffer = buffer;
                m_Length = length;
            }

            public int Length => m_Length;

            public void CopyTo(T[] array) => Copy(this, array);

            public void CopyTo(UnsafeArray<T> array) => Copy(this, array);

            public T[] ToArray()
            {
                T[] dst = new T[m_Length];
                Copy(this, dst, m_Length);

                return dst;
            }

            public unsafe UnsafeArray<U>.ReadOnly Reinterpret<U>()
                where U : unmanaged
            {
                CheckReinterpretSize<U>();
                return new UnsafeArray<U>.ReadOnly(m_Buffer, m_Length);
            }

            public unsafe T this[int index]
            {
                get
                {
                    CheckElementReadAccess(index);
                    return UnsafeUtility.ReadArrayElement<T>(m_Buffer, index);
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckElementReadAccess(int index)
            {
                if (index < 0 || index >= m_Length)
                {
                    throw new IndexOutOfRangeException($"Index {(object)index} is out of range (must be between 0 and {(object)(m_Length - 1)}).");
                }
            }

            public unsafe bool IsCreated => (IntPtr)m_Buffer != IntPtr.Zero;

            public Enumerator GetEnumerator() => new Enumerator(in this);

            IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            [ExcludeFromDocs]
            public struct Enumerator : IEnumerator<T>, IEnumerator, IDisposable
            {
                private ReadOnly m_Array;
                private int m_Index;

                public Enumerator(in ReadOnly array)
                {
                    m_Array = array;
                    m_Index = -1;
                }

                public void Dispose() { }

                public bool MoveNext()
                {
                    ++m_Index;
                    return m_Index < m_Array.Length;
                }

                public void Reset() => m_Index = -1;

                public T Current
                {
                    get => m_Array[m_Index];
                }

                object IEnumerator.Current
                {
                    get => Current;
                }
            }
        }

        internal struct UnsafeArrayDisposeJob : IJob
        {
            internal UnsafeArrayDispose Data;

            public void Execute() => Data.Dispose();
        }

        internal struct UnsafeArrayDispose
        {
            [NativeDisableUnsafePtrRestriction] internal unsafe void* m_Buffer;
            internal Allocator m_AllocatorLabel;

            public unsafe void Dispose() => UnsafeUtility.Free(m_Buffer, m_AllocatorLabel);
        }
    }
}