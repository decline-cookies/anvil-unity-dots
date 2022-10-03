using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    [BurstCompatible]
    public struct DataStreamTargetResolver : IDisposable
    {
        [ReadOnly] private UnsafeParallelHashMap<byte, DataStreamResolver> m_ResolversByTarget;

        internal DataStreamTargetResolver(JobResolveTargetMapping jobResolveTargetMapping)
        {
            int numChannels = jobResolveTargetMapping.Mapping.Count;
            m_ResolversByTarget = new UnsafeParallelHashMap<byte, DataStreamResolver>(numChannels, Allocator.Persistent);
            foreach (KeyValuePair<byte, Dictionary<byte, ResolveTargetData>> entry in jobResolveTargetMapping.Mapping)
            {
                m_ResolversByTarget.Add(entry.Key, new DataStreamResolver(entry.Value));
            }
        }

        public void Dispose()
        {
            if (!m_ResolversByTarget.IsCreated)
            {
                return;
            }

            foreach (KeyValue<byte, DataStreamResolver> entry in m_ResolversByTarget)
            {
                entry.Value.Dispose();
            }

            m_ResolversByTarget.Dispose();
        }

        internal void Resolve<TResolveTarget, TResolvedInstance>(TResolveTarget resolveTarget,
                                                                 byte context,
                                                                 int laneIndex,
                                                                 ref TResolvedInstance resolvedInstance)
            where TResolveTarget : Enum
            where TResolvedInstance : unmanaged, IProxyInstance
        {
            byte key = UnsafeUtility.As<TResolveTarget, byte>(ref resolveTarget);
            Debug_EnsureContainsResolveTarget(key);
            DataStreamResolver resolver = m_ResolversByTarget[key];
            resolver.Resolve(context,
                             laneIndex,
                             ref resolvedInstance);
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureContainsResolveTarget(byte resolveTargetKey)
        {
            if (!m_ResolversByTarget.ContainsKey(resolveTargetKey))
            {
                throw new InvalidOperationException($"Trying to get Resolve Target with key of {resolveTargetKey} but no target exists! Did this job require the right Resolve Target?");
            }
        }
    }
}
