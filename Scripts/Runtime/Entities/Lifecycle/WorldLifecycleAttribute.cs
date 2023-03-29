using Anvil.CSharp.Logging;
using System;

namespace Anvil.Unity.DOTS.Entities
{
    [AttributeUsage(AttributeTargets.Class)]
    public class WorldLifecycleAttribute : Attribute
    {
        private static readonly Type ABSTRACT_WORLD_ENTITY_LIFECYCLE_TYPE = typeof(AbstractEntityLifecycleStatusSystem);
        
        public readonly Type WorldLifecycleType;

        public WorldLifecycleAttribute(Type worldLifecycleType)
        {
            if (worldLifecycleType == null)
            {
                throw new ArgumentNullException($"World Lifecycle type must not be null!");
            }

            if (!ABSTRACT_WORLD_ENTITY_LIFECYCLE_TYPE.IsAssignableFrom(worldLifecycleType))
            {
                throw new InvalidOperationException($"World Lifecycle type of {worldLifecycleType.GetReadableName()} is not a subclass of {ABSTRACT_WORLD_ENTITY_LIFECYCLE_TYPE.GetReadableName()}!");
            }

            WorldLifecycleType = worldLifecycleType;
        }
    }
}
