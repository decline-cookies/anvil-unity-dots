using System;

namespace Anvil.Unity.DOTS.Entities
{
    //TODO: Maybe allow properties as well in the future? TaskFlowGraph will need updates to support that.
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class ResolveTargetForAttribute : Attribute
    {
        internal object ResolveTarget { get; }

        internal Type ResolveTargetEnumType { get; }

        //TODO: Once we get C# 10 support, change this to a generic Attribute 
        //TODO: Ex: public ResolveTargetAttribute<TResolveTarget>(TResolveTarget resolveTarget) where TResolveTarget : Enum
        public ResolveTargetForAttribute(object resolveTarget)
        {
            ResolveTarget = resolveTarget;
            ResolveTargetEnumType = resolveTarget.GetType();
            ResolveTargetUtil.Debug_EnsureEnumValidity(resolveTarget);
        }
    }
}
