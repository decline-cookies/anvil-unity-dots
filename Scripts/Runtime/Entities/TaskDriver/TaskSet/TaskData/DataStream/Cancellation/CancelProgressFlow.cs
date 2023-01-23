using Anvil.CSharp.Collections;
using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System.Collections.Generic;
using System.Reflection;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.TaskDriver
{
    internal class CancelProgressFlow : AbstractAnvilBase
    {
        public static readonly BulkScheduleDelegate<CancelProgressFlow> SCHEDULE_FUNCTION = BulkSchedulingUtil.CreateSchedulingDelegate<CancelProgressFlow>(nameof(Schedule), BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly List<CancelProgressFlowNode> m_CancelProgressFlowNodes;
        private readonly Dictionary<int, List<CancelProgressFlowNode>> m_CancelFlowHierarchy;
        private readonly BulkJobScheduler<CancelProgressFlowNode>[] m_OrderedBulkJobSchedulers;

        private readonly string m_DebugString;

        public CancelProgressFlow(AbstractTaskDriver topLevelTaskDriver)
        {
            m_CancelProgressFlowNodes = new List<CancelProgressFlowNode>();
            m_CancelFlowHierarchy = new Dictionary<int, List<CancelProgressFlowNode>>();
            
            //Build up the hierarchy of when things should be scheduled so that the bottom most get a chance first
            //This will ensure the order of jobs executed to allow for possible 1 frame bubble up of completes
            BuildSchedulingHierarchy(topLevelTaskDriver, 0, null);

            //Get all the jobs for updating progress back in order of execution to ensure that progress propagates
            //up the chain correctly
            m_OrderedBulkJobSchedulers = CreateOrderedBulkJobSchedulers();
            
            //TODO: #108 - Lazy create this as needed if that's the case when decorating the job.
            m_DebugString = BuildDebugString();
        }

        protected override void DisposeSelf()
        {
            m_CancelProgressFlowNodes.DisposeAllAndTryClear();
            m_OrderedBulkJobSchedulers.DisposeAllAndTryClear();
            
            base.DisposeSelf();
        }

        public override string ToString()
        {
            return m_DebugString;
        }

        private BulkJobScheduler<CancelProgressFlowNode>[] CreateOrderedBulkJobSchedulers()
        {
            List<BulkJobScheduler<CancelProgressFlowNode>> orderedBulkJobSchedulers = new List<BulkJobScheduler<CancelProgressFlowNode>>();
            
            //In order for cancel progress to propagate correctly, we need to first check our deepest nodes in the chain
            //and they will write to their parents if we should keep waiting for the cancellation to be complete or not.
            //This builds up the order in which we need to execute those jobs so that we're doing it in the most 
            //efficient manner yet still preserving the integrity of the chain. A complete can cascade from the lowest 
            //level up to the top in one frame this way.
            
            int maxDepth = m_CancelFlowHierarchy.Count - 1;
            for (int depth = maxDepth; depth >= 0; --depth)
            {
                List<CancelProgressFlowNode> cancelProgressFlowNodesAtDepth = m_CancelFlowHierarchy[depth];
                BulkJobScheduler<CancelProgressFlowNode> bulkJobScheduler = new BulkJobScheduler<CancelProgressFlowNode>(cancelProgressFlowNodesAtDepth.ToArray());
                orderedBulkJobSchedulers.Add(bulkJobScheduler);
            }

            return orderedBulkJobSchedulers.ToArray();
        }

        private string BuildDebugString()
        {
            string debugString = string.Empty;
            for (int i = 0; i < m_CancelFlowHierarchy.Count; ++i)
            {
                List<CancelProgressFlowNode> cancelProgressFlowNodesAtDepth = m_CancelFlowHierarchy[i];
                debugString += $"Depth: {i}\n";
                foreach (CancelProgressFlowNode node in cancelProgressFlowNodesAtDepth)
                {
                    debugString += $"{node}\n";
                }
            }

            return debugString;
        }

        private void BuildSchedulingHierarchy(ITaskSetOwner taskSetOwner, int depth, CancelProgressFlowNode parent)
        {
            List<CancelProgressFlowNode> cancelFlows = GetOrCreateAtDepth(depth);
            List<CancelProgressFlowNode> cancelFlowsOneDeeper = GetOrCreateAtDepth(depth + 1);
            
            //If we don't have any Cancellable Data, then none of our children/systems do either, so we can skip
            if (!taskSetOwner.HasCancellableData)
            {
                return;
            }

            CancelProgressFlowNode taskDriverNode = new CancelProgressFlowNode(taskSetOwner, parent);
            cancelFlows.Add(taskDriverNode);
            m_CancelProgressFlowNodes.Add(taskDriverNode);
                
            //If our system has Cancellable Data, we need a node for it as well but one level deeper
            if (taskSetOwner.TaskDriverSystem.HasCancellableData)
            {
                CancelProgressFlowNode systemNode = new CancelProgressFlowNode(taskSetOwner.TaskDriverSystem, taskDriverNode);
                cancelFlowsOneDeeper.Add(systemNode);
                m_CancelProgressFlowNodes.Add(systemNode);
            }
                
            //Drill down into the children
            foreach (AbstractTaskDriver subTaskDriver in taskSetOwner.SubTaskDrivers)
            {
                BuildSchedulingHierarchy(subTaskDriver, depth + 1, taskDriverNode);
            }
        }
        
        private List<CancelProgressFlowNode> GetOrCreateAtDepth(int depth)
        {
            if (!m_CancelFlowHierarchy.TryGetValue(depth, out List<CancelProgressFlowNode> cancelProgressFlowNodes))
            {
                cancelProgressFlowNodes = new List<CancelProgressFlowNode>();
                m_CancelFlowHierarchy.Add(depth, cancelProgressFlowNodes);
            }

            return cancelProgressFlowNodes;
        }


        private JobHandle Schedule(JobHandle dependsOn)
        {
            int len = m_OrderedBulkJobSchedulers.Length;
            if (len == 0)
            {
                return dependsOn;
            }

            foreach (BulkJobScheduler<CancelProgressFlowNode> bulkJobScheduler in m_OrderedBulkJobSchedulers)
            {
                dependsOn = ScheduleBulkSchedulers(bulkJobScheduler, dependsOn);
            }

            return dependsOn;
        }
        
        private JobHandle ScheduleBulkSchedulers(BulkJobScheduler<CancelProgressFlowNode> bulkJobScheduler, JobHandle dependsOn)
        {
            return bulkJobScheduler.Schedule(dependsOn, CancelProgressFlowNode.CHECK_PROGRESS_SCHEDULE_FUNCTION);
        }
    }
}
