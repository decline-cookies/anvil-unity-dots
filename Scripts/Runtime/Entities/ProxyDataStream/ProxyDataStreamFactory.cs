using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    internal static class ProxyDataStreamFactory
    {
        private static readonly Type I_PROXY_INSTANCE_TYPE = typeof(IProxyInstance);
        private static readonly MethodInfo PROTOTYPE_METHOD = typeof(ProxyDataStreamFactory).GetMethod(nameof(CreateProxyDataStream), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly Dictionary<Type, MethodInfo> TYPED_GENERIC_METHODS = new Dictionary<Type, MethodInfo>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            //TODO: Double check this works as expected in a build. https://github.com/decline-cookies/anvil-unity-dots/pull/58/files#r974334409
            TYPED_GENERIC_METHODS.Clear();
        }

        public static AbstractProxyDataStream Create(Type instanceType)
        {
            Debug_CheckType(instanceType);
            if (!TYPED_GENERIC_METHODS.TryGetValue(instanceType, out MethodInfo typedGenericMethod))
            {
                typedGenericMethod = PROTOTYPE_METHOD.MakeGenericMethod(instanceType);
                TYPED_GENERIC_METHODS.Add(instanceType, typedGenericMethod);
            }

            return (AbstractProxyDataStream)typedGenericMethod.Invoke(null, null);
        }

        private static ProxyDataStream<TData> CreateProxyDataStream<TData>()
            where TData : unmanaged, IProxyInstance
        {
            return new ProxyDataStream<TData>();
        }
        
        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void Debug_CheckType(Type proxyInstanceType)
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
