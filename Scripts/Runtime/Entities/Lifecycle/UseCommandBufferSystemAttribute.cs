using Anvil.CSharp.Logging;
using System;
using Unity.Entities;

namespace Anvil.Unity.DOTS.Entities
{
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct)]
    public class UseCommandBufferSystemAttribute : Attribute
    {
        private static readonly Type COMMAND_BUFFER_SYSTEM_TYPE = typeof(EntityCommandBufferSystem);
        
        public Type CommandBufferSystemType { get; }

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
