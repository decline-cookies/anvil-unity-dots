using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    internal static class ProxyDataStreamFactory
    {
        private static readonly MethodInfo PROTOTYPE_METHOD = typeof(ProxyDataStreamFactory).GetMethod(nameof(CreateProxyDataStream), BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly Dictionary<Type, MethodInfo> TYPED_GENERIC_METHODS = new Dictionary<Type, MethodInfo>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            TYPED_GENERIC_METHODS.Clear();
        }

        public static IProxyDataStream Create(Type dataType)
        {
            //TODO: Ensure Type is unmanaged and IProxyData
            if (!TYPED_GENERIC_METHODS.TryGetValue(dataType, out MethodInfo typedGenericMethod))
            {
                typedGenericMethod = PROTOTYPE_METHOD.MakeGenericMethod(dataType);
                TYPED_GENERIC_METHODS.Add(dataType, typedGenericMethod);
            }

            return (IProxyDataStream)typedGenericMethod.Invoke(null, null);
        }

        private static ProxyDataStream<TData> CreateProxyDataStream<TData>()
            where TData : unmanaged, IProxyData
        {
            return new ProxyDataStream<TData>();
        }
    }
}
