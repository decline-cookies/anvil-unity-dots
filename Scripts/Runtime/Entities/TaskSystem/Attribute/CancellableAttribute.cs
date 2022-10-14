using System;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// An attribute which will register the corresponding <see cref="TaskStream{TInstance}"/> as data that
    /// should have it's own Cancel job to handle gracefully unwinding it's state.
    /// </summary>
    //TODO: #63 - Expand to support Properties
    [AttributeUsage(AttributeTargets.Field)]
    public class CancellableAttribute : Attribute
    {
        
    }
}
