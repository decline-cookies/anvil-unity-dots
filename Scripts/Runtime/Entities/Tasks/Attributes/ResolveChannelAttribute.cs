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

        public ResolveChannelAttribute(object resolveChannel)
        {
            ResolveChannel = resolveChannel;
            ResolveChannelUtil.Debug_EnsureEnumValidity(resolveChannel);
        }
    }
}
