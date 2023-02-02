using Anvil.CSharp.Logging;
using System;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    /// <summary>
    /// Attribute similar to <see cref="UpdateInGroupAttribute"/> but used to specify
    /// which <see cref="EntityCommandBufferSystem"/> should be used for a given system.
    /// </summary>
    /// <remarks>
    /// Runtime checks will enforce valid types.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct)]
    public class UseCommandBufferSystemAttribute : Attribute
    {
        private static readonly Type COMMAND_BUFFER_SYSTEM_TYPE = typeof(EntityCommandBufferSystem);
        
        /// <summary>
        /// The type of <see cref="EntityCommandBufferSystem"/> to use
        /// </summary>
        public Type CommandBufferSystemType { get; }

        /// <summary>
        /// Creates the attribute to define which <see cref="EntityCommandBufferSystem"/> to use.
        /// </summary>
        /// <param name="commandBufferSystemType">
        /// The type of <see cref="EntityCommandBufferSystem"/> to use
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown if a null type is passed in.</exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the type is not a <see cref="EntityCommandBufferSystem"/>
        /// </exception>
        public UseCommandBufferSystemAttribute(Type commandBufferSystemType)
        {
            if (commandBufferSystemType == null)
            {
                throw new ArgumentNullException($"Command Buffer System type must not be null!");
            }

            if (!COMMAND_BUFFER_SYSTEM_TYPE.IsAssignableFrom(commandBufferSystemType))
            {
                throw new InvalidOperationException($"Command Buffer System type of {commandBufferSystemType.GetReadableName()} is not a subclass of {COMMAND_BUFFER_SYSTEM_TYPE.GetReadableName()}!");
            }
            
            CommandBufferSystemType = commandBufferSystemType;
        }
    }
}
