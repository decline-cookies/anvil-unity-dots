using System;
using System.Reflection;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Util
{
    /// <summary>
    /// Helper extensions when working with <see cref="EntityQuery"/>s.
    /// </summary>
    public static class EntityQueryExtensions
    {
        private delegate ComponentType[] GetReadAndWriteTypesDelegate(ref EntityQuery entityQuery);
        
        //Delegates are MUCH faster than Invoking on the MethodInfo.
        private static readonly MethodInfo s_EntityQuery_GetReadAndWriteTypes_MethodInfo = typeof(EntityQuery).GetMethod("GetReadAndWriteTypes", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly GetReadAndWriteTypesDelegate s_GetReadAndWriteTypes = (GetReadAndWriteTypesDelegate)Delegate.CreateDelegate(typeof(GetReadAndWriteTypesDelegate), s_EntityQuery_GetReadAndWriteTypes_MethodInfo);
        
        
        /// <summary>
        /// Exposes the internal function on an <see cref="EntityQuery"/> to give the
        /// <see cref="ComponentType"/> that will read and/or write.
        /// </summary>
        /// <param name="entityQuery">The <see cref="EntityQuery"/> instance to get the types from.</param>
        /// <returns>An <see cref="ComponentType"/> array of types that will read and/or write</returns>
        public static ComponentType[] GetReadWriteComponentTypes(this EntityQuery entityQuery)
        {
            ComponentType[] componentTypes = s_GetReadAndWriteTypes(ref entityQuery);
            return componentTypes;
        }
    }
}
