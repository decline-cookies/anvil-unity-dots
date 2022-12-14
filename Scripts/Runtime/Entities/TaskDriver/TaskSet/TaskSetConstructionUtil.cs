using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal static class TaskSetConstructionUtil
    {
        private static readonly Type TASK_DRIVER_TYPE = typeof(AbstractTaskDriver);
        private static readonly Type I_DRIVER_DATA_STREAM = typeof(IDriverDataStream<>);
        private static readonly Type I_SYSTEM_DATA_STREAM = typeof(ISystemDataStream<>);
        private static readonly Type I_ENTITY_PROXY_INSTANCE_TYPE = typeof(IEntityProxyInstance);
        private static readonly MethodInfo PROTOTYPE_CREATE_DATA_STREAM_METHOD = typeof(TaskSet).GetMethod(nameof(TaskSet.CreateDataStream), BindingFlags.Instance | BindingFlags.Public);

        private static readonly HashSet<Type> VALID_DATA_STREAM_TYPES = new HashSet<Type>()
        {
            I_DRIVER_DATA_STREAM, I_SYSTEM_DATA_STREAM
        };

        public static TaskSet CreateTaskSetForTaskDriver(AbstractTaskDriver taskDriver)
        {
            GetDataStreamFieldsFromTaskDriverType(taskDriver.GetType(),
                                                  out List<FieldInfo> driverDataStreamFields,
                                                  out List<FieldInfo> systemDataStreamFields);

            TaskSet taskSet = new TaskSet(taskDriver);

            //For all the driver field types we create and assign.
            foreach (FieldInfo field in driverDataStreamFields)
            {
                AbstractDataStream dataStream = CreateDataStream(field, taskSet);
                AssignDataStreamToTaskDriverField(field, taskDriver, dataStream);
            }

            //For all the system field types, we get the data from the system and assign.
            TaskSet systemTaskSet = ((ITaskSetOwner)taskDriver).TaskDriverSystem.TaskSet;
            foreach (FieldInfo field in systemDataStreamFields)
            {
                Type instanceType = field.FieldType.GenericTypeArguments[0];
                AbstractDataStream dataStream = systemTaskSet.GetDataStreamByType(instanceType);
                AssignDataStreamToTaskDriverField(field, taskDriver, dataStream);
            }
            
            return taskSet;
        } 

        public static TaskSet CreateTaskSetForTaskSystem(AbstractTaskDriverSystem taskDriverSystem)
        {
            GetDataStreamFieldsFromTaskDriverType(taskDriverSystem.TaskDriverType,
                                                  out List<FieldInfo> driverDataStreamFields,
                                                  out List<FieldInfo> systemDataStreamFields);

            TaskSet taskSet = new TaskSet(taskDriverSystem);

            //For a system, we only care about the System Data Fields and we just need to create the streams
            foreach (FieldInfo field in systemDataStreamFields)
            {
                CreateDataStream(field, taskSet);
            }

            return taskSet;
        }

        private static void AssignDataStreamToTaskDriverField(FieldInfo fieldInfo, AbstractTaskDriver taskDriver, AbstractDataStream dataStream)
        {
            Debug_EnsureFieldNotSet(fieldInfo, taskDriver);
            fieldInfo.SetValue(taskDriver, dataStream);
        }

        private static AbstractDataStream CreateDataStream(FieldInfo fieldInfo, TaskSet taskSet)
        {
            CancelBehaviour cancelBehaviour = GetCancelBehaviourForDataStream(fieldInfo);
            Type instanceType = fieldInfo.FieldType.GenericTypeArguments[0];
            MethodInfo typedMethod = PROTOTYPE_CREATE_DATA_STREAM_METHOD.MakeGenericMethod(instanceType);
            AbstractDataStream dataStream = (AbstractDataStream)typedMethod.Invoke(taskSet,
                                                                                   new object[]
                                                                                   {
                                                                                       taskSet.TaskSetOwner, cancelBehaviour
                                                                                   });
            return dataStream;
        }

        private static CancelBehaviour GetCancelBehaviourForDataStream(MemberInfo fieldInfo)
        {
            DataStreamCancelBehaviourAttribute cancelBehaviourAttribute = fieldInfo.GetCustomAttribute<DataStreamCancelBehaviourAttribute>();
            return cancelBehaviourAttribute?.CancelBehaviour ?? CancelBehaviour.Default;
        }

        private static void GetDataStreamFieldsFromTaskDriverType(Type taskDriverType,
                                                                  out List<FieldInfo> driverDataStreamFields,
                                                                  out List<FieldInfo> systemDataStreamFields)
        {
            Debug_EnsureTaskDriverType(taskDriverType);

            driverDataStreamFields = new List<FieldInfo>();
            systemDataStreamFields = new List<FieldInfo>();

            FieldInfo[] fields = taskDriverType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo field in fields)
            {
                Type fieldType = field.FieldType;

                if (!IsDataStreamFieldValid(field))
                {
                    continue;
                }

                Type genericTypeDefinition = fieldType.GetGenericTypeDefinition();
                Type instanceType = fieldType.GenericTypeArguments[0];

                Debug_CheckInstanceType(instanceType);

                switch (genericTypeDefinition)
                {
                    case not null when genericTypeDefinition == I_DRIVER_DATA_STREAM:
                        driverDataStreamFields.Add(field);
                        break;
                    case not null when genericTypeDefinition == I_SYSTEM_DATA_STREAM:
                        systemDataStreamFields.Add(field);
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid Field with name {field.Name} on {field.ReflectedType} has generic type definition of {genericTypeDefinition} but no code path satisfies!");
                }
            }
        }

        private static bool IsDataStreamFieldValid(FieldInfo fieldInfo)
        {
            Type fieldType = fieldInfo.FieldType;
            //Can't create if it's not a generic type...
            if (!fieldType.IsGenericType)
            {
                return false;
            }

            //Must be one of the types we're allowed to auto create
            Type genericTypeDefinition = fieldType.GetGenericTypeDefinition();
            if (!VALID_DATA_STREAM_TYPES.Contains(genericTypeDefinition))
            {
                return false;
            }

            //If the field should be ignored and not auto-create...
            if (fieldInfo.GetCustomAttribute<IgnoreDataStreamAutoCreationAttribute>() != null)
            {
                return false;
            }

            Debug_CheckFieldIsReadOnly(fieldInfo);
            Debug_CheckFieldTypeGenericTypeArguments(fieldType);
            
            return true;
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void Debug_EnsureTaskDriverType(Type taskDriverType)
        {
            if (!TASK_DRIVER_TYPE.IsAssignableFrom(taskDriverType))
            {
                throw new InvalidOperationException($"Trying to parse fields on {taskDriverType} but it's not a {TASK_DRIVER_TYPE}!");
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
        private static void Debug_EnsureFieldNotSet(FieldInfo fieldInfo, AbstractTaskDriver taskDriver)
        {
            if (fieldInfo.GetValue(taskDriver) != null)
            {
                throw new InvalidOperationException($"Field with name {fieldInfo.Name} on {fieldInfo.ReflectedType} is already set! Did you call manually create it?");
            }
        }
    }
}
