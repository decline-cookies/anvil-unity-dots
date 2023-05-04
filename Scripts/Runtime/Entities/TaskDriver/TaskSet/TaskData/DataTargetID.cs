using System;
using System.Runtime.InteropServices;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    [BurstCompatible]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct DataTargetID : IEquatable<DataTargetID>
    {
        private static readonly DataTargetID UNSET_DATA_TARGET_ID = default; 
        // public static implicit operator DataTargetID(int id) => new DataTargetID(id);
        // public static implicit operator int(DataTargetID id) => id.m_Value;
        
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
            get => this != UNSET_DATA_TARGET_ID;
        }

        public DataTargetID(int value)
        {
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
