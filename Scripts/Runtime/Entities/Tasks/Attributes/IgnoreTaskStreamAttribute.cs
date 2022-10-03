using System;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// An attribute which will ignore auto-generation of the <see cref="TaskStream{TInstance}"/> if present.
    /// </summary>
    //TODO: #63 - Expand support to Properties
    [AttributeUsage(AttributeTargets.Field)]
    public class IgnoreTaskStreamAttribute : Attribute
    {
    }
}
