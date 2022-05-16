using System;
using Anvil.CSharp.Logging;
using Unity.Collections;
using Anvil.Unity.Logging;

namespace Anvil.Unity.DOTS.Logging
{
    [BurstCompatible]
    public readonly struct BurstableLogger<PrefixStringType> where PrefixStringType : struct, INativeList<byte>, IUTF8Bytes
    {
        public readonly PrefixStringType MessagePrefix;

        [NotBurstCompatible]
        public BurstableLogger(Log.Logger logger, string appendToMessagePrefix)
        {
            string managedMessagePrefix = logger.MessagePrefix + appendToMessagePrefix;
            MessagePrefix = default;
            CopyError error = FixedStringMethods.CopyFrom(ref MessagePrefix, managedMessagePrefix);

            switch (error)
            {
                case CopyError.None:
                    break;

                case CopyError.Truncation:
                    logger.Error($"The message prefix is too long for this {nameof(BurstableLogger<PrefixStringType>)} and will be truncated (MaxLength:{MessagePrefix.Capacity} PrefixLength:{managedMessagePrefix.Length}). Select a different larger prefix string type for this logger.");
                    break;

                default:
                    throw new Exception("Unknown message prefix error: " + error);
            }
        }

        public BurstableLogger(in PrefixStringType messagePrefix)
        {
            MessagePrefix = messagePrefix;
        }

        [UnityLogListener.Exclude]
        public void Debug(in FixedString64Bytes message)
        {
            AssertMessageLength(message);
            UnityEngine.Debug.Log($"{MessagePrefix}{message}");
        }

        [UnityLogListener.Exclude]
        public void Debug<T>(in T message) where T : struct, INativeList<byte>, IUTF8Bytes
        {
            AssertMessageLength(message);
            UnityEngine.Debug.Log($"{MessagePrefix}{message}");
        }

        [UnityLogListener.Exclude]
        public void Warning(in FixedString64Bytes message)
        {
            AssertMessageLength(message);
            UnityEngine.Debug.LogWarning($"{MessagePrefix}{message}");
        }

        [UnityLogListener.Exclude]
        public void Warning<T>(in T message) where T : struct, INativeList<byte>, IUTF8Bytes
        {
            AssertMessageLength(message);
            UnityEngine.Debug.LogWarning($"{MessagePrefix}{message}");
        }

        [UnityLogListener.Exclude]
        public void Error(in FixedString64Bytes message)
        {
            AssertMessageLength(message);
            UnityEngine.Debug.LogError($"{MessagePrefix}{message}");
        }

        [UnityLogListener.Exclude]
        public void Error<T>(in T message) where T : struct, INativeList<byte>, IUTF8Bytes
        {
            AssertMessageLength(message);
            UnityEngine.Debug.LogError($"{MessagePrefix}{message}");
        }

        private void AssertMessageLength<T>(in T message) where T : struct, INativeList<byte>, IUTF8Bytes
        {
#if UNITY_ASSERTIONS
            //Manually assert since Debug.Assert doesn't provide any useful information and does not support custom messages.
            if (message.Length == message.Capacity)
            {
                UnityEngine.Debug.LogError($"The next logged message is too long and will be truncated. Consider using a larger FixedString type. MaxLength: {message.Capacity}");
            }

            if (message.Length + MessagePrefix.Length > FixedString4096Bytes.UTF8MaxLengthInBytes)
            {
                UnityEngine.Debug.LogError($"The next MessagePrefix + Message is larger than the largest fixed string({FixedString4096Bytes.UTF8MaxLengthInBytes}) and will be truncated. MessageLength:{message.Length} MessagePrefix: {MessagePrefix.Length}");
            }
#endif
        }
    }
}
