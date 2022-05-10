using System;
using System.Reflection;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Util
{
    public static class EntityQueryExtensions
    {
        private delegate ComponentType[] GetReadAndWriteTypesDelegate(ref EntityQuery entityQuery);
        
        private static readonly MethodInfo s_EntityQuery_GetReadAndWriteTypes_MethodInfo = typeof(EntityQuery).GetMethod("GetReadAndWriteTypes", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly GetReadAndWriteTypesDelegate s_GetReadAndWriteTypes = (GetReadAndWriteTypesDelegate)Delegate.CreateDelegate(typeof(GetReadAndWriteTypesDelegate), s_EntityQuery_GetReadAndWriteTypes_MethodInfo);
        

        public static ComponentType[] GetReadWriteComponentTypes(this EntityQuery entityQuery)
        {
            ComponentType[] componentTypes = s_GetReadAndWriteTypes(ref entityQuery);
            return componentTypes;
        }
    }
}
