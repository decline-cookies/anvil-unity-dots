// using Anvil.Unity.DOTS.Jobs;
// using System.Collections.Generic;
// using Unity.Entities;
//
// namespace Anvil.Unity.DOTS.Entities.Tasks
// {
// #if DEBUG
//     /// <summary>
//     /// System that will handle populating profiling counters once per frame when guaranteed that all writing jobs
//     /// have completed.
//     /// </summary>
//     //TODO: #86 - Revisit with Entities 1.0 for "Create Before/After"
//     //TODO: #108 - Revisit to see if there is a better way to handle acquire/release. Or can we do it all in a job? 
//     [UpdateInGroup(typeof(PresentationSystemGroup), OrderLast = true)]
//     public partial class DataStreamProfilingSystem : AbstractAnvilSystemBase
//     {
//         private readonly List<AbstractDataStream> m_AllDataStreams;
//         
//         private TaskFlowGraph m_TaskFlowGraph;
//         private bool m_HasInitialized;
//
//         public DataStreamProfilingSystem()
//         {
//             m_AllDataStreams = new List<AbstractDataStream>();
//         }
//
//         protected override void OnCreate()
//         {
//             m_TaskFlowGraph = World.GetExistingSystem<TaskFlowSystem>().TaskFlowGraph;
//             base.OnCreate();
//         }
//
//         protected override void OnStartRunning()
//         {
//             if (m_HasInitialized)
//             {
//                 return;
//             }
//
//             m_HasInitialized = true;
//
//             m_TaskFlowGraph.AddAllDataStreamsTo(m_AllDataStreams);
//             
//             base.OnStartRunning();
//         }
//
//         protected override void OnUpdate()
//         {
//             foreach (AbstractDataStream dataStream in m_AllDataStreams)
//             {
//                 dataStream.AccessController.Acquire(AccessType.SharedRead);
//                 
//                 dataStream.PopulateProfiler();
//                 
//                 dataStream.AccessController.Release();
//             }
//         }
//     }
// #endif
// }
