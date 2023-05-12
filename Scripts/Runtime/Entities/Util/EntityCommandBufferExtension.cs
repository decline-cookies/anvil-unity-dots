using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    public static class EntityCommandBufferExtension
    {
        private static readonly Type I_SHARED_COMPONENT_DATA = typeof(ISharedComponentData);
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            Type wrapperGenericType = typeof(Wrapper<>);
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                {
                    continue;
                }
                foreach (Type type in assembly.GetTypes())
                {
                    if (!type.IsValueType || !I_SHARED_COMPONENT_DATA.IsAssignableFrom(type))
                    {
                        continue;
                    }

                    Type specificWrapperType = wrapperGenericType.MakeGenericType(type);
                    MethodInfo initFunction = specificWrapperType.GetMethod("Init", BindingFlags.Static | BindingFlags.Public);
                    initFunction.Invoke(null, null);

                }
            }
        }
        
        public static void SetSharedCompatibleNoBurst<T>(this EntityCommandBuffer ecb, Entity e, T sharedComponentData)
            where T : struct, ISharedComponentData
        {
            Wrapper<T>.SetSharedComponentNoBurst(ecb, e, sharedComponentData);
        }
    }
    
    public static class DelegateCreator
    {
        private static readonly Action<Type[]> MakeNewCustomDelegate = (Action<Type[]>)Delegate.CreateDelegate(typeof(Action<Type[]>), typeof(Expression).Assembly.GetType("System.Linq.Expressions.Compiler.DelegateHelpers").GetMethod("MakeNewCustomDelegate", BindingFlags.NonPublic | BindingFlags.Static));

        public static Type NewDelegateType(params Type[] parameters)
        {
            Type[] args = new Type[parameters.Length];
            parameters.CopyTo(args, 0);
            args[args.Length-1] = ret;
            return MakeNewCustomDelegate(args);
        }
    }


    internal class Wrapper<T>
        where T : struct, ISharedComponentData
    {
        private sealed class BurstCompatible { }

        private static readonly SharedStatic<FunctionPointer<SetSharedComponentDelegate>> s_SharedStaticFunctionPointer
            = SharedStatic<FunctionPointer<SetSharedComponentDelegate>>.GetOrCreate<BurstCompatible>();

        private delegate void SetSharedComponentDelegate(EntityCommandBuffer ecb, Entity e, T sharedComponentData);

        public static void Init()
        {
            var typedDelegate = DelegateCreator.NewDelegateType()
            s_SharedStaticFunctionPointer.Data = new FunctionPointer<SetSharedComponentDelegate>(Marshal.GetFunctionPointerForDelegate((SetSharedComponentDelegate)SetSharedComponent));
        }

        public static void SetSharedComponentNoBurst(EntityCommandBuffer ecb, Entity e, T sharedComponentData)
        {
            s_SharedStaticFunctionPointer.Data.Invoke(ecb, e, sharedComponentData);
        }
        
        private static void SetSharedComponent(EntityCommandBuffer ecb, Entity e, T sharedComponentData)
        {
            ecb.SetSharedComponent(e, sharedComponentData);
        }
    }
}
