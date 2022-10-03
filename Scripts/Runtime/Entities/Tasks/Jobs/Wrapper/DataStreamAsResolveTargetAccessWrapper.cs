using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class DataStreamAsResolveTargetAccessWrapper : IAccessWrapper
    {
        public static DataStreamAsResolveTargetAccessWrapper Create<TResolveTarget>(TResolveTarget resolveTarget, List<ResolveTargetData> resolveTargetData)
            where TResolveTarget : Enum
        {
            ResolveTargetUtil.Debug_EnsureEnumValidity(resolveTarget);
            return new DataStreamAsResolveTargetAccessWrapper(UnsafeUtility.As<TResolveTarget, byte>(ref resolveTarget), resolveTargetData);
        }

        public byte ResolveTarget { get; }

        private readonly List<ResolveTargetData> m_ResolveTargetData;
        private NativeArray<JobHandle> m_Dependencies;

        private DataStreamAsResolveTargetAccessWrapper(byte resolveTarget, List<ResolveTargetData> resolveTargetData)
        {
            ResolveTarget = resolveTarget;
            m_ResolveTargetData = resolveTargetData;
            m_Dependencies = new NativeArray<JobHandle>(m_ResolveTargetData.Count, Allocator.Persistent);
        }

        public void Dispose()
        {
            m_Dependencies.Dispose();
        }

        public JobHandle Acquire()
        {
            for (int i = 0; i < m_ResolveTargetData.Count; ++i)
            {
                m_Dependencies[i] = m_ResolveTargetData[i].DataStream.AccessController.AcquireAsync(AccessType.SharedWrite);
            }
            return JobHandle.CombineDependencies(m_Dependencies);
        }

        public void Release(JobHandle releaseAccessDependency)
        {
            foreach (ResolveTargetData resolveTargetData in m_ResolveTargetData)
            {
                resolveTargetData.DataStream.AccessController.ReleaseAsync(releaseAccessDependency);
            }
        }
    }
}
