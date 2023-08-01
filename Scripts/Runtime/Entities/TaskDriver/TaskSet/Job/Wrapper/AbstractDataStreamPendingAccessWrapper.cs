using Anvil.CSharp.Logging;
using Anvil.Unity.DOTS.Jobs;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal abstract class AbstractDataStreamPendingAccessWrapper<T> : AbstractAccessWrapper
        where T : AbstractDataStream, IAbstractDataStream
    {
        protected readonly T m_DefaultStream;

        protected AbstractDataStreamPendingAccessWrapper(T defaultStream, AccessType accessType, AbstractJobConfig.Usage usage)
            : base(accessType, usage)
        {
            m_DefaultStream = defaultStream;
            DEBUG_TrackRequiredStream(defaultStream);
        }

        public T GetInstance(IAbstractDataStream explicitStream = null)
        {
            DEBUG_EnforceExplicitStream(explicitStream);
            DEBUG_EnsureStreamWasRequired(explicitStream);

            return (T)explicitStream ?? m_DefaultStream;
        }

        public override void MergeStateFrom(AbstractAccessWrapper other)
        {
            base.MergeStateFrom(other);

            AbstractDataStreamPendingAccessWrapper<T> otherTyped = (AbstractDataStreamPendingAccessWrapper<T>)other;
            m_DEBUG_RequiredStreams.UnionWith(otherTyped.m_DEBUG_RequiredStreams);
            DEBUG_TrackRequiredStream(otherTyped.m_DefaultStream);
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        // #if ANVIL_DEBUG_SAFETY
        private HashSet<T> m_DEBUG_RequiredStreams;

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void DEBUG_TrackRequiredStream(T stream)
        {
            m_DEBUG_RequiredStreams ??= new HashSet<T>(1);
            m_DEBUG_RequiredStreams.Add(stream);
        }

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void DEBUG_EnsureStreamWasRequired(IAbstractDataStream stream)
        {
            T typedStream = (T)stream;
            if (stream == null || m_DEBUG_RequiredStreams.Contains(typedStream))
            {
                return;
            }

            throw new Exception($"The explicit stream instance requested was not set as required. DataTargetID:{typedStream.DataTargetID}");
        }

        [Conditional("ANVIL_DEBUG_SAFETY")]
        private void DEBUG_EnforceExplicitStream(IAbstractDataStream stream)
        {
            int requiredStreamCount = m_DEBUG_RequiredStreams.Count;
            if (stream == null && requiredStreamCount > 1)
            {
                throw new Exception($"More than one stream has set this type as a requirement. The exact stream must be provided on retrieval. Type:{typeof(T).GetReadableName()}");
            }

            if (stream != null && requiredStreamCount == 1)
            {
                Logger.Warning($"An explicit stream was provided when not required. Consider using default fulfillment. Type:{typeof(T).GetReadableName()}");
            }
        }
    }
}