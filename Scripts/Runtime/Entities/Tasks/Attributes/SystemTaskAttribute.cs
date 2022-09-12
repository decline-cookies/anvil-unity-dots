using System;
using System.Reflection;

namespace Anvil.Unity.DOTS.Entities
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SystemTaskAttribute : Attribute
    {
        internal readonly Action<IUpdateJobConfig> ConfigurationFunction;

        public SystemTaskAttribute(Action<IUpdateJobConfig>  configurationFunction)
        {
            ConfigurationFunction = configurationFunction;
        }
    }
}
