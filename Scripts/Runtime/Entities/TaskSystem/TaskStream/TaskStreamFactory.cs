using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal static class TaskStreamFactory
    {
        private static readonly Type I_ENTITY_PROXY_INSTANCE_TYPE = typeof(IEntityProxyInstance);
        private static readonly Type ABSTRACT_TASK_STREAM_TYPE = typeof(AbstractTaskStream);
        private static readonly MethodInfo PROTOTYPE_METHOD = typeof(TaskStreamFactory).GetMethod(nameof(CreateTaskStream), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly Dictionary<Type, MethodInfo> TYPED_GENERIC_METHODS = new Dictionary<Type, MethodInfo>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            //TODO: #70 - Double check this works as expected in a build. https://github.com/decline-cookies/anvil-unity-dots/pull/58/files#r974334409
            TYPED_GENERIC_METHODS.Clear();
        }

        public static void CreateTaskStreams(AbstractTaskSystem taskSystem, List<AbstractTaskStream> taskSystemTaskStreams)
        {
            CreateTaskStreams(taskSystem.GetType(), taskSystem, taskSystemTaskStreams);
        }

        public static void CreateTaskStreams(AbstractTaskDriver taskDriver, List<AbstractTaskStream> taskDriverTaskStreams)
        {
            CreateTaskStreams(taskDriver.GetType(), taskDriver, taskDriverTaskStreams);
        }
        
        private static void CreateTaskStreams(Type type, object instance, List<AbstractTaskStream> taskStreams)
        {
            //Get all the fields
            FieldInfo[] systemFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo field in systemFields)
            {
                if (!ABSTRACT_TASK_STREAM_TYPE.IsAssignableFrom(field.FieldType))
                {
                    continue;
                }

                IgnoreTaskStreamAttribute ignoreTaskStreamAttribute = field.GetCustomAttribute<IgnoreTaskStreamAttribute>();
                if (ignoreTaskStreamAttribute != null)
                {
                    continue;
                }

                Debug_CheckFieldIsReadOnly(field);
                Debug_CheckFieldTypeGenericTypeArguments(field.FieldType);

                TaskStreamFlags flags = TaskStreamFlags.Default;

                flags |= field.GetCustomAttribute<ResolveTargetAttribute>() != null
                    ? TaskStreamFlags.IsResolveTarget
                    : TaskStreamFlags.Default;

                flags |= field.GetCustomAttribute<CancellableAttribute>() != null
                    ? TaskStreamFlags.IsCancellable
                    : TaskStreamFlags.Default;
                
                //Get the data type 
                Type entityProxyInstanceType = field.FieldType.GenericTypeArguments[0];
                AbstractTaskStream taskStream = Create(field.FieldType, 
                                                       entityProxyInstanceType,
                                                       flags);
                
                //Populate the incoming list so we can handle disposal nicely
                taskStreams.Add(taskStream);

                Debug_EnsureFieldNotSet(field, instance);
                //Ensure the System's field is set to the task stream
                field.SetValue(instance, taskStream);
            }
        }

        private static AbstractTaskStream Create(Type taskStreamType, Type instanceType, TaskStreamFlags flags)
        {
            Debug_CheckInstanceType(instanceType);
            if (!TYPED_GENERIC_METHODS.TryGetValue(taskStreamType, out MethodInfo typedGenericMethod))
            {
                typedGenericMethod = PROTOTYPE_METHOD.MakeGenericMethod(taskStreamType);
                TYPED_GENERIC_METHODS.Add(taskStreamType, typedGenericMethod);
            }

            return (AbstractTaskStream)typedGenericMethod.Invoke(null, new object[]{flags});
        }

        private static TTaskStream CreateTaskStream<TTaskStream>(TaskStreamFlags flags)
            where TTaskStream : AbstractTaskStream
        {
            return (TTaskStream)Activator.CreateInstance(typeof(TTaskStream), 
                                                         BindingFlags.Instance | BindingFlags.NonPublic, 
                                                         null, 
                                                         new object[]{flags}, 
                                                         null, 
                                                         null);
        }
        
        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void Debug_CheckInstanceType(Type proxyInstanceType)
        {
            if (!I_ENTITY_PROXY_INSTANCE_TYPE.IsAssignableFrom(proxyInstanceType))
            {
                throw new InvalidOperationException($"Type {proxyInstanceType} does not implement {I_ENTITY_PROXY_INSTANCE_TYPE}!");
            }

            if (!UnsafeUtility.IsUnmanaged(proxyInstanceType))
            {
                throw new InvalidOperationException($"Type {proxyInstanceType} is not unmanaged!");
            }
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void Debug_CheckFieldIsReadOnly(FieldInfo fieldInfo)
        {
            if (!fieldInfo.IsInitOnly)
            {
                throw new InvalidOperationException($"Field with name {fieldInfo.Name} on {fieldInfo.ReflectedType} is not marked as \"readonly\", please ensure that it is.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void Debug_CheckFieldTypeGenericTypeArguments(Type fieldType)
        {
            if (fieldType.GenericTypeArguments.Length != 1)
            {
                throw new InvalidOperationException($"Type {fieldType} is to be used to create a {typeof(EntityProxyDataStream<>)} but {fieldType} doesn't have the expected 1 generic type!");
            }
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void Debug_EnsureFieldNotSet(FieldInfo fieldInfo, object instance)
        {
            if (fieldInfo.GetValue(instance) != null)
            {
                throw new InvalidOperationException($"Field with name {fieldInfo.Name} on {fieldInfo.ReflectedType} is already set! Did you call {nameof(CreateTaskStreams)} more than once?");
            }
        }
    }
}
