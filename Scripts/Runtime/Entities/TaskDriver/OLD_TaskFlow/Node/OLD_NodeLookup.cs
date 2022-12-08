// using System;
// using System.Collections.Generic;
// using System.Diagnostics;
//
// namespace Anvil.Unity.DOTS.Entities.Tasks
// {
//     internal class NodeLookup : AbstractNodeLookup
//     {
//         private readonly Dictionary<AbstractDataStream, DataStreamNode> m_NodesByDataStream;
//
//         public byte Context { get; }
//
//         public NodeLookup(TaskFlowGraph taskGraph,
//                           AbstractTaskDriverSystem taskSystem,
//                           AbstractTaskDriver taskDriver) : base(taskGraph, taskSystem, taskDriver)
//         {
//             m_NodesByDataStream = new Dictionary<AbstractDataStream, DataStreamNode>();
//             Context = taskDriver?.Context ?? taskSystem.Context;
//         }
//
//         public void CreateDataStreamNodes(AbstractDataStream dataStream)
//         {
//             CreateDataStreamNode(dataStream);
//         }
//
//         private void CreateDataStreamNode(AbstractDataStream dataStream)
//         {
//             Debug_EnsureNoDuplicateDataStreamNodes(dataStream);
//             DataStreamNode node = new DataStreamNode(this,
//                                                      dataStream,
//                                                      TaskGraph,
//                                                      TaskSystem,
//                                                      TaskDriver);
//             m_NodesByDataStream.Add(dataStream, node);
//         }
//
//         public bool IsDataStreamRegistered(AbstractDataStream dataStream)
//         {
//             return m_NodesByDataStream.ContainsKey(dataStream);
//         }
//
//         public DataStreamNode this[AbstractDataStream dataStream]
//         {
//             get
//             {
//                 Debug_EnsureExists(dataStream);
//                 return m_NodesByDataStream[dataStream];
//             }
//         }
//
//         public void AddResolveTargetDataStreamsTo<TResolveTargetType>(JobResolveTargetMapping jobResolveTargetMapping)
//             where TResolveTargetType : unmanaged, IEntityProxyInstance
//         {
//             //TODO: #78 - Optimization, this could be pretty slow since we're only searching for one type but have to check all possible nodes.
//             //TODO: It would be better to already have the nodes categorized by type or even better, by resolveTarget and type so it's faster to build.
//             Type resolveTargetType = typeof(TResolveTargetType);
//             foreach (DataStreamNode node in m_NodesByDataStream.Values)
//             {
//                 if (resolveTargetType != node.EntityProxyInstanceType)
//                 {
//                     continue;
//                 }
//
//                 jobResolveTargetMapping.RegisterDataStream<TResolveTargetType>((IPendingDataStream)node.DataStream, Context);
//             }
//         }
//
//         //*************************************************************************************************************
//         // SAFETY
//         //*************************************************************************************************************
//
//         [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
//         private void Debug_EnsureNoDuplicateDataStreamNodes(AbstractDataStream dataStream)
//         {
//             if (m_NodesByDataStream.ContainsKey(dataStream))
//             {
//                 throw new InvalidOperationException($"Trying to create a new {nameof(DataStreamNode)} with instance of {dataStream} but one already exists!");
//             }
//         }
//
//         [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
//         private void Debug_EnsureExists(AbstractDataStream dataStream)
//         {
//             if (!m_NodesByDataStream.ContainsKey(dataStream))
//             {
//                 throw new InvalidOperationException($"Trying to access a {nameof(DataStreamNode)} with instance of {dataStream} but it doesn't exist!");
//             }
//         }
//     }
// }
