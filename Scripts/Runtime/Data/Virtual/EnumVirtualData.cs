using Anvil.CSharp.Core;
using System;
using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Data
{
    public class EnumVirtualData<TKey, TEnum> : AbstractAnvilBase
        where TKey : struct, IEquatable<TKey>
        where TEnum : Enum
    {
        private struct EnumWrapper : ILookupValue<TKey>
        {
            public TKey Key
            {
                get;
            }

            public TEnum Value
            {
                get;
            }

            public EnumWrapper(TKey key, TEnum value)
            {
                Key = key;
                Value = value;
            }
        }

        private readonly Dictionary<TEnum, VirtualData<TKey, EnumWrapper>> m_VirtualDataLookup = new Dictionary<TEnum, VirtualData<TKey, EnumWrapper>>();

        public EnumVirtualData()
        {
            TEnum[] values = (TEnum[])Enum.GetValues(typeof(TEnum));
            foreach (TEnum value in values)
            {
                m_VirtualDataLookup.Add(value, new VirtualData<TKey, EnumWrapper>(BatchStrategy.MaximizeChunk));
            }
        }

        protected override void DisposeSelf()
        {
            foreach (VirtualData<TKey, EnumWrapper> value in m_VirtualDataLookup.Values)
            {
                value.Dispose();
            }
            m_VirtualDataLookup.Clear();
        }
    }
}
