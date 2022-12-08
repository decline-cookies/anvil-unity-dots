using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Unity.Entities;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal static class TaskDriverConstructionUtil
    {
        private static readonly Type ABSTRACT_TASK_DRIVER_TYPE = typeof(AbstractTaskDriver);
        private static readonly MethodInfo PROTOTYPE_METHOD = typeof(TaskDriverConstructionUtil).GetMethod(nameof(CreateTaskDriver), BindingFlags.Static | BindingFlags.NonPublic);
        
        public static List<AbstractTaskDriver> CreateSubTaskDrivers(AbstractTaskDriver taskDriver)
        {
            List<AbstractTaskDriver> subTaskDrivers = new List<AbstractTaskDriver>();
            
            Type type = taskDriver.GetType();
            FieldInfo[] taskDriverFields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            foreach (FieldInfo field in taskDriverFields)
            {
                //If the field is not a TaskDriver, we'll skip
                if (!ABSTRACT_TASK_DRIVER_TYPE.IsAssignableFrom(field.FieldType))
                {
                    continue;
                }

                //If the field was marked to not be auto-created, we'll skip
                if (field.GetCustomAttribute<IgnoreTaskDriverAutoCreationAttribute>() != null)
                {
                    continue;
                }
                
                Debug_CheckFieldIsReadOnly(field);

                AbstractTaskDriver subTaskDriver = Create(field.FieldType, taskDriver);
                subTaskDrivers.Add(subTaskDriver);

                Debug_EnsureFieldNotSet(field, taskDriver);
                
                field.SetValue(taskDriver, subTaskDriver);
            }

            return subTaskDrivers;
        }

        private static AbstractTaskDriver Create(Type taskDriverType, AbstractTaskDriver parentTaskDriver)
        {
            MethodInfo creationMethod = PROTOTYPE_METHOD.MakeGenericMethod(taskDriverType);
            return (AbstractTaskDriver)creationMethod.Invoke(null, new object[]{parentTaskDriver.World, parentTaskDriver});
        }

        private static TTaskDriver CreateTaskDriver<TTaskDriver>(World world, AbstractTaskDriver parent)
            where TTaskDriver : AbstractTaskDriver
        {
            return (TTaskDriver)Activator.CreateInstance(typeof(TTaskDriver), world, parent);
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
