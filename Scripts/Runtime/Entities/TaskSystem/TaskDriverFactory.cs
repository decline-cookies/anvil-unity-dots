using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Unity.Entities;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal static class TaskDriverFactory
    {
        private static readonly Type ABSTRACT_TASK_DRIVER_TYPE = typeof(AbstractTaskDriver);
        private static readonly MethodInfo PROTOTYPE_METHOD = typeof(TaskDriverFactory).GetMethod(nameof(CreateTaskDriver), BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly Dictionary<Type, MethodInfo> TYPED_GENERIC_METHODS = new Dictionary<Type, MethodInfo>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            //TODO: #70 - Double check this works as expected in a build. https://github.com/decline-cookies/anvil-unity-dots/pull/58/files#r974334409
            TYPED_GENERIC_METHODS.Clear();
        }
        
        public static void CreateSubTaskDrivers(AbstractTaskDriver taskDriver, List<AbstractTaskDriver> subTaskDrivers)
        {
            Type type = taskDriver.GetType();
            FieldInfo[] taskDriverFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo field in taskDriverFields)
            {
                if (!ABSTRACT_TASK_DRIVER_TYPE.IsAssignableFrom(field.FieldType))
                {
                    continue;
                }
                
                Debug_CheckFieldIsReadOnly(field);

                AbstractTaskDriver subTaskDriver = Create(field.FieldType, taskDriver.World);
                subTaskDrivers.Add(subTaskDriver);

                Debug_EnsureFieldNotSet(field, taskDriver);
                
                field.SetValue(taskDriver, subTaskDriver);
            }
        }

        private static AbstractTaskDriver Create(Type taskDriverType, World world)
        {
            if (!TYPED_GENERIC_METHODS.TryGetValue(taskDriverType, out MethodInfo typedGenericMethod))
            {
                typedGenericMethod = PROTOTYPE_METHOD.MakeGenericMethod(taskDriverType);
                TYPED_GENERIC_METHODS.Add(taskDriverType, typedGenericMethod);
            }

            return (AbstractTaskDriver)typedGenericMethod.Invoke(null, new object[]{world});
        }

        private static TTaskDriver CreateTaskDriver<TTaskDriver>(World world)
            where TTaskDriver : AbstractTaskDriver
        {
            return (TTaskDriver)Activator.CreateInstance(typeof(TTaskDriver), world);
        }
        
        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void Debug_CheckFieldIsReadOnly(FieldInfo fieldInfo)
        {
            if (!fieldInfo.IsInitOnly)
            {
                throw new InvalidOperationException($"Field with name {fieldInfo.Name} on {fieldInfo.ReflectedType} is not marked as \"readonly\", please ensure that it is.");
            }
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void Debug_EnsureFieldNotSet(FieldInfo fieldInfo, object instance)
        {
            if (fieldInfo.GetValue(instance) != null)
            {
                throw new InvalidOperationException($"Field with name {fieldInfo.Name} on {fieldInfo.ReflectedType} is already set! Did you call {nameof(CreateSubTaskDrivers)} more than once?");
            }
        }
    }
}
