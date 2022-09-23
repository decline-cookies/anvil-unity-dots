using System;

namespace Anvil.Unity.DOTS.Entities
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class ResolveChannelAttribute : Attribute
    {
        public object ResolveChannel
        {
            get;
        }
        
        //TODO: Once we get C# 10 support, change this to a generic Attribute 
        //TODO: Ex: public ResolveChannelAttribute<TResolveChannel>(TResolveChannel resolveChannel) where TResolveChannel : Enum
        public ResolveChannelAttribute(object resolveChannel)
        {
            ResolveChannel = resolveChannel;
            ResolveChannelUtil.Debug_EnsureEnumValidity(resolveChannel);
        }
    }
}
