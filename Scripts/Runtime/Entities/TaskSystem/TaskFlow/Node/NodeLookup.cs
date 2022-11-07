using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    internal class NodeLookup : AbstractNodeLookup
    {
        private readonly Dictionary<AbstractTypedDataStream<>, DataStreamNode> m_NodesByDataStream;
        private readonly Dictionary<CancelRequestDataStream, CancelRequestsNode> m_NodesByCancelRequests;

        public byte Context { get; }

        public NodeLookup(TaskFlowGraph taskGraph,
                          AbstractTaskSystem taskSystem,
                          AbstractTaskDriver taskDriver) : base(taskGraph, taskSystem, taskDriver)
        {
            m_NodesByDataStream = new Dictionary<AbstractTypedDataStream<>, DataStreamNode>();
            m_NodesByCancelRequests = new Dictionary<CancelRequestDataStream, CancelRequestsNode>();
            Context = taskDriver?.Context ?? taskSystem.Context;
        }

        public void CreateDataStreamNodes(AbstractTypedDataStream<> dataStream)
        {
            CreateDataStreamNode(dataStream);
            //TODO: Add the PendingCancelled stream?
            // CreateDataStreamNode(dataStream);
        }

        private void CreateDataStreamNode(AbstractTypedDataStream<> dataStream)
        {
            Debug_EnsureNoDuplicateDataStreamNodes(dataStream);
            DataStreamNode node = new DataStreamNode(this,
                                                     dataStream,
                                                     TaskGraph,
                                                     TaskSystem,
                                                     TaskDriver);
            m_NodesByDataStream.Add(dataStream, node);
        }

        public void CreateCancelRequestsNode(CancelRequestDataStream cancelRequestDataStream)
        {
            Debug_EnsureNoDuplicateCancelRequestNodes(cancelRequestDataStream);
            CancelRequestsNode node = new CancelRequestsNode(this,
                                                             cancelRequestDataStream,
                                                             TaskGraph,
                                                             TaskSystem,
                                                             TaskDriver);
            m_NodesByCancelRequests.Add(cancelRequestDataStream, node);
        }

        public bool IsDataStreamRegistered(AbstractTypedDataStream<> dataStream)
        {
            return m_NodesByDataStream.ContainsKey(dataStream);
        }

        public DataStreamNode this[AbstractTypedDataStream<> dataStream]
        {
            get
            {
                Debug_EnsureExists(dataStream);
                return m_NodesByDataStream[dataStream];
            }
        }

        public void AddDataStreamsTo(List<AbstractTypedDataStream<>> dataStreams)
        {
            foreach (AbstractTypedDataStream<> dataStream in m_NodesByDataStream.Keys)
            {
                dataStreams.Add(dataStream);
            }
        }

        public void AddResolveTargetDataStreamsTo<TResolveTargetType>(JobResolveTargetMapping jobResolveTargetMapping)
            where TResolveTargetType : unmanaged, IEntityProxyInstance
        {
            //TODO: #78 - Optimization, this could be pretty slow since we're only searching for one type but have to check all possible nodes.
            //TODO: It would be better to already have the nodes categorized by type or even better, by resolveTarget and type so it's faster to build.
            Type resolveTargetType = typeof(TResolveTargetType);
            //TODO: Changed so that we don't care if its a ResolveTarget, if the type matches, it's valid
            foreach (DataStreamNode node in m_NodesByDataStream.Values)
            {
                if (resolveTargetType != node.EntityProxyInstanceType)
                {
                    continue;
                }

                jobResolveTargetMapping.RegisterDataStream((AbstractTypedDataStream<TResolveTargetType>)node.DataStream, Context);
            }
        }

        //*************************************************************************************************************
        // SAFETY
        //*************************************************************************************************************

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNoDuplicateDataStreamNodes(AbstractTypedDataStream<> dataStream)
        {
            if (m_NodesByDataStream.ContainsKey(dataStream))
            {
                throw new InvalidOperationException($"Trying to create a new {nameof(DataStreamNode)} with instance of {dataStream} but one already exists!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureNoDuplicateCancelRequestNodes(CancelRequestDataStream cancelRequestDataStream)
        {
            if (m_NodesByCancelRequests.ContainsKey(cancelRequestDataStream))
            {
                throw new InvalidOperationException($"Trying to create a new {nameof(CancelRequestsNode)} with instance of {cancelRequestDataStream} but one already exists!");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void Debug_EnsureExists(AbstractTypedDataStream<> dataStream)
        {
            if (!m_NodesByDataStream.ContainsKey(dataStream))
            {
                throw new InvalidOperationException($"Trying to access a {nameof(DataStreamNode)} with instance of {dataStream} but it doesn't exist!");
            }
        }
    }
}
