using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal static class DataStreamFactory
    {
        private static readonly Type I_ENTITY_PROXY_INSTANCE_TYPE = typeof(IEntityProxyInstance);
        private static readonly Type ABSTRACT_ENTITY_PROXY_DATA_STREAM_TYPE = typeof(AbstractDataStream);
        private static readonly MethodInfo PROTOTYPE_METHOD = typeof(DataStreamFactory).GetMethod(nameof(CreateDataStream), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly Dictionary<Type, MethodInfo> TYPED_GENERIC_METHODS = new Dictionary<Type, MethodInfo>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            //TODO: #70 - Double check this works as expected in a build. https://github.com/decline-cookies/anvil-unity-dots/pull/58/files#r974334409
            TYPED_GENERIC_METHODS.Clear();
        }

        public static void CreateDataStreams(AbstractTaskSystem taskSystem, List<AbstractDataStream> taskSystemTaskStreams)
        {
            CreateDataStreams(taskSystem.GetType(), taskSystem, taskSystemTaskStreams, taskSystem.CancelFlow.RequestDataStream);
        }

        public static void CreateDataStreams(AbstractTaskDriver taskDriver, List<AbstractDataStream> taskDriverTaskStreams)
        {
            CreateDataStreams(taskDriver.GetType(), taskDriver, taskDriverTaskStreams, taskDriver.CancelFlow.RequestDataStream);
        }
        
        private static void CreateDataStreams(Type type, object instance, List<AbstractDataStream> taskStreams, CancelRequestDataStream cancelRequestDataStream)
        {
            Debug_EnsureCancelRequestsNotNull(cancelRequestDataStream);
            
            //Get all the fields
            FieldInfo[] systemFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo field in systemFields)
            {
                if (!ABSTRACT_ENTITY_PROXY_DATA_STREAM_TYPE.IsAssignableFrom(field.FieldType))
                {
                    continue;
                }

                IgnoreDataStreamAttribute ignoreDataStreamAttribute = field.GetCustomAttribute<IgnoreDataStreamAttribute>();
                if (ignoreDataStreamAttribute != null)
                {
                    continue;
                }
                
                //TODO: Find and replace all TaskStream to DataStream
                
                Debug_CheckFieldIsReadOnly(field);
                Debug_CheckFieldTypeGenericTypeArguments(field.FieldType);
                
                //Get the data type 
                Type entityProxyInstanceType = field.FieldType.GenericTypeArguments[0];
                AbstractDataStream dataStream = Create(field.FieldType, 
                                                         entityProxyInstanceType,
                                                         cancelRequestDataStream);
                
                //Populate the incoming list so we can handle disposal nicely
                taskStreams.Add(dataStream);

                Debug_EnsureFieldNotSet(field, instance);
                //Ensure the System's field is set to the task stream
                field.SetValue(instance, dataStream);
            }
        }

        private static AbstractDataStream Create(Type taskStreamType, Type instanceType, CancelRequestDataStream cancelRequestDataStream)
        {
            Debug_CheckInstanceType(instanceType);
            if (!TYPED_GENERIC_METHODS.TryGetValue(taskStreamType, out MethodInfo typedGenericMethod))
            {
                typedGenericMethod = PROTOTYPE_METHOD.MakeGenericMethod(taskStreamType);
                TYPED_GENERIC_METHODS.Add(taskStreamType, typedGenericMethod);
            }

            return (AbstractDataStream)typedGenericMethod.Invoke(null, new object[]{cancelRequestDataStream});
        }

        private static TTaskStream CreateDataStream<TTaskStream>(CancelRequestDataStream cancelRequestDataStream)
            where TTaskStream : AbstractDataStream
        {
            return (TTaskStream)Activator.CreateInstance(typeof(TTaskStream), 
                                                         BindingFlags.Instance | BindingFlags.NonPublic, 
                                                         null, 
                                                         new object[]{cancelRequestDataStream}, 
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
                throw new InvalidOperationException($"Type {fieldType} is to be used to create a {typeof(DataStream<>)} but {fieldType} doesn't have the expected 1 generic type!");
            }
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void Debug_EnsureFieldNotSet(FieldInfo fieldInfo, object instance)
        {
            if (fieldInfo.GetValue(instance) != null)
            {
                throw new InvalidOperationException($"Field with name {fieldInfo.Name} on {fieldInfo.ReflectedType} is already set! Did you call {nameof(CreateDataStreams)} more than once?");
            }
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void Debug_EnsureCancelRequestsNotNull(CancelRequestDataStream cancelRequestDataStream)
        {
            if (cancelRequestDataStream == null)
            {
                throw new InvalidOperationException($"{nameof(CancelRequestDataStream)} is null! Code change caused an ordering issue maybe?");
            }
        }
    }
}
