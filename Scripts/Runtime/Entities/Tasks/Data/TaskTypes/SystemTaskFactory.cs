using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    internal static class SystemTaskFactory
    {
        private static readonly MethodInfo PROTOTYPE_METHOD = typeof(SystemTaskFactory).GetMethod(nameof(CreateSystemTask), BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly Dictionary<Type, MethodInfo> TYPED_GENERIC_METHODS = new Dictionary<Type, MethodInfo>();
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            TYPED_GENERIC_METHODS.Clear();
        }
        
        public static ISystemTask Create(Type dataType, IProxyDataStream proxyDataStream)
        {
            //TODO: Ensure Type is unmanaged and IProxyData
            if (!TYPED_GENERIC_METHODS.TryGetValue(dataType, out MethodInfo typedGenericMethod))
            {
                typedGenericMethod = PROTOTYPE_METHOD.MakeGenericMethod(dataType);
                TYPED_GENERIC_METHODS.Add(dataType, typedGenericMethod);
            }

            return (ISystemTask)typedGenericMethod.Invoke(null, new []{proxyDataStream});
        }
        
        private static SystemTask<TData> CreateSystemTask<TData>(ProxyDataStream<TData> proxyDataStream)
            where TData : unmanaged, IProxyData
        {
            return new SystemTask<TData>(proxyDataStream);
        }
    }
}
