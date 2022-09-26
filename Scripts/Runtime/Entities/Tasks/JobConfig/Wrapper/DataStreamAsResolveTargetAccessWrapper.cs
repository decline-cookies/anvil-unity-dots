using Anvil.Unity.DOTS.Jobs;
using System;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities
{
    internal class DataStreamAsResolveTargetAccessWrapper : DataStreamAccessWrapper
    {
        public static DataStreamAsResolveTargetAccessWrapper Create<TResolveTarget>(TResolveTarget resolveTarget, ResolveTargetData wrapper)
            where TResolveTarget : Enum
        {
            ResolveTargetUtil.Debug_EnsureEnumValidity(resolveTarget);
            return new DataStreamAsResolveTargetAccessWrapper(UnsafeUtility.As<TResolveTarget, byte>(ref resolveTarget), wrapper);
        }

        public byte ResolveTarget
        {
            get;
        }

        public byte Context
        {
            get;
        }

        private DataStreamAsResolveTargetAccessWrapper(byte resolveTarget, ResolveTargetData wrapper) : base(wrapper.DataStream, AccessType.SharedWrite)
        {
            ResolveTarget = resolveTarget;
            Context = wrapper.Context;
        }
    }
}
