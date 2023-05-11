using Anvil.Unity.DOTS.Core;
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    [BurstCompatible]
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct DataTargetID : IEquatable<DataTargetID>,
                                            IToFixedString<FixedString32Bytes>
    {
        private static readonly DataTargetID UNSET_ID = default;

        public static bool operator ==(DataTargetID lhs, DataTargetID rhs)
        {
            return lhs.m_Value == rhs.m_Value && lhs.m_Value == rhs.m_Value;
        }

        public static bool operator !=(DataTargetID lhs, DataTargetID rhs)
        {
            return !(lhs == rhs);
        }
        
        
        private readonly int m_Value;
        
        public bool IsValid
        {
            get => this != UNSET_ID;
        }

        public DataTargetID(int value)
        {
            Debug.Assert(value != UNSET_ID.m_Value);
            m_Value = value;
        }
        
        public bool Equals(DataTargetID other)
        {
            return this == other;
        }

        public override bool Equals(object compare)
        {
            return compare is DataTargetID id && Equals(id);
        }

        public override int GetHashCode()
        {
            return m_Value.GetHashCode();
        }

        public override string ToString()
        {
            return m_Value.ToString();
        }
        
        [BurstCompatible]
        public FixedString32Bytes ToFixedString()
        {
            return $"{m_Value}";
        }
    }
}
