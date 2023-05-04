using System;
using System.Runtime.InteropServices;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities
{
    [BurstCompatible]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct DataOwnerID : IEquatable<DataOwnerID>
    {
        private static readonly DataOwnerID UNSET_DATA_OWNER_ID = default;

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
            get => this != UNSET_DATA_OWNER_ID;
        }

        public DataOwnerID(int value)
        {
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
            return $"{m_Value}";
        }
    }
}
