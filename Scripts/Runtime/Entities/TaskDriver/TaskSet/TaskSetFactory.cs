using Anvil.CSharp.Data;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal static class TaskSetFactory
    {
        private static readonly FieldInfo TASK_DRIVER_SYSTEM_COMMON_TASK_SET_FIELD = typeof(AbstractTaskDriverSystem).GetField(nameof(AbstractTaskDriverSystem.CommonTaskSet), BindingFlags.Instance | BindingFlags.Public);
        private static readonly Type GENERIC_TASK_DRIVER_SYSTEM_TYPE = typeof(TaskDriverSystem<>);
        private static readonly Type I_CANCELLABLE_DATA_STREAM = typeof(ICancellableDataStream<>);
        private static readonly Type I_DATA_STREAM = typeof(IDataStream<>);
        private static readonly Type I_CANCEL_RESULT_DATA_STREAM = typeof(ICancelResultDataStream<>);
        private static readonly Type I_COMMON_CANCELLABLE_DATA_STREAM = typeof(ICommonCancellableDataStream<>);
        private static readonly Type I_COMMON_DATA_STREAM = typeof(ICommonDataStream<>);
        private static readonly Type I_COMMON_CANCEL_RESULT_DATA_STREAM = typeof(ICommonCancelResultDataStream<>);
        private static readonly Type I_ENTITY_PROXY_INSTANCE_TYPE = typeof(IEntityProxyInstance);
        private static readonly Dictionary<Type, MethodInfo> TYPED_GENERIC_CREATION_METHODS = new Dictionary<Type, MethodInfo>();
        private static readonly MethodInfo PROTOTYPE_CREATE_DATA_STREAM_METHOD = typeof(AbstractTaskSet).GetMethod(nameof(AbstractTaskSet.CreateDataStream), BindingFlags.Instance | BindingFlags.Public);
        private static readonly MethodInfo PROTOTYPE_CREATE_CANCELLABLE_DATA_STREAM_METHOD = typeof(AbstractTaskSet).GetMethod(nameof(AbstractTaskSet.CreateCancellableDataStream), BindingFlags.Instance | BindingFlags.Public);
        private static readonly MethodInfo PROTOTYPE_CREATE_CANCEL_RESULT_DATA_STREAM_METHOD = typeof(AbstractTaskSet).GetMethod(nameof(AbstractTaskSet.CreateCancelResultDataStream), BindingFlags.Instance | BindingFlags.Public);

        private static readonly HashSet<Type> COMMON_TASK_SET_DATA_STREAMS = new HashSet<Type>()
        {
            I_COMMON_DATA_STREAM, I_COMMON_CANCELLABLE_DATA_STREAM, I_COMMON_CANCEL_RESULT_DATA_STREAM
        };

        private static readonly HashSet<Type> TASK_SET_DATA_STREAMS = new HashSet<Type>()
        {
            I_DATA_STREAM, I_CANCELLABLE_DATA_STREAM, I_CANCEL_RESULT_DATA_STREAM
        };

        public static void CreateTaskSets(AbstractTaskDriver taskDriver)
        {
            World world = taskDriver.World;
            Type taskDriverType = taskDriver.GetType();
            Type taskDriverSystemType = GENERIC_TASK_DRIVER_SYSTEM_TYPE.MakeGenericType(taskDriverType);

            //Get the shared CoreTaskWork for this type
            AbstractTaskDriverSystem taskDriverSystem = (AbstractTaskDriverSystem)world.GetOrCreateSystem(taskDriverSystemType);

            CommonTaskSet commonTaskSet = GetOrCreateCommonTaskSet(world, taskDriverType, taskDriverSystem);
        }

        private static CommonTaskSet GetOrCreateCommonTaskSet(World world, Type taskDriverType, AbstractTaskDriverSystem taskDriverSystem)
        {
            if (taskDriverSystem.CommonTaskSet == null)
            {
                CreateCommonTaskSet(world, taskDriverType, taskDriverSystem);
            }

            return taskDriverSystem.CommonTaskSet;
        }

        private static void CreateCommonTaskSet(World world, Type taskDriverType, AbstractTaskDriverSystem taskDriverSystem)
        {
            CommonTaskSet commonTaskSet = new CommonTaskSet(world, taskDriverType, taskDriverSystem);

            GetValidFields(taskDriverType, out List<FieldInfo> commonTaskSetFields, out List<FieldInfo> taskSetFields);

            //Create the data streams for this task set
            CreateDataStreams(commonTaskSetFields, commonTaskSet);

            //Assign the value to the system via reflection
            TASK_DRIVER_SYSTEM_COMMON_TASK_SET_FIELD.SetValue(taskDriverSystem, commonTaskSet);
        }

        private static void GetValidFields(Type taskDriverType, out List<FieldInfo> commonTaskSetFields, out List<FieldInfo> taskSetFields)
        {
            commonTaskSetFields = new List<FieldInfo>();
            taskSetFields = new List<FieldInfo>();

            FieldInfo[] fields = taskDriverType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo field in fields)
            {
                Type fieldType = field.FieldType;
                //Can't create if it's not a generic type...
                if (!fieldType.IsGenericType)
                {
                    continue;
                }

                Type genericTypeDefinition = fieldType.GetGenericTypeDefinition();

                bool isCommonTaskSetDataStream = COMMON_TASK_SET_DATA_STREAMS.Contains(genericTypeDefinition);
                bool isTaskSetDataStream = TASK_SET_DATA_STREAMS.Contains(genericTypeDefinition);

                //If the field isn't a type we're looking for
                if (!isCommonTaskSetDataStream
                 && !isTaskSetDataStream)
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

                if (isCommonTaskSetDataStream)
                {
                    commonTaskSetFields.Add(field);
                }
                else if (isTaskSetDataStream)
                {
                    taskSetFields.Add(field);
                }
            }
        }

        private static void CreateDataStreams(List<FieldInfo> commonTaskSetFields, CommonTaskSet commonTaskSet)
        {
            foreach (FieldInfo commonTaskSetField in commonTaskSetFields)
            {
                AbstractDataStream dataStream = CreateDataStreamFromFieldInfo(commonTaskSetField, commonTaskSet);
            }
        }

        private static AbstractDataStream CreateDataStreamFromFieldInfo(FieldInfo fieldInfo, AbstractTaskSet taskSet)
        {
            Type fieldType = fieldInfo.FieldType;
            Type genericTypeDefinition = fieldType.GetGenericTypeDefinition();
            Type instanceType = fieldType.GenericTypeArguments[0];
            MethodInfo createFunction = GetOrCreateMethod(instanceType, genericTypeDefinition);

            AbstractDataStream dataStream = (AbstractDataStream)createFunction.Invoke(taskSet,
                                                                                      null);
            return dataStream;
        }

        private static MethodInfo GetOrCreateMethod(Type instanceType, Type genericTypeDefinition)
        {
            if (!TYPED_GENERIC_CREATION_METHODS.TryGetValue(instanceType, out MethodInfo typedGenericMethod))
            {
                MethodInfo methodInfo = null;
                if (genericTypeDefinition == I_DATA_STREAM
                 || genericTypeDefinition == I_COMMON_DATA_STREAM)
                {
                    methodInfo = PROTOTYPE_CREATE_DATA_STREAM_METHOD;
                }
                else if (genericTypeDefinition == I_CANCELLABLE_DATA_STREAM
                      || genericTypeDefinition == I_COMMON_CANCELLABLE_DATA_STREAM)
                {
                    methodInfo = PROTOTYPE_CREATE_CANCELLABLE_DATA_STREAM_METHOD;
                }
                else if (genericTypeDefinition == I_CANCEL_RESULT_DATA_STREAM
                      || genericTypeDefinition == I_COMMON_CANCEL_RESULT_DATA_STREAM)
                {
                    methodInfo = PROTOTYPE_CREATE_CANCEL_RESULT_DATA_STREAM_METHOD;
                }

                typedGenericMethod = methodInfo.MakeGenericMethod(instanceType);
                TYPED_GENERIC_CREATION_METHODS.Add(instanceType, typedGenericMethod);
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
        private static void Debug_EnsureFieldNotSet(FieldInfo fieldInfo, AbstractTaskDriver taskDriver)
        {
            if (fieldInfo.GetValue(taskDriver) != null)
            {
                throw new InvalidOperationException($"Field with name {fieldInfo.Name} on {fieldInfo.ReflectedType} is already set! Did you call manually create it?");
            }
        }
    }
}
