using Anvil.CSharp.Core;
using System;
using System.Collections.Generic;

namespace Anvil.Unity.DOTS.Entities
{
    public class DataFlowNode : AbstractAnvilBase
    {
        public static readonly DataFlowPath[] FlowPathValues = (DataFlowPath[])Enum.GetValues(typeof(DataFlowPath));

        public enum DataFlowPath
        {
            Populate,
            Update
        }

        public enum Owner
        {
            System,
            Driver
        }

        public IProxyDataStream DataStream
        {
            get;
        }

        public Owner DataOwner
        {
            get;
        }

        public ITaskSystem TaskSystem
        {
            get;
        }

        public ITaskDriver TaskDriver
        {
            get;
        }

        private readonly Dictionary<DataFlowPath, List<IJobConfig>> m_JobFlowMap;

        public DataFlowNode(IProxyDataStream dataStream, ITaskSystem taskSystem, ITaskDriver taskDriver)
        {
            DataStream = dataStream;
            TaskSystem = taskSystem;
            TaskDriver = taskDriver;
            DataOwner = (TaskDriver == null)
                ? Owner.System
                : Owner.Driver;
            m_JobFlowMap = new Dictionary<DataFlowPath, List<IJobConfig>>();
        }

        protected override void DisposeSelf()
        {
            DataStream.Dispose();
            m_JobFlowMap.Clear();
            base.DisposeSelf();
        }

        internal void AddJobConfig(DataFlowPath path, IJobConfig jobConfig)
        {
            List<IJobConfig> taskProcessors = GetOrCreateJobConfigs(path);
            taskProcessors.Add(jobConfig);
        }

        internal List<IJobConfig> GetJobConfigsFor(DataFlowPath path)
        {
            return GetOrCreateJobConfigs(path);
        }

        internal bool HasJobsFor(DataFlowPath path)
        {
            return GetOrCreateJobConfigs(path).Count > 0;
        }

        private List<IJobConfig> GetOrCreateJobConfigs(DataFlowPath path)
        {
            if (!m_JobFlowMap.TryGetValue(path, out List<IJobConfig> jobConfigs))
            {
                jobConfigs = new List<IJobConfig>();
                m_JobFlowMap.Add(path, jobConfigs);
            }

            return jobConfigs;
        }

        internal string ToLocationString()
        {
            return (TaskDriver == null)
                ? $"{TaskSystem.GetType().Name}"
                : $"{TaskDriver.GetType().Name} as part of the {TaskSystem.GetType().Name} system";
        }
    }
}
