using System;
using System.Diagnostics;
using System.Reflection;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities
{
    internal unsafe class NativeArrayAccessWrapper : IAccessWrapper
    {
        private static readonly FieldInfo s_NativeArray_Allocator = typeof(NativeArray<>).GetField("m_AllocatorLabel", BindingFlags.Instance | BindingFlags.NonPublic);
        

        public static NativeArrayAccessWrapper Create<T>(NativeArray<T> array)
            where T : struct
        {
            return new NativeArrayAccessWrapper(typeof(T),
                                                NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks(array),
                                                array.Length,
                                                //TODO: There's probably a way to create a delegate here but I'm not sure
                                                (Allocator)s_NativeArray_Allocator.GetValue(array));
        }

        private readonly Type m_Type;
        private readonly void* m_NativeArrayPtr;
        private readonly int m_Length;
        private readonly Allocator m_Allocator;

        private NativeArrayAccessWrapper(Type type, void* nativeArrayPtr, int length, Allocator allocator)
        {
            m_Type = type;
            m_NativeArrayPtr = nativeArrayPtr;
            m_Length = length;
            m_Allocator = allocator;
        }
        
        public void Dispose()
        {
            //Not needed
        }

        public JobHandle Acquire()
        {
            //Does nothing - We don't know what the access could be here, up to the author to manage
            return default;
        }

        public void Release(JobHandle releaseAccessDependency)
        {
            //Does nothing - We don't know what the access could be here, up to the author to manage
        }

        public NativeArray<T> ResolveNativeArray<T>()
            where T : unmanaged
        {
            Debug_EnsureSameType(typeof(T));   
            return NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(m_NativeArrayPtr,
                                                                                m_Length,
                                                                                m_Allocator);
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureSameType(Type type)
        {
            if (m_Type != type)
            {
                throw new InvalidOperationException($"Trying to resolve to NativeArray of type {type} but {m_Type} was stored!");
            }
        }
    }
}
