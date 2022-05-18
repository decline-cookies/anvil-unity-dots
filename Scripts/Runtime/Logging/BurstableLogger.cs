using System;
using Anvil.CSharp.Logging;
using Unity.Collections;
using Anvil.Unity.Logging;

namespace Anvil.Unity.DOTS.Logging
{
    /// <summary>
    /// A context specific instance that provides a mechanism to emit logs with minimum boilerplate.
    /// Automatically decorates log messages with contextual information including:
    ///  - Optional, per instance, message prefix
    /// </summary>
    /// <typeparam name="PrefixStringType">
    /// The FixedString type to use for the message prefix
    /// Select the shortest fixed string that will fit the prefix length.
    /// (usually <see cref="FixedString32Bytes" />)
    /// </typeparam>
    /// <remarks>
    /// In the future, the goal of this type is to provide all the same information that 
    /// <see cref="Log.Logger" /> provides. That is not possible with Burst today but should be 
    /// possible in the future without making any changes at the consuming end. At the moment, the 
    /// contextual information is added when the message passes through <see cref="UnityLogListener"/>.
    /// This information will appear in the output of all active <see cref="ILogHandler" /> implementations.
    /// except for the Editor console (<see cref="UnityLogHandler"/>).
    /// This is a Burst limitation.
    /// </remarks>
    [BurstCompatible]
    public readonly struct BurstableLogger<PrefixStringType> where PrefixStringType : struct, INativeList<byte>, IUTF8Bytes
    {
        /// <summary>
        /// The custom prefix to prepend to all messages sent through this instance.
        /// </summary>
        public readonly PrefixStringType MessagePrefix;

        /// <summary>
        /// Creates an instance of <see cref="BurstableLogger{PrefixStringType}"/> from a 
        /// <see cref="Log.Logger" /> instance.
        /// </summary>
        /// <param name="logger">The <see cref="Log.Logger" /> to copy configuration from.</param>
        /// <param name="appendToMessagePrefix">
        /// A string to append to the <see cref="Log.Logger" />'s existing prefix.
        /// </param>
        /// <exception cref="Exception">
        /// Thrown if there is an unknown error encountered when configuring the prefix string.
        /// </exception>
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

        /// <summary>
        /// Creates an instance of <see cref="BurstableLogger{PrefixStringType}"/>.
        /// </summary>
        /// <param name="messagePrefix">The custom prefix to prepend to all messages sent through this instance.</param>
        public BurstableLogger(in PrefixStringType messagePrefix)
        {
            MessagePrefix = messagePrefix;
        }

        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <param name="message">
        /// The message to log. (max length: <see cref="FixedString64Bytes.Capacity"/>)
        /// </param>
        [UnityLogListener.Exclude]
        public void Debug(in FixedString64Bytes message)
        {
            AssertMessageLength(message);
            UnityEngine.Debug.Log($"{MessagePrefix}{message}");
        }

        /// <summary>
        /// Logs a message.
        /// </summary>
        /// <typeparam name="T">
        /// The FixedString type for the message
        /// Select the shortest fixed string that will fit the message length.
        /// </typeparam>
        /// <param name="message">
        /// The message to log. (max length: <see cref="T.Capacity"/>)
        /// </param>
        [UnityLogListener.Exclude]
        public void Debug<T>(in T message) where T : struct, INativeList<byte>, IUTF8Bytes
        {
            AssertMessageLength(message);
            UnityEngine.Debug.Log($"{MessagePrefix}{message}");
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <param name="message">
        /// The message to log. (max length: <see cref="FixedString64Bytes.Capacity"/>)
        /// </param>
        [UnityLogListener.Exclude]
        public void Warning(in FixedString64Bytes message)
        {
            AssertMessageLength(message);
            UnityEngine.Debug.LogWarning($"{MessagePrefix}{message}");
        }

        /// <summary>
        /// Logs a warning message.
        /// </summary>
        /// <typeparam name="T">
        /// The FixedString type for the message
        /// Select the shortest fixed string that will fit the message length.
        /// </typeparam>
        /// <param name="message">
        /// The message to log. (max length: <see cref="T.Capacity"/>)
        /// </param>
        [UnityLogListener.Exclude]
        public void Warning<T>(in T message) where T : struct, INativeList<byte>, IUTF8Bytes
        {
            AssertMessageLength(message);
            UnityEngine.Debug.LogWarning($"{MessagePrefix}{message}");
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <param name="message">
        /// The message to log. (max length: <see cref="FixedString64Bytes.Capacity"/>)
        /// </param>
        [UnityLogListener.Exclude]
        public void Error(in FixedString64Bytes message)
        {
            AssertMessageLength(message);
            UnityEngine.Debug.LogError($"{MessagePrefix}{message}");
        }

        /// <summary>
        /// Logs an error message.
        /// </summary>
        /// <typeparam name="T">
        /// The FixedString type for the message
        /// Select the shortest fixed string that will fit the message length.
        /// </typeparam>
        /// <param name="message">
        /// The message to log. (max length: <see cref="T.Capacity"/>)
        /// </param>
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
