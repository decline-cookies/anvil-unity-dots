using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Anvil.Unity.DOTS.Entities
{
    internal class NodeLookup : AbstractNodeLookup
    {
        private readonly Dictionary<AbstractProxyDataStream, DataStreamNode> m_NodesByDataStream;
        private readonly Dictionary<CancelRequestsDataStream, CancelRequestsNode> m_NodesByCancelRequests;
        private TaskDriverCancellationPropagator m_CancellationPropagator;

        public byte Context { get; }

        public NodeLookup(TaskFlowGraph taskGraph,
                          ITaskSystem taskSystem,
                          ITaskDriver taskDriver) : base(taskGraph, taskSystem, taskDriver)
        {
            m_NodesByDataStream = new Dictionary<AbstractProxyDataStream, DataStreamNode>();
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

        public DataStreamNode CreateDataStreamNode(AbstractTaskStream taskStream, AbstractProxyDataStream dataStream)
        {
            Debug_EnsureNoDuplicateDataStreamNodes(dataStream);
            DataStreamNode node = new DataStreamNode(this,
                                                     dataStream,
                                                     TaskGraph,
                                                     TaskSystem,
                                                     TaskDriver,
                                                     taskStream);
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
                                                                            TaskDriver.GetCancelRequestsDataStream(),
                                                                            TaskSystem.GetCancelRequestsDataStream(),
                                                                            TaskDriver.GetSubTaskDriverCancelRequests());
            return m_CancellationPropagator;
        }

        public bool IsDataStreamRegistered(AbstractProxyDataStream dataStream)
        {
            return m_NodesByDataStream.ContainsKey(dataStream);
        }

        public DataStreamNode this[AbstractProxyDataStream dataStream]
        {
            get
            {
                Debug_EnsureExists(dataStream);
                return m_NodesByDataStream[dataStream];
            }
        }

        public void PopulateWithDataStreams(List<AbstractProxyDataStream> dataStreams)
        {
            foreach (AbstractProxyDataStream dataStream in m_NodesByDataStream.Keys)
            {
                dataStreams.Add(dataStream);
            }
        }

        public void PopulateWithResolveTargetDataStreams<TResolveTarget>(JobResolveTargetMapping jobResolveTargetMapping, TResolveTarget resolveTarget)
            where TResolveTarget : Enum
        {
            foreach (DataStreamNode node in m_NodesByDataStream.Values)
            {
                if (!node.IsResolveTarget(resolveTarget))
                {
                    continue;
                }

                jobResolveTargetMapping.RegisterDataStream(resolveTarget, node.DataStream, Context);
            }
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNoDuplicateDataStreamNodes(AbstractProxyDataStream dataStream)
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
        private void Debug_EnsureExists(AbstractProxyDataStream dataStream)
        {
            if (!m_NodesByDataStream.ContainsKey(dataStream))
            {
                throw new InvalidOperationException($"Trying to access a {nameof(DataStreamNode)} with instance of {dataStream} but it doesn't exist!");
            }
        }
    }
}
