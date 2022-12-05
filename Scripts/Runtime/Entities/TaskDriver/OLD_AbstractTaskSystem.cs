using Anvil.CSharp.Collections;
using Anvil.CSharp.Data;
using Anvil.CSharp.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Unity.Entities;
using Unity.Jobs;

namespace Anvil.Unity.DOTS.Entities.Tasks
{
    /// <summary>
    /// A <see cref="SystemBase"/> that is used in the Task System for running generic jobs on generic data en mass
    /// in conjunction with context specific <see cref="AbstractTaskDriver"/>s that will populate the generic data
    /// and receive the results, should they exist.
    /// </summary>
    public abstract class OLD_AbstractTaskSystem : AbstractAnvilSystemBase
    {
        
        

        
        private TaskFlowGraph m_TaskFlowGraph;
        

        protected override void OnCreate()
        {
            base.OnCreate();
            //Initialize the TaskFlowGraph based on our World
            InitTaskFlowGraph(World);
        }

        private void InitTaskFlowGraph(World world)
        {
            //We could get called multiple times
            if (m_TaskFlowGraph != null)
            {
                return;
            }

            m_TaskFlowGraph = world.GetOrCreateSystem<TaskFlowSystem>().TaskFlowGraph;
            //TODO: Investigate if we can just have a Register method with overloads for each type: #66, #67, and/or #68 - https://github.com/decline-cookies/anvil-unity-dots/pull/87/files#r995025025
            m_TaskFlowGraph.RegisterTaskSystem(this);
        }

        protected override void OnDestroy()
        {

            m_TaskDriverContextProvider.Dispose();

            //Note: We don't dispose TaskDrivers here because their parent or direct reference will do so.
            TaskDrivers.Clear();

            //Dispose all the data we own
            m_JobConfigs.DisposeAllAndTryClear();
            TaskData.Dispose();

            base.OnDestroy();
        }

        public override string ToString()
        {
            return GetType().GetReadableName();
        }

        internal void Harden()
        {
            
            m_IsHardened = true;

            foreach (AbstractTaskDriver taskDriver in TaskDrivers)
            {
                taskDriver.Harden();
            }
            
            
        }

        //*************************************************************************************************************
        // CONFIGURATION
        //*************************************************************************************************************

        

        

        

        

        


        


        

        

      


       
        
    }
}
