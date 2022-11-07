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
        private static readonly Type I_CANCELLABLE_DATA_STREAM = typeof(ICancellableDataStream<>);
        private static readonly Type I_DATA_STREAM = typeof(IDataStream<>);
        private static readonly Type I_CANCEL_RESULT_DATA_STREAM = typeof(ICancelResultDataStream<>);
        private static readonly MethodInfo PROTOTYPE_DATA_STREAM_METHOD = typeof(DataStreamFactory).GetMethod(nameof(CreateTypedDataStream), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo PROTOTYPE_CANCELLABLE_DATA_STREAM_METHOD = typeof(DataStreamFactory).GetMethod(nameof(CreateTypedCancellableDataStream), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo PROTOTYPE_CANCEL_RESULT_DATA_STREAM_METHOD = typeof(DataStreamFactory).GetMethod(nameof(CreateTypedCancelResultDataStream), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly Dictionary<Type, MethodInfo> TYPED_GENERIC_DATA_STREAM_METHODS = new Dictionary<Type, MethodInfo>();
        private static readonly Dictionary<Type, MethodInfo> TYPED_GENERIC_CANCELLABLE_DATA_STREAM_METHODS = new Dictionary<Type, MethodInfo>();
        private static readonly Dictionary<Type, MethodInfo> TYPED_GENERIC_CANCEL_RESULT_DATA_STREAM_METHODS = new Dictionary<Type, MethodInfo>();

        private static readonly HashSet<Type> DATA_STREAM_TYPES = new HashSet<Type>()
        {
            I_DATA_STREAM, I_CANCELLABLE_DATA_STREAM, I_CANCEL_RESULT_DATA_STREAM
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            //TODO: #70 - Double check this works as expected in a build. https://github.com/decline-cookies/anvil-unity-dots/pull/58/files#r974334409
            TYPED_GENERIC_DATA_STREAM_METHODS.Clear();
            TYPED_GENERIC_CANCELLABLE_DATA_STREAM_METHODS.Clear();
            TYPED_GENERIC_CANCEL_RESULT_DATA_STREAM_METHODS.Clear();
        }

        private static void DetermineContext(TaskData taskData, out Type type, out object instance)
        {
            if (taskData.TaskDriver != null)
            {
                type = taskData.TaskDriver.GetType();
                instance = taskData.TaskDriver;
            }
            else
            {
                type = taskData.TaskSystem.GetType();
                instance = taskData.TaskSystem;
            }
        }

        public static void CreateDataStreams(TaskData taskData)
        {
            DetermineContext(taskData, out Type type, out object instance);
            
            //Get all the fields
            FieldInfo[] systemFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo field in systemFields)
            {
                //Can't create if it's not a generic type...
                if (!field.FieldType.IsGenericType)
                {
                    continue;
                }
                
                //If it's not a valid type to create...
                if (!DATA_STREAM_TYPES.Contains(field.FieldType))
                {
                    continue;
                }

                //If the field should be ignored and not auto-generated...
                IgnoreDataStreamAttribute ignoreDataStreamAttribute = field.GetCustomAttribute<IgnoreDataStreamAttribute>();
                if (ignoreDataStreamAttribute != null)
                {
                    continue;
                }
                
                Debug_CheckFieldIsReadOnly(field);
                Debug_CheckFieldTypeGenericTypeArguments(field.FieldType);

                Type genericTypeDefinition = field.FieldType.GetGenericTypeDefinition();
                Type entityProxyInstanceType = field.FieldType.GenericTypeArguments[0];
                
                Debug_CheckInstanceType(entityProxyInstanceType);

                object createdInstance = null;
                if (genericTypeDefinition == I_DATA_STREAM)
                {
                    createdInstance = CreateDataStream(type, taskData);
                }
                else if (genericTypeDefinition == I_CANCELLABLE_DATA_STREAM)
                {
                    createdInstance = CreateCancellableDataStream(type, taskData);
                }
                else if (genericTypeDefinition == I_CANCEL_RESULT_DATA_STREAM)
                {
                    createdInstance = CreateCancelResultDataStream(type, taskData);
                }
                
                //TODO: Find and replace all TaskStream to DataStream

                Debug_EnsureFieldNotSet(field, instance);
                //Ensure the System's field is set to the task stream
                field.SetValue(instance, createdInstance);
            }
        }

        private static object CreateDataStream(Type dataStreamType, TaskData taskData)
        {
            MethodInfo createFunction = GetOrCreateMethod(dataStreamType, TYPED_GENERIC_DATA_STREAM_METHODS, PROTOTYPE_DATA_STREAM_METHOD);
            IInternalDataStream dataStream = (IInternalDataStream)createFunction.Invoke(null, new object[]{taskData});
            return dataStream;
        }

        private static DataStream<TInstance> CreateTypedDataStream<TInstance>(TaskData taskData)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            DataStream<TInstance> dataStream = new DataStream<TInstance>(taskData.CancelRequestDataStream, taskData.TaskDriver, taskData.TaskSystem);
            taskData.RegisterDataStream(dataStream);
            return dataStream;
        }
        
        private static object CreateCancellableDataStream(Type dataStreamType, TaskData taskData)
        {
            MethodInfo createFunction = GetOrCreateMethod(dataStreamType, TYPED_GENERIC_CANCELLABLE_DATA_STREAM_METHODS, PROTOTYPE_CANCELLABLE_DATA_STREAM_METHOD);
            IInternalCancellableDataStream dataStream = (IInternalCancellableDataStream)createFunction.Invoke(null, new object[]{taskData});
            return dataStream;
        }

        private static CancellableDataStream<TInstance> CreateTypedCancellableDataStream<TInstance>(TaskData taskData)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            CancellableDataStream<TInstance> dataStream = new CancellableDataStream<TInstance>(taskData.CancelRequestDataStream, taskData.TaskDriver, taskData.TaskSystem);
            taskData.RegisterDataStream(dataStream);
            return dataStream;
        }
        
        private static object CreateCancelResultDataStream(Type dataStreamType, TaskData taskData)
        {
            MethodInfo createFunction = GetOrCreateMethod(dataStreamType, TYPED_GENERIC_CANCEL_RESULT_DATA_STREAM_METHODS, PROTOTYPE_CANCEL_RESULT_DATA_STREAM_METHOD);
            IInternalCancelResultDataStream dataStream = (IInternalCancelResultDataStream)createFunction.Invoke(null, new object[]{taskData});
            return dataStream;
        }

        private static CancelResultDataStream<TInstance> CreateTypedCancelResultDataStream<TInstance>(TaskData taskData)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            CancelResultDataStream<TInstance> dataStream = new CancelResultDataStream<TInstance>(taskData.TaskDriver, taskData.TaskSystem);
            taskData.RegisterDataStream(dataStream);
            return dataStream;
        }

        private static MethodInfo GetOrCreateMethod(Type type, Dictionary<Type, MethodInfo> lookup, MethodInfo prototype)
        {
            if (!lookup.TryGetValue(type, out MethodInfo typedGenericMethod))
            {
                typedGenericMethod = prototype.MakeGenericMethod(type);
                lookup.Add(type, typedGenericMethod);
            }

            return typedGenericMethod;
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
    }
}
