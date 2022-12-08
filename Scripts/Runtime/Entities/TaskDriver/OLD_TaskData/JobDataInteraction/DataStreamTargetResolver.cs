using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// Represents a collection of <see cref="DataStreamResolver"/>s keyed on different resolve targets.
    /// </summary>
    [BurstCompatible]
    internal struct DataStreamTargetResolver
    {
        [ReadOnly] private UnsafeParallelHashMap<uint, DataStreamResolver> m_ResolversByTarget;

        internal DataStreamTargetResolver(JobResolveTargetMapping jobResolveTargetMapping)
        {
            int numChannels = jobResolveTargetMapping.Mapping.Count;
            m_ResolversByTarget = new UnsafeParallelHashMap<uint, DataStreamResolver>(numChannels, Allocator.Persistent);
            foreach (KeyValuePair<uint, Dictionary<byte, ResolveTargetData>> entry in jobResolveTargetMapping.Mapping)
            {
                m_ResolversByTarget.Add(entry.Key, new DataStreamResolver(entry.Value));
            }
        }

        internal void Dispose()
        {
            if (!m_ResolversByTarget.IsCreated)
            {
                return;
            }

            foreach (KeyValue<uint, DataStreamResolver> entry in m_ResolversByTarget)
            {
                entry.Value.Dispose();
            }

            m_ResolversByTarget.Dispose();
        }

        internal void Resolve<TResolveTargetType>(byte context,
                                                  int laneIndex,
                                                  ref TResolveTargetType resolvedInstance)
            where TResolveTargetType : unmanaged, IEntityProxyInstance
        {
            uint id = ResolveTargetUtil.GetResolveTargetID<TResolveTargetType>();
            Debug_EnsureContainsResolveTarget(id);
            DataStreamResolver resolver = m_ResolversByTarget[id];
            resolver.Resolve(context,
                             laneIndex,
                             ref resolvedInstance);
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureContainsResolveTarget(uint resolveTargetID)
        {
            if (!m_ResolversByTarget.ContainsKey(resolveTargetID))
            {
                throw new InvalidOperationException($"Trying to get Resolve Target with key of {resolveTargetID} but no target exists! Did this job require the right Resolve Target?");
            }
        }
    }
}
