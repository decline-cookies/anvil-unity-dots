using Anvil.Unity.DOTS.Core;
using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

namespace Anvil.Unity.DOTS.Entities
{
    [BurstCompatible]
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct DataOwnerID : IEquatable<DataOwnerID>,
                                           IToFixedString<FixedString32Bytes>
    {
        private static readonly DataOwnerID UNSET_ID = default;

        public static bool operator ==(DataOwnerID lhs, DataOwnerID rhs)
        {
            return lhs.m_Value == rhs.m_Value && lhs.m_Value == rhs.m_Value;
        }

        public static bool operator !=(DataOwnerID lhs, DataOwnerID rhs)
        {
            return !(lhs == rhs);
        }
        
        
        private readonly int m_Value;

        public bool IsValid
        {
            get => this != UNSET_ID;
        }

        public DataOwnerID(int value)
        {
            Debug.Assert(value != UNSET_ID.m_Value);
            m_Value = value;
        }
        
        public bool Equals(DataOwnerID other)
        {
            return this == other;
        }

        public override bool Equals(object compare)
        {
            return compare is DataOwnerID id && Equals(id);
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
            return $"Value: {m_Value}";
        }
    }
}
