using System;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// An attribute which will register the corresponding <see cref="TaskStream{TInstance}"/> as a target
    /// that Update or Cancel jobs can write to.
    /// </summary>
    //TODO: #63 - Expand to support Properties
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class ResolveTargetForAttribute : Attribute
    {
        internal object ResolveTarget { get; }

        internal Type ResolveTargetEnumType { get; }
        
        //TODO: #64 - Remove the enum and rename this.
        public ResolveTargetForAttribute(object resolveTarget)
        {
            ResolveTarget = resolveTarget;
            ResolveTargetEnumType = resolveTarget.GetType();
            ResolveTargetUtil.Debug_EnsureEnumValidity(resolveTarget);
        }
    }
}
