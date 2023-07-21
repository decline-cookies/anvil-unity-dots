using Anvil.CSharp.Core;
using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal abstract class AbstractAccessWrapper : AbstractAnvilBase
    {
        public JobConfigDataID ID { get; }

        internal AccessType AccessType { get; }


        protected AbstractAccessWrapper(AccessType accessType, AbstractJobConfig.Usage usage)
        {
            AccessType = accessType;
            ID = new JobConfigDataID(GetType(), usage);
            Debug_SetWrapperType();
        }

        public abstract JobHandle AcquireAsync();
        public abstract void ReleaseAsync(JobHandle dependsOn);

        /// <summary>
        /// Merge the state from another wrapper instance of the same type
        /// </summary>
        /// <param name="other"></param>
        public virtual void MergeStateFrom(AbstractAccessWrapper other)
        {
            Debug_EnsureSameID(other);
            Debug_EnsureSameType(other);
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        // ReSharper disable once InconsistentNaming
        public Type Debug_WrapperType { get; private set; }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_SetWrapperType()
        {
            Type type = GetType();
            Debug_WrapperType = type.IsGenericType
                ? type.GetGenericTypeDefinition()
                : type;
        }

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void Debug_EnsureSameID(AbstractAccessWrapper wrapper)
        {
            if (ID != wrapper.ID)
            {
                throw new Exception($"Cannot merge two wrappers with different IDs. This:{ID}, Other:{wrapper.ID}");
            }
        }

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void Debug_EnsureSameType(AbstractAccessWrapper wrapper)
        {
            Type thisType = GetType();
            Type otherType = wrapper.GetType();
            if (thisType != otherType)
            {
                throw new Exception($"Cannot merge two wrappers of different types. This:{thisType.GetReadableName()}, Other:{otherType.GetReadableName()}");
            }
        }
    }
}