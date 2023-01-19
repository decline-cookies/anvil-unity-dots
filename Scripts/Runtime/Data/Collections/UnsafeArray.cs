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
    public struct UnsafeArray<T> : IDisposable,
                                   IEnumerable<T>,
                                   IEnumerable,
                                   IEquatable<UnsafeArray<T>>
        where T : unmanaged
    {
        [NativeDisableUnsafePtrRestriction] internal unsafe void* m_Buffer;
        internal int m_Length;
        internal int m_MinIndex;
        internal int m_MaxIndex;
        internal Allocator m_AllocatorLabel;


        public unsafe UnsafeArray(int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            UnsafeArray<T>.Allocate(length, allocator, out this);
            if ((options & NativeArrayOptions.ClearMemory) != NativeArrayOptions.ClearMemory)
                return;
            UnsafeUtility.MemClear(this.m_Buffer, (long)this.Length * (long)UnsafeUtility.SizeOf<T>());
        }

        public UnsafeArray(T[] array, Allocator allocator)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            UnsafeArray<T>.Allocate(array.Length, allocator, out this);
            UnsafeArray<T>.Copy(array, this);
        }

        public UnsafeArray(UnsafeArray<T> array, Allocator allocator)
        {
            UnsafeArray<T>.Allocate(array.Length, allocator, out this);
            UnsafeArray<T>.Copy(array, 0, this, 0, array.Length);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckAllocateArguments(int length, Allocator allocator, long totalSize)
        {
            if (allocator <= Allocator.None)
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be >= 0");
            UnsafeArray<T>.IsUnmanagedAndThrow();
        }

        private static unsafe void Allocate(int length, Allocator allocator, out UnsafeArray<T> array)
        {
            long num = (long)UnsafeUtility.SizeOf<T>() * (long)length;
            UnsafeArray<T>.CheckAllocateArguments(length, allocator, num);
            array = new UnsafeArray<T>();
            array.m_Buffer = UnsafeUtility.Malloc(num, UnsafeUtility.AlignOf<T>(), allocator);
            array.m_Length = length;
            array.m_AllocatorLabel = allocator;
            array.m_MinIndex = 0;
            array.m_MaxIndex = length - 1;
        }

        public int Length
        {
            get => this.m_Length;
        }

        [BurstDiscard]
        internal static void IsUnmanagedAndThrow()
        {
            if (!UnsafeUtility.IsValidNativeContainerElementType<T>())
                throw new InvalidOperationException($"{(object)typeof(T)} used in UnsafeArray<{(object)typeof(T)}> must be unmanaged (contain no managed types) and cannot itself be a native container type.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private unsafe void CheckElementReadAccess(int index)
        {
            if (index < this.m_MinIndex
             || index > this.m_MaxIndex)
                this.FailOutOfRangeError(index);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private unsafe void CheckElementWriteAccess(int index)
        {
            if (index < this.m_MinIndex
             || index > this.m_MaxIndex)
                this.FailOutOfRangeError(index);
        }

        public unsafe T this[int index]
        {
            get
            {
                this.CheckElementReadAccess(index);
                return UnsafeUtility.ReadArrayElement<T>(this.m_Buffer, index);
            }
            [WriteAccessRequired] set
            {
                this.CheckElementWriteAccess(index);
                UnsafeUtility.WriteArrayElement<T>(this.m_Buffer, index, value);
            }
        }

        public unsafe bool IsCreated
        {
            get => (IntPtr)this.m_Buffer != IntPtr.Zero;
        }

        [WriteAccessRequired]
        public unsafe void Dispose()
        {
            if ((IntPtr)this.m_Buffer == IntPtr.Zero)
                throw new ObjectDisposedException("The UnsafeArray is already disposed.");
            if (this.m_AllocatorLabel == Allocator.Invalid)
                throw new InvalidOperationException("The UnsafeArray can not be Disposed because it was not allocated with a valid allocator.");
            if (this.m_AllocatorLabel > Allocator.None)
            {
                UnsafeUtility.Free(this.m_Buffer, this.m_AllocatorLabel);
                this.m_AllocatorLabel = Allocator.Invalid;
            }

            this.m_Buffer = (void*)null;
        }

        public unsafe JobHandle Dispose(JobHandle inputDeps)
        {
            if (this.m_AllocatorLabel == Allocator.Invalid)
                throw new InvalidOperationException("The UnsafeArray can not be Disposed because it was not allocated with a valid allocator.");
            if ((IntPtr)this.m_Buffer == IntPtr.Zero)
                throw new InvalidOperationException("The UnsafeArray is already disposed.");
            if (this.m_AllocatorLabel > Allocator.None)
            {
                JobHandle jobHandle = new UnsafeArrayDisposeJob()
                {
                    Data = new UnsafeArrayDispose()
                    {
                        m_Buffer = this.m_Buffer, m_AllocatorLabel = this.m_AllocatorLabel
                    }
                }.Schedule<UnsafeArrayDisposeJob>(inputDeps);
                this.m_Buffer = (void*)null;
                this.m_AllocatorLabel = Allocator.Invalid;
                return jobHandle;
            }

            this.m_Buffer = (void*)null;
            return inputDeps;
        }

        [WriteAccessRequired]
        public void CopyFrom(T[] array) => UnsafeArray<T>.Copy(array, this);

        [WriteAccessRequired]
        public void CopyFrom(UnsafeArray<T> array) => UnsafeArray<T>.Copy(array, this);

        public void CopyTo(T[] array) => UnsafeArray<T>.Copy(this, array);

        public void CopyTo(UnsafeArray<T> array) => UnsafeArray<T>.Copy(this, array);

        public T[] ToArray()
        {
            T[] dst = new T[this.Length];
            UnsafeArray<T>.Copy(this, dst, this.Length);
            return dst;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void FailOutOfRangeError(int index)
        {
            if (index < this.Length
             && (this.m_MinIndex != 0 || this.m_MaxIndex != this.Length - 1))
                throw new IndexOutOfRangeException($"Index {(object)index} is out of restricted IJobParallelFor range [{(object)this.m_MinIndex}...{(object)this.m_MaxIndex}] in ReadWriteBuffer.\n" + "ReadWriteBuffers are restricted to only read & write the element at the job index. You can use double buffering strategies to avoid race conditions due to reading & writing in parallel to the same elements from a job.");
            throw new IndexOutOfRangeException($"Index {(object)index} is out of range of '{(object)this.Length}' Length.");
        }

        public UnsafeArray<T>.Enumerator GetEnumerator() => new UnsafeArray<T>.Enumerator(ref this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => (IEnumerator<T>)new UnsafeArray<T>.Enumerator(ref this);

        IEnumerator IEnumerable.GetEnumerator() => (IEnumerator)this.GetEnumerator();

        public unsafe bool Equals(UnsafeArray<T> other) => this.m_Buffer == other.m_Buffer && this.m_Length == other.m_Length;

        public override bool Equals(object obj) => obj is UnsafeArray<T> other && this.Equals(other);

        public override unsafe int GetHashCode() => (int)this.m_Buffer * 397 ^ this.m_Length;

        public static bool operator ==(UnsafeArray<T> left, UnsafeArray<T> right) => left.Equals(right);

        public static bool operator !=(UnsafeArray<T> left, UnsafeArray<T> right) => !left.Equals(right);

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckCopyLengths(int srcLength, int dstLength)
        {
            if (srcLength != dstLength)
                throw new ArgumentException("source and destination length must be the same");
        }

        public static void Copy(UnsafeArray<T> src, UnsafeArray<T> dst)
        {
            UnsafeArray<T>.CheckCopyLengths(src.Length, dst.Length);
            UnsafeArray<T>.Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(UnsafeArray<T>.ReadOnly src, UnsafeArray<T> dst)
        {
            UnsafeArray<T>.CheckCopyLengths(src.Length, dst.Length);
            UnsafeArray<T>.Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(T[] src, UnsafeArray<T> dst)
        {
            UnsafeArray<T>.CheckCopyLengths(src.Length, dst.Length);
            UnsafeArray<T>.Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(UnsafeArray<T> src, T[] dst)
        {
            UnsafeArray<T>.CheckCopyLengths(src.Length, dst.Length);
            UnsafeArray<T>.Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(UnsafeArray<T>.ReadOnly src, T[] dst)
        {
            UnsafeArray<T>.CheckCopyLengths(src.Length, dst.Length);
            UnsafeArray<T>.Copy(src, 0, dst, 0, src.Length);
        }

        public static void Copy(UnsafeArray<T> src, UnsafeArray<T> dst, int length) => UnsafeArray<T>.Copy(src, 0, dst, 0, length);

        public static void Copy(UnsafeArray<T>.ReadOnly src, UnsafeArray<T> dst, int length) => UnsafeArray<T>.Copy(src, 0, dst, 0, length);

        public static void Copy(T[] src, UnsafeArray<T> dst, int length) => UnsafeArray<T>.Copy(src, 0, dst, 0, length);

        public static void Copy(UnsafeArray<T> src, T[] dst, int length) => UnsafeArray<T>.Copy(src, 0, dst, 0, length);

        public static void Copy(UnsafeArray<T>.ReadOnly src, T[] dst, int length) => UnsafeArray<T>.Copy(src, 0, dst, 0, length);

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckCopyArguments(int srcLength,
                                               int srcIndex,
                                               int dstLength,
                                               int dstIndex,
                                               int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length), "length must be equal or greater than zero.");
            if (srcIndex < 0
             || srcIndex > srcLength
             || srcIndex == srcLength && srcLength > 0)
                throw new ArgumentOutOfRangeException(nameof(srcIndex), "srcIndex is outside the range of valid indexes for the source UnsafeArray.");
            if (dstIndex < 0
             || dstIndex > dstLength
             || dstIndex == dstLength && dstLength > 0)
                throw new ArgumentOutOfRangeException(nameof(dstIndex), "dstIndex is outside the range of valid indexes for the destination UnsafeArray.");
            if (srcIndex + length > srcLength)
                throw new ArgumentException("length is greater than the number of elements from srcIndex to the end of the source UnsafeArray.", nameof(length));
            if (srcIndex + length < 0)
                throw new ArgumentException("srcIndex + length causes an integer overflow");
            if (dstIndex + length > dstLength)
                throw new ArgumentException("length is greater than the number of elements from dstIndex to the end of the destination UnsafeArray.", nameof(length));
            if (dstIndex + length < 0)
                throw new ArgumentException("dstIndex + length causes an integer overflow");
        }

        public static unsafe void Copy(UnsafeArray<T> src,
                                       int srcIndex,
                                       UnsafeArray<T> dst,
                                       int dstIndex,
                                       int length)
        {
            UnsafeArray<T>.CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            UnsafeUtility.MemCpy((void*)((IntPtr)dst.m_Buffer + dstIndex * UnsafeUtility.SizeOf<T>()), (void*)((IntPtr)src.m_Buffer + srcIndex * UnsafeUtility.SizeOf<T>()), (long)(length * UnsafeUtility.SizeOf<T>()));
        }

        public static unsafe void Copy(UnsafeArray<T>.ReadOnly src,
                                       int srcIndex,
                                       UnsafeArray<T> dst,
                                       int dstIndex,
                                       int length)
        {
            UnsafeArray<T>.CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            UnsafeUtility.MemCpy((void*)((IntPtr)dst.m_Buffer + dstIndex * UnsafeUtility.SizeOf<T>()), (void*)((IntPtr)src.m_Buffer + srcIndex * UnsafeUtility.SizeOf<T>()), (long)(length * UnsafeUtility.SizeOf<T>()));
        }

        public static unsafe void Copy(T[] src,
                                       int srcIndex,
                                       UnsafeArray<T> dst,
                                       int dstIndex,
                                       int length)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));
            UnsafeArray<T>.CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            GCHandle gcHandle = GCHandle.Alloc((object)src, GCHandleType.Pinned);
            IntPtr num = gcHandle.AddrOfPinnedObject();
            UnsafeUtility.MemCpy((void*)((IntPtr)dst.m_Buffer + dstIndex * UnsafeUtility.SizeOf<T>()), (void*)((IntPtr)(void*)num + srcIndex * UnsafeUtility.SizeOf<T>()), (long)(length * UnsafeUtility.SizeOf<T>()));
            gcHandle.Free();
        }

        public static unsafe void Copy(UnsafeArray<T> src,
                                       int srcIndex,
                                       T[] dst,
                                       int dstIndex,
                                       int length)
        {
            if (dst == null)
                throw new ArgumentNullException(nameof(dst));
            UnsafeArray<T>.CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            GCHandle gcHandle = GCHandle.Alloc((object)dst, GCHandleType.Pinned);
            UnsafeUtility.MemCpy((void*)((IntPtr)(void*)gcHandle.AddrOfPinnedObject() + dstIndex * UnsafeUtility.SizeOf<T>()), (void*)((IntPtr)src.m_Buffer + srcIndex * UnsafeUtility.SizeOf<T>()), (long)(length * UnsafeUtility.SizeOf<T>()));
            gcHandle.Free();
        }

        public static unsafe void Copy(UnsafeArray<T>.ReadOnly src,
                                       int srcIndex,
                                       T[] dst,
                                       int dstIndex,
                                       int length)
        {
            if (dst == null)
                throw new ArgumentNullException(nameof(dst));
            UnsafeArray<T>.CheckCopyArguments(src.Length, srcIndex, dst.Length, dstIndex, length);
            GCHandle gcHandle = GCHandle.Alloc((object)dst, GCHandleType.Pinned);
            UnsafeUtility.MemCpy((void*)((IntPtr)(void*)gcHandle.AddrOfPinnedObject() + dstIndex * UnsafeUtility.SizeOf<T>()), (void*)((IntPtr)src.m_Buffer + srcIndex * UnsafeUtility.SizeOf<T>()), (long)(length * UnsafeUtility.SizeOf<T>()));
            gcHandle.Free();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckReinterpretLoadRange<U>(int sourceIndex)
            where U : unmanaged
        {
            long num1 = (long)UnsafeUtility.SizeOf<T>();
            long num2 = (long)UnsafeUtility.SizeOf<U>();
            long num3 = (long)this.Length * num1;
            long num4 = (long)sourceIndex * num1;
            long num5 = num4 + num2;
            if (num4 < 0L
             || num5 > num3)
                throw new ArgumentOutOfRangeException(nameof(sourceIndex), "loaded byte range must fall inside container bounds");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckReinterpretStoreRange<U>(int destIndex)
            where U : unmanaged
        {
            long num1 = (long)UnsafeUtility.SizeOf<T>();
            long num2 = (long)UnsafeUtility.SizeOf<U>();
            long num3 = (long)this.Length * num1;
            long num4 = (long)destIndex * num1;
            long num5 = num4 + num2;
            if (num4 < 0L
             || num5 > num3)
                throw new ArgumentOutOfRangeException(nameof(destIndex), "stored byte range must fall inside container bounds");
        }

        public unsafe U ReinterpretLoad<U>(int sourceIndex)
            where U : unmanaged
        {
            this.CheckReinterpretLoadRange<U>(sourceIndex);
            long offset = UnsafeUtility.SizeOf<T>() * sourceIndex;
            return UnsafeUtility.ReadArrayElement<U>((byte*)m_Buffer + sourceIndex, 0);
        }

        public unsafe void ReinterpretStore<U>(int destIndex, U data)
            where U : unmanaged
        {
            this.CheckReinterpretStoreRange<U>(destIndex);
            long offset = UnsafeUtility.SizeOf<T>() * destIndex;
            UnsafeUtility.WriteArrayElement<U>((byte*)m_Buffer + offset, 0, data);
        }

        private unsafe UnsafeArray<U> InternalReinterpret<U>(int length)
            where U : unmanaged
        {
            UnsafeArray<U> unsafeArray = UnsafeArrayUtility.ConvertExistingDataToUnsafeArray<U>(this.m_Buffer, length, this.m_AllocatorLabel);
            return unsafeArray;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckReinterpretSize<U>()
            where U : unmanaged
        {
            if (UnsafeUtility.SizeOf<T>() != UnsafeUtility.SizeOf<U>())
                throw new InvalidOperationException($"Types {(object)typeof(T)} and {(object)typeof(U)} are different sizes - direct reinterpretation is not possible. If this is what you intended, use Reinterpret(<type size>)");
        }

        public UnsafeArray<U> Reinterpret<U>()
            where U : unmanaged
        {
            UnsafeArray<T>.CheckReinterpretSize<U>();
            return this.InternalReinterpret<U>(this.Length);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckReinterpretSize<U>(long tSize,
                                             long uSize,
                                             int expectedTypeSize,
                                             long byteLen,
                                             long uLen)
        {
            if (tSize != (long)expectedTypeSize)
                throw new InvalidOperationException($"Type {(object)typeof(T)} was expected to be {(object)expectedTypeSize} but is {(object)tSize} bytes");
            if (uLen * uSize != byteLen)
                throw new InvalidOperationException($"Types {(object)typeof(T)} (array length {(object)this.Length}) and {(object)typeof(U)} cannot be aliased due to size constraints. The size of the types and lengths involved must line up.");
        }

        public UnsafeArray<U> Reinterpret<U>(int expectedTypeSize)
            where U : unmanaged
        {
            long tSize = (long)UnsafeUtility.SizeOf<T>();
            long uSize = (long)UnsafeUtility.SizeOf<U>();
            long byteLen = (long)this.Length * tSize;
            long num = byteLen / uSize;
            this.CheckReinterpretSize<U>(tSize, uSize, expectedTypeSize, byteLen, num);
            return this.InternalReinterpret<U>((int)num);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckGetSubArrayArguments(int start, int length)
        {
            if (start < 0)
                throw new ArgumentOutOfRangeException(nameof(start), "start must be >= 0");
            if (start + length > this.Length)
                throw new ArgumentOutOfRangeException(nameof(length), $"sub array range {(object)start}-{(object)(start + length - 1)} is outside the range of the native array 0-{(object)(this.Length - 1)}");
            if (start + length < 0)
                throw new ArgumentException($"sub array range {(object)start}-{(object)(start + length - 1)} caused an integer overflow and is outside the range of the native array 0-{(object)(this.Length - 1)}");
        }

        public unsafe UnsafeArray<T> GetSubArray(int start, int length)
        {
            this.CheckGetSubArrayArguments(start, length);

            long offset = UnsafeUtility.SizeOf<T>() * start;
            UnsafeArray<T> unsafeArray = UnsafeArrayUtility.ConvertExistingDataToUnsafeArray<T>((byte*)m_Buffer + offset, length, Allocator.None);
            return unsafeArray;
        }

        public unsafe UnsafeArray<T>.ReadOnly AsReadOnly() => new UnsafeArray<T>.ReadOnly(this.m_Buffer, this.m_Length);

        [ExcludeFromDocs]
        public struct Enumerator : IEnumerator<T>,
                                   IEnumerator,
                                   IDisposable
        {
            private UnsafeArray<T> m_Array;
            private int m_Index;

            public Enumerator(ref UnsafeArray<T> array)
            {
                this.m_Array = array;
                this.m_Index = -1;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                ++this.m_Index;
                return this.m_Index < this.m_Array.Length;
            }

            public void Reset() => this.m_Index = -1;

            public T Current
            {
                get => this.m_Array[this.m_Index];
            }

            object IEnumerator.Current
            {
                get => (object)this.Current;
            }
        }

        /// <summary>
        ///   <para>UnsafeArray interface constrained to read-only operation.</para>
        /// </summary>
        [NativeContainerIsReadOnly]
        [DebuggerDisplay("Length = {Length}")]
        public struct ReadOnly : IEnumerable<T>,
                                 IEnumerable
        {
            [NativeDisableUnsafePtrRestriction] internal unsafe void* m_Buffer;
            internal int m_Length;

            internal unsafe ReadOnly(void* buffer, int length)
            {
                this.m_Buffer = buffer;
                this.m_Length = length;
            }

            public int Length => this.m_Length;

            public void CopyTo(T[] array) => UnsafeArray<T>.Copy(this, array);

            public void CopyTo(UnsafeArray<T> array) => UnsafeArray<T>.Copy(this, array);

            public T[] ToArray()
            {
                T[] dst = new T[this.m_Length];
                UnsafeArray<T>.Copy(this, dst, this.m_Length);
                return dst;
            }

            public unsafe UnsafeArray<U>.ReadOnly Reinterpret<U>()
                where U : unmanaged
            {
                UnsafeArray<T>.CheckReinterpretSize<U>();
                return new UnsafeArray<U>.ReadOnly(this.m_Buffer, this.m_Length);
            }

            public unsafe T this[int index]
            {
                get
                {
                    this.CheckElementReadAccess(index);
                    return UnsafeUtility.ReadArrayElement<T>(this.m_Buffer, index);
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private unsafe void CheckElementReadAccess(int index)
            {
                if (index < 0
                 || index >= this.m_Length)
                    throw new IndexOutOfRangeException($"Index {(object)index} is out of range (must be between 0 and {(object)(this.m_Length - 1)}).");
            }

            public unsafe bool IsCreated => (IntPtr)this.m_Buffer != IntPtr.Zero;

            public UnsafeArray<T>.ReadOnly.Enumerator GetEnumerator() => new UnsafeArray<T>.ReadOnly.Enumerator(in this);

            IEnumerator<T> IEnumerable<T>.GetEnumerator() => (IEnumerator<T>)this.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => (IEnumerator)this.GetEnumerator();

            [ExcludeFromDocs]
            public struct Enumerator : IEnumerator<T>,
                                       IEnumerator,
                                       IDisposable
            {
                private UnsafeArray<T>.ReadOnly m_Array;
                private int m_Index;

                public Enumerator(in UnsafeArray<T>.ReadOnly array)
                {
                    this.m_Array = array;
                    this.m_Index = -1;
                }

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    ++this.m_Index;
                    return this.m_Index < this.m_Array.Length;
                }

                public void Reset() => this.m_Index = -1;

                public T Current
                {
                    get => this.m_Array[this.m_Index];
                }

                object IEnumerator.Current
                {
                    get => (object)this.Current;
                }
            }
        }
        
        internal struct UnsafeArrayDisposeJob : IJob
        {
            internal UnsafeArrayDispose Data;

            public void Execute() => this.Data.Dispose();
        }
        
        internal struct UnsafeArrayDispose
        {
            [NativeDisableUnsafePtrRestriction]
            internal unsafe void* m_Buffer;
            internal Allocator m_AllocatorLabel;

            public unsafe void Dispose() => UnsafeUtility.Free(this.m_Buffer, this.m_AllocatorLabel);
        }
    }
}
