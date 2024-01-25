using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    public static class ComponentDependencyManagerInternalIntegrity
    {
        private static readonly Type s_ComponentDependencyManagerType = typeof(ComponentDependencyManager);
        private static readonly FieldInfo s_FieldInfo_m_TypeArrayIndices = s_ComponentDependencyManagerType.GetField("m_TypeArrayIndices", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo s_FieldInfo_m_DependencyHandles = s_ComponentDependencyManagerType.GetField("m_DependencyHandles", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo s_FieldInfo_kMaxReadJobHandles = s_ComponentDependencyManagerType.GetField("kMaxReadJobHandles", BindingFlags.Static | BindingFlags.NonPublic);
        private static readonly int s_FieldOffset_m_TypeArrayIndices = UnsafeUtility.GetFieldOffset(s_FieldInfo_m_TypeArrayIndices);
        private static readonly int s_FieldOffset_m_DependencyHandles = UnsafeUtility.GetFieldOffset(s_FieldInfo_m_DependencyHandles);


        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            StructLayoutAttribute structLayoutAttribute = typeof(ComponentDependencyManager).StructLayoutAttribute;
            Debug.Assert(structLayoutAttribute is
            {
                Value: LayoutKind.Sequential
            }, $"Type {typeof(ComponentDependencyManager)} must be sequential layout");

            int maxReadHandles = (int)s_FieldInfo_kMaxReadJobHandles.GetValue(null);
            if (maxReadHandles != ComponentDependencyManagerInternal.MAX_READ_HANDLES)
            {
                throw new InvalidProgramException($"{typeof(ComponentDependencyManager)} has changed it's inner layout. Please update {nameof(ComponentDependencyManagerInternal.MAX_READ_HANDLES)} to match {maxReadHandles}.");
            }

            if (s_FieldOffset_m_TypeArrayIndices != ComponentDependencyManagerInternal.FIELD_OFFSET_TYPE_ARRAY_INDICES)
            {
                throw new InvalidProgramException($"{typeof(ComponentDependencyManager)} has changed it's inner layout. Please update {nameof(ComponentDependencyManagerInternal.FIELD_OFFSET_TYPE_ARRAY_INDICES)} to match {s_FieldOffset_m_TypeArrayIndices}.");
            }
            if (s_FieldOffset_m_DependencyHandles != ComponentDependencyManagerInternal.FIELD_OFFSET_DEPENDENCY_HANDLES)
            {
                throw new InvalidProgramException($"{typeof(ComponentDependencyManager)} has changed it's inner layout. Please update {nameof(ComponentDependencyManagerInternal.FIELD_OFFSET_DEPENDENCY_HANDLES)} to match {s_FieldOffset_m_DependencyHandles}.");
            }



            Type innerDependencyHandleType = s_ComponentDependencyManagerType.GetNestedType("DependencyHandle", BindingFlags.Instance | BindingFlags.NonPublic);
            Type ourDependencyHandleType = typeof(ComponentDependencyManagerInternal.DependencyHandle);

            FieldInfo[] innerFields = innerDependencyHandleType.GetFields();
            FieldInfo[] ourFields = ourDependencyHandleType.GetFields();

            if (innerFields.Length != ourFields.Length)
            {
                throw new InvalidProgramException($"{typeof(ComponentDependencyManager)} has changed it's inner DependencyHandle type. Please update {nameof(ComponentDependencyManagerInternal.DependencyHandle)} to match.");
            }

            for (int i = 0; i < innerFields.Length; ++i)
            {
                FieldInfo innerField = innerFields[i];
                FieldInfo ourField = ourFields[i];

                if (innerField.Name != ourField.Name
                 || innerField.FieldType != ourField.FieldType
                 || UnsafeUtility.GetFieldOffset(innerField) != UnsafeUtility.GetFieldOffset(ourField))
                {
                    throw new InvalidProgramException($"{typeof(ComponentDependencyManager)} has changed it's inner DependencyHandle type's fields. Field: {innerField.Name}. Please update {nameof(ComponentDependencyManagerInternal.DependencyHandle)} to match.");
                }
            }
        }
    }
}