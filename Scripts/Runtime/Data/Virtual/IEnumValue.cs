using System;

namespace Anvil.Unity.DOTS.Data
{
    public interface IEnumValue<TEnum>
        where TEnum : Enum
    {
        public TEnum Value
        {
            get;
        }    
    }
}
