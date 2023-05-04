using System;
using System.Runtime.InteropServices;
using Unity.Collections;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    [BurstCompatible]
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct TaskSetOwnerID : IEquatable<TaskSetOwnerID>
    {
        private static readonly TaskSetOwnerID UNSET_TASKSET_OWNER_ID = default; 
        // public static implicit operator TaskSetOwnerID(int id) => new TaskSetOwnerID(id);
        // public static implicit operator int(TaskSetOwnerID id) => id.m_Value;
        
        public static bool operator ==(TaskSetOwnerID lhs, TaskSetOwnerID rhs)
        {
            return lhs.m_Value == rhs.m_Value && lhs.m_Value == rhs.m_Value;
        }

        public static bool operator !=(TaskSetOwnerID lhs, TaskSetOwnerID rhs)
        {
            return !(lhs == rhs);
        }
        
        
        private readonly int m_Value;

        public bool IsValid
        {
            get => this != UNSET_TASKSET_OWNER_ID;
        }

        public TaskSetOwnerID(int value)
        {
            m_Value = value;
        }
        
        public bool Equals(TaskSetOwnerID other)
        {
            return this == other;
        }

        public override bool Equals(object compare)
        {
            return compare is TaskSetOwnerID id && Equals(id);
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
