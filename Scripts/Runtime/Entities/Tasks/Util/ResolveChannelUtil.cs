using System;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities
{
    internal static class ResolveChannelUtil
    {
        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void Debug_EnsureEnumValidity<TResolveChannel>(TResolveChannel resolveChannel)
            where TResolveChannel : Enum
        {
            Debug_EnsureEnumIsSizedProperly(typeof(TResolveChannel));
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void Debug_EnsureEnumValidity(object resolveChannel)
        {
            Type type = resolveChannel.GetType();
            if (!type.IsEnum)
            {
                throw new InvalidOperationException($"Resolve Channel Type is {type} but needs to be a {typeof(Enum)}");
            }

            Debug_EnsureEnumIsSizedProperly(type);
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void Debug_EnsureEnumIsSizedProperly(Type type)
        {
            int sizeOfType = UnsafeUtility.SizeOf(type);
            int sizeOfByte = UnsafeUtility.SizeOf<byte>();
            
            if (sizeOfType != sizeOfByte)
            {
                throw new InvalidOperationException($"Resolve Channel Enum is of size {sizeOfType} bytes but needs to be the size of a {typeof(byte)} or {sizeOfByte} byte");
            }
        }
    }
}
