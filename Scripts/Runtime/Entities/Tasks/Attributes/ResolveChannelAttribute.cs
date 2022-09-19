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
            //TODO: Safety
            Type type = ResolveChannel.GetType();
            if (!type.IsEnum)
            {
                throw new InvalidOperationException($"Resolve Channel Type is {type} but needs to be a {typeof(Enum)}");
            }
        }
    }
}
