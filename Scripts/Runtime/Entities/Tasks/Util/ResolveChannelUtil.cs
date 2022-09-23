using System;
using System.Diagnostics;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities
{
    internal static class ResolveChannelUtil
    {
        public const byte CANCEL = byte.MaxValue;
        
        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void Debug_EnsureEnumValidity<TResolveChannel>(TResolveChannel resolveChannel)
            where TResolveChannel : Enum
        {
            Debug_EnsureEnumIsSizedProperly(typeof(TResolveChannel));
            Debug_EnsureValueIsValid(resolveChannel);
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
            Debug_EnsureValueIsValid(resolveChannel);
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

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void Debug_EnsureValueIsValid(object resolveChannel)
        {
            byte value = UnsafeUtility.As<object, byte>(ref resolveChannel);
            Debug_EnsureValueIsValid(value);
        }
        
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void Debug_EnsureValueIsValid<TResolveChannel>(TResolveChannel resolveChannel)
            where TResolveChannel : Enum
        {
            byte value = UnsafeUtility.As<TResolveChannel, byte>(ref resolveChannel);
            Debug_EnsureValueIsValid(value);
        }

        private static void Debug_EnsureValueIsValid(byte value)
        {
            if (value >= CANCEL)
            {
                throw new InvalidOperationException($"Resolve Channel Enum is set to {value} but that is a reserved value for signaling Cancelling. Please choose another value.");
            }
        }
    }
}
