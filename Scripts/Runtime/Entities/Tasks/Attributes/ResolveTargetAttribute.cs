using System;

namespace Anvil.Unity.DOTS.Entities
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class ResolveTargetAttribute : Attribute
    {
        public object ResolveTarget
        {
            get;
        }
        
        //TODO: Once we get C# 10 support, change this to a generic Attribute 
        //TODO: Ex: public ResolveTargetAttribute<TResolveTarget>(TResolveTarget resolveTarget) where TResolveTarget : Enum
        public ResolveTargetAttribute(object resolveTarget)
        {
            ResolveTarget = resolveTarget;
            ResolveTargetUtil.Debug_EnsureEnumValidity(resolveTarget);
        }
    }
}
