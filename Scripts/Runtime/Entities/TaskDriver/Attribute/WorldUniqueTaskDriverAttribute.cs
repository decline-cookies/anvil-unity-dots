using System;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    /// <summary>
    /// Constrains an <see cref="AbstractTaskDriver"/> to a single instance per world.
    /// </summary>
    /// <remarks>
    /// This is enforced by the <see cref="AbstractTaskDriverSystem"/> when registering the
    /// <see cref="AbstractTaskDriver"/> instance.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class WorldUniqueTaskDriverAttribute : Attribute
    {
        public WorldUniqueTaskDriverAttribute() { }
    }
}