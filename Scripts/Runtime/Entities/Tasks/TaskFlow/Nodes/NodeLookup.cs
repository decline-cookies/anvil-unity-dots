using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class NodeLookup : AbstractNodeLookup
    {
        private readonly Dictionary<AbstractEntityProxyDataStream, DataStreamNode> m_NodesByDataStream;
        private readonly Dictionary<CancelRequestsDataStream, CancelRequestsNode> m_NodesByCancelRequests;
        private TaskDriverCancellationPropagator m_CancellationPropagator;

        public byte Context { get; }

        public NodeLookup(TaskFlowGraph taskGraph,
                          AbstractTaskSystem taskSystem,
                          AbstractTaskDriver taskDriver) : base(taskGraph, taskSystem, taskDriver)
        {
            m_NodesByDataStream = new Dictionary<AbstractEntityProxyDataStream, DataStreamNode>();
            m_NodesByCancelRequests = new Dictionary<CancelRequestsDataStream, CancelRequestsNode>();
            Context = taskDriver?.Context ?? taskSystem.Context;
        }

        protected override void DisposeSelf()
        {
            foreach (DataStreamNode node in m_NodesByDataStream.Values)
            {
                node.Dispose();
            }

            foreach (CancelRequestsNode cancelRequestsNode in m_NodesByCancelRequests.Values)
            {
                cancelRequestsNode.Dispose();
            }

            m_CancellationPropagator?.Dispose();

            m_NodesByDataStream.Clear();
            m_NodesByCancelRequests.Clear();

            base.DisposeSelf();
        }

        public DataStreamNode CreateDataStreamNode(AbstractTaskStream taskStream, AbstractEntityProxyDataStream dataStream, bool isResolveTarget)
        {
            Debug_EnsureNoDuplicateDataStreamNodes(dataStream);
            DataStreamNode node = new DataStreamNode(this,
                                                     dataStream,
                                                     TaskGraph,
                                                     TaskSystem,
                                                     TaskDriver,
                                                     taskStream,
                                                     isResolveTarget);
            m_NodesByDataStream.Add(dataStream, node);
            return node;
        }

        public CancelRequestsNode CreateCancelRequestsNode(CancelRequestsDataStream cancelRequestsDataStream)
        {
            Debug_EnsureNoDuplicateCancelRequestNodes(cancelRequestsDataStream);
            CancelRequestsNode node = new CancelRequestsNode(this,
                                                             cancelRequestsDataStream,
                                                             TaskGraph,
                                                             TaskSystem,
                                                             TaskDriver);
            m_NodesByCancelRequests.Add(cancelRequestsDataStream, node);
            return node;
        }

        public TaskDriverCancellationPropagator CreateCancellationPropagator()
        {
            m_CancellationPropagator = new TaskDriverCancellationPropagator(TaskDriver,
                                                                            TaskDriver.CancelRequestsDataStream,
                                                                            TaskSystem.GetCancelRequestsDataStream(),
                                                                            TaskDriver.GetSubTaskDriverCancelRequests());
            return m_CancellationPropagator;
        }

        public bool IsDataStreamRegistered(AbstractEntityProxyDataStream dataStream)
        {
            return m_NodesByDataStream.ContainsKey(dataStream);
        }

        public DataStreamNode this[AbstractEntityProxyDataStream dataStream]
        {
            get
            {
                Debug_EnsureExists(dataStream);
                return m_NodesByDataStream[dataStream];
            }
        }

        public void PopulateWithDataStreams(List<AbstractEntityProxyDataStream> dataStreams)
        {
            foreach (AbstractEntityProxyDataStream dataStream in m_NodesByDataStream.Keys)
            {
                dataStreams.Add(dataStream);
            }
        }

        public void PopulateWithResolveTargetDataStreams<TResolveTargetType>(JobResolveTargetMapping jobResolveTargetMapping)
            where TResolveTargetType : unmanaged, IEntityProxyInstance
        {
            Type resolveTargetType = typeof(TResolveTargetType);
            foreach (DataStreamNode node in m_NodesByDataStream.Values)
            {
                if (!node.IsResolveTarget || resolveTargetType != node.EntityProxyInstanceType)
                {
                    continue;
                }

                jobResolveTargetMapping.RegisterDataStream((EntityProxyDataStream<TResolveTargetType>)node.DataStream, Context);
            }
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNoDuplicateDataStreamNodes(AbstractEntityProxyDataStream dataStream)
        {
            if (m_NodesByDataStream.ContainsKey(dataStream))
            {
                throw new InvalidOperationException($"Trying to create a new {nameof(DataStreamNode)} with instance of {dataStream} but one already exists!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNoDuplicateCancelRequestNodes(CancelRequestsDataStream cancelRequestsDataStream)
        {
            if (m_NodesByCancelRequests.ContainsKey(cancelRequestsDataStream))
            {
                throw new InvalidOperationException($"Trying to create a new {nameof(CancelRequestsNode)} with instance of {cancelRequestsDataStream} but one already exists!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureExists(AbstractEntityProxyDataStream dataStream)
        {
            if (!m_NodesByDataStream.ContainsKey(dataStream))
            {
                throw new InvalidOperationException($"Trying to access a {nameof(DataStreamNode)} with instance of {dataStream} but it doesn't exist!");
            }
        }
    }
}
