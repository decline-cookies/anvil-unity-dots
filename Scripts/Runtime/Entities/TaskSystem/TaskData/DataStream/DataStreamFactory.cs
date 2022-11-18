using Anvil.CSharp.Logging;
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
        private static readonly Type I_DRIVER_CANCELLABLE_DATA_STREAM = typeof(IDriverCancellableDataStream<>);
        private static readonly Type I_DRIVER_DATA_STREAM = typeof(IDriverDataStream<>);
        private static readonly Type I_DRIVER_CANCEL_RESULT_DATA_STREAM = typeof(IDriverCancelResultDataStream<>);
        private static readonly Type I_SYSTEM_CANCELLABLE_DATA_STREAM = typeof(ISystemCancellableDataStream<>);
        private static readonly Type I_SYSTEM_DATA_STREAM = typeof(ISystemDataStream<>);
        private static readonly Type I_SYSTEM_CANCEL_RESULT_DATA_STREAM = typeof(ISystemCancelResultDataStream<>);
        private static readonly MethodInfo PROTOTYPE_DATA_STREAM_METHOD = typeof(DataStreamFactory).GetMethod(nameof(CreateTypedDataStream), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo PROTOTYPE_CANCELLABLE_DATA_STREAM_METHOD = typeof(DataStreamFactory).GetMethod(nameof(CreateTypedCancellableDataStream), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly MethodInfo PROTOTYPE_CANCEL_RESULT_DATA_STREAM_METHOD = typeof(DataStreamFactory).GetMethod(nameof(CreateTypedCancelResultDataStream), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly Dictionary<Type, MethodInfo> TYPED_GENERIC_DATA_STREAM_METHODS = new Dictionary<Type, MethodInfo>();
        private static readonly Dictionary<Type, MethodInfo> TYPED_GENERIC_CANCELLABLE_DATA_STREAM_METHODS = new Dictionary<Type, MethodInfo>();
        private static readonly Dictionary<Type, MethodInfo> TYPED_GENERIC_CANCEL_RESULT_DATA_STREAM_METHODS = new Dictionary<Type, MethodInfo>();

        private static readonly HashSet<Type> DRIVER_DATA_STREAM_TYPES = new HashSet<Type>()
        {
            I_DRIVER_DATA_STREAM, I_DRIVER_CANCELLABLE_DATA_STREAM, I_DRIVER_CANCEL_RESULT_DATA_STREAM
        };

        private static readonly HashSet<Type> SYSTEM_DATA_STREAM_TYPES = new HashSet<Type>()
        {
            I_SYSTEM_DATA_STREAM, I_SYSTEM_CANCELLABLE_DATA_STREAM, I_SYSTEM_CANCEL_RESULT_DATA_STREAM
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            //TODO: #70 - Double check this works as expected in a build. https://github.com/decline-cookies/anvil-unity-dots/pull/58/files#r974334409
            TYPED_GENERIC_DATA_STREAM_METHODS.Clear();
            TYPED_GENERIC_CANCELLABLE_DATA_STREAM_METHODS.Clear();
            TYPED_GENERIC_CANCEL_RESULT_DATA_STREAM_METHODS.Clear();
        }

        private static void DetermineContext(TaskData taskData,
                                             out Type type,
                                             out object instance,
                                             out bool isSystem)
        {
            if (taskData.TaskDriver != null)
            {
                type = taskData.TaskDriver.GetType();
                instance = taskData.TaskDriver;
                isSystem = false;
            }
            else
            {
                type = taskData.TaskSystem.GetType();
                instance = taskData.TaskSystem;
                isSystem = true;
            }
        }

        public static void CreateDataStreams(TaskData taskData)
        {
            DetermineContext(taskData,
                             out Type type,
                             out object instance,
                             out bool isSystem);

            CreateDataStreams(taskData,
                              type,
                              instance,
                              isSystem);
        }

        private static void ConfigureTypes(bool isSystem,
                                           out HashSet<Type> validTypes,
                                           out HashSet<Type> invalidTypes,
                                           out Type dataStreamType,
                                           out Type cancellableDataStreamType,
                                           out Type cancelResultDataStreamType)
        {
            if (isSystem)
            {
                validTypes = SYSTEM_DATA_STREAM_TYPES;
                invalidTypes = DRIVER_DATA_STREAM_TYPES;
                dataStreamType = I_SYSTEM_DATA_STREAM;
                cancellableDataStreamType = I_SYSTEM_CANCELLABLE_DATA_STREAM;
                cancelResultDataStreamType = I_SYSTEM_CANCEL_RESULT_DATA_STREAM;
            }
            else
            {
                validTypes = DRIVER_DATA_STREAM_TYPES;
                invalidTypes = SYSTEM_DATA_STREAM_TYPES;
                dataStreamType = I_DRIVER_DATA_STREAM;
                cancellableDataStreamType = I_DRIVER_CANCELLABLE_DATA_STREAM;
                cancelResultDataStreamType = I_DRIVER_CANCEL_RESULT_DATA_STREAM;
            }
        }

        private static void CreateDataStreams(TaskData taskData,
                                              Type type,
                                              object instance,
                                              bool isSystem)
        {
            ConfigureTypes(isSystem,
                           out HashSet<Type> validTypes,
                           out HashSet<Type> invalidTypes,
                           out Type dataStreamType,
                           out Type cancellableDataStreamType,
                           out Type cancelResultDataStreamType);

            //Get all the fields
            FieldInfo[] systemFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo field in systemFields)
            {
                Type fieldType = field.FieldType;
                //Can't create if it's not a generic type...
                if (!fieldType.IsGenericType)
                {
                    continue;
                }

                Type genericTypeDefinition = fieldType.GetGenericTypeDefinition();

                //If it's an invalid type we want to throw a runtime error
                if (invalidTypes.Contains(genericTypeDefinition))
                {
                    throw new InvalidOperationException($"Field {field.FieldType} on {type.GetReadableName()} is invalid!\n{typeof(AbstractTaskSystem).GetReadableName()} must use {string.Join(",", SYSTEM_DATA_STREAM_TYPES)}\n{typeof(AbstractTaskDriver).GetReadableName()} must use {string.Join(",", DRIVER_DATA_STREAM_TYPES)}");
                }

                //If it's not a valid type to create...
                if (!validTypes.Contains(genericTypeDefinition))
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
                Debug_CheckFieldTypeGenericTypeArguments(fieldType);

                Type instanceType = fieldType.GenericTypeArguments[0];

                Debug_CheckInstanceType(instanceType);

                object createdInstance = CreateInstance(genericTypeDefinition,
                                                        dataStreamType,
                                                        cancellableDataStreamType,
                                                        cancelResultDataStreamType,
                                                        instanceType,
                                                        taskData);

                Debug_EnsureFieldNotSet(field, instance);
                //Ensure the System's field is set to the task stream
                field.SetValue(instance, createdInstance);
            }
        }

        private static object CreateInstance(Type genericTypeDefinition,
                                             Type dataStreamType,
                                             Type cancellableDataStreamType,
                                             Type cancelResultDataStreamType,
                                             Type instanceType,
                                             TaskData taskData)
        {
            if (genericTypeDefinition == dataStreamType)
            {
                return CreateDataStream(instanceType, taskData);
            }

            if (genericTypeDefinition == cancellableDataStreamType)
            {
                return CreateCancellableDataStream(instanceType, taskData);
            }

            if (genericTypeDefinition == cancelResultDataStreamType)
            {
                return CreateCancelResultDataStream(instanceType, taskData);
            }

            throw new InvalidOperationException($"Trying to create a Data Stream with generic type definition of {genericTypeDefinition.GetReadableName()} but no code path satisfies!");
        }

        private static object CreateDataStream(Type instanceType, TaskData taskData)
        {
            MethodInfo createFunction = GetOrCreateMethod(instanceType, TYPED_GENERIC_DATA_STREAM_METHODS, PROTOTYPE_DATA_STREAM_METHOD);
            IUntypedDataStream dataStream = (IUntypedDataStream)createFunction.Invoke(null,
                                                                                      new object[]
                                                                                      {
                                                                                          taskData
                                                                                      });
            return dataStream;
        }

        private static object CreateTypedDataStream<TInstance>(TaskData taskData)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            DataStream<TInstance> dataStream = new DataStream<TInstance>(taskData.CancelRequestDataStream, taskData.TaskDriver, taskData.TaskSystem);
            taskData.RegisterDataStream(dataStream);
            return dataStream;
        }

        private static object CreateCancellableDataStream(Type instanceType, TaskData taskData)
        {
            MethodInfo createFunction = GetOrCreateMethod(instanceType, TYPED_GENERIC_CANCELLABLE_DATA_STREAM_METHODS, PROTOTYPE_CANCELLABLE_DATA_STREAM_METHOD);
            IUntypedCancellableDataStream dataStream = (IUntypedCancellableDataStream)createFunction.Invoke(null,
                                                                                                            new object[]
                                                                                                            {
                                                                                                                taskData
                                                                                                            });
            return dataStream;
        }

        private static object CreateTypedCancellableDataStream<TInstance>(TaskData taskData)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            CancellableDataStream<TInstance> dataStream = new CancellableDataStream<TInstance>(taskData.CancelRequestDataStream, taskData.TaskDriver, taskData.TaskSystem);
            taskData.RegisterDataStream(dataStream);
            return dataStream;
        }

        private static object CreateCancelResultDataStream(Type instanceType, TaskData taskData)
        {
            MethodInfo createFunction = GetOrCreateMethod(instanceType, TYPED_GENERIC_CANCEL_RESULT_DATA_STREAM_METHODS, PROTOTYPE_CANCEL_RESULT_DATA_STREAM_METHOD);
            IUntypedCancelResultDataStream dataStream = (IUntypedCancelResultDataStream)createFunction.Invoke(null,
                                                                                                              new object[]
                                                                                                              {
                                                                                                                  taskData
                                                                                                              });
            return dataStream;
        }

        private static object CreateTypedCancelResultDataStream<TInstance>(TaskData taskData)
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
