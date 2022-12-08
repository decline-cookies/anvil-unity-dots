using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using System;
using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class TaskSet : AbstractAnvilBase
    {
        private readonly List<AbstractDataStream> m_DataStreamsWithDefaultCancellation;
        private readonly List<AbstractDataStream> m_DataStreamsWithExplicitCancellation;
        private readonly List<AbstractDataStream> m_DataStreamsWithNoCancellation;

        private readonly List<AbstractDataStream> m_AllPublicDataStreams;
        private readonly Dictionary<Type, AbstractDataStream> m_PublicDataStreamsByType;

        private readonly List<AbstractJobConfig> m_JobConfigs;


        public ITaskSetOwner TaskSetOwner { get; }
        public CancelRequestDataStream CancelRequestDataStream { get; }
        public CancelProgressDataStream CancelProgressDataStream { get; }
        public CancelCompleteDataStream CancelCompleteDataStream { get; }

        public TaskSet(ITaskSetOwner taskSetOwner)
        {
            TaskSetOwner = taskSetOwner;
            m_JobConfigs = new List<AbstractJobConfig>();

            m_DataStreamsWithDefaultCancellation = new List<AbstractDataStream>();
            m_DataStreamsWithExplicitCancellation = new List<AbstractDataStream>();
            m_DataStreamsWithNoCancellation = new List<AbstractDataStream>();
            m_PublicDataStreamsByType = new Dictionary<Type, AbstractDataStream>();

            CancelRequestDataStream = new CancelRequestDataStream(TaskSetOwner);
            CancelProgressDataStream = new CancelProgressDataStream(TaskSetOwner);
            CancelCompleteDataStream = new CancelCompleteDataStream(TaskSetOwner);
        }

        protected override void DisposeSelf()
        {
            m_JobConfigs.DisposeAllAndTryClear();

            base.DisposeSelf();
        }

        public DataStream<TInstance> CreateDataStream<TInstance>(CancelBehaviour cancelBehaviour)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            DataStream<TInstance> dataStream = new DataStream<TInstance>(TaskSetOwner, cancelBehaviour);
            switch (cancelBehaviour)
            {
                case CancelBehaviour.Default:
                    m_DataStreamsWithDefaultCancellation.Add(dataStream);
                    break;
                case CancelBehaviour.None:
                    m_DataStreamsWithNoCancellation.Add(dataStream);
                    break;
                case CancelBehaviour.Explicit:
                    m_DataStreamsWithExplicitCancellation.Add(dataStream);
                    //TODO: Add second data stream for pending cancel
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(cancelBehaviour), cancelBehaviour, null);
            }

            m_PublicDataStreamsByType.Add(typeof(TInstance), dataStream);
            m_AllPublicDataStreams.Add(dataStream);

            return dataStream;
        }

        public AbstractDataStream GetDataStreamByType(Type type)
        {
            return m_PublicDataStreamsByType[type];
        }

        public IJobConfig ConfigureJobToCancel<TInstance>(IAbstractDataStream<TInstance> dataStream,
                                                             JobConfigScheduleDelegates.ScheduleCancelJobDelegate<TInstance> scheduleJobFunction,
                                                             BatchStrategy batchStrategy)
            where TInstance : unmanaged, IEntityProxyInstance
        {
            JobConfigFactory.CreateCancelJobConfig()
        }
    }
}
