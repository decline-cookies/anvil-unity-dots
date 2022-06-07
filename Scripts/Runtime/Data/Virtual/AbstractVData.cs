using Anvil.CSharp.Core;
using Anvil.Unity.DOTS.Jobs;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Anvil.Unity.DOTS.Data
{
    public abstract class AbstractVData : AbstractAnvilBase
    {
        protected class NullVData : AbstractVData
        {
        }
        protected static readonly NullVData NULL_VDATA = new NullVData();
        
        private readonly AbstractVData m_Input;
        private readonly HashSet<AbstractVData> m_Outputs;
        
        protected AccessController AccessController
        {
            get;
        }

        internal AbstractVData()
        {
            Debug.Assert(GetType() == typeof(NullVData), $"Incorrect code path");
        }

        protected AbstractVData(AbstractVData input)
        {
            Debug.Assert(input != null, $"{this} was created by passing in a source but that source is null! Double check that it has been created.");
            
            AccessController = new AccessController();
            m_Input = input;
            m_Outputs = new HashSet<AbstractVData>();
            
            if (!(m_Input is NullVData))
            {
                RegisterOutput(m_Input);
            }
        }

        protected override void DisposeSelf()
        {
            m_Input?.UnregisterOutput(this);
            AccessController.Dispose();
            m_Outputs.Clear();
            
            base.DisposeSelf();
        }
        
        protected JobHandle AcquireOutputsAsync(JobHandle dependsOn)
        {
            if (m_Outputs.Count == 0)
            {
                return dependsOn;
            }
            
            //Get write access to all possible channels that we can write a response to.
            //+1 to include the incoming dependency
            NativeArray<JobHandle> allDependencies = new NativeArray<JobHandle>(m_Outputs.Count + 1, Allocator.Temp);
            allDependencies[0] = dependsOn;
            int index = 1;
            foreach (AbstractVData destinationData in m_Outputs)
            {
                allDependencies[index] = destinationData.AcquireForOutputAsync();
                index++;
            }
            
            return JobHandle.CombineDependencies(allDependencies);
        }

        protected void ReleaseOutputsAsync(JobHandle releaseAccessDependency)
        {
            if (m_Outputs.Count == 0)
            {
                return;
            }
            foreach (AbstractVData destinationData in m_Outputs)
            {
                destinationData.ReleaseForOutputAsync(releaseAccessDependency);
            }
        }
        
        private void RegisterOutput(AbstractVData output)
        {
            m_Outputs.Add(output);
        }

        private void UnregisterOutput(AbstractVData output)
        {
            if (IsDisposed)
            {
                return;
            }
            m_Outputs.Remove(output);
        }
        
        private JobHandle AcquireForOutputAsync()
        {
            //TODO: Collections Checks
            return AccessController.AcquireAsync(AccessType.SharedWrite);
        }

        private void ReleaseForOutputAsync(JobHandle releaseAccessDependency)
        {
            //TODO: Collections Checks
            AccessController.ReleaseAsync(releaseAccessDependency);
        }
    }
}
