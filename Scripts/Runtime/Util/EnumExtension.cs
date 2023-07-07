using System;
using System.Diagnostics;
using Unity.Burst;
using Debug = UnityEngine.Debug;

namespace Anvil.Unity.DOTS.Util
{
    /// <summary>
    /// Extension methods that help deal with enums in a DOTS project.
    /// </summary>
    public static class EnumExtension
    {
        public static int ToBurstInt<TEnum>(this TEnum value)
            where TEnum : unmanaged, Enum
        {
            return (int)value.ToBurstValue();
        }

        /// <summary>
        /// Converts an enum to a value that burst will actually print out.
        /// Without this method enums print out their fully qualified type name in burst compiled code.
        /// </summary>
        /// <param name="value">The enum value.</param>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <returns>A long representing the value of the enum.</returns>
        /// <exception cref="Exception">Thrown when the backing type is not <see cref="byte"/> or unsigned.</exception>
        /// <remarks>
        /// Inspired By: <see cref="Unity.MemoryProfiler.Editor.Extensions.EnumExtension.GetValueUnsigned"/>
        /// This method is not required for Burst >=1.8.3 as the issue was been fixed.
        /// https://docs.unity3d.com/Packages/com.unity.burst@1.8/changelog/CHANGELOG.html#fixed-1
        /// TODO: #86 - When upgrading to ECS 1.0
        /// </remarks>
        public static long ToBurstValue<TEnum>(this TEnum value)
            where TEnum : unmanaged, Enum
        {
            AssertIsByteOrSignedEnum<TEnum>();
            unsafe
            {
                return sizeof(TEnum) switch
                {
                    1 => (*(byte*)(&value)),
                    2 => (*(short*)(&value)),
                    4 => (*(int*)(&value)),
                    8 => (*(long*)(&value)),
                    // Should never happen unless C# adds new types that enum supports.
                    _ => throw new Exception("Size does not match a known Enum backing type.")
                };
            }
        }

        [BurstDiscard]
        [Conditional("DEBUG")]
        private static void AssertIsByteOrSignedEnum<TEnum>() where TEnum : Enum
        {
            Type underlyingType = Enum.GetUnderlyingType(typeof(TEnum));
            if (underlyingType == typeof(byte)
                || underlyingType == typeof(short)
                || underlyingType == typeof(int)
                || underlyingType == typeof(long))
            {
                return;
            }

            Debug.LogWarning($"{nameof(ToBurstValue)}() only supports enums that derive from {nameof(Byte)} or signed types. Values emitted are the bytes interpreted as a signed value and won't match the values defined on the type if larger than their signed counterpart's max value.");
        }
    }
}