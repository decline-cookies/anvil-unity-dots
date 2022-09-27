using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    internal static class TaskStreamFactory
    {
        private static readonly Type I_PROXY_INSTANCE_TYPE = typeof(IProxyInstance);
        private static readonly MethodInfo PROTOTYPE_METHOD = typeof(TaskStreamFactory).GetMethod(nameof(CreateTaskStream), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly Dictionary<Type, MethodInfo> TYPED_GENERIC_METHODS = new Dictionary<Type, MethodInfo>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            //TODO: Double check this works as expected in a build. https://github.com/decline-cookies/anvil-unity-dots/pull/58/files#r974334409
            TYPED_GENERIC_METHODS.Clear();
        }

        public static AbstractTaskStream Create(Type taskStreamType, Type instanceType)
        {
            Debug_CheckInstanceType(instanceType);
            if (!TYPED_GENERIC_METHODS.TryGetValue(taskStreamType, out MethodInfo typedGenericMethod))
            {
                typedGenericMethod = PROTOTYPE_METHOD.MakeGenericMethod(taskStreamType, instanceType);
                TYPED_GENERIC_METHODS.Add(taskStreamType, typedGenericMethod);
            }

            return (AbstractTaskStream)typedGenericMethod.Invoke(null, null);
        }

        private static TTaskStream CreateTaskStream<TTaskStream, TData>()
            where TTaskStream : AbstractTaskStream, ITaskStream<TData>, new()
            where TData : unmanaged, IProxyInstance
        {
            return new TTaskStream();
        }
        
        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void Debug_CheckInstanceType(Type proxyInstanceType)
        {
            if (!I_PROXY_INSTANCE_TYPE.IsAssignableFrom(proxyInstanceType))
            {
                throw new InvalidOperationException($"Type {proxyInstanceType} does not implement {I_PROXY_INSTANCE_TYPE}!");
            }

            if (!UnsafeUtility.IsUnmanaged(proxyInstanceType))
            {
                throw new InvalidOperationException($"Type {proxyInstanceType} is not unmanaged!");
            }
        }
    }
}
