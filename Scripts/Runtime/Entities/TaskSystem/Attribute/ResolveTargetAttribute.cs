using System;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// An attribute which will register the corresponding <see cref="TaskStream{TInstance}"/> as a target
    /// that Update or Cancel jobs can write to.
    /// </summary>
    //TODO: #63 - Expand to support Properties
    [AttributeUsage(AttributeTargets.Field)]
    public class ResolveTargetAttribute : Attribute
    {
    }
}
