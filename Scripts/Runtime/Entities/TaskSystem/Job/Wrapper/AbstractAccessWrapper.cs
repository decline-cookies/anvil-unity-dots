using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Diagnostics;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal abstract class AbstractAccessWrapper : AbstractAnvilBase
    {
        public JobConfigDataID ID { get; }

        protected AccessType AccessType { get; }


        protected AbstractAccessWrapper(AccessType accessType, AbstractJobConfig.Usage usage)
        {
            AccessType = accessType;
            ID = new JobConfigDataID(GetType(), usage);
            Debug_SetWrapperType();
        }

        public abstract JobHandle Acquire();
        public abstract void Release(JobHandle releaseAccessDependency);

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        // ReSharper disable once InconsistentNaming
        public Type Debug_WrapperType { get; private set; }
#endif

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_SetWrapperType()
        {
            Type type = GetType();
            Debug_WrapperType = type.IsGenericType
                ? type.GetGenericTypeDefinition()
                : type;
        }
    }
}
