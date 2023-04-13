using System;
using Anvil.CSharp.Logging;
using Unity.Collections;
using Anvil.Unity.Logging;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Logging
{
    /// <summary>
    /// A context specific instance that provides a mechanism to emit logs with minimum boilerplate.
    /// Automatically decorates log messages with contextual information including:
    ///  - Optional, per instance, message prefix
    /// </summary>
    /// <typeparam name="TPrefixStringType">
    /// The FixedString type to use for the message prefix
    /// Select the shortest fixed string that will fit the prefix length.
    /// (usually <see cref="FixedString32Bytes" />)
    /// </typeparam>
    /// <remarks>
    /// NOTE: Unlike <see cref="Logger"/> this logger is thread safe when burst compilation is enabled.
    /// This is only possible because of the unique path that logs take through <see cref="UnityLogListener"/>
    /// from a burst context.
    /// TODO: #95
    ///
    /// In the future, the goal of this type is to provide all the same information that
    /// <see cref="Logger" /> provides. That is not possible with Burst today but should be
    /// possible in the future without making any changes at the consuming end. At the moment, the
    /// contextual information is added when the message passes through <see cref="UnityLogListener"/>.
    /// This information will appear in the output of all active <see cref="AbstractLogHandler" /> implementations.
    /// except for the Editor console (<see cref="UnityLogHandler"/>).
    /// This is a Burst limitation.
    /// </remarks>
    [BurstCompatible]
    public readonly struct BurstableLogger<TPrefixStringType> where TPrefixStringType : struct, INativeList<byte>, IUTF8Bytes
    {
        private const int UNSET_THREAD_INDEX = -1;

        /// <summary>
        /// The custom prefix to prepend to all messages sent through this instance.
        /// </summary>
        public readonly TPrefixStringType MessagePrefix;

        [NativeSetThreadIndex] private readonly int m_ThreadIndex;

        /// <summary>
        /// Creates an instance of <see cref="BurstableLogger{PrefixStringType}"/> from a
        /// <see cref="Logger" /> instance.
        /// </summary>
        /// <param name="logger">The <see cref="Logger" /> to copy configuration from.</param>
        /// <param name="appendToMessagePrefix">
        /// A string to append to the <see cref="Logger" />'s existing prefix.
        /// </param>
        /// <exception cref="Exception">
        /// Thrown if there is an unknown error encountered when configuring the prefix string.
        /// </exception>
        [NotBurstCompatible]
        public BurstableLogger(Logger logger, string appendToMessagePrefix)
        {
            m_ThreadIndex = UNSET_THREAD_INDEX;
            string managedMessagePrefix = logger.MessagePrefix + appendToMessagePrefix;
            MessagePrefix = default;

            CopyError error = MessagePrefix.CopyFrom(managedMessagePrefix);


            switch (error)
            {
                case CopyError.None:
                    break;

                case CopyError.Truncation:
                    logger.Error($"The message prefix is too long for this {nameof(BurstableLogger<TPrefixStringType>)} and will be truncated (MaxLength:{MessagePrefix.Capacity} PrefixLength:{managedMessagePrefix.Length}). Select a different larger prefix string type for this logger.");
                    break;

                default:
                    throw new Exception("Unknown message prefix error: " + error);
            }
        }

        /// <summary>
        /// Creates an instance of <see cref="BurstableLogger{PrefixStringType}"/>.
        /// </summary>
        /// <param name="messagePrefix">The custom prefix to prepend to all messages sent through this instance.</param>
        public BurstableLogger(in TPrefixStringType messagePrefix)
        {
            m_ThreadIndex = UNSET_THREAD_INDEX;
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
            Debug<FixedString64Bytes>(message);
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
            if (m_ThreadIndex == UNSET_THREAD_INDEX)
            {
                UnityEngine.Debug.Log($"{MessagePrefix}{message}");
            }
            else
            {
                UnityEngine.Debug.Log($"(Thread:{m_ThreadIndex}) {MessagePrefix}{message}");
            }
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
            Warning<FixedString64Bytes>(message);
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
            if (m_ThreadIndex == UNSET_THREAD_INDEX)
            {
                UnityEngine.Debug.LogWarning($"{MessagePrefix}{message}");
            }
            else
            {
                UnityEngine.Debug.LogWarning($"(Thread:{m_ThreadIndex}) {MessagePrefix}{message}");
            }
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
            Error<FixedString64Bytes>(message);
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
            if (m_ThreadIndex == UNSET_THREAD_INDEX)
            {
                UnityEngine.Debug.LogError($"{MessagePrefix}{message}");
            }
            else
            {
                UnityEngine.Debug.LogError($"(Thread:{m_ThreadIndex}) {MessagePrefix}{message}");
            }
        }

        private void AssertMessageLength<T>(in T message) where T : struct, INativeList<byte>, IUTF8Bytes
        {
#if UNITY_ASSERTIONS
            //Manually assert since Debug.Assert doesn't provide any useful information and does not support custom messages.

            // Assume that message that was filled to capacity was truncated. It falsely errors when message length was
            // exactly equal to capacity but it's the best we can do.
            if (message.Length == message.Capacity)
            {
                UnityEngine.Debug.LogError($"The next logged message is too long and will be truncated. Consider using a larger FixedString type. MessageLength:{message.Length}, MaxLength: {message.Capacity}");
            }

            if (message.Length + MessagePrefix.Length > FixedString4096Bytes.UTF8MaxLengthInBytes)
            {
                UnityEngine.Debug.LogError($"The next MessagePrefix + Message is larger than the largest fixed string({FixedString4096Bytes.UTF8MaxLengthInBytes}) and will be truncated. MessageLength:{message.Length}, MessagePrefix: {MessagePrefix.Length}");
            }
#endif
        }
    }
}