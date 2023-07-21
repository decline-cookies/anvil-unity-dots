using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class CancelRequestsPendingAccessWrapper : AbstractAccessWrapper
    {
        private readonly CancelRequestsDataStream m_DefaultStream;

        public CancelRequestsPendingAccessWrapper(
            CancelRequestsDataStream defaultStream,
            AccessType accessType,
            AbstractJobConfig.Usage usage)
            : base(accessType, usage)
        {
            m_DefaultStream = defaultStream;
            DEBUG_TrackRequiredStream(defaultStream);
        }

        public override JobHandle AcquireAsync()
        {
            return m_DefaultStream.AcquirePendingAsync(AccessType);
        }

        public override void ReleaseAsync(JobHandle dependsOn)
        {
            m_DefaultStream.ReleasePendingAsync(dependsOn);
        }

        public CancelRequestsDataStream GetInstance(IAbstractCancelRequestDataStream explicitStream = null)
        {
            DEBUG_EnforceExplicitStream(explicitStream);
            DEBUG_EnsureStreamWasRequired(explicitStream);

            return (CancelRequestsDataStream)explicitStream ?? m_DefaultStream;
        }

        public override void MergeStateFrom(AbstractAccessWrapper other)
        {
            base.MergeStateFrom(other);

            CancelRequestsPendingAccessWrapper otherTyped = (CancelRequestsPendingAccessWrapper)other;
            m_DEBUG_RequiredStreams.UnionWith(otherTyped.m_DEBUG_RequiredStreams);
            DEBUG_TrackRequiredStream(otherTyped.m_DefaultStream);
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        // #if ANVIL_DEBUG_SAFETY
        private HashSet<CancelRequestsDataStream> m_DEBUG_RequiredStreams;

        [Conditional("ANVIL_DEBUG_SAFETY")]
        public void DEBUG_TrackRequiredStream(CancelRequestsDataStream stream)
        {
            m_DEBUG_RequiredStreams ??= new HashSet<CancelRequestsDataStream>(1);
            m_DEBUG_RequiredStreams.Add(stream);
        }

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void DEBUG_EnsureStreamWasRequired(IAbstractCancelRequestDataStream stream)
        {
            CancelRequestsDataStream cancelStream = (CancelRequestsDataStream)stream;
            if (stream == null || m_DEBUG_RequiredStreams.Contains(cancelStream))
            {
                return;
            }

            throw new Exception($"The explicit stream instance requested was not set as required. DataTargetID:{cancelStream.DataTargetID}");
        }

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void DEBUG_EnforceExplicitStream(IAbstractCancelRequestDataStream stream)
        {
            int requiredStreamCount = m_DEBUG_RequiredStreams.Count;
            if (stream == null && requiredStreamCount > 1)
            {
                throw new Exception($"More than one stream has set this type as a requirement. The exact stream must be provided on retrieval. Type:{typeof(CancelRequestsDataStream).GetReadableName()}");
            }

            if (stream != null && requiredStreamCount == 1)
            {
                Logger.Warning($"An explicit stream was provided when not required. Consider using default fulfillment. Type:{typeof(CancelRequestsDataStream).GetReadableName()}");
            }
        }
    }
}